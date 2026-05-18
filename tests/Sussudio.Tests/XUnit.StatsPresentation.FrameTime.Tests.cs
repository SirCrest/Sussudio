using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace Sussudio.Tests;

public partial class StatsPresentationTests
{
    [Fact]
    public void FrameTimeOverlay_UsesDetectedFpsBoundedRange()
    {
        var presentationType = RequireType("Sussudio.ViewModels.StatsPresentationBuilder");
        var resolveRange = presentationType.GetMethod("ResolveFrameTimeRange", ReflectionFlags.Static)
            ?? throw new InvalidOperationException("ResolveFrameTimeRange was not found.");

        var range120 = resolveRange.Invoke(null, new object[] { 120.0 })
            ?? throw new InvalidOperationException("ResolveFrameTimeRange returned null for 120fps.");
        AssertNearlyEqual(1000.0 / 150.0, GetDoubleProperty(range120, "MinMs"), 0.0001);
        AssertNearlyEqual(1000.0 / 90.0, GetDoubleProperty(range120, "MaxMs"), 0.0001);
        AssertNearlyEqual(1000.0 / 120.0, GetDoubleProperty(range120, "ExpectedMs"), 0.0001);

        var normalizedExpected = (GetDoubleProperty(range120, "ExpectedMs") - GetDoubleProperty(range120, "MinMs")) /
                                 GetDoubleProperty(range120, "SpanMs");
        AssertNearlyEqual(0.375, normalizedExpected, 0.0001);

        var fallbackRange = resolveRange.Invoke(null, new object[] { 0.0 })
            ?? throw new InvalidOperationException("ResolveFrameTimeRange returned null for fallback fps.");
        AssertNearlyEqual(1000.0 / 75.0, GetDoubleProperty(fallbackRange, "MinMs"), 0.0001);
        AssertNearlyEqual(1000.0 / 45.0, GetDoubleProperty(fallbackRange, "MaxMs"), 0.0001);
        AssertNearlyEqual(1000.0 / 60.0, GetDoubleProperty(fallbackRange, "ExpectedMs"), 0.0001);
    }

