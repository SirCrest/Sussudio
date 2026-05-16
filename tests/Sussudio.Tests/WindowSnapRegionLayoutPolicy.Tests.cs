using System;
using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

public sealed class WindowSnapRegionLayoutPolicyTests
{
    private const string PolicyTypeName = "Sussudio.Controllers.WindowSnapRegionLayoutPolicy";
    private const string ActionTypeName = "Sussudio.Models.AutomationWindowAction";

    [Theory]
    [InlineData("SnapLeft", 10, 20, 50, 55)]
    [InlineData("SnapRight", 60, 20, 51, 55)]
    [InlineData("SnapTopLeft", 10, 20, 50, 27)]
    [InlineData("SnapTopRight", 60, 20, 51, 27)]
    [InlineData("SnapBottomLeft", 10, 47, 50, 28)]
    [InlineData("SnapBottomRight", 60, 47, 51, 28)]
    [InlineData("Center", 44, 40, 33, 15)]
    public void ResolveTargetBounds_PreservesExistingSnapGeometry(string actionName, int x, int y, int width, int height)
    {
        var policyType = SussudioAssembly.Load().GetType(PolicyTypeName, throwOnError: true)!;
        var actionType = SussudioAssembly.Load().GetType(ActionTypeName, throwOnError: true)!;
        var method = policyType.GetMethod("ResolveTargetBounds", BindingFlags.Public | BindingFlags.Static)!;
        var parameterTypes = method.GetParameters();
        var workArea = CreateStruct(parameterTypes[1].ParameterType, 10, 20, 101, 55);
        var currentSize = CreateStruct(parameterTypes[2].ParameterType, 33, 15);
        var action = Enum.Parse(actionType, actionName);

        var result = method.Invoke(null, new[] { action, workArea, currentSize });

        Assert.NotNull(result);
        AssertRect(result!, x, y, width, height);
    }

    [Theory]
    [InlineData("Restore")]
    [InlineData(null)]
    public void ResolveTargetBounds_ReturnsNullForNonSnapActions(string? actionName)
    {
        var policyType = SussudioAssembly.Load().GetType(PolicyTypeName, throwOnError: true)!;
        var actionType = SussudioAssembly.Load().GetType(ActionTypeName, throwOnError: true)!;
        var method = policyType.GetMethod("ResolveTargetBounds", BindingFlags.Public | BindingFlags.Static)!;
        var parameterTypes = method.GetParameters();
        var workArea = CreateStruct(parameterTypes[1].ParameterType, 10, 20, 101, 55);
        var currentSize = CreateStruct(parameterTypes[2].ParameterType, 33, 15);
        var action = actionName is null
            ? Enum.ToObject(actionType, 999)
            : Enum.Parse(actionType, actionName);

        var result = method.Invoke(null, new[] { action, workArea, currentSize });

        Assert.Null(result);
    }

    private static object CreateStruct(Type type, params int[] args)
        => Activator.CreateInstance(type, args.Cast<object>().ToArray())!;

    private static void AssertRect(object rect, int x, int y, int width, int height)
    {
        Assert.Equal(x, ReadIntProperty(rect, "X"));
        Assert.Equal(y, ReadIntProperty(rect, "Y"));
        Assert.Equal(width, ReadIntProperty(rect, "Width"));
        Assert.Equal(height, ReadIntProperty(rect, "Height"));
    }

    private static int ReadIntProperty(object instance, string propertyName)
    {
        var type = instance.GetType();
        var property = type.GetProperty(propertyName, ReflectionFlags.Instance);
        if (property != null)
        {
            return (int)property.GetValue(instance)!;
        }

        return (int)type.GetField(propertyName, ReflectionFlags.Instance)!.GetValue(instance)!;
    }
}
