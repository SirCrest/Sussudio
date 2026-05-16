using System.Text;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionOptionalTextFormatter;

namespace Sussudio.Tools;

public static partial class DiagnosticSessionResultFormatter
{
    private static void AppendFlashbackExport(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine(
            "Flashback Export: " +
            $"observed={result.FlashbackExportObserved} " +
            $"activeEnd={result.FlashbackExportActiveAtEnd} " +
            $"statusEnd={FormatOptional(result.FlashbackExportStatusAtEnd)} " +
            $"failureKindEnd={FormatOptional(result.FlashbackExportFailureKindAtEnd)} " +
            $"messageEnd={FormatOptional(result.FlashbackExportMessageAtEnd)} " +
            $"forceRotateFallbacksEnd={result.FlashbackExportForceRotateFallbacksAtEnd} " +
            $"forceRotateFallbacksDelta={result.FlashbackExportForceRotateFallbacksDelta} " +
            $"lastForceRotateFallbackSegmentsEnd={result.FlashbackExportLastForceRotateFallbackSegmentsAtEnd} " +
            $"lastResultIdEnd={result.LastExportIdAtEnd} " +
            $"lastSuccessEnd={FormatOptional(result.LastExportSuccessAtEnd)} " +
            $"lastMessageEnd={FormatOptional(result.LastExportMessageAtEnd)} " +
            $"pathEnd={FormatOptional(result.FlashbackExportOutputPathAtEnd)} " +
            $"maxElapsedMs={result.FlashbackExportMaxElapsedMsObserved} " +
            $"maxProgressAgeMs={result.FlashbackExportMaxLastProgressAgeMsObserved} " +
            $"maxBytes={FormatBytes(result.FlashbackExportMaxOutputBytesObserved)} " +
            $"maxThroughput={FormatBytes((long)result.FlashbackExportMaxThroughputBytesPerSecObserved)}/s");
    }
}
