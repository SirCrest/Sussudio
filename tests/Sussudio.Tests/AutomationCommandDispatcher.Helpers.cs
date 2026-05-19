using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    private static object CreateAutomationCommandDispatcher(string? authToken)
    {
        var dispatcherType = RequireType("Sussudio.Services.Automation.AutomationCommandDispatcher");
        var viewModelType = RequireType("Sussudio.Services.Automation.IAutomationViewModel");
        var diagnosticsType = RequireType("Sussudio.Services.Contracts.IAutomationDiagnosticsHub");
        var windowControlType = RequireType("Sussudio.Services.Contracts.IAutomationWindowControl");
        var viewModel = CreateThrowingProxy(viewModelType);
        var constructor = GetAutomationCommandDispatcherConstructor(dispatcherType);

        return constructor.Invoke(new[]
        {
            CreateAutomationViewModelPorts(viewModel),
            CreateThrowingProxy(diagnosticsType),
            CreateThrowingProxy(windowControlType),
            authToken
        });
    }

    private static object CreateAutomationCommandDispatcher(
        object viewModel,
        object diagnosticsHub,
        object windowControl,
        string? authToken)
    {
        var dispatcherType = RequireType("Sussudio.Services.Automation.AutomationCommandDispatcher");
        var constructor = GetAutomationCommandDispatcherConstructor(dispatcherType);

        return constructor.Invoke(new[]
        {
            CreateAutomationViewModelPorts(viewModel),
            diagnosticsHub,
            windowControl,
            authToken
        });
    }

    private static ConstructorInfo GetAutomationCommandDispatcherConstructor(Type dispatcherType)
        => dispatcherType
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(ctor =>
            {
                var parameters = ctor.GetParameters();
                return parameters.Length == 4 &&
                       parameters[0].ParameterType.FullName == "Sussudio.Services.Automation.AutomationViewModelPorts";
            });

    private static object CreateAutomationViewModelPorts(object viewModel)
    {
        var portsType = RequireType("Sussudio.Services.Automation.AutomationViewModelPorts");
        var fromMethod = portsType.GetMethod("From", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                         ?? throw new InvalidOperationException("AutomationViewModelPorts.From was not found.");
        return fromMethod.Invoke(null, new[] { viewModel })
               ?? throw new InvalidOperationException("AutomationViewModelPorts.From returned null.");
    }

    private static object CreateConfiguredProxy(Type interfaceType, Func<MethodInfo?, object?[]?, object?> handler)
    {
        var createMethod = typeof(DispatchProxy)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(method =>
                method.Name == "Create" &&
                method.IsGenericMethodDefinition &&
                method.GetGenericArguments().Length == 2)
            .MakeGenericMethod(interfaceType, typeof(ConfiguredAutomationProxy));
        var proxy = createMethod.Invoke(null, null)
                    ?? throw new InvalidOperationException($"Failed to create proxy for {interfaceType.FullName}.");
        ((ConfiguredAutomationProxy)proxy).Handler = handler;
        return proxy;
    }

    private static object? GetDefaultReturnValue(MethodInfo? method)
    {
        var returnType = method?.ReturnType ?? typeof(void);
        if (returnType == typeof(void))
        {
            return null;
        }

        if (returnType == typeof(Task))
        {
            return Task.CompletedTask;
        }

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultType = returnType.GetGenericArguments()[0];
            var result = resultType.IsValueType ? Activator.CreateInstance(resultType) : null;
            var fromResult = typeof(Task).GetMethod(nameof(Task.FromResult), BindingFlags.Public | BindingFlags.Static)!
                .MakeGenericMethod(resultType);
            return fromResult.Invoke(null, new[] { result });
        }

        return returnType.IsValueType ? Activator.CreateInstance(returnType) : null;
    }

    private static object CreateAutomationCommandRequest(
        string commandName,
        string? authToken,
        string payloadJson,
        int? manifestRevision = null)
    {
        var requestType = RequireType("Sussudio.Models.AutomationCommandRequest");
        var commandType = RequireType("Sussudio.Models.AutomationCommandKind");
        var request = Activator.CreateInstance(requestType)
                      ?? throw new InvalidOperationException("Failed to create AutomationCommandRequest.");
        using var payload = JsonDocument.Parse(payloadJson);
        SetPropertyBackingField(request, "Command", Enum.Parse(commandType, commandName));
        SetPropertyBackingField(request, "CorrelationId", Guid.NewGuid().ToString("N"));
        SetPropertyBackingField(request, "AuthToken", authToken);
        SetPropertyBackingField(request, "ManifestRevision", manifestRevision);
        SetPropertyBackingField(request, "Payload", payload.RootElement.Clone());
        return request;
    }

    private static async Task<object> ExecuteAutomationCommandAsync(object dispatcher, object request)
    {
        var execute = dispatcher.GetType().GetMethod("ExecuteAsync", BindingFlags.Instance | BindingFlags.Public)
                      ?? throw new InvalidOperationException("AutomationCommandDispatcher.ExecuteAsync was not found.");
        var task = (Task)execute.Invoke(dispatcher, new object[] { request, CancellationToken.None })!;
        await task.ConfigureAwait(false);
        return task.GetType().GetProperty("Result")?.GetValue(task)
               ?? throw new InvalidOperationException("AutomationCommandDispatcher.ExecuteAsync returned no result.");
    }

    private static void AssertAutomationResponse(
        object response,
        bool success,
        string? errorCode,
        string status,
        string scenario)
    {
        AssertEqual(success, (bool)GetPublicProperty(response, "Success")!, $"{scenario}: Success");
        AssertEqual(errorCode, (string?)GetPublicProperty(response, "ErrorCode"), $"{scenario}: ErrorCode");
        var actualStatus = GetPublicProperty(response, "Status")!;
        var actualStatusName = JsonNamingPolicy.SnakeCaseLower.ConvertName(actualStatus.ToString()!);
        AssertEqual(status, actualStatusName, $"{scenario}: Status");
    }

    private static object? GetPublicProperty(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)
                       ?? throw new InvalidOperationException($"{instance.GetType().Name}.{propertyName} was not found.");
        return property.GetValue(instance);
    }

    public class ConfiguredAutomationProxy : DispatchProxy
    {
        public Func<MethodInfo?, object?[]?, object?> Handler { get; set; } =
            (_, _) => throw new NotSupportedException("No handler configured.");

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            => Handler(targetMethod, args);
    }

    private static object CreateTaskFromResult(Type resultType, object? result)
    {
        var fromResult = typeof(Task).GetMethod(nameof(Task.FromResult), BindingFlags.Public | BindingFlags.Static)!
            .MakeGenericMethod(resultType);
        return fromResult.Invoke(null, new[] { result })
               ?? throw new InvalidOperationException($"Failed to create Task<{resultType.Name}>.");
    }
}
