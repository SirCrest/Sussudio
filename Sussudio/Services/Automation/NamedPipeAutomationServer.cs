using System;
using System.IO.Pipes;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Services.Runtime;
using Sussudio.Tools;

namespace Sussudio.Services.Automation;

public sealed partial class NamedPipeAutomationServer : IDisposable, IAsyncDisposable
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
            "SUSSUDIO_AUTOMATION_REQUEST_TIMEOUT_MS",
            defaultValue: 300000,
            minValue: 1000,
            maxValue: 300000);
    }

    public string PipeName => _pipeName;
    internal bool AuthTokenRequired => _authTokenRequired;
}
