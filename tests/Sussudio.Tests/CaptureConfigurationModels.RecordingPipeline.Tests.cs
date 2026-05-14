using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

static partial class Program
{
    private static Task RecordingPipelineOptions_DefaultsAndCapacityBounds()
    {
        var optionsType = RequireType("Sussudio.Models.RecordingPipelineOptions");
        var dropPolicyType = RequireType("Sussudio.Models.VideoFrameDropPolicy");
        AssertEnumValues(dropPolicyType, ("DropOldest", 0), ("DropNewest", 1));
        AssertDeclaredConfigProperties(
            optionsType,
            new ConfigPropertySpec[]
            {
                ConfigProperty("TargetVideoLatencyMs", typeof(int), ConfigSetterExpectation.Set),
                ConfigProperty("MinBufferedVideoFrames", typeof(int), ConfigSetterExpectation.Set),
                ConfigProperty("MaxBufferedVideoFrames", typeof(int), ConfigSetterExpectation.Set),
                ConfigProperty("VideoDropPolicy", dropPolicyType, ConfigSetterExpectation.Set)
            });

        var options = CreateConfigInstance(optionsType);
        var resolve = optionsType.GetMethod("ResolveVideoQueueCapacity", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("RecordingPipelineOptions.ResolveVideoQueueCapacity not found.");
        AssertEqual(250, GetIntProperty(options, "TargetVideoLatencyMs"), "RecordingPipelineOptions.TargetVideoLatencyMs default");
        AssertEqual(4, GetIntProperty(options, "MinBufferedVideoFrames"), "RecordingPipelineOptions.MinBufferedVideoFrames default");
        AssertEqual(30, GetIntProperty(options, "MaxBufferedVideoFrames"), "RecordingPipelineOptions.MaxBufferedVideoFrames default");
        AssertEqual(ParseEnum("Sussudio.Models.VideoFrameDropPolicy", "DropOldest"), GetPropertyValue(options, "VideoDropPolicy"), "RecordingPipelineOptions.VideoDropPolicy default");
        AssertEqual(15, (int)resolve.Invoke(options, new object[] { 60d })!, "RecordingPipelineOptions 60fps default capacity");
        AssertEqual(15, (int)resolve.Invoke(options, new object[] { -1d })!, "RecordingPipelineOptions non-positive frame rate fallback");

        SetPropertyOrBackingField(options, "TargetVideoLatencyMs", 1);
        AssertEqual(4, (int)resolve.Invoke(options, new object[] { 60d })!, "RecordingPipelineOptions latency floor clamps to min");

        SetPropertyOrBackingField(options, "MinBufferedVideoFrames", 0);
        SetPropertyOrBackingField(options, "MaxBufferedVideoFrames", 2);
        AssertEqual(1, (int)resolve.Invoke(options, new object[] { 10d })!, "RecordingPipelineOptions min floor supports one-frame queue");

        SetPropertyOrBackingField(options, "TargetVideoLatencyMs", 250);
        SetPropertyOrBackingField(options, "MinBufferedVideoFrames", 8);
        SetPropertyOrBackingField(options, "MaxBufferedVideoFrames", 4);
        AssertEqual(8, (int)resolve.Invoke(options, new object[] { 120d })!, "RecordingPipelineOptions max lower than min clamps to min");

        SetPropertyOrBackingField(options, "VideoDropPolicy", ParseEnum("Sussudio.Models.VideoFrameDropPolicy", "DropNewest"));
        AssertEqual(ParseEnum("Sussudio.Models.VideoFrameDropPolicy", "DropNewest"), GetPropertyValue(options, "VideoDropPolicy"), "RecordingPipelineOptions.VideoDropPolicy round-trip");

        return Task.CompletedTask;
    }

    private static Task RecordingPipelineOptions_ResolvesVideoQueueCapacity()
    {
        var options = CreateInstance("Sussudio.Models.RecordingPipelineOptions");

        // Default: 250ms latency, min=4, max=30
        // At 60fps: ceil(60 * 250 / 1000) = ceil(15) = 15 -> clamp(15, 4, 30) = 15
        var method = options.GetType().GetMethod("ResolveVideoQueueCapacity")!;
        var at60 = (int)method.Invoke(options, new object[] { 60.0 })!;
        AssertEqual(15, at60, "60fps default latency");

        // At 120fps: ceil(120 * 250 / 1000) = ceil(30) = 30 -> clamp(30, 4, 30) = 30
        var at120 = (int)method.Invoke(options, new object[] { 120.0 })!;
        AssertEqual(30, at120, "120fps default latency");

        // At 30fps: ceil(30 * 250 / 1000) = ceil(7.5) = 8 -> clamp(8, 4, 30) = 8
        var at30 = (int)method.Invoke(options, new object[] { 30.0 })!;
        AssertEqual(8, at30, "30fps default latency");

        // Zero fps falls back to 60fps: ceil(60 * 250 / 1000) = 15
        var atZero = (int)method.Invoke(options, new object[] { 0.0 })!;
        AssertEqual(15, atZero, "0fps fallback to 60");

        return Task.CompletedTask;
    }
}
