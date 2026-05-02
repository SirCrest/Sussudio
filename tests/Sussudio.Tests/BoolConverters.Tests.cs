using System;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task BoolConverters_PreserveInversionAndVisibilityMappings()
    {
        var inverseBoolType = RequireType("Sussudio.Converters.InverseBoolConverter");
        var boolToVisType = RequireType("Sussudio.Converters.BoolToVisibilityConverter");
        var inverseBoolToVisType = RequireType("Sussudio.Converters.BoolToInverseVisibilityConverter");

        var converterInterfaceType = RequireInterface(inverseBoolType, "Microsoft.UI.Xaml.Data.IValueConverter");
        AssertImplementsInterface(boolToVisType, converterInterfaceType);
        AssertImplementsInterface(inverseBoolToVisType, converterInterfaceType);
        var visibilityType = converterInterfaceType.Assembly.GetType("Microsoft.UI.Xaml.Visibility");
        AssertNotNull(visibilityType, "Microsoft.UI.Xaml.Visibility");
        var visibleValue = Enum.Parse(visibilityType!, "Visible");
        var collapsedValue = Enum.Parse(visibilityType!, "Collapsed");

        var inverseConvert = RequireConverterMethod(inverseBoolType, "Convert");
        var inverseConvertBack = RequireConverterMethod(inverseBoolType, "ConvertBack");
        var boolToVisibilityConvert = RequireConverterMethod(boolToVisType, "Convert");
        var boolToVisibilityConvertBack = RequireConverterMethod(boolToVisType, "ConvertBack");
        var inverseVisibilityConvert = RequireConverterMethod(inverseBoolToVisType, "Convert");
        var inverseVisibilityConvertBack = RequireConverterMethod(inverseBoolToVisType, "ConvertBack");

        var inverseInstance = Activator.CreateInstance(inverseBoolType)!;
        AssertEqual(
            false,
            (bool)inverseConvert.Invoke(inverseInstance, new object?[] { true, typeof(bool), null, "" })!,
            "InverseBoolConverter.Convert(true)");
        AssertEqual(
            true,
            (bool)inverseConvert.Invoke(inverseInstance, new object?[] { false, typeof(bool), null, "" })!,
            "InverseBoolConverter.Convert(false)");
        AssertEqual(
            false,
            (bool)inverseConvertBack.Invoke(inverseInstance, new object?[] { true, typeof(bool), null, "" })!,
            "InverseBoolConverter.ConvertBack(true)");
        AssertEqual(
            true,
            (bool)inverseConvertBack.Invoke(inverseInstance, new object?[] { false, typeof(bool), null, "" })!,
            "InverseBoolConverter.ConvertBack(false)");
        var nonBoolSentinel = new object();
        AssertSame(
            nonBoolSentinel,
            inverseConvert.Invoke(inverseInstance, new object?[] { nonBoolSentinel, typeof(bool), null, "" }),
            "InverseBoolConverter passes through non-bool");
        AssertSame(
            nonBoolSentinel,
            inverseConvertBack.Invoke(inverseInstance, new object?[] { nonBoolSentinel, typeof(bool), null, "" }),
            "InverseBoolConverter.ConvertBack passes through non-bool");
        AssertEqual(
            null,
            inverseConvert.Invoke(inverseInstance, new object?[] { null, typeof(bool), null, "" }),
            "InverseBoolConverter.Convert(null)");
        AssertEqual(
            null,
            inverseConvertBack.Invoke(inverseInstance, new object?[] { null, typeof(bool), null, "" }),
            "InverseBoolConverter.ConvertBack(null)");

        var boolToVisibility = Activator.CreateInstance(boolToVisType)!;
        var visible = boolToVisibilityConvert.Invoke(boolToVisibility, new object?[] { true, visibilityType, null, "" });
        var collapsed = boolToVisibilityConvert.Invoke(boolToVisibility, new object?[] { false, visibilityType, null, "" });
        AssertNotNull(visible, "BoolToVisibilityConverter.Convert(true) result");
        AssertNotNull(collapsed, "BoolToVisibilityConverter.Convert(false) result");
        AssertEqual(
            visibleValue,
            visible,
            "BoolToVisibilityConverter.Convert(true)");
        AssertEqual(
            collapsedValue,
            collapsed,
            "BoolToVisibilityConverter.Convert(false)");
        AssertEqual(
            collapsedValue,
            boolToVisibilityConvert.Invoke(boolToVisibility, new object?[] { "not-a-bool", visibilityType, null, "" }),
            "BoolToVisibilityConverter.Convert(non-bool)");
        AssertEqual(
            collapsedValue,
            boolToVisibilityConvert.Invoke(boolToVisibility, new object?[] { null, visibilityType, null, "" }),
            "BoolToVisibilityConverter.Convert(null)");
        AssertEqual(
            true,
            (bool)boolToVisibilityConvertBack.Invoke(boolToVisibility, new object?[] { visibleValue, typeof(bool), null, "" })!,
            "BoolToVisibilityConverter.ConvertBack(Visible)");
        AssertEqual(
            false,
            (bool)boolToVisibilityConvertBack.Invoke(boolToVisibility, new object?[] { collapsedValue, typeof(bool), null, "" })!,
            "BoolToVisibilityConverter.ConvertBack(Collapsed)");
        AssertEqual(
            false,
            (bool)boolToVisibilityConvertBack.Invoke(boolToVisibility, new object?[] { nonBoolSentinel, typeof(bool), null, "" })!,
            "BoolToVisibilityConverter.ConvertBack(non-visibility)");
        AssertEqual(
            false,
            (bool)boolToVisibilityConvertBack.Invoke(boolToVisibility, new object?[] { null, typeof(bool), null, "" })!,
            "BoolToVisibilityConverter.ConvertBack(null)");

        var inverseVisibility = Activator.CreateInstance(inverseBoolToVisType)!;
        AssertEqual(
            collapsedValue,
            inverseVisibilityConvert.Invoke(inverseVisibility, new object?[] { true, visibilityType, null, "" }),
            "BoolToInverseVisibilityConverter.Convert(true)");
        AssertEqual(
            visibleValue,
            inverseVisibilityConvert.Invoke(inverseVisibility, new object?[] { false, visibilityType, null, "" }),
            "BoolToInverseVisibilityConverter.Convert(false)");
        AssertEqual(
            visibleValue,
            inverseVisibilityConvert.Invoke(inverseVisibility, new object?[] { "not-a-bool", visibilityType, null, "" }),
            "BoolToInverseVisibilityConverter.Convert(non-bool)");
        AssertEqual(
            visibleValue,
            inverseVisibilityConvert.Invoke(inverseVisibility, new object?[] { null, visibilityType, null, "" }),
            "BoolToInverseVisibilityConverter.Convert(null)");
        AssertEqual(
            false,
            (bool)inverseVisibilityConvertBack.Invoke(inverseVisibility, new object?[] { visibleValue, typeof(bool), null, "" })!,
            "BoolToInverseVisibilityConverter.ConvertBack(Visible)");
        AssertEqual(
            true,
            (bool)inverseVisibilityConvertBack.Invoke(inverseVisibility, new object?[] { collapsedValue, typeof(bool), null, "" })!,
            "BoolToInverseVisibilityConverter.ConvertBack(Collapsed)");
        AssertEqual(
            true,
            (bool)inverseVisibilityConvertBack.Invoke(inverseVisibility, new object?[] { nonBoolSentinel, typeof(bool), null, "" })!,
            "BoolToInverseVisibilityConverter.ConvertBack(non-visibility)");
        AssertEqual(
            true,
            (bool)inverseVisibilityConvertBack.Invoke(inverseVisibility, new object?[] { null, typeof(bool), null, "" })!,
            "BoolToInverseVisibilityConverter.ConvertBack(null)");

        return Task.CompletedTask;
    }

    private static MethodInfo RequireConverterMethod(Type type, string methodName)
    {
        var method = type.GetMethod(methodName, new[] { typeof(object), typeof(Type), typeof(object), typeof(string) });
        AssertNotNull(method, $"{type.Name}.{methodName}(object, Type, object, string)");
        return method!;
    }

    private static Type RequireInterface(Type type, string interfaceName)
    {
        var interfaceType = type.GetInterface(interfaceName);
        AssertNotNull(interfaceType, $"{type.Name} implements {interfaceName}");
        return interfaceType!;
    }

    private static void AssertImplementsInterface(Type type, Type interfaceType)
    {
        if (!interfaceType.IsAssignableFrom(type))
        {
            throw new InvalidOperationException($"{type.Name}: expected implementation of {interfaceType.FullName}.");
        }
    }

    private static void AssertSame(object expected, object? actual, string fieldName)
    {
        if (!ReferenceEquals(expected, actual))
        {
            throw new InvalidOperationException($"{fieldName}: expected same object instance.");
        }
    }
}
