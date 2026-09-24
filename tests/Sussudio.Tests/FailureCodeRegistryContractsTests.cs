using System.Reflection;
using Sussudio.Tools;
using Xunit;

namespace Sussudio.Tests;

// Exercises the "every declared code, for tests and diagnostics that need to
// prove the registry and the emitting sites agree" contract that
// AutomationErrorCodes, AutomationPipeErrorCodes, and RecordingFailureCodes expose on their All
// collections: a code added as a const but forgotten in All (or vice versa)
// is a real wire-contract regression, not a style nit.
public sealed class FailureCodeRegistryContractsTests
{
    [Theory]
    [InlineData("Sussudio.Services.Automation.AutomationErrorCodes")]
    [InlineData("Sussudio.Services.Recording.RecordingFailureCodes")]
    [InlineData("Sussudio.Tools.AutomationPipeErrorCodes")]
    public void EveryDeclaredCodeIsUniqueAndListedExactlyOnceInAll(string typeName)
    {
        var type = typeName == typeof(AutomationPipeErrorCodes).FullName
            ? typeof(AutomationPipeErrorCodes)
            : SussudioAssembly.Load().GetType(typeName, throwOnError: true)!;

        var declaredCodes = type
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToArray();

        Assert.NotEmpty(declaredCodes);
        Assert.Equal(declaredCodes.Length, declaredCodes.Distinct(StringComparer.Ordinal).Count());

        var allProperty = type.GetProperty("All", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"{typeName}.All was not found.");
        var allCodes = ((IEnumerable<string>)allProperty.GetValue(null)!).ToArray();

        Assert.Equal(
            declaredCodes.OrderBy(code => code, StringComparer.Ordinal),
            allCodes.OrderBy(code => code, StringComparer.Ordinal));
    }

    [Theory]
    [InlineData(nameof(AutomationPipeErrorCodes.ConnectTimeout), "pipe-connect-timeout")]
    [InlineData(nameof(AutomationPipeErrorCodes.AccessDenied), "pipe-access-denied")]
    [InlineData(nameof(AutomationPipeErrorCodes.ConnectFailed), "pipe-connect-failed")]
    [InlineData(nameof(AutomationPipeErrorCodes.UnknownCommand), "unknown-command")]
    [InlineData(nameof(AutomationPipeErrorCodes.ResponseTimeout), "pipe-response-timeout")]
    [InlineData(nameof(AutomationPipeErrorCodes.ProtocolError), "pipe-protocol-error")]
    [InlineData(nameof(AutomationPipeErrorCodes.InvalidJson), "pipe-invalid-json")]
    [InlineData(nameof(AutomationPipeErrorCodes.IoError), "pipe-io-error")]
    [InlineData(nameof(AutomationPipeErrorCodes.Canceled), "pipe-canceled")]
    public void PipeClientCodesPreserveLegacyWireSpellings(string memberName, string expectedCode)
    {
        var member = typeof(AutomationPipeErrorCodes).GetField(memberName, BindingFlags.Public | BindingFlags.Static)!;
        Assert.Equal(expectedCode, member.GetRawConstantValue());
    }
}
