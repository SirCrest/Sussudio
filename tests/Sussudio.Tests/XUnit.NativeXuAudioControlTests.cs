using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

public sealed class NativeXuAudioControlTests
{
    [Fact]
    public void CompleteReadbackMatchesAllControlBytes()
    {
        var expected = CloneProfile("AnalogReference");
        var result = Compare(expected, expected.ToArray());

        Assert.True(result.Matches);
        Assert.Equal(ControlIndexes().Length, result.Checked);
        Assert.Equal(0, result.Mismatched);
        Assert.Equal(0, result.Missing);
    }

    [Fact]
    public void EveryControlByteMustMatch()
    {
        var expected = CloneProfile("AnalogReference");
        var indexes = ControlIndexes();
        foreach (var index in indexes)
        {
            var actual = expected.ToArray();
            actual[index] ^= 0xFF;

            var result = Compare(expected, actual);

            Assert.False(result.Matches, $"Control byte {index} mismatch was accepted.");
            Assert.Equal(indexes.Length, result.Checked);
            Assert.Equal(1, result.Mismatched);
            Assert.Equal(0, result.Missing);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(149)]
    public void TruncatedReadbackCannotVerifyAvailableMatchingBytes(int length)
    {
        var expected = CloneProfile("AnalogReference");
        var actual = expected.Take(length).ToArray();

        AssertIncomplete(Compare(expected, actual), length);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(149)]
    public void TruncatedExpectedPayloadCannotVerifyCompleteReadback(int length)
    {
        var actual = CloneProfile("AnalogReference");
        var expected = actual.Take(length).ToArray();

        AssertIncomplete(Compare(expected, actual), length);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(149)]
    public void EqualIncompletePayloadsCannotVerifyControlState(int length)
    {
        var expected = CloneProfile("AnalogReference").Take(length).ToArray();

        AssertIncomplete(Compare(expected, expected.ToArray()), length);
    }

    [Fact]
    public void ReadbackMayOmitSuffixAfterAllControlBytes()
    {
        var expected = CloneProfile("AnalogReference").Concat(new byte[] { 0xA5, 0x5A }).ToArray();
        var actual = expected.Take(150).ToArray();
        Assert.All(ControlIndexes(), index => Assert.InRange(index, 0, actual.Length - 1));

        var result = Compare(expected, actual);

        Assert.True(result.Matches);
        Assert.Equal(ControlIndexes().Length, result.Checked);
        Assert.Equal(0, result.Mismatched);
        Assert.Equal(0, result.Missing);
    }

    [Fact]
    public void DynamicAndOtherNoncontrolDifferencesDoNotInvalidateReadback()
    {
        var expected = CloneProfile("AnalogReference").Concat(new byte[] { 0xA5, 0x5A }).ToArray();
        var actual = expected.ToArray();
        var controls = ControlIndexes().ToHashSet();
        var dynamicIndexes = CloneIndexes("DynamicByteIndexes");
        Assert.NotEmpty(dynamicIndexes);
        Assert.All(dynamicIndexes, index => Assert.DoesNotContain(index, controls));
        for (var index = 0; index < actual.Length; index++)
        {
            if (!controls.Contains(index))
            {
                actual[index] ^= 0xFF;
            }
        }

        var result = Compare(expected, actual);

        Assert.True(result.Matches);
        Assert.Equal(controls.Count, result.Checked);
        Assert.Equal(0, result.Mismatched);
        Assert.Equal(0, result.Missing);
    }

    [Fact]
    public void ComparingReadbackDoesNotMutateEitherPayload()
    {
        var expected = CloneProfile("AnalogReference");
        var actual = CloneProfile("HdmiReference");
        var expectedBefore = expected.ToArray();
        var actualBefore = actual.ToArray();

        Assert.False(Compare(expected, actual).Matches);

        Assert.Equal(expectedBefore, expected);
        Assert.Equal(actualBefore, actual);
    }

    [Theory]
    [InlineData("HdmiReference", "HDMI")]
    [InlineData("AnalogReference", "Analog")]
    public void CapturedModeRemainsRecognizableWhenDynamicBytesChange(string profile, string expectedMode)
    {
        var payload = CloneProfile(profile);
        foreach (var index in CloneIndexes("DynamicByteIndexes"))
        {
            payload[index] ^= 0xFF;
        }

        var decision = ServiceType.GetMethod("DecodeInput", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { payload })!;

        Assert.Equal(expectedMode, decision.GetType().GetProperty("Label")!.GetValue(decision));
        Assert.Equal(1d, (double)decision.GetType().GetProperty("Confidence")!.GetValue(decision)!);
    }

    [Fact]
    public void EmptyPayloadDoesNotIdentifyAnAudioMode()
    {
        var decision = ServiceType.GetMethod("DecodeInput", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { Array.Empty<byte>() })!;

        Assert.Equal("Unknown", decision.GetType().GetProperty("Label")!.GetValue(decision));
        Assert.Equal(0d, (double)decision.GetType().GetProperty("Confidence")!.GetValue(decision)!);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-audio-mode")]
    public void InvalidModeDoesNotSelectAReference(string? mode)
    {
        object?[] args = { mode, null };
        var accepted = (bool)ServiceType.GetMethod("TryGetTargetInputReference", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, args)!;

        Assert.False(accepted);
        Assert.Empty((byte[])args[1]!);
    }

    private static void AssertIncomplete((bool Matches, int Checked, int Mismatched, int Missing) result, int availableLength)
    {
        var indexes = ControlIndexes();
        Assert.False(result.Matches);
        Assert.Equal(indexes.Count(index => index < availableLength), result.Checked);
        Assert.Equal(0, result.Mismatched);
        Assert.Equal(indexes.Count(index => index >= availableLength), result.Missing);
        Assert.True(result.Missing > 0);
    }

    private static (bool Matches, int Checked, int Mismatched, int Missing) Compare(byte[] expected, byte[] actual)
    {
        object?[] args = { expected, actual, 0, 0, 0 };
        var matches = (bool)ServiceType.GetMethod("ControlBytesMatch", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, args)!;
        return (matches, (int)args[2]!, (int)args[3]!, (int)args[4]!);
    }

    private static byte[] CloneProfile(string name)
        => ((byte[])ServiceType.GetField(name, BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!).ToArray();

    private static int[] CloneIndexes(string name)
        => ((int[])ServiceType.GetField(name, BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!).ToArray();

    private static int[] ControlIndexes()
        => CloneIndexes("InputByteIndexes").Concat(CloneIndexes("GainByteIndexes")).ToArray();

    private static Type ServiceType
        => SussudioAssembly.Load().GetType("Sussudio.Services.Audio.NativeXuAudioControlService", throwOnError: true)!;
}
