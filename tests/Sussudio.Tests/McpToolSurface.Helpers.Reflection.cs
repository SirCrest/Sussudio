using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Type RequireMcpType(string typeName)
    {
        var assembly = LoadToolAssemblyIsolated(Path.Combine("tools", "McpServer", "bin", "Debug", "net8.0", "McpServer.dll"));
        return assembly.GetType(typeName)
               ?? throw new InvalidOperationException($"{typeName} was not found in McpServer.dll.");
    }

    private static object CreateMcpPipeClient(string pipeName)
    {
        var type = RequireMcpType("McpServer.PipeClient");
        return Activator.CreateInstance(
                   type,
                   BindingFlags.Instance | BindingFlags.NonPublic,
                   binder: null,
                   args: new object?[] { pipeName },
                   culture: null)
               ?? throw new InvalidOperationException("Failed to create MCP PipeClient.");
    }

    private static object CreateDefaultMcpPipeClient()
    {
        var type = RequireMcpType("McpServer.PipeClient");
        return Activator.CreateInstance(type)
               ?? throw new InvalidOperationException("Failed to create default MCP PipeClient.");
    }

    private static async Task<string> InvokeMcpToolStringAsync(Type type, string methodName, params object?[] args)
    {
        var method = ResolveMcpToolMethod(type, methodName, args.Length);
        var task = method.Invoke(null, args) as Task
            ?? throw new InvalidOperationException($"{type.FullName}.{methodName} did not return a Task.");
        await task.ConfigureAwait(false);
        var result = task.GetType().GetProperty("Result")?.GetValue(task)
            ?? throw new InvalidOperationException($"{type.FullName}.{methodName} returned null.");
        return result is string text
            ? text
            : GetMcpToolResultText(result);
    }

    private static async Task<object> InvokeMcpToolResultAsync(Type type, string methodName, params object?[] args)
    {
        var method = ResolveMcpToolMethod(type, methodName, args.Length);
        var task = method.Invoke(null, args) as Task
            ?? throw new InvalidOperationException($"{type.FullName}.{methodName} did not return a Task.");
        await task.ConfigureAwait(false);
        return task.GetType().GetProperty("Result")?.GetValue(task)
            ?? throw new InvalidOperationException($"{type.FullName}.{methodName} returned null.");
    }

    private static MethodInfo ResolveMcpToolMethod(Type type, string methodName, int argumentCount)
    {
        var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(method => string.Equals(method.Name, methodName, StringComparison.Ordinal))
            .ToArray();
        if (methods.Length == 0)
        {
            throw new InvalidOperationException($"{type.FullName}.{methodName} was not found.");
        }

        var matchingMethod = methods.SingleOrDefault(method => method.GetParameters().Length == argumentCount);
        if (matchingMethod != null)
        {
            return matchingMethod;
        }

        var shapes = string.Join(
            ", ",
            methods.Select(method => $"{method.Name}({string.Join(", ", method.GetParameters().Select(parameter => parameter.ParameterType.Name))})"));
        throw new InvalidOperationException(
            $"{type.FullName}.{methodName} had no overload accepting {argumentCount} argument(s). Available: {shapes}");
    }

    private static string GetMcpToolResultText(object? result)
    {
        if (result is null)
        {
            throw new InvalidOperationException("MCP tool result was null.");
        }

        var content = GetPropertyValue(result, "Content") as System.Collections.IEnumerable
            ?? throw new InvalidOperationException("MCP tool result content was not enumerable.");
        foreach (var item in content)
        {
            var text = GetPropertyValue(item, "Text") as string;
            if (text is not null)
            {
                return text;
            }
        }

        throw new InvalidOperationException("MCP tool result did not contain text content.");
    }

    private static bool GetMcpToolResultIsError(object? result)
    {
        if (result is null)
        {
            throw new InvalidOperationException("MCP tool result was null.");
        }

        return Convert.ToBoolean(GetPropertyValue(result, "IsError"), CultureInfo.InvariantCulture);
    }

    private static async Task<string> InvokeFormatterBatchAsync(
        MethodInfo executeBatch,
        object pipeClient,
        string emptyMessage,
        Array commands)
    {
        var task = executeBatch.Invoke(null, new object?[] { pipeClient, emptyMessage, commands }) as Task<string>
            ?? throw new InvalidOperationException("ToolCommandFormatter.ExecuteBatchAsync did not return Task<string>.");
        return await task.ConfigureAwait(false);
    }
}
