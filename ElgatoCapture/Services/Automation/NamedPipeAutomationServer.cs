using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using ElgatoCapture.Tools;
using ElgatoCapture.Services.Capture;
using ElgatoCapture.Services.Recording;
using ElgatoCapture.Services.Runtime;
using ElgatoCapture.Services.Telemetry;

namespace ElgatoCapture.Services.Automation;

public sealed class NamedPipeAutomationServer : IDisposable, IAsyncDisposable
{
    public const string DefaultPipeName = AutomationPipeProtocol.DefaultPipeName;

    private readonly IAutomationCommandDispatcher _commandDispatcher;
    private readonly string _pipeName;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly int _requestTimeoutMs;
    private readonly byte[]? _pipeSecurityDescriptor;
    private readonly string _pipeSecurityMode;
    private readonly bool _authTokenRequired;
    private readonly Func<byte[], NamedPipeServerStream> _secureServerStreamFactory;
    private readonly Func<NamedPipeServerStream> _defaultServerStreamFactory;
    private CancellationTokenSource? _cts;
    private Task? _serverTask;
    private bool _disposed;
    private bool _explicitSecurityFailed;

    private const uint PipeAccessDuplex = 0x00000003;
    private const uint FileFlagOverlapped = 0x40000000;
    private const uint PipeTypeByte = 0x00000000;
    private const uint PipeReadModeByte = 0x00000000;
    private const uint PipeWait = 0x00000000;
    private const uint PipeUnlimitedInstances = 255;

    private readonly record struct CommandExecutionResult(
        AutomationCommandResponse Response,
        bool DispatchContinues);

    public NamedPipeAutomationServer(
        IAutomationCommandDispatcher commandDispatcher,
        string? pipeName = null,
        bool authTokenRequired = false)
        : this(
            commandDispatcher,
            pipeName,
            authTokenRequired,
            CreatePipeSecurityDescriptor(),
            secureServerStreamFactory: null,
            defaultServerStreamFactory: null)
    {
    }

    internal NamedPipeAutomationServer(
        IAutomationCommandDispatcher commandDispatcher,
        string? pipeName,
        bool authTokenRequired,
        (byte[]? SecurityDescriptor, string Mode) pipeSecurity,
        Func<byte[], NamedPipeServerStream>? secureServerStreamFactory,
        Func<NamedPipeServerStream>? defaultServerStreamFactory)
    {
        _commandDispatcher = commandDispatcher ?? throw new ArgumentNullException(nameof(commandDispatcher));
        _pipeName = string.IsNullOrWhiteSpace(pipeName) ? DefaultPipeName : pipeName;
        _authTokenRequired = authTokenRequired;
        _pipeSecurityDescriptor = pipeSecurity.SecurityDescriptor;
        _pipeSecurityMode = pipeSecurity.Mode;
        _secureServerStreamFactory = secureServerStreamFactory ?? CreateServerStreamWithSecurityDescriptor;
        _defaultServerStreamFactory = defaultServerStreamFactory ?? CreateDefaultServerStream;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
        _requestTimeoutMs = EnvironmentHelpers.GetIntFromEnv(
            "ELGATOCAPTURE_AUTOMATION_REQUEST_TIMEOUT_MS",
            defaultValue: 300000,
            minValue: 1000,
            maxValue: 300000);
    }

    public string PipeName => _pipeName;
    internal bool AuthTokenRequired => _authTokenRequired;

