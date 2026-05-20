using System;
using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.Tools;

public static partial class AutomationCommandCatalog
{
    private static IReadOnlyList<AutomationCommandMetadata> BuildEntries()
    {
        var entries = Enum.GetValues<AutomationCommandKind>()
            .Select(CreateDefault)
            .ToDictionary(entry => entry.Kind);

        RegisterCoreEntries(entries);
        RegisterCaptureEntries(entries);
        RegisterUiEntries(entries);
        RegisterFlashbackEntries(entries);
        RegisterVerificationEntries(entries);

        return entries.Values.ToArray();
    }
}
