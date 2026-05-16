using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationCommandKind_PreservesNumericValuesThroughGetAutomationManifest()
    {
        var enumType = RequireType("Sussudio.Models.AutomationCommandKind");
        var expectedCommands = ExpectedAutomationCommands();
        AssertEqual(expectedCommands.Length, Enum.GetValues(enumType).Length, "AutomationCommandKind value count");

        for (int i = 0; i < expectedCommands.Length; i++)
        {
            var (name, value) = expectedCommands[i];
            var parsed = Enum.Parse(enumType, name);
            AssertEqual(value, Convert.ToInt32(parsed), $"AutomationCommandKind.{name}");
            if (!Enum.IsDefined(enumType, value))
            {
                throw new InvalidOperationException(
                    $"AutomationCommandKind missing sequential value {value}.");
            }
        }

        return Task.CompletedTask;
    }

    private static (string Name, int Value)[] ExpectedAutomationCommands()
        => AutomationCommandGoldenTable.ExpectedCommands();

    private static Task AutomationWindowAction_HasExpectedValues()
    {
        var enumType = RequireType("Sussudio.Models.AutomationWindowAction");
        var names = Enum.GetNames(enumType);

        // Verify expected members exist.
        var expectedNames = new[]
        {
            "Minimize", "Maximize", "Restore", "Close",
            "SnapLeft", "SnapRight", "SnapTopLeft", "SnapTopRight",
            "SnapBottomLeft", "SnapBottomRight", "Center", "Move", "Resize"
        };
        AssertEqual(expectedNames.Length, names.Length, "AutomationWindowAction count");

        foreach (var expected in expectedNames)
        {
            if (!Enum.IsDefined(enumType, Enum.Parse(enumType, expected)))
            {
                throw new InvalidOperationException(
                    $"AutomationWindowAction missing expected value '{expected}'.");
            }
        }

        return Task.CompletedTask;
    }
}
