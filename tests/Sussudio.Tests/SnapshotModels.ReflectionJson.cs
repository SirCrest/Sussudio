using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Sussudio.Tests;

public partial class SnapshotModelsTests
{
    // LoggingJsonContext.Tests covers the production source-generated routing; this harness
    // validates the DTO reflection JSON shape because it loads the app in an isolated context.
    private static object ReflectionJsonRoundTrip(Type type, object value)
    {
        var json = JsonSerializer.Serialize(value, type);
        using var document = JsonDocument.Parse(json);
        AssertReflectionJsonPropertyNames(type, document.RootElement);
        return JsonSerializer.Deserialize(json, type)
            ?? throw new InvalidOperationException($"{type.Name} reflection JSON round-trip returned null.");
    }

    private static void AssertReflectionJsonPropertyNames(Type type, JsonElement rootElement)
    {
        if (rootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException($"{type.Name} reflection JSON should serialize as an object.");
        }

        var actualNames = rootElement.EnumerateObject()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);
        var expectedNames = GetExpectedRegisteredReflectionJsonPropertyNames(type)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        foreach (var expectedName in expectedNames)
        {
            if (!actualNames.Contains(expectedName))
            {
                throw new InvalidOperationException($"{type.Name} reflection JSON missing property '{expectedName}'.");
            }
        }

        var unexpectedNames = actualNames
            .Except(expectedNames, StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        if (unexpectedNames.Length != 0)
        {
            throw new InvalidOperationException(
                $"{type.Name} reflection JSON emitted unexpected properties: {string.Join(", ", unexpectedNames)}.");
        }
    }

    private static IEnumerable<string> GetExpectedRegisteredReflectionJsonPropertyNames(Type type)
    {
        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetMethod == null || property.GetIndexParameters().Length != 0)
            {
                continue;
            }

            var declaringType = property.DeclaringType ?? type;
            if (!SnapshotPropertySpecsByType.TryGetValue(declaringType, out var expectedProperties))
            {
                throw new InvalidOperationException(
                    $"{type.Name}.{property.Name} reflection JSON check requires registered specs for {declaringType.Name}.");
            }

            var matchedExpectedProperty = expectedProperties.Any(
                expected => string.Equals(expected.Name, property.Name, StringComparison.Ordinal));
            if (!matchedExpectedProperty)
            {
                throw new InvalidOperationException(
                    $"{type.Name}.{property.Name} reflection JSON check was not covered by the registered {declaringType.Name} property specs.");
            }

            yield return property.Name;
        }
    }
}
