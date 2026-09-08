using Sussudio.Services.Automation;
using Xunit;

namespace Sussudio.Tests;

/// <summary>
/// Executes the linked production classifier. This is the diagnostic that tells an
/// operator which stage of capture → decode → jitter → render → present is losing
/// frames, so the gates (is there enough sample?) and the precedence between stages
/// are the contract, not the exact evidence wording.
///
/// Previously only pinned by a source-text assertion on the record declaration.
/// </summary>
public sealed class PreviewPacingSlowStageClassifierTests
{
    // 60 fps target, a ready preview sample, healthy tail, no incidents anywhere.
    private static PreviewPacingClassificationInput Healthy() => new()
    {
        IsPreviewing = true,
        TargetFrameRate = 60,
        PreviewCadenceSampleCount = 60,
        PreviewCadenceSampleDurationMs = 30_000,
        PreviewCadenceObservedFps = 60,
        PreviewCadenceOnePercentLowFps = 60,
        PreviewCadenceP99IntervalMs = 16.0,
    };

    [Fact]
    public void ClassifyRejectsANullInput()
    {
        Assert.Throws<ArgumentNullException>(() => PreviewPacingSlowStageClassifier.Classify(null!));
    }

    // ── Gates ────────────────────────────────────────────────────────────────

    [Fact]
    public void AnInactivePreviewIsNotDiagnosed()
    {
        var result = PreviewPacingSlowStageClassifier.Classify(new PreviewPacingClassificationInput
        {
            IsPreviewing = false,
            TargetFrameRate = 60,
        });

        Assert.Equal("InsufficientSample", result.LikelySlowStage);
        Assert.Equal("Low", result.Confidence);
        Assert.Contains("not active", result.Evidence, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AnUnknownTargetFrameRateIsNotDiagnosed()
    {
        // No TargetFrameRate, no CaptureExpectedFrameRate and no expected interval
        // means there is no budget to compare against.
        var result = PreviewPacingSlowStageClassifier.Classify(new PreviewPacingClassificationInput
        {
            IsPreviewing = true,
        });

        Assert.Equal("InsufficientSample", result.LikelySlowStage);
        Assert.Equal("Low", result.Confidence);
    }

    [Theory]
    // Too few samples, even with a full-duration window.
    [InlineData(59, 30_000)]
    // Enough samples, but the window is shorter than the 30s minimum.
    [InlineData(600, 29_999)]
    [InlineData(0, 0)]
    public void AnUnderfilledSampleWindowIsNotDiagnosed(int sampleCount, double durationMs)
    {
        var input = new PreviewPacingClassificationInput
        {
            IsPreviewing = true,
            TargetFrameRate = 60,
            PreviewCadenceSampleCount = sampleCount,
            PreviewCadenceSampleDurationMs = durationMs,
            PreviewCadenceOnePercentLowFps = 10,
            PreviewCadenceP99IntervalMs = 500,
        };

        var result = PreviewPacingSlowStageClassifier.Classify(input);

        Assert.Equal("InsufficientSample", result.LikelySlowStage);
    }

    [Fact]
    public void TheSampleWindowIsExactlySixtyFramesAndThirtySecondsBelowOneHundredFps()
    {
        var atThreshold = new PreviewPacingClassificationInput
        {
            IsPreviewing = true,
            TargetFrameRate = 60,
            PreviewCadenceSampleCount = 60,
            PreviewCadenceSampleDurationMs = 30_000,
            PreviewCadenceOnePercentLowFps = 10,
        };

        Assert.NotEqual(
            "InsufficientSample",
            PreviewPacingSlowStageClassifier.Classify(atThreshold).LikelySlowStage);
    }

    [Fact]
    public void HighRefreshTargetsRequireProportionallyMoreSamples()
    {
        // At >= 100 fps the window scales to targetFps * 10, so 1000 samples is
        // still short of the 1200 a 120 fps preview needs.
        var shortWindow = new PreviewPacingClassificationInput
        {
            IsPreviewing = true,
            TargetFrameRate = 120,
            PreviewCadenceSampleCount = 1_000,
            PreviewCadenceSampleDurationMs = 30_000,
            PreviewCadenceOnePercentLowFps = 10,
        };
        Assert.Equal(
            "InsufficientSample",
            PreviewPacingSlowStageClassifier.Classify(shortWindow).LikelySlowStage);

        var fullWindow = new PreviewPacingClassificationInput
        {
            IsPreviewing = true,
            TargetFrameRate = 120,
            PreviewCadenceSampleCount = 1_200,
            PreviewCadenceSampleDurationMs = 30_000,
            PreviewCadenceOnePercentLowFps = 10,
        };
        Assert.NotEqual(
            "InsufficientSample",
            PreviewPacingSlowStageClassifier.Classify(fullWindow).LikelySlowStage);
    }

    [Theory]
    [InlineData("RecentMjpegDropped")]
    [InlineData("RecentMjpegFailures")]
    [InlineData("RecentPreviewJitterDropped")]
    [InlineData("RecentPreviewJitterDeadlineDrops")]
    [InlineData("RecentPreviewJitterUnderflows")]
    [InlineData("RecentRendererDropped")]
    [InlineData("RecentD3DMissedRefreshes")]
    [InlineData("RecentD3DStatsFailures")]
    [InlineData("RecentD3DFrameLatencyWaitTimeoutCount")]
    public void AnyHardIncidentBypassesTheSampleWindowGate(string counter)
    {
        // Dropped or failed frames are proof of a problem on their own; waiting 30s
        // for a cadence window before reporting them would hide real incidents.
        var input = new PreviewPacingClassificationInput
        {
            IsPreviewing = true,
            TargetFrameRate = 60,
            PreviewCadenceSampleCount = 0,
            PreviewCadenceSampleDurationMs = 0,
            RecentMjpegDropped = counter == "RecentMjpegDropped" ? 1 : 0,
            RecentMjpegFailures = counter == "RecentMjpegFailures" ? 1 : 0,
            RecentPreviewJitterDropped = counter == "RecentPreviewJitterDropped" ? 1 : 0,
            RecentPreviewJitterDeadlineDrops = counter == "RecentPreviewJitterDeadlineDrops" ? 1 : 0,
            RecentPreviewJitterUnderflows = counter == "RecentPreviewJitterUnderflows" ? 1 : 0,
            RecentRendererDropped = counter == "RecentRendererDropped" ? 1 : 0,
            RecentD3DMissedRefreshes = counter == "RecentD3DMissedRefreshes" ? 1 : 0,
            RecentD3DStatsFailures = counter == "RecentD3DStatsFailures" ? 1 : 0,
            RecentD3DFrameLatencyWaitTimeoutCount = counter == "RecentD3DFrameLatencyWaitTimeoutCount" ? 1 : 0,
        };

        var result = PreviewPacingSlowStageClassifier.Classify(input);

        Assert.NotEqual("InsufficientSample", result.LikelySlowStage);
    }

    // ── Target resolution ────────────────────────────────────────────────────

    [Fact]
    public void TheCaptureRateSuppliesTheBudgetWhenNoPreviewTargetIsSet()
    {
        var result = PreviewPacingSlowStageClassifier.Classify(new PreviewPacingClassificationInput
        {
            IsPreviewing = true,
            CaptureExpectedFrameRate = 60,
            PreviewCadenceSampleCount = 60,
            PreviewCadenceSampleDurationMs = 30_000,
            PreviewCadenceOnePercentLowFps = 60,
            PreviewCadenceP99IntervalMs = 16.0,
        });

        Assert.NotEqual("InsufficientSample", result.LikelySlowStage);
    }

    [Fact]
    public void TheExpectedIntervalSuppliesTheBudgetAsALastResort()
    {
        var result = PreviewPacingSlowStageClassifier.Classify(new PreviewPacingClassificationInput
        {
            IsPreviewing = true,
            PreviewCadenceExpectedIntervalMs = 1000.0 / 60.0,
            PreviewCadenceSampleCount = 60,
            PreviewCadenceSampleDurationMs = 30_000,
            PreviewCadenceOnePercentLowFps = 60,
            PreviewCadenceP99IntervalMs = 16.0,
        });

        Assert.NotEqual("InsufficientSample", result.LikelySlowStage);
    }

    // ── Healthy pacing ───────────────────────────────────────────────────────

    [Fact]
    public void HealthyPacingReportsNoSuspectStage()
    {
        var result = PreviewPacingSlowStageClassifier.Classify(Healthy());

        Assert.Equal("Unknown", result.LikelySlowStage);
        Assert.Equal("None", result.Confidence);
    }

    [Theory]
    // 1%-low must fall below 98% of target before it counts as degraded.
    [InlineData(59.0, "Unknown")]
    [InlineData(58.8, "Unknown")]
    [InlineData(58.0, "SourceCapture")]
    public void TheOnePercentLowThresholdSitsAtNinetyEightPercentOfTarget(
        double onePercentLowFps,
        string expectedStage)
    {
        // Pair the weak tail with a capture-side incident so that when the tail does
        // count as degraded the classifier has a lane to attribute it to.
        var input = new PreviewPacingClassificationInput
        {
            IsPreviewing = true,
            TargetFrameRate = 60,
            PreviewCadenceSampleCount = 60,
            PreviewCadenceSampleDurationMs = 30_000,
            PreviewCadenceOnePercentLowFps = onePercentLowFps,
            PreviewCadenceP99IntervalMs = 16.0,
            CaptureCadenceEstimatedDroppedFrames = 5,
        };

        Assert.Equal(expectedStage, PreviewPacingSlowStageClassifier.Classify(input).LikelySlowStage);
    }

    [Fact]
    public void AP99IntervalWithinEightPercentOfBudgetIsNotDegraded()
    {
        var withinBudget = new PreviewPacingClassificationInput
        {
            IsPreviewing = true,
            TargetFrameRate = 60,
            PreviewCadenceSampleCount = 60,
            PreviewCadenceSampleDurationMs = 30_000,
            PreviewCadenceOnePercentLowFps = 60,
            // 16.667ms budget * 1.08 = 18.0ms; stay just under.
            PreviewCadenceP99IntervalMs = 17.9,
        };

        Assert.Equal("Unknown", PreviewPacingSlowStageClassifier.Classify(withinBudget).LikelySlowStage);
    }

    // ── Stage attribution and precedence ─────────────────────────────────────

    [Fact]
    public void DroppedCaptureFramesAreAttributedToTheSourceWithHighConfidence()
    {
        var input = new PreviewPacingClassificationInput
        {
            IsPreviewing = true,
            TargetFrameRate = 60,
            PreviewCadenceSampleCount = 60,
            PreviewCadenceSampleDurationMs = 30_000,
            PreviewCadenceOnePercentLowFps = 50,
            CaptureCadenceEstimatedDroppedFrames = 12,
        };

        var result = PreviewPacingSlowStageClassifier.Classify(input);

        Assert.Equal("SourceCapture", result.LikelySlowStage);
        Assert.Equal("High", result.Confidence);
        Assert.Contains("drops=12", result.Evidence, StringComparison.Ordinal);
    }

    [Fact]
    public void ASevereCaptureGapAloneIsEnoughToImplicateTheSource()
    {
        var input = new PreviewPacingClassificationInput
        {
            IsPreviewing = true,
            TargetFrameRate = 60,
            PreviewCadenceSampleCount = 60,
            PreviewCadenceSampleDurationMs = 30_000,
            PreviewCadenceOnePercentLowFps = 50,
            CaptureCadenceSevereGapCount = 1,
        };

        var result = PreviewPacingSlowStageClassifier.Classify(input);

        Assert.Equal("SourceCapture", result.LikelySlowStage);
        Assert.Equal("High", result.Confidence);
    }

    [Fact]
    public void ADegradedCaptureTailWithoutIncidentsIsOnlyMediumConfidence()
    {
        var input = new PreviewPacingClassificationInput
        {
            IsPreviewing = true,
            TargetFrameRate = 60,
            CaptureExpectedFrameRate = 60,
            PreviewCadenceSampleCount = 60,
            PreviewCadenceSampleDurationMs = 30_000,
            PreviewCadenceOnePercentLowFps = 50,
            CaptureCadenceSampleCount = 60,
            CaptureCadenceSampleDurationMs = 30_000,
            CaptureCadenceOnePercentLowFps = 50,
        };

        var result = PreviewPacingSlowStageClassifier.Classify(input);

        Assert.Equal("SourceCapture", result.LikelySlowStage);
        Assert.Equal("Medium", result.Confidence);
    }

    [Fact]
    public void TheSourceIsBlamedBeforeAnyDownstreamStage()
    {
        // Upstream frame loss explains downstream symptoms, so reporting MjpegDecode
        // here would send an operator to the wrong subsystem.
        var input = new PreviewPacingClassificationInput
        {
            IsPreviewing = true,
            TargetFrameRate = 60,
            PreviewCadenceSampleCount = 60,
            PreviewCadenceSampleDurationMs = 30_000,
            PreviewCadenceOnePercentLowFps = 50,
            CaptureCadenceEstimatedDroppedFrames = 5,
            // Loud downstream evidence that must not win.
            MjpegDecodeP95Ms = 250,
            MjpegPipelineP95Ms = 300,
            MjpegPipelineMaxMs = 900,
            RecentMjpegDropped = 40,
            RecentPreviewJitterDeadlineDrops = 30,
            RecentRendererDropped = 25,
            PreviewD3DTotalFrameCpuP99Ms = 200,
        };

        Assert.Equal("SourceCapture", PreviewPacingSlowStageClassifier.Classify(input).LikelySlowStage);
    }

    [Fact]
    public void ASmallCaptureDropPercentageStillImplicatesTheSource()
    {
        var input = new PreviewPacingClassificationInput
        {
            IsPreviewing = true,
            TargetFrameRate = 60,
            PreviewCadenceSampleCount = 60,
            PreviewCadenceSampleDurationMs = 30_000,
            PreviewCadenceOnePercentLowFps = 50,
            // Threshold is > 0.1 percent.
            CaptureCadenceEstimatedDropPercent = 0.2,
        };

        Assert.Equal("SourceCapture", PreviewPacingSlowStageClassifier.Classify(input).LikelySlowStage);
    }

    [Fact]
    public void ACaptureDropPercentageAtOrBelowTheFloorDoesNotImplicateTheSource()
    {
        var input = new PreviewPacingClassificationInput
        {
            IsPreviewing = true,
            TargetFrameRate = 60,
            PreviewCadenceSampleCount = 60,
            PreviewCadenceSampleDurationMs = 30_000,
            PreviewCadenceOnePercentLowFps = 50,
            CaptureCadenceEstimatedDropPercent = 0.1,
        };

        Assert.NotEqual("SourceCapture", PreviewPacingSlowStageClassifier.Classify(input).LikelySlowStage);
    }

    [Fact]
    public void EveryClassificationCarriesNonEmptyEvidence()
    {
        // The evidence string is what an operator reads; an empty one makes the
        // verdict unactionable.
        var inputs = new[]
        {
            new PreviewPacingClassificationInput { IsPreviewing = false },
            new PreviewPacingClassificationInput { IsPreviewing = true },
            Healthy(),
            new PreviewPacingClassificationInput
            {
                IsPreviewing = true,
                TargetFrameRate = 60,
                PreviewCadenceSampleCount = 60,
                PreviewCadenceSampleDurationMs = 30_000,
                PreviewCadenceOnePercentLowFps = 50,
                CaptureCadenceEstimatedDroppedFrames = 3,
            },
        };

        foreach (var input in inputs)
        {
            var result = PreviewPacingSlowStageClassifier.Classify(input);

            Assert.False(string.IsNullOrWhiteSpace(result.LikelySlowStage));
            Assert.False(string.IsNullOrWhiteSpace(result.Confidence));
            Assert.False(string.IsNullOrWhiteSpace(result.Evidence));
        }
    }

    [Fact]
    public void ClassificationIsDeterministicForTheSameInput()
    {
        var input = new PreviewPacingClassificationInput
        {
            IsPreviewing = true,
            TargetFrameRate = 60,
            PreviewCadenceSampleCount = 60,
            PreviewCadenceSampleDurationMs = 30_000,
            PreviewCadenceOnePercentLowFps = 50,
            CaptureCadenceEstimatedDroppedFrames = 4,
        };

        Assert.Equal(
            PreviewPacingSlowStageClassifier.Classify(input),
            PreviewPacingSlowStageClassifier.Classify(input));
    }
}
