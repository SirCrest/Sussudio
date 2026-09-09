using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

// Exercises the "every declared code, for tests and diagnostics that need to
// prove the registry and the emitting sites agree" contract that
// AutomationErrorCodes and RecordingFailureCodes both document on their All
// collections: a code added as a const but forgotten in All (or vice versa)
// is a real wire-contract regression, not a style nit.
public sealed class FailureCodeRegistryContractsTests
{
    [Theory]
    [InlineData("Sussudio.Services.Automation.AutomationErrorCodes")]
    [InlineData("Sussudio.Services.Recording.RecordingFailureCodes")]
    public void EveryDeclaredCodeIsUniqueAndListedExactlyOnceInAll(string typeName)
    {
        var type = SussudioAssembly.Load().GetType(typeName, throwOnError: true)!;

        var declaredCodes = type
            .GetFields(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToArray();

        Assert.NotEmpty(declaredCodes);
        Assert.Equal(declaredCodes.Length, declaredCodes.Distinct(StringComparer.Ordinal).Count());

        var allProperty = type.GetProperty("All", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"{typeName}.All was not found.");
        var allCodes = ((IEnumerable<string>)allProperty.GetValue(null)!).ToArray();

        Assert.Equal(
            declaredCodes.OrderBy(code => code, StringComparer.Ordinal),
            allCodes.OrderBy(code => code, StringComparer.Ordinal));
    }
}
