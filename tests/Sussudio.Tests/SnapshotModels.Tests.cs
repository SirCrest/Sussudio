using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Sussudio.Tests;

public partial class SnapshotModelsTests
{
    private enum SnapshotSetterExpectation
    {
        InitOnly,
        None
    }

    private enum SnapshotNullability
    {
        NotApplicable,
        NotNull,
        Nullable
    }

    private sealed record SnapshotPropertySpec(
        string Name,
        Type Type,
        SnapshotSetterExpectation Setter = SnapshotSetterExpectation.InitOnly,
        SnapshotNullability Nullability = SnapshotNullability.NotApplicable,
        SnapshotNullability ElementNullability = SnapshotNullability.NotApplicable);

    private static readonly Dictionary<Type, SnapshotPropertySpec[]> SnapshotPropertySpecsByType = new();

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

    private static void AssertDeclaredProperties(Type type, SnapshotPropertySpec[] expectedProperties)
    {
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .ToDictionary(property => property.Name, StringComparer.Ordinal);
        var actualNames = properties.Keys.OrderBy(name => name, StringComparer.Ordinal).ToArray();
        var expectedNames = expectedProperties.Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        if (!actualNames.SequenceEqual(expectedNames))
        {
            throw new InvalidOperationException(
                $"{type.Name} public property set changed. Expected: {string.Join(", ", expectedNames)}; actual: {string.Join(", ", actualNames)}.");
        }

        SnapshotPropertySpecsByType[type] = expectedProperties;
        foreach (var expected in expectedProperties)
        {
            RequireSnapshotProperty(type, expected);
        }
    }

    private static SnapshotPropertySpec NonNullString(string name)
        => new(name, typeof(string), Nullability: SnapshotNullability.NotNull);

    private static SnapshotPropertySpec NullableString(string name)
        => new(name, typeof(string), Nullability: SnapshotNullability.Nullable);

    private static SnapshotPropertySpec NonNullRef(
        string name,
        Type type,
        SnapshotNullability elementNullability = SnapshotNullability.NotApplicable)
        => new(name, type, Nullability: SnapshotNullability.NotNull, ElementNullability: elementNullability);

    private static SnapshotPropertySpec GetterOnly(string name, Type type)
        => new(name, type, SnapshotSetterExpectation.None);

    private static PropertyInfo RequireSnapshotProperty(Type type, SnapshotPropertySpec expected)
    {
        var property = type.GetProperty(expected.Name, BindingFlags.Instance | BindingFlags.Public);
        AssertNotNull(property, $"{type.Name}.{expected.Name}");
        AssertEqual(expected.Type, property!.PropertyType, $"{type.Name}.{expected.Name} property type");
        if (property.GetMethod == null || !property.GetMethod.IsPublic)
        {
            throw new InvalidOperationException($"{type.Name}.{expected.Name} must expose a public getter.");
        }

        if (expected.Setter == SnapshotSetterExpectation.None)
        {
            if (property.SetMethod != null)
            {
                throw new InvalidOperationException($"{type.Name}.{expected.Name} must not expose a setter.");
            }
        }
        else
        {
            if (property.SetMethod == null || !property.SetMethod.IsPublic)
            {
                throw new InvalidOperationException($"{type.Name}.{expected.Name} must expose a public init setter.");
            }

            var isInitOnly = property.SetMethod.ReturnParameter.GetRequiredCustomModifiers()
                .Any(modifier => modifier.FullName == "System.Runtime.CompilerServices.IsExternalInit");
            if (!isInitOnly)
            {
                throw new InvalidOperationException($"{type.Name}.{expected.Name} must be init-only.");
            }
        }

        if (expected.Nullability != SnapshotNullability.NotApplicable)
        {
            var nullability = new NullabilityInfoContext().Create(property);
            var expectedState = expected.Nullability == SnapshotNullability.Nullable
                ? NullabilityState.Nullable
                : NullabilityState.NotNull;
            AssertEqual(expectedState, nullability.ReadState, $"{type.Name}.{expected.Name} read nullability");
            if (expected.Setter == SnapshotSetterExpectation.InitOnly)
            {
                AssertEqual(expectedState, nullability.WriteState, $"{type.Name}.{expected.Name} write nullability");
            }

            if (expected.ElementNullability != SnapshotNullability.NotApplicable)
            {
                var elementNullability = property.PropertyType.IsArray
                    ? nullability.ElementType
                    : nullability.GenericTypeArguments.FirstOrDefault();
                if (elementNullability == null)
                {
                    throw new InvalidOperationException($"{type.Name}.{expected.Name} did not expose element nullability.");
                }

                var expectedElementState = expected.ElementNullability == SnapshotNullability.Nullable
                    ? NullabilityState.Nullable
                    : NullabilityState.NotNull;
                AssertEqual(expectedElementState, elementNullability.ReadState, $"{type.Name}.{expected.Name} element read nullability");
                AssertEqual(expectedElementState, elementNullability.WriteState, $"{type.Name}.{expected.Name} element write nullability");
            }
        }

        return property;
    }

    private static object CreateGenericList(Type elementType, object item)
    {
        var listType = typeof(List<>).MakeGenericType(elementType);
        var list = Activator.CreateInstance(listType)
            ?? throw new InvalidOperationException($"Failed to create List<{elementType.Name}>.");
        listType.GetMethod("Add", new[] { elementType })!.Invoke(list, new[] { item });
        return list;
    }

    private static object GetSingleEnumerableItem(object value)
    {
        var items = ((IEnumerable)value).Cast<object>().ToArray();
        AssertEqual(1, items.Length, "IEnumerable item count");
        return items[0];
    }

    private static void AssertNonNullStringValue(
        object instance,
        string propertyName,
        string expectedValue,
        string fieldName)
    {
        var value = GetPropertyValue(instance, propertyName)
            ?? throw new InvalidOperationException($"{fieldName}: expected non-null string value.");
        AssertEqual(expectedValue, value, fieldName);
    }
}
