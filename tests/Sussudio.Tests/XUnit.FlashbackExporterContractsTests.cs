using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class FlashbackExporterContractsTests
{
    public FlashbackExporterContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task FlashbackSuppressedExceptionsUseAppLogs()
        => global::Program.FlashbackSuppressedExceptionsUseAppLogs();

    [Fact]
    public Task FlashbackExporterCleanupIgnoresNonexistentDirectories()
        => global::Program.FlashbackExporter_CleanupOrphanedTempFiles_HandlesNonexistentDirectory();

    [Fact]
    public Task FlashbackExporterCleanupDeletesOrphanedTempFiles()
        => global::Program.FlashbackExporter_CleanupOrphanedTempFiles_DeletesTempFiles();

    [Fact]
    public Task FlashbackExporterDoesNotScanUserOutputDirectoryForOrphans()
        => global::Program.FlashbackExporter_DoesNotScanUserOutputDirectoryForOrphans();

    [Fact]
    public Task FlashbackExporterTaskWrappersDisposeLinkedCancellation()
        => global::Program.FlashbackExporter_TaskRunWrappers_DisposeLinkedCancellation();

    [Fact]
    public Task FlashbackExporterOwnershipIsSplitAcrossFocusedPartials()
        => global::Program.FlashbackExporter_OwnershipIsSplitAcrossFocusedPartials();

    [Fact]
    public Task FlashbackExporterRejectsNullRequests()
        => global::Program.FlashbackExporter_RejectsNullRequests();

    [Fact]
    public Task FlashbackExporterFailsWhenInputFileIsMissing()
        => global::Program.FlashbackExporter_ExportAsync_ReturnsFailure_WhenInputFileNotFound();

    [Fact]
    public Task FlashbackExporterFailsWhenOutputPathIsEmpty()
        => global::Program.FlashbackExporter_ExportAsync_ReturnsFailure_WhenOutputPathEmpty();

    [Fact]
    public Task FlashbackExporterFailsWhenNoSegmentPathsAreProvided()
        => global::Program.FlashbackExporter_ExportSegmentsAsync_ReturnsFailure_WhenNoSegments();

    [Fact]
    public Task FlashbackExporterOutputPathValidationReturnsFailure()
        => global::Program.FlashbackExporter_OutputPathValidation_ReturnsFailure();

    [Fact]
    public Task FlashbackExportFailureClassifierMapsCommandFailures()
        => global::Program.FlashbackExportFailureClassifier_MapsCommandFailures();

    [Fact]
    public Task FlashbackExporterRejectsDirectoryOutputPaths()
        => global::Program.FlashbackExporter_ExportAsync_ReturnsFailure_WhenOutputPathIsDirectory();

    [Fact]
    public Task FlashbackExporterRejectsInvalidExportRanges()
        => global::Program.FlashbackExporter_RejectsInvalidExportRanges();

    [Fact]
    public Task FlashbackRejectedExportDiagnosticsPreserveAttemptedRange()
        => global::Program.FlashbackExportRejectedDiagnostics_PreserveAttemptedRange();

    [Fact]
    public Task FlashbackExporterRejectsEmptySegmentPaths()
        => global::Program.FlashbackExporter_RejectsEmptySegmentPaths();

    [Fact]
    public Task FlashbackExporterRejectsDuplicateSegmentPaths()
        => global::Program.FlashbackExporter_RejectsDuplicateSegmentPaths();

    [Fact]
    public Task FlashbackExporterProgressCallbacksAreBestEffort()
        => global::Program.FlashbackExporter_ProgressCallbacksAreBestEffort();

    [Fact]
    public Task FlashbackExporterReleasesBufferedSegmentPacketsOnFailures()
        => global::Program.FlashbackExporter_ReleasesBufferedSegmentPacketsOnFailures();

    [Fact]
    public Task FlashbackExporterTimestampConversionsAreSaturating()
        => global::Program.FlashbackExporter_TimestampConversionsAreSaturating();

    [Fact]
    public Task FlashbackExporterInputStreamCountsAreBounded()
        => global::Program.FlashbackExporter_InputStreamCountsAreBounded();

    [Fact]
    public Task FlashbackExporterSegmentTemplateValidationGuardsMissingVideoStreams()
        => global::Program.FlashbackExporter_SegmentTemplateValidation_GuardsMissingVideoStream();

    [Fact]
    public Task FlashbackExporterFailsWhenRequestedSegmentsAreSkipped()
        => global::Program.FlashbackExporter_FailsWhenRequestedSegmentsAreSkipped();

    [Fact]
    public Task FlashbackExporterReturnsCancellationResultWhileWaitingForExportLock()
        => global::Program.FlashbackExporter_ReturnsCancellationResult_WhenLockWaitCancelled();

    [Fact]
    public Task FlashbackExporterCancellationWinsBeforeValidation()
        => global::Program.FlashbackExporter_CancellationWinsBeforeValidation();

    [Fact]
    public Task FlashbackExporterFailsFastWhenSegmentFilesAreGone()
        => global::Program.FlashbackExporter_ReturnsFailure_WhenSegmentFilesAreGone();

    [Fact]
    public Task FlashbackExporterDisposeTimeoutDoesNotTearDownActiveNativeState()
        => global::Program.FlashbackExporter_DisposeTimeoutDoesNotTearDownActiveNativeState();

    [Fact]
    public Task FlashbackExporterRejectsOutputPathsThatOverwriteSourceSegments()
        => global::Program.FlashbackExporter_RejectsOutputPathThatOverwritesSource();

    [Fact]
    public Task FlashbackExporterInvalidTempOutputPreservesExistingExports()
        => global::Program.FlashbackExporter_InvalidTempOutputDoesNotReplaceExistingExport();

    [Fact]
    public Task FlashbackExporterRefusesToOverwriteExistingDestinationWhenForceIsFalse()
        => global::Program.FlashbackExporter_RefusesOverwriteWhenDestinationExistsAndForceFalse();

    [Fact]
    public Task FlashbackExporterOverwritesExistingDestinationWhenForceIsTrue()
        => global::Program.FlashbackExporter_OverwritesWhenForceTrue();

    [Fact]
    public Task FlashbackExporterDeletesInvalidMovedFinalOutputs()
        => global::Program.FlashbackExporter_FinalValidationFailureDeletesMovedOutput();

    [Fact]
    public Task FlashbackExporterRejectsBlockedTempOutputPathsBeforeNativeExport()
        => global::Program.FlashbackExporter_RejectsBlockedTempOutputPathBeforeNativeExport();
}
