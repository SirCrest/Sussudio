using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

// Shared reflection helpers for capture configuration model contract tests.
static partial class Program
{
    private enum ConfigSetterExpectation
    {
        Set,
        InitOnly,
        None
    }

    private enum ConfigNullability
    {
        NotApplicable,
        NotNull,
        Nullable
    }

    private enum ConfigPropertyScope
    {
        Instance,
        Static
    }

    private sealed record ConfigPropertySpec(
        string Name,
        Type Type,
        ConfigSetterExpectation Setter,
        ConfigNullability Nullability = ConfigNullability.NotApplicable,
        ConfigNullability ElementNullability = ConfigNullability.NotApplicable,
        ConfigPropertyScope Scope = ConfigPropertyScope.Instance,
        bool IsRequired = false);

    private static ConfigPropertySpec ConfigProperty(
        string name,
        Type type,
        ConfigSetterExpectation setter,
        ConfigNullability Nullability = ConfigNullability.NotApplicable,
        ConfigNullability ElementNullability = ConfigNullability.NotApplicable,
        ConfigPropertyScope scope = ConfigPropertyScope.Instance,
        bool isRequired = false)
        => new(name, type, setter, Nullability, ElementNullability, scope, isRequired);

    private static ConfigPropertySpec ConfigString(
        string name,
        ConfigSetterExpectation setter,
        ConfigNullability nullability)
        => ConfigProperty(name, typeof(string), setter, nullability);

    private static ConfigPropertySpec RequiredConfigString(
        string name,
        ConfigSetterExpectation setter)
        => ConfigProperty(name, typeof(string), setter, ConfigNullability.NotNull, isRequired: true);

    private static ConfigPropertySpec RequiredConfigProperty(
        string name,
        Type type,
        ConfigSetterExpectation setter)
        => ConfigProperty(name, type, setter, isRequired: true);

    private static void AssertDeclaredConfigProperties(Type type, ConfigPropertySpec[] expectedProperties)
    {
        var instanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;
        var staticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly;
        var actualNames = type.GetProperties(instanceFlags | staticFlags)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var expectedNames = expectedProperties
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        if (!actualNames.SequenceEqual(expectedNames))
        {
            throw new InvalidOperationException(
                $"{type.Name} public property set changed. Expected: {string.Join(", ", expectedNames)}; actual: {string.Join(", ", actualNames)}.");
        }

        foreach (var expected in expectedProperties)
        {
            var flags = expected.Scope == ConfigPropertyScope.Static ? staticFlags : instanceFlags;
            var property = type.GetProperty(expected.Name, flags)
                ?? throw new InvalidOperationException($"{type.Name}.{expected.Name} was not found.");
            AssertEqual(expected.Type, property.PropertyType, $"{type.Name}.{expected.Name} property type");
            AssertEqual(
                expected.IsRequired,
                property.GetCustomAttribute<RequiredMemberAttribute>() != null,
                $"{type.Name}.{expected.Name} required-member metadata");
            if (property.GetMethod == null || !property.GetMethod.IsPublic)
            {
                throw new InvalidOperationException($"{type.Name}.{expected.Name} must expose a public getter.");
            }

            if (expected.Setter == ConfigSetterExpectation.None)
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
                    throw new InvalidOperationException($"{type.Name}.{expected.Name} must expose a public setter.");
                }

                var isInitOnly = IsInitOnlySetter(property);
                AssertEqual(
                    expected.Setter == ConfigSetterExpectation.InitOnly,
                    isInitOnly,
                    $"{type.Name}.{expected.Name} init-only setter");
            }

            if (expected.Nullability != ConfigNullability.NotApplicable)
            {
                var nullability = new NullabilityInfoContext().Create(property);
                var expectedState = expected.Nullability == ConfigNullability.Nullable
                    ? NullabilityState.Nullable
                    : NullabilityState.NotNull;
                AssertEqual(expectedState, nullability.ReadState, $"{type.Name}.{expected.Name} read nullability");
                if (expected.Setter != ConfigSetterExpectation.None)
                {
                    AssertEqual(expectedState, nullability.WriteState, $"{type.Name}.{expected.Name} write nullability");
                }

                if (expected.ElementNullability != ConfigNullability.NotApplicable)
                {
                    var elementNullability = property.PropertyType.IsArray
                        ? nullability.ElementType
                        : nullability.GenericTypeArguments.FirstOrDefault();
                    if (elementNullability == null)
                    {
                        throw new InvalidOperationException($"{type.Name}.{expected.Name} did not expose element nullability.");
                    }

                    var expectedElementState = expected.ElementNullability == ConfigNullability.Nullable
                        ? NullabilityState.Nullable
                        : NullabilityState.NotNull;
                    AssertEqual(expectedElementState, elementNullability.ReadState, $"{type.Name}.{expected.Name} element read nullability");
                    AssertEqual(expectedElementState, elementNullability.WriteState, $"{type.Name}.{expected.Name} element write nullability");
                }
            }
        }
    }

    private static bool IsInitOnlySetter(PropertyInfo property)
        => property.SetMethod?.ReturnParameter.GetRequiredCustomModifiers()
            .Any(modifier => modifier.FullName == "System.Runtime.CompilerServices.IsExternalInit") == true;

    private static object CreateResolutionFormatDictionary(Type mediaFormatType)
        => Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(
               typeof(string),
               typeof(List<>).MakeGenericType(mediaFormatType)))
           ?? throw new InvalidOperationException("Failed to create resolution format dictionary.");

    private static void AddResolutionFormats(
        object formatsByResolution,
        Type mediaFormatType,
        string resolutionKey,
        params object[] formats)
        => ((IDictionary)formatsByResolution).Add(
            resolutionKey,
            CreateMediaFormatList(mediaFormatType, formats));

    private static object CreateMediaFormatList(Type mediaFormatType, params object[] formats)
    {
        var list = (IList)(Activator.CreateInstance(typeof(List<>).MakeGenericType(mediaFormatType))
                           ?? throw new InvalidOperationException("Failed to create media format list."));
        foreach (var format in formats)
        {
            list.Add(format);
        }

        return list;
    }

    private static object CreateTestMediaFormat(
        Type mediaFormatType,
        uint width,
        uint height,
        double frameRate,
        string pixelFormat,
        bool isHdr)
    {
        var format = CreateConfigInstance(mediaFormatType);
        SetPropertyOrBackingField(format, "Width", width);
        SetPropertyOrBackingField(format, "Height", height);
        SetPropertyOrBackingField(format, "FrameRate", frameRate);
        SetPropertyOrBackingField(format, "PixelFormat", pixelFormat);
        SetPropertyOrBackingField(format, "IsHdr", isHdr);
        return format;
    }

    private static void AssertSequenceEqual<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual, string fieldName)
    {
        if (!expected.SequenceEqual(actual))
        {
            throw new InvalidOperationException(
                $"Assertion failed for {fieldName}. Expected: {string.Join(", ", expected)}; actual: {string.Join(", ", actual)}.");
        }
    }

    private static object CreateConfigInstance(Type type)
        => Activator.CreateInstance(type, nonPublic: true)
           ?? throw new InvalidOperationException($"Failed to create {type.Name}.");

    private static void AssertEnumValues(Type enumType, params (string Name, int Value)[] expectedValues)
    {
        AssertEqual(expectedValues.Length, Enum.GetNames(enumType).Length, $"{enumType.Name} value count");
        foreach (var (name, value) in expectedValues)
        {
            var parsed = Enum.Parse(enumType, name);
            AssertEqual(value, Convert.ToInt32(parsed), $"{enumType.Name}.{name}");
        }
    }
}