    public bool Start()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(NamedPipeAutomationServer));
        }

        if (_serverTask != null)
        {
            return true;
        }

        if (IsDefaultSecurityDisallowed())
        {
            Logger.Log(
                "Automation pipe server disabled because explicit Windows pipe security is unavailable " +
                $"and {AutomationPipeProtocol.AutomationKeyEnvVar} is not configured ({_pipeSecurityMode}).");
            return false;
        }

        NamedPipeServerStream initialServer;
        try
        {
            initialServer = CreateServerStream();
        }
        catch (AutomationPipeSecurityException ex)
        {
            Logger.Log($"Automation pipe server disabled: {ex.Message}");
            TraceFallback($"[{DateTime.Now:O}] security disabled: {ex}");
            return false;
        }
        catch (Exception ex)
        {
            Logger.Log($"Automation pipe server startup failed: {ex.Message}");
            TraceFallback($"[{DateTime.Now:O}] startup failed: {ex}");
            return false;
        }

        _cts = new CancellationTokenSource();
        _serverTask = Task.Run(() => RunServerLoopAsync(initialServer, _cts.Token));
        Logger.Log($"Automation pipe server started on '{_pipeName}' ({_pipeSecurityMode}).");
        return true;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        var serverTask = _serverTask;
        if (serverTask == null)
        {
            return;
        }

        _cts?.Cancel();
        _serverTask = null;

        try
        {
            await serverTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            // Server loop didn't stop within 5s — proceed with cleanup.
        }
        catch (OperationCanceledException)
        {
            /* Expected during shutdown — server loop cancelled via disposal */
        }

        _cts?.Dispose();
        _cts = null;
        Logger.Log("Automation pipe server stopped.");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StopAsync().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await StopAsync().ConfigureAwait(false);
    }

    private async Task RunServerLoopAsync(NamedPipeServerStream? initialServer, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var server = initialServer ?? CreateServerStream();
                initialServer = null;

                await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                await HandleConnectionSafelyAsync(server, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                /* Expected during shutdown — exit the accept loop */
                break;
            }
            catch (AutomationPipeSecurityException ex)
            {
                Logger.Log($"Automation pipe server disabled: {ex.Message}");
                TraceFallback($"[{DateTime.Now:O}] security disabled: {ex}");
                break;
            }
            catch (Exception ex)
            {
                Logger.Log($"Automation pipe server loop error: {ex.Message}");
                TraceFallback($"[{DateTime.Now:O}] loop error: {ex}");
                await Task.Delay(250, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task HandleConnectionSafelyAsync(NamedPipeServerStream server, CancellationToken cancellationToken)
    {
        try
        {
            await HandleConnectionAsync(server, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            /* Expected during shutdown — connection cancelled while handling client */
        }
        catch (IOException ioEx)
        {
            Logger.Log($"Automation pipe connection I/O error: {ioEx.Message}");
            TraceFallback($"[{DateTime.Now:O}] connection io error: {ioEx}");
        }
        catch (Exception ex)
        {
            Logger.Log($"Automation pipe connection error: {ex.Message}");
            TraceFallback($"[{DateTime.Now:O}] connection error: {ex}");
        }
        finally
        {
            try
            {
                server.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning($"Suppressed exception in NamedPipeAutomationServer pipe dispose: {ex.Message}");
            }
        }
    }

    private async Task HandleConnectionAsync(NamedPipeServerStream server, CancellationToken cancellationToken)
    {
        AutomationCommandResponse response;
        var requestTimeout = new CancellationTokenSource(_requestTimeoutMs);
        var requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(requestTimeout.Token, cancellationToken);
        var disposeRequestCancellation = true;

        using var reader = new StreamReader(server, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);
        using var writer = new StreamWriter(server, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), bufferSize: 4096, leaveOpen: true)
        {
            AutoFlush = true
        };

        try
        {
            var requestLine = await reader.ReadLineAsync().WaitAsync(requestCancellation.Token).ConfigureAwait(false);
            var request = string.IsNullOrWhiteSpace(requestLine)
                ? null
                : JsonSerializer.Deserialize<AutomationCommandRequest>(requestLine, _jsonOptions);

            if (request == null)
            {
                response = CreateErrorResponse("Request payload was empty.", "invalid-request");
            }
            else
            {
                var execution = await ExecuteCommandWithTimeoutAsync(
                    request,
                    requestTimeout,
                    requestCancellation,
                    cancellationToken).ConfigureAwait(false);
                response = execution.Response;
                disposeRequestCancellation = !execution.DispatchContinues;
            }
        }
        catch (JsonException ex)
        {
            response = CreateErrorResponse($"Invalid JSON request: {ex.Message}", "invalid-json");
        }
        catch (OperationCanceledException)
        {
            var timedOut = requestTimeout.IsCancellationRequested;
            response = CreateErrorResponse(
                timedOut ? $"Request timed out after {_requestTimeoutMs} ms." : "Request canceled.",
                timedOut ? "request-timeout" : "canceled");
        }
        catch (Exception ex)
        {
            response = CreateErrorResponse($"Request execution failed: {ex.Message}", "execution-failed");
        }
        finally
        {
            if (disposeRequestCancellation)
            {
                requestCancellation.Dispose();
                requestTimeout.Dispose();
            }
        }

        var responseLine = JsonSerializer.Serialize(response, _jsonOptions);
        await writer.WriteLineAsync(responseLine).ConfigureAwait(false);
    }

    private async Task<CommandExecutionResult> ExecuteCommandWithTimeoutAsync(
        AutomationCommandRequest request,
        CancellationTokenSource requestTimeout,
        CancellationTokenSource requestCancellation,
        CancellationToken serverCancellation)
    {
        var dispatchTask = _commandDispatcher.ExecuteAsync(request, requestCancellation.Token);
        if (await WaitForDispatchCompletionAsync(dispatchTask, requestCancellation.Token).ConfigureAwait(false))
        {
            var response = await dispatchTask.ConfigureAwait(false);
            if (requestTimeout.IsCancellationRequested &&
                string.Equals(response.ErrorCode, "canceled", StringComparison.OrdinalIgnoreCase))
            {
                response = CreateRequestTimeoutResponse();
            }

            return new CommandExecutionResult(response, DispatchContinues: false);
        }

        if (serverCancellation.IsCancellationRequested && !requestTimeout.IsCancellationRequested)
        {
            throw new OperationCanceledException(serverCancellation);
        }

        if (!requestTimeout.IsCancellationRequested)
        {
            requestTimeout.Cancel();
        }

        ObserveTimedOutDispatch(dispatchTask, request.Command, requestTimeout, requestCancellation);
        return new CommandExecutionResult(CreateRequestTimeoutResponse(), DispatchContinues: true);
    }

    private static async Task<bool> WaitForDispatchCompletionAsync(
        Task<AutomationCommandResponse> dispatchTask,
        CancellationToken cancellationToken)
    {
        if (dispatchTask.IsCompleted)
        {
            return true;
        }

        var cancellationCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = cancellationToken.Register(
            static state => ((TaskCompletionSource<bool>)state!).TrySetResult(true),
            cancellationCompletion);
        var completedTask = await Task.WhenAny(dispatchTask, cancellationCompletion.Task).ConfigureAwait(false);
        return ReferenceEquals(completedTask, dispatchTask);
    }

    private void ObserveTimedOutDispatch(
        Task<AutomationCommandResponse> dispatchTask,
        AutomationCommandKind command,
        CancellationTokenSource requestTimeout,
        CancellationTokenSource requestCancellation)
    {
        _ = ObserveTimedOutDispatchAsync(dispatchTask, command, requestTimeout, requestCancellation);
    }

    private async Task ObserveTimedOutDispatchAsync(
        Task<AutomationCommandResponse> dispatchTask,
        AutomationCommandKind command,
        CancellationTokenSource requestTimeout,
        CancellationTokenSource requestCancellation)
    {
        try
        {
            var response = await dispatchTask.ConfigureAwait(false);
            Logger.Log(
                $"Automation command completed after request timeout: command={command} success={response.Success} error={response.ErrorCode ?? "(none)"}");
        }
        catch (OperationCanceledException ex)
        {
            Logger.Log($"Automation command canceled after request timeout: command={command} message={ex.Message}");
        }
        catch (Exception ex)
        {
            Logger.Log($"Automation command failed after request timeout: command={command} error={ex.Message}");
            Logger.LogException(ex);
        }
        finally
        {
            requestCancellation.Dispose();
            requestTimeout.Dispose();
        }
    }

    private NamedPipeServerStream CreateServerStream()
    {
        if (_pipeSecurityDescriptor != null && OperatingSystem.IsWindows() && !_explicitSecurityFailed)
        {
            try
            {
                return _secureServerStreamFactory(_pipeSecurityDescriptor);
            }
            catch (Exception ex)
            {
                _explicitSecurityFailed = true;
                if (!_authTokenRequired)
                {
                    throw new AutomationPipeSecurityException(
                        "Explicit Windows pipe security failed and no automation token is configured.",
                        ex);
                }

                Logger.Log($"Automation pipe explicit security fallback to token-required default security: {ex.Message}");
            }
        }

        if (IsDefaultSecurityDisallowed())
        {
            throw new AutomationPipeSecurityException(
                "Default Windows pipe security is disabled unless automation token auth is required.");
        }

        return _defaultServerStreamFactory();
    }

    private NamedPipeServerStream CreateDefaultServerStream()
    {
        return new NamedPipeServerStream(
            _pipeName,
            PipeDirection.InOut,
            NamedPipeServerStream.MaxAllowedServerInstances,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous);
    }

    private bool IsDefaultSecurityDisallowed()
        => AutomationPipeSecurityPolicy.ShouldDisableDefaultSecurityFallback(
            OperatingSystem.IsWindows(),
            _pipeSecurityDescriptor != null,
            _explicitSecurityFailed,
            _authTokenRequired);

    private NamedPipeServerStream CreateServerStreamWithSecurityDescriptor(byte[] securityDescriptor)
    {
        IntPtr securityDescriptorPtr = IntPtr.Zero;
        try
        {
            securityDescriptorPtr = Marshal.AllocHGlobal(securityDescriptor.Length);
            Marshal.Copy(securityDescriptor, 0, securityDescriptorPtr, securityDescriptor.Length);

            var securityAttributes = new SECURITY_ATTRIBUTES
            {
                nLength = Marshal.SizeOf<SECURITY_ATTRIBUTES>(),
                lpSecurityDescriptor = securityDescriptorPtr,
                bInheritHandle = 0
            };

            var fullPipeName = $@"\\.\pipe\{_pipeName}";
            var pipeHandle = CreateNamedPipe(
                fullPipeName,
                PipeAccessDuplex | FileFlagOverlapped,
                PipeTypeByte | PipeReadModeByte | PipeWait,
                PipeUnlimitedInstances,
                0,
                0,
                0,
                ref securityAttributes);

            if (pipeHandle.IsInvalid)
            {
                var error = Marshal.GetLastWin32Error();
                pipeHandle.Dispose();
                throw new IOException($"CreateNamedPipe failed with Win32 error {error}.");
            }

            return new NamedPipeServerStream(PipeDirection.InOut, isAsync: true, isConnected: false, pipeHandle);
        }
        finally
        {
            if (securityDescriptorPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(securityDescriptorPtr);
            }
        }
    }

    private static (byte[]? SecurityDescriptor, string Mode) CreatePipeSecurityDescriptor()
    {
        if (!OperatingSystem.IsWindows())
        {
            return (null, "default-nonwindows");
        }

        try
        {
            var currentIdentity = WindowsIdentity.GetCurrent();
            var currentUserSid = currentIdentity.User;
            if (currentUserSid == null)
            {
                return (null, "default-security-no-user-sid");
            }

            var security = new PipeSecurity();
            security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

            // Restrict access to this user plus trusted local administrators/system.
            security.AddAccessRule(new PipeAccessRule(
                currentUserSid,
                PipeAccessRights.FullControl,
                AccessControlType.Allow));
            security.AddAccessRule(new PipeAccessRule(
                new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
                PipeAccessRights.FullControl,
                AccessControlType.Allow));
            security.AddAccessRule(new PipeAccessRule(
                new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
                PipeAccessRights.FullControl,
                AccessControlType.Allow));

            // Keep the automation pipe on explicit Windows security, but avoid adding a
            // mandatory integrity SACL. Creating named objects with a SACL requires
            // SeSecurityPrivilege on some systems; without it CreateNamedPipe fails
            // with ERROR_PRIVILEGE_NOT_HELD and disables MCP/ecctl entirely.
            return (security.GetSecurityDescriptorBinaryForm(), $"explicit-security-user+admins+system ({currentUserSid.Value})");
        }
        catch (Exception ex)
        {
            Logger.Log($"Automation pipe security setup fallback: {ex.Message}");
            return (null, $"default-security-fallback ({ex.GetType().Name})");
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SECURITY_ATTRIBUTES
    {
        public int nLength;
        public IntPtr lpSecurityDescriptor;
        public int bInheritHandle;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafePipeHandle CreateNamedPipe(
        string lpName,
        uint dwOpenMode,
        uint dwPipeMode,
        uint nMaxInstances,
        uint nOutBufferSize,
        uint nInBufferSize,
        uint nDefaultTimeOut,
        ref SECURITY_ATTRIBUTES lpSecurityAttributes);

    private static AutomationCommandResponse CreateErrorResponse(string message, string errorCode) => new()
    {
        Success = false,
        CorrelationId = Guid.NewGuid().ToString("N"),
        Status = "error",
        Message = message,
        ErrorCode = errorCode
    };

    private AutomationCommandResponse CreateRequestTimeoutResponse()
        => CreateErrorResponse($"Request timed out after {_requestTimeoutMs} ms.", "request-timeout");

    private static void TraceFallback(string line)
    {
        try
        {
            var path = RuntimePaths.GetRepoLogFile("ElgatoCapture_AutomationPipe.log");
            File.AppendAllText(path, line + Environment.NewLine);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning($"Suppressed exception in NamedPipeAutomationServer.TraceFallback: {ex.Message}");
        }
    }

    private sealed class AutomationPipeSecurityException : Exception
    {
        public AutomationPipeSecurityException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
