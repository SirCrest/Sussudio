using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task StatsDockEncoderPresentation_FormatsCodecAndBitrate()
    {
        var builderType = RequireType("Sussudio.ViewModels.StatsPresentationBuilder");
        var snapshotType = RequireType("Sussudio.StatsSnapshot");
        var buildDockPresentation = builderType.GetMethod("BuildDockPresentation", BindingFlags.Static | BindingFlags.Public)
            ?? throw new InvalidOperationException("BuildDockPresentation was not found.");

        object Build(string? codecName, bool recording = true)
        {
            var snapshot = CreateUninitializedObject(snapshotType);
            SetPropertyBackingField(snapshot, "Recording", recording);
            SetPropertyBackingField(snapshot, "EncoderCodecName", codecName);
            SetPropertyBackingField(snapshot, "EncoderWidth", 3840);
            SetPropertyBackingField(snapshot, "EncoderHeight", 2160);
            SetPropertyBackingField(snapshot, "EncoderFrameRate", 59.94);
            SetPropertyBackingField(snapshot, "EncoderTargetBitRate", 50_000_000u);
            SetPropertyBackingField(snapshot, "AvSyncEncoderDriftMs", (double?)2.25d);
            SetPropertyBackingField(snapshot, "AvSyncEncoderCorrectionSamples", (long?)7L);
            SetPropertyBackingField(snapshot, "VisualCadenceMotionConfidence", string.Empty);

            return buildDockPresentation.Invoke(null, new[] { snapshot })
                ?? throw new InvalidOperationException("BuildDockPresentation returned null.");
        }

        var hevc = Build("hevc_nvenc");
        AssertEqual(true, GetBoolProperty(hevc, "EncoderActive"), "HEVC encoder active");
        AssertEqual("HEVC (NVENC)", GetStringProperty(hevc, "EncoderCodec"), "HEVC encoder label");
        AssertEqual("3840 x 2160", GetStringProperty(hevc, "EncoderResolution"), "HEVC encoder resolution");
        AssertEqual("59.94 fps", GetStringProperty(hevc, "EncoderFrameRate"), "HEVC encoder frame rate");
        AssertEqual("50 Mbps", GetStringProperty(hevc, "EncoderBitrate"), "HEVC encoder bitrate");
        AssertEqual(true, GetBoolProperty(hevc, "EncoderDriftVisible"), "encoder drift visible while recording");
        AssertEqual("+2.2ms (7 corr)", GetStringProperty(hevc, "EncoderDrift"), "encoder drift text");

        var av1 = Build("av1_nvenc");
        AssertEqual("AV1 (NVENC)", GetStringProperty(av1, "EncoderCodec"), "AV1 encoder label");

        var passthrough = Build("software_custom");
        AssertEqual("software_custom", GetStringProperty(passthrough, "EncoderCodec"), "unknown encoder label passthrough");

        var inactive = Build(null);
        AssertEqual(false, GetBoolProperty(inactive, "EncoderActive"), "inactive encoder hidden");
        AssertEqual(string.Empty, GetStringProperty(inactive, "EncoderCodec"), "inactive encoder codec");

        var idleDrift = Build("h264_nvenc", recording: false);
        AssertEqual(false, GetBoolProperty(idleDrift, "EncoderDriftVisible"), "encoder drift hidden while idle");
        AssertEqual(string.Empty, GetStringProperty(idleDrift, "EncoderDrift"), "idle encoder drift text");

        return Task.CompletedTask;
    }
}
