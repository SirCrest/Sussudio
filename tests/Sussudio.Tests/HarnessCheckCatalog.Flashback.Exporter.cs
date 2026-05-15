using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddFlashbackExporterChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "Flashback suppressed exceptions use app logs",
            FlashbackSuppressedExceptionsUseAppLogs);
        await AddCheckAsync(results,
            "Flashback exporter cleanup ignores nonexistent directories",
            FlashbackExporter_CleanupOrphanedTempFiles_HandlesNonexistentDirectory);
        await AddCheckAsync(results,
            "Flashback exporter cleanup deletes orphaned temp files",
            FlashbackExporter_CleanupOrphanedTempFiles_DeletesTempFiles);
        await AddCheckAsync(results,
            "Flashback exporter does not scan user output directory for orphans",
            FlashbackExporter_DoesNotScanUserOutputDirectoryForOrphans);
        await AddCheckAsync(results,
            "Flashback exporter task wrappers dispose linked cancellation",
            FlashbackExporter_TaskRunWrappers_DisposeLinkedCancellation);
        await AddCheckAsync(results,
            "Flashback exporter ownership is split across focused partials",
            FlashbackExporter_OwnershipIsSplitAcrossFocusedPartials);
        await AddCheckAsync(results,
            "Flashback exporter rejects null requests",
            FlashbackExporter_RejectsNullRequests);
        await AddCheckAsync(results,
            "Flashback exporter fails when input file is missing",
            FlashbackExporter_ExportAsync_ReturnsFailure_WhenInputFileNotFound);
        await AddCheckAsync(results,
            "Flashback exporter fails when output path is empty",
            FlashbackExporter_ExportAsync_ReturnsFailure_WhenOutputPathEmpty);
        await AddCheckAsync(results,
            "Flashback exporter fails when no segment paths are provided",
            FlashbackExporter_ExportSegmentsAsync_ReturnsFailure_WhenNoSegments);
        await AddCheckAsync(results,
            "Flashback exporter output path validation returns failure",
            FlashbackExporter_OutputPathValidation_ReturnsFailure);
        await AddCheckAsync(results,
            "Flashback export failure classifier maps command failures",
            FlashbackExportFailureClassifier_MapsCommandFailures);
        await AddCheckAsync(results,
            "Flashback exporter rejects directory output paths",
            FlashbackExporter_ExportAsync_ReturnsFailure_WhenOutputPathIsDirectory);
        await AddCheckAsync(results,
            "Flashback exporter rejects invalid export ranges",
            FlashbackExporter_RejectsInvalidExportRanges);
        await AddCheckAsync(results,
            "Flashback rejected export diagnostics preserve attempted range",
            FlashbackExportRejectedDiagnostics_PreserveAttemptedRange);
        await AddCheckAsync(results,
            "Flashback exporter rejects empty segment paths",
            FlashbackExporter_RejectsEmptySegmentPaths);
        await AddCheckAsync(results,
            "Flashback exporter rejects duplicate segment paths",
            FlashbackExporter_RejectsDuplicateSegmentPaths);
        await AddCheckAsync(results,
            "Flashback exporter progress callbacks are best effort",
            FlashbackExporter_ProgressCallbacksAreBestEffort);
        await AddCheckAsync(results,
            "Flashback exporter releases buffered segment packets on failures",
            FlashbackExporter_ReleasesBufferedSegmentPacketsOnFailures);
        await AddCheckAsync(results,
            "Flashback exporter timestamp conversions are saturating",
            FlashbackExporter_TimestampConversionsAreSaturating);
        await AddCheckAsync(results,
            "Flashback exporter input stream counts are bounded",
            FlashbackExporter_InputStreamCountsAreBounded);
        await AddCheckAsync(results,
            "Flashback exporter segment template validation guards missing video streams",
            FlashbackExporter_SegmentTemplateValidation_GuardsMissingVideoStream);
        await AddCheckAsync(results,
            "Flashback exporter fails when requested segments are skipped",
            FlashbackExporter_FailsWhenRequestedSegmentsAreSkipped);
        await AddCheckAsync(results,
            "Flashback exporter returns cancellation result while waiting for export lock",
            FlashbackExporter_ReturnsCancellationResult_WhenLockWaitCancelled);
        await AddCheckAsync(results,
            "Flashback exporter cancellation wins before validation",
            FlashbackExporter_CancellationWinsBeforeValidation);
        await AddCheckAsync(results,
            "Flashback exporter fails fast when segment files are gone",
            FlashbackExporter_ReturnsFailure_WhenSegmentFilesAreGone);
        await AddCheckAsync(results,
            "Flashback exporter dispose timeout does not tear down active native state",
            FlashbackExporter_DisposeTimeoutDoesNotTearDownActiveNativeState);
        await AddCheckAsync(results,
            "Flashback exporter rejects output paths that overwrite source segments",
            FlashbackExporter_RejectsOutputPathThatOverwritesSource);
        await AddCheckAsync(results,
            "Flashback exporter invalid temp output preserves existing exports",
            FlashbackExporter_InvalidTempOutputDoesNotReplaceExistingExport);
        await AddCheckAsync(results,
            "Flashback exporter refuses to overwrite existing destination when force is false",
            FlashbackExporter_RefusesOverwriteWhenDestinationExistsAndForceFalse);
        await AddCheckAsync(results,
            "Flashback exporter overwrites existing destination when force is true",
            FlashbackExporter_OverwritesWhenForceTrue);
        await AddCheckAsync(results,
            "Flashback exporter deletes invalid moved final outputs",
            FlashbackExporter_FinalValidationFailureDeletesMovedOutput);
        await AddCheckAsync(results,
            "Flashback exporter rejects blocked temp output paths before native export",
            FlashbackExporter_RejectsBlockedTempOutputPathBeforeNativeExport);
    }
}
