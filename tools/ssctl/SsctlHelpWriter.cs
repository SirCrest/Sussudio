using System.IO;

namespace Sussudio.Tools.Ssctl;

// Owns the operator-facing ssctl help facade. Program decides when help is
// shown; the partial family keeps section text and catalog-backed command
// lines in focused files.
internal static partial class SsctlHelpWriter
{
    internal static void Write(TextWriter writer)
    {
        WriteHeader(writer);
        WriteQuerySection(writer);
        WriteControlSection(writer);
        WriteConfigureSection(writer);
        WriteDeviceSection(writer);
        WriteFlashbackSection(writer);
        WriteWindowSection(writer);
        WriteWaitVerifySection(writer);
        WriteFlagsSection(writer);
    }
}
