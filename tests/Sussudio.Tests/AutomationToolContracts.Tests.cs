using System.Reflection;

// Shared reflection helpers for automation tool contract tests.
static partial class Program
{
    private static Type RequireSharedToolType(string typeName)
    {
        var assembly = LoadToolAssembly(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
        return assembly.GetType(typeName)
               ?? throw new InvalidOperationException($"{typeName} was not found in the shared tool assembly.");
    }

    private static Type RequireAutomationContractType(string typeName)
    {
        var assembly = typeof(Sussudio.Tools.AutomationCommandCatalog).Assembly;
        return assembly.GetType(typeName)
               ?? throw new InvalidOperationException($"{typeName} was not found in the automation contracts assembly.");
    }

    private static T GetConstant<T>(Type type, string name)
    {
        var field = type.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"{type.FullName}.{name} was not found.");
        return (T)field.GetRawConstantValue()!;
    }

    private static MethodInfo RequireNonPublicStaticMethod(Type type, string name)
        => type.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
           ?? throw new InvalidOperationException($"{type.FullName}.{name} was not found.");

    private static object[] GetCatalogEntries(Type catalogType)
    {
        var entriesProperty = catalogType.GetProperty("Entries", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("AutomationCommandCatalog.Entries not found.");
        return ((System.Collections.IEnumerable)entriesProperty.GetValue(null)!)
            .Cast<object>()
            .ToArray();
    }

    private static object[] GetMetadataCollection(object metadata, string name)
        => ((System.Collections.IEnumerable)GetMetadataProperty(metadata, name)!)
            .Cast<object>()
            .ToArray();

    private static void AssertPayloadFieldsMatchShape(object entry, string commandName, string payloadShape)
    {
        var expectedFields = ParsePayloadShape(payloadShape);
        var actualFields = GetMetadataCollection(entry, "PayloadFields");
        AssertEqual(expectedFields.Length, actualFields.Length, $"{commandName} typed payload field count");

        for (var i = 0; i < expectedFields.Length; i++)
        {
            var actual = actualFields[i];
            AssertEqual(expectedFields[i].Name, (string)GetMetadataProperty(actual, "Name")!, $"{commandName} payload field {i} name");
            AssertEqual(expectedFields[i].Type, GetMetadataProperty(actual, "Type")!.ToString(), $"{commandName} payload field {i} type");
            AssertEqual(expectedFields[i].Required, (bool)GetMetadataProperty(actual, "Required")!, $"{commandName} payload field {i} required");
        }

        var distinctNames = actualFields
            .Select(field => (string)GetMetadataProperty(field, "Name")!)
            .Distinct(StringComparer.Ordinal)
            .Count();
        AssertEqual(actualFields.Length, distinctNames, $"{commandName} unique typed payload field names");
    }

    private static (string Name, string Type, bool Required)[] ParsePayloadShape(string payloadShape)
    {
        var trimmed = payloadShape.Trim();
        if (string.Equals(trimmed, "{}", StringComparison.Ordinal))
        {
            return Array.Empty<(string Name, string Type, bool Required)>();
        }

        if (!trimmed.StartsWith("{", StringComparison.Ordinal) ||
            !trimmed.EndsWith("}", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unsupported payload shape '{payloadShape}'.");
        }

        var inner = trimmed[1..^1].Trim();
        if (string.IsNullOrWhiteSpace(inner))
        {
            return Array.Empty<(string Name, string Type, bool Required)>();
        }

        return inner
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(fieldShape =>
            {
                var parts = fieldShape.Split(':', 2, StringSplitOptions.TrimEntries);
                if (parts.Length != 2)
                {
                    throw new InvalidOperationException($"Unsupported payload field shape '{fieldShape}'.");
                }

                var rawName = parts[0];
                var required = !rawName.EndsWith("?", StringComparison.Ordinal);
                var name = required ? rawName : rawName[..^1];
                return (name, NormalizePayloadFieldType(parts[1]), required);
            })
            .ToArray();
    }

    private static string NormalizePayloadFieldType(string payloadType)
        => payloadType.Trim().ToLowerInvariant() switch
        {
            "string" => "String",
            "bool" => "Boolean",
            "int" => "Integer",
            "double" => "Number",
            "array" => "Array",
            "object" => "Object",
            _ => throw new InvalidOperationException($"Unsupported payload field type '{payloadType}'.")
        };

    private static void AssertCatalogMetadata(
        Type catalogType,
        Type enumType,
        Type pathPolicyType,
        string commandName,
        int timeoutMs,
        bool requiresReadyDevices,
        string pathPolicy,
        string payloadShapeContains)
    {
        var get = RequireNonPublicStaticMethod(catalogType, "Get");
        var enumValue = Enum.Parse(enumType, commandName);
        var metadata = get.Invoke(null, new[] { enumValue })
            ?? throw new InvalidOperationException($"Catalog metadata for {commandName} was null.");
        AssertEqual(commandName, (string)GetMetadataProperty(metadata, "Name")!, $"{commandName} catalog name");
        AssertEqual(timeoutMs, (int)GetMetadataProperty(metadata, "ResponseTimeoutMs")!, $"{commandName} catalog timeout");
        AssertEqual(requiresReadyDevices, (bool)GetMetadataProperty(metadata, "RequiresReadyDevices")!, $"{commandName} catalog readiness");
        AssertEqual(
            Enum.Parse(pathPolicyType, pathPolicy).ToString(),
            GetMetadataProperty(metadata, "PathPolicy")!.ToString(),
            $"{commandName} catalog path policy");
        AssertContains((string)GetMetadataProperty(metadata, "PayloadShape")!, payloadShapeContains);
    }

    private static object? GetMetadataProperty(object metadata, string name)
        => metadata.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public)
               ?.GetValue(metadata)
           ?? throw new InvalidOperationException($"Metadata property '{name}' was not found.");

    private static void AssertNotEmpty(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Assertion failed for {fieldName}: expected non-empty text.");
        }
    }

    private static void AssertThrows<TException>(Action action, string fieldName)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TargetInvocationException ex) when (ex.InnerException is TException)
        {
            return;
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException($"Assertion failed for {fieldName}: expected {typeof(TException).Name}.");
    }
}