    [Fact]
    public void FrameTimeOverlayGeometry_ProjectsGraphCoordinates()
    {
        var presentationType = RequireType("Sussudio.ViewModels.StatsPresentationBuilder");
        var geometryType = RequireType("Sussudio.Controllers.FrameTimeOverlayGeometry");
        var resolveRange = presentationType.GetMethod("ResolveFrameTimeRange", ReflectionFlags.Static)
            ?? throw new InvalidOperationException("ResolveFrameTimeRange was not found.");
        var resolveCanvasSize = geometryType.GetMethod("ResolveCanvasSize", ReflectionFlags.Static)
            ?? throw new InvalidOperationException("ResolveCanvasSize was not found.");
        var projectSample = geometryType.GetMethod("ProjectSample", ReflectionFlags.Static)
            ?? throw new InvalidOperationException("ProjectSample was not found.");
        var projectExpectedLine = geometryType.GetMethod("ProjectExpectedLine", ReflectionFlags.Static)
            ?? throw new InvalidOperationException("ProjectExpectedLine was not found.");

        var range120 = resolveRange.Invoke(null, new object[] { 120.0 })
            ?? throw new InvalidOperationException("ResolveFrameTimeRange returned null for 120fps.");
        var fallbackCanvasSize = resolveCanvasSize.Invoke(null, new object[] { 1.0, 0.0 })
            ?? throw new InvalidOperationException("ResolveCanvasSize returned null for fallback dimensions.");
        AssertNearlyEqual(500, GetDoubleProperty(fallbackCanvasSize, "Width"), 0.0001);
        AssertNearlyEqual(92, GetDoubleProperty(fallbackCanvasSize, "Height"), 0.0001);

        var canvasSize = resolveCanvasSize.Invoke(null, new object[] { 300.0, 100.0 })
            ?? throw new InvalidOperationException("ResolveCanvasSize returned null for explicit dimensions.");
        var minPoint = projectSample.Invoke(null, new object[] { 0, 3, GetDoubleProperty(range120, "MinMs"), range120, canvasSize })
            ?? throw new InvalidOperationException("ProjectSample returned null for min sample.");
        var expectedPoint = projectSample.Invoke(null, new object[] { 1, 3, GetDoubleProperty(range120, "ExpectedMs"), range120, canvasSize })
            ?? throw new InvalidOperationException("ProjectSample returned null for expected sample.");
        var maxPoint = projectSample.Invoke(null, new object[] { 2, 3, GetDoubleProperty(range120, "MaxMs"), range120, canvasSize })
            ?? throw new InvalidOperationException("ProjectSample returned null for max sample.");
        var clippedLowPoint = projectSample.Invoke(null, new object[] { 1, 3, GetDoubleProperty(range120, "MinMs") - 100, range120, canvasSize })
            ?? throw new InvalidOperationException("ProjectSample returned null for clipped-low sample.");
        var clippedHighPoint = projectSample.Invoke(null, new object[] { 1, 3, GetDoubleProperty(range120, "MaxMs") + 100, range120, canvasSize })
            ?? throw new InvalidOperationException("ProjectSample returned null for clipped-high sample.");

        AssertNearlyEqual(0, GetDoubleProperty(minPoint, "X"), 0.0001);
        AssertNearlyEqual(100, GetDoubleProperty(minPoint, "Y"), 0.0001);
        AssertNearlyEqual(150, GetDoubleProperty(expectedPoint, "X"), 0.0001);
        AssertNearlyEqual(62.5, GetDoubleProperty(expectedPoint, "Y"), 0.0001);
        AssertNearlyEqual(300, GetDoubleProperty(maxPoint, "X"), 0.0001);
        AssertNearlyEqual(0, GetDoubleProperty(maxPoint, "Y"), 0.0001);
        AssertNearlyEqual(100, GetDoubleProperty(clippedLowPoint, "Y"), 0.0001);
        AssertNearlyEqual(0, GetDoubleProperty(clippedHighPoint, "Y"), 0.0001);

        var expectedLine = projectExpectedLine.Invoke(null, new[] { range120, canvasSize })
            ?? throw new InvalidOperationException("ProjectExpectedLine returned null.");
        AssertNearlyEqual(300, GetDoubleProperty(expectedLine, "X2"), 0.0001);
        AssertNearlyEqual(62.5, GetDoubleProperty(expectedLine, "Y"), 0.0001);
    }

    private static Type RequireType(string typeName)
        => SussudioAssembly.Load().GetType(typeName, throwOnError: true)!;

    private static string ReadRepoFile(string relativePath)
    {
        var path = Path.Combine(FindRepoRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        return File.ReadAllText(path).Replace("\r\n", "\n");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null)
        {
            var gitPath = Path.Combine(dir.FullName, ".git");
            if (Directory.Exists(gitPath) || File.Exists(gitPath))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return Environment.CurrentDirectory;
    }

    private static object CreateUninitializedObject(Type type)
        => RuntimeHelpers.GetUninitializedObject(type);

    private static void SetPropertyBackingField(object instance, string propertyName, object? value)
    {
        var field = instance.GetType().GetField($"<{propertyName}>k__BackingField", ReflectionFlags.Instance)
            ?? throw new InvalidOperationException($"Backing field for {propertyName} was not found.");
        field.SetValue(instance, value);
    }

    private static object? GetPropertyValue(object instance, string propertyName)
        => instance.GetType().GetProperty(propertyName, ReflectionFlags.Instance)!.GetValue(instance);

    private static string GetStringProperty(object instance, string propertyName)
        => GetPropertyValue(instance, propertyName) as string
           ?? throw new InvalidOperationException($"{propertyName} was not a string.");

    private static bool GetBoolProperty(object instance, string propertyName)
        => (bool)(GetPropertyValue(instance, propertyName)
                  ?? throw new InvalidOperationException($"{propertyName} was not a bool."));

    private static double GetDoubleProperty(object instance, string propertyName)
        => Convert.ToDouble(GetPropertyValue(instance, propertyName), System.Globalization.CultureInfo.InvariantCulture);

    private static void AssertNearlyEqual(double expected, double actual, double tolerance)
        => Assert.True(
            Math.Abs(expected - actual) <= tolerance,
            $"Expected {expected:0.####}, got {actual:0.####}; tolerance {tolerance:0.####}.");
}
