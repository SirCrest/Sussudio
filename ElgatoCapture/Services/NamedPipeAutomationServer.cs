using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Text;
using System.Text.Json;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;

namespace ElgatoCapture.Services;

public sealed class NamedPipeAutomationServer : IDisposable, IAsyncDisposable
{
    private readonly IAutomationCommandDispatcher _commandDispatcher;
    private readonly string _pipeName;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly int _requestTimeoutMs;
    private readonly byte[]? _pipeSecurityDescriptor;
    private readonly string _pipeSecurityMode;
    private CancellationTokenSource? _cts;
    private Task? _serverTask;
    private bool _disposed;

    private const uint PipeAccessDuplex = 0x00000003;
    private const uint FileFlagOverlapped = 0x40000000;
    private const uint PipeTypeByte = 0x00000000;
    private const uint PipeReadModeByte = 0x00000000;
    private const uint PipeWait = 0x00000000;
    private const uint PipeUnlimitedInstances = 255;
    private const uint TokenAdjustPrivileges = 0x0020;
    private const uint TokenQuery = 0x0008;
    private const uint SePrivilegeEnabled = 0x00000002;

    public NamedPipeAutomationServer(
        IAutomationCommandDispatcher commandDispatcher,
        string? pipeName = null)
    {
        _commandDispatcher = commandDispatcher ?? throw new ArgumentNullException(nameof(commandDispatcher));
        _pipeName = string.IsNullOrWhiteSpace(pipeName) ? "ElgatoCaptureAutomation" : pipeName;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        _requestTimeoutMs = EnvironmentHelpers.GetIntFromEnv(
            "ELGATOCAPTURE_AUTOMATION_REQUEST_TIMEOUT_MS",
            defaultValue: 300000,
            minValue: 1000,
            maxValue: 300000);
        (_pipeSecurityDescriptor, _pipeSecurityMode) = CreatePipeSecurityDescriptor();
    }

    public string PipeName => _pipeName;

    public void Start()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(NamedPipeAutomationServer));
        }

        if (_serverTask != null)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _serverTask = Task.Run(() => RunServerLoopAsync(_cts.Token));
        Logger.Log($"Automation pipe server started on '{_pipeName}' ({_pipeSecurityMode}).");
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
            // Ignore.
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

    private async Task RunServerLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var server = CreateServerStream();

                await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                await HandleConnectionSafelyAsync(server, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
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
            // Expected on shutdown.
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
            catch
            {
                // Best effort.
            }
        }
    }

    private async Task HandleConnectionAsync(NamedPipeServerStream server, CancellationToken cancellationToken)
    {
        AutomationCommandResponse response;
        using var requestTimeout = new CancellationTokenSource(_requestTimeoutMs);
        using var readCancellation = CancellationTokenSource.CreateLinkedTokenSource(requestTimeout.Token, cancellationToken);

        using var reader = new StreamReader(server, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);
        using var writer = new StreamWriter(server, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), bufferSize: 4096, leaveOpen: true)
        {
            AutoFlush = true
        };

        try
        {
            var requestLine = await reader.ReadLineAsync().WaitAsync(readCancellation.Token).ConfigureAwait(false);
            var request = string.IsNullOrWhiteSpace(requestLine)
                ? null
                : JsonSerializer.Deserialize<AutomationCommandRequest>(requestLine, _jsonOptions);

            if (request == null)
            {
                response = CreateErrorResponse("Request payload was empty.", "invalid-request");
            }
            else
            {
                response = await _commandDispatcher.ExecuteAsync(request, requestTimeout.Token).ConfigureAwait(false);
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

        var responseLine = JsonSerializer.Serialize(response, _jsonOptions);
        await writer.WriteLineAsync(responseLine).ConfigureAwait(false);
    }

    private NamedPipeServerStream CreateServerStream()
    {
        if (_pipeSecurityDescriptor != null && OperatingSystem.IsWindows())
        {
            try
            {
                return CreateServerStreamWithSecurityDescriptor(_pipeSecurityDescriptor);
            }
            catch (Exception ex)
            {
                Logger.Log($"Automation pipe explicit security fallback: {ex.Message}");
            }
        }

        return new NamedPipeServerStream(
            _pipeName,
            PipeDirection.InOut,
            NamedPipeServerStream.MaxAllowedServerInstances,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous);
    }

    private NamedPipeServerStream CreateServerStreamWithSecurityDescriptor(byte[] securityDescriptor)
    {
        IntPtr securityDescriptorPtr = IntPtr.Zero;
        try
        {
            TryEnableSeSecurityPrivilege();

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

            try
            {
                security.SetSecurityDescriptorSddlForm(
                    "S:(ML;;NW;;;ME)",
                    AccessControlSections.Audit);
                return (security.GetSecurityDescriptorBinaryForm(), $"explicit-security-user+admins+system-medium-il ({currentUserSid.Value})");
            }
            catch (Exception integrityEx)
            {
                Logger.Log($"Automation pipe integrity label fallback: {integrityEx.Message}");
                return (security.GetSecurityDescriptorBinaryForm(), $"explicit-security-user+admins+system ({currentUserSid.Value})");
            }
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

    private static void TryEnableSeSecurityPrivilege()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        if (!OpenProcessToken(GetCurrentProcess(), TokenAdjustPrivileges | TokenQuery, out var tokenHandle))
        {
            return;
        }

        try
        {
            if (!LookupPrivilegeValue(null, "SeSecurityPrivilege", out var privilegeLuid))
            {
                return;
            }

            var tokenPrivileges = new TOKEN_PRIVILEGES
            {
                PrivilegeCount = 1,
                Privileges = new LUID_AND_ATTRIBUTES
                {
                    Luid = privilegeLuid,
                    Attributes = SePrivilegeEnabled
                }
            };

            if (!AdjustTokenPrivileges(tokenHandle, disableAllPrivileges: false, ref tokenPrivileges, 0, IntPtr.Zero, IntPtr.Zero))
            {
                return;
            }
        }
        finally
        {
            CloseHandle(tokenHandle);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID_AND_ATTRIBUTES
    {
        public LUID Luid;
        public uint Attributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TOKEN_PRIVILEGES
    {
        public uint PrivilegeCount;
        public LUID_AND_ATTRIBUTES Privileges;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(
        IntPtr processHandle,
        uint desiredAccess,
        out IntPtr tokenHandle);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool LookupPrivilegeValue(
        string? lpSystemName,
        string lpName,
        out LUID luid);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool AdjustTokenPrivileges(
        IntPtr tokenHandle,
        bool disableAllPrivileges,
        ref TOKEN_PRIVILEGES newState,
        uint bufferLength,
        IntPtr previousState,
        IntPtr returnLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);

    private static AutomationCommandResponse CreateErrorResponse(string message, string errorCode) => new()
    {
        Success = false,
        CorrelationId = Guid.NewGuid().ToString("N"),
        Status = "error",
        Message = message,
        ErrorCode = errorCode
    };

    private static void TraceFallback(string line)
    {
        try
        {
            var path = RuntimePaths.GetRepoLogFile("ElgatoCapture_AutomationPipe.log");
            File.AppendAllText(path, line + Environment.NewLine);
        }
        catch
        {
            // Best effort only.
        }
    }
}
