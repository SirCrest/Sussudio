using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class NamedPipeAutomationServer
{
    private sealed class ConnectionSession
    {
        private readonly NamedPipeAutomationServer _owner;
        private readonly NamedPipeServerStream _server;
        private readonly CancellationToken _serverCancellation;

        public ConnectionSession(
            NamedPipeAutomationServer owner,
            NamedPipeServerStream server,
            CancellationToken serverCancellation)
        {
            _owner = owner;
            _server = server;
            _serverCancellation = serverCancellation;
        }

        public async Task RunAsync()
        {
            AutomationCommandResponse response;
            var requestTimeout = new CancellationTokenSource(_owner._requestTimeoutMs);
            var requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(requestTimeout.Token, _serverCancellation);
            var disposeRequestCancellation = true;

            using var reader = new StreamReader(_server, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);
            using var writer = new StreamWriter(_server, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), bufferSize: 4096, leaveOpen: true)
            {
                AutoFlush = true
            };

            try
            {
                var requestLine = await reader.ReadLineAsync().WaitAsync(requestCancellation.Token).ConfigureAwait(false);
                var request = string.IsNullOrWhiteSpace(requestLine)
                    ? null
                    : JsonSerializer.Deserialize<AutomationCommandRequest>(requestLine, _owner._jsonOptions);

                if (request == null)
                {
                    response = CreateErrorResponse("Request payload was empty.", "invalid-request");
                }
                else
                {
                    LogReceivedRequest(request);

                    var execution = await ExecuteCommandWithTimeoutAsync(
                        request,
                        requestTimeout,
                        requestCancellation).ConfigureAwait(false);
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
                    timedOut ? $"Request timed out after {_owner._requestTimeoutMs} ms." : "Request canceled.",
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

            var responseLine = JsonSerializer.Serialize(response, _owner._jsonOptions);
            await writer.WriteLineAsync(responseLine).ConfigureAwait(false);
        }

        private void LogReceivedRequest(AutomationCommandRequest request)
        {
            var clientPid = TryGetClientProcessId();
            Logger.Log(
                $"AUTOMATION_PIPE_RECV command={request.Command} correlationId={request.CorrelationId} clientPid={(clientPid == 0 ? "?" : clientPid.ToString())}");
        }

        private uint TryGetClientProcessId()
        {
            try
            {
                var handle = _server.SafePipeHandle.DangerousGetHandle();
                if (handle != IntPtr.Zero && GetNamedPipeClientProcessId(handle, out var clientPid))
                {
                    return clientPid;
                }
            }
            catch
            {
                // PID lookup is best-effort.
            }

            return 0;
        }

        private async Task<CommandExecutionResult> ExecuteCommandWithTimeoutAsync(
            AutomationCommandRequest request,
            CancellationTokenSource requestTimeout,
            CancellationTokenSource requestCancellation)
        {
            var dispatchTask = _owner._commandDispatcher.ExecuteAsync(request, requestCancellation.Token);
            if (await WaitForDispatchCompletionAsync(dispatchTask, requestCancellation.Token).ConfigureAwait(false))
            {
                var response = await dispatchTask.ConfigureAwait(false);
                if (requestTimeout.IsCancellationRequested &&
                    string.Equals(response.ErrorCode, "canceled", StringComparison.OrdinalIgnoreCase))
                {
                    response = _owner.CreateRequestTimeoutResponse();
                }

                return new CommandExecutionResult(response, DispatchContinues: false);
            }

            if (_serverCancellation.IsCancellationRequested && !requestTimeout.IsCancellationRequested)
            {
                throw new OperationCanceledException(_serverCancellation);
            }

            if (!requestTimeout.IsCancellationRequested)
            {
                requestTimeout.Cancel();
            }

            ObserveTimedOutDispatch(dispatchTask, request.Command, requestTimeout, requestCancellation);
            return new CommandExecutionResult(_owner.CreateRequestTimeoutResponse(), DispatchContinues: true);
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

        private AutomationCommandResponse CreateErrorResponse(string message, string errorCode)
            => NamedPipeAutomationServer.CreateErrorResponse(message, errorCode);

        private readonly record struct CommandExecutionResult(
            AutomationCommandResponse Response,
            bool DispatchContinues);
    }
}
