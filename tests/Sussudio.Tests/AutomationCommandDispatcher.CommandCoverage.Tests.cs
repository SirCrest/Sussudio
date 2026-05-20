using System.Threading.Tasks;

static partial class Program
{
    internal static Task AutomationCommandDispatcher_AllCommandKinds_AreHandled()
    {
        // Every AutomationCommandKind value must be explicitly handled: either
        // as the pre-switch Authenticate check, as a handler-table key, as an
        // explicit focused-helper equality check, or as a case label in the
        // custom switch. This test reads the dispatcher source and verifies each
        // enum name appears in at least one of those locations.
        var dispatcherText = ReadAutomationCommandDispatcherFamilyText();

        var commandKindType = RequireType("Sussudio.Models.AutomationCommandKind");
        var names = Enum.GetNames(commandKindType);

        foreach (var name in names)
        {
            var inTrivialHandlers = dispatcherText.Contains($"[AutomationCommandKind.{name}]");
            var inFocusedHelper = dispatcherText.Contains($"command == AutomationCommandKind.{name}");
            var inSwitchCase = dispatcherText.Contains($"case AutomationCommandKind.{name}:");
            var isAuthenticate = name == "Authenticate" &&
                dispatcherText.Contains("request.Command == AutomationCommandKind.Authenticate");

            AssertEqual(
                true,
                inTrivialHandlers || inFocusedHelper || inSwitchCase || isAuthenticate,
                $"AutomationCommandKind.{name} must be handled in a handler table, focused helper, switch case, or the pre-switch Authenticate check");
        }

        return Task.CompletedTask;
    }
}
