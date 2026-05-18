using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace Sussudio.Tests;

public partial class CaptureConfigurationModelsTests
{
    private enum SetterExpectation
    {
        Set,
        InitOnly,
        None
    }

    private enum NullabilityExpectation
    {
        NotApplicable,
        NotNull,
        Nullable
    }

    private enum PropertyScope
    {
        Instance,
        Static
    }

    private sealed record PropertySpec(
        string Name,
        Type Type,
        SetterExpectation Setter,
        NullabilityExpectation Nullability = NullabilityExpectation.NotApplicable,
        NullabilityExpectation ElementNullability = NullabilityExpectation.NotApplicable,
        PropertyScope Scope = PropertyScope.Instance,
        bool IsRequired = false);


    private static PropertySpec Property(
        string name,
        Type type,
        SetterExpectation setter,
        NullabilityExpectation nullability = NullabilityExpectation.NotApplicable,
        NullabilityExpectation elementNullability = NullabilityExpectation.NotApplicable,
        PropertyScope scope = PropertyScope.Instance,
        bool isRequired = false)
        => new(name, type, setter, nullability, elementNullability, scope, isRequired);

    private static PropertySpec String(string name, SetterExpectation setter, NullabilityExpectation nullability)
        => Property(name, typeof(string), setter, nullability);

    private static PropertySpec RequiredString(string name, SetterExpectation setter)
        => Property(name, typeof(string), setter, NullabilityExpectation.NotNull, isRequired: true);

    private static PropertySpec RequiredProperty(string name, Type type, SetterExpectation setter)
        => Property(name, type, setter, isRequired: true);

    private static void AssertDeclaredProperties(Type type, IReadOnlyCollection<PropertySpec> expectedProperties)
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
        Assert.Equal(expectedNames, actualNames);

        foreach (var expected in expectedProperties)
        {
            var flags = expected.Scope == PropertyScope.Static ? staticFlags : instanceFlags;
            var property = type.GetProperty(expected.Name, flags);
            Assert.NotNull(property);
            Assert.Equal(expected.Type, property!.PropertyType);
            Assert.Equal(expected.IsRequired, property.GetCustomAttribute<RequiredMemberAttribute>() != null);
            Assert.NotNull(property.GetMethod);
            Assert.True(property.GetMethod!.IsPublic);

            if (expected.Setter == SetterExpectation.None)
            {
                Assert.Null(property.SetMethod);
            }
            else
            {
                Assert.NotNull(property.SetMethod);
                Assert.True(property.SetMethod!.IsPublic);
                Assert.Equal(expected.Setter == SetterExpectation.InitOnly, IsInitOnlySetter(property));
            }

            AssertNullability(type, property, expected);
        }
    }

    private static void AssertNullability(Type type, PropertyInfo property, PropertySpec expected)
    {
        if (expected.Nullability == NullabilityExpectation.NotApplicable)
        {
            return;
        }

        var nullability = new NullabilityInfoContext().Create(property);
        var expectedState = expected.Nullability == NullabilityExpectation.Nullable
            ? NullabilityState.Nullable
            : NullabilityState.NotNull;
        Assert.Equal(expectedState, nullability.ReadState);
        if (expected.Setter != SetterExpectation.None)
        {
            Assert.Equal(expectedState, nullability.WriteState);
        }

        if (expected.ElementNullability == NullabilityExpectation.NotApplicable)
        {
            return;
        }

        var elementNullability = property.PropertyType.IsArray
            ? nullability.ElementType
            : nullability.GenericTypeArguments.FirstOrDefault();
        Assert.NotNull(elementNullability);
        var expectedElementState = expected.ElementNullability == NullabilityExpectation.Nullable
            ? NullabilityState.Nullable
            : NullabilityState.NotNull;
        Assert.Equal(expectedElementState, elementNullability!.ReadState);
        Assert.Equal(expectedElementState, elementNullability.WriteState);
    }

    private static bool IsInitOnlySetter(PropertyInfo property)
        => property.SetMethod?.ReturnParameter.GetRequiredCustomModifiers()
            .Any(modifier => modifier.FullName == "System.Runtime.CompilerServices.IsExternalInit") == true;

    private static Type RequireType(Assembly asm, string name)
        => asm.GetType(name, throwOnError: true)!;

    private static MethodInfo RequireMethod(Type type, string name, BindingFlags flags)
    {
        var method = type.GetMethod(name, flags);
        Assert.NotNull(method);
        return method!;
    }

    private static object CreateInstance(Type type)
        => Activator.CreateInstance(type, nonPublic: true)!;

    private static object? Get(object instance, string propertyName)
        => instance.GetType().GetProperty(propertyName, ReflectionFlags.Instance | ReflectionFlags.Static)!.GetValue(instance);

    private static T Get<T>(object instance, string propertyName)
        => (T)Get(instance, propertyName)!;

    private static void Set(object instance, string propertyName, object? value)
    {
        var type = instance.GetType();
        var property = type.GetProperty(propertyName, ReflectionFlags.Instance | ReflectionFlags.Static);
        if (property?.SetMethod != null)
        {
            property.SetValue(instance, value);
            return;
        }

        var field = type.GetField($"<{propertyName}>k__BackingField", ReflectionFlags.Instance | ReflectionFlags.Static)
            ?? type.GetField($"_{char.ToLowerInvariant(propertyName[0])}{propertyName[1..]}", ReflectionFlags.Instance | ReflectionFlags.Static)
            ?? type.GetField(propertyName, ReflectionFlags.Instance | ReflectionFlags.Static);
        Assert.NotNull(field);
        field!.SetValue(instance, value);
    }

    private static object? Invoke(object instance, string methodName)
        => RequireMethod(instance.GetType(), methodName, ReflectionFlags.Instance).Invoke(instance, Array.Empty<object>());

    private static object ParseEnum(Assembly asm, string typeName, string value)
        => Enum.Parse(RequireType(asm, typeName), value);

    private static void AssertEnumValues(Type enumType, params (string Name, int Value)[] expectedValues)
    {
        Assert.Equal(expectedValues.Length, Enum.GetNames(enumType).Length);
        foreach (var (name, value) in expectedValues)
        {
            Assert.Equal(value, Convert.ToInt32(Enum.Parse(enumType, name)));
        }
    }
}
