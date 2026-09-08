using System.Collections;
using System.Globalization;
using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

// Executes pure helpers from the built probe. These checks do not run native
// transports or establish execution of the finally-based restoration sequences.
public sealed class NativeXuProbePayloadContractsTests
{
    private const int ReadbackCommand = 0x43;
    private const BindingFlags StaticMethods = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    [Fact]
    public void I2cFrameBuildsKnownRequestBytes()
    {
        var payload = Convert.FromHexString("004A02000400");
        var originalPayload = payload.ToArray();

        var frame = BuildFrame(0x1C, payload);

        Assert.Equal(Convert.FromHexString("A10A00001C000000004A02000400E9"), frame);
        Assert.Equal(originalPayload, payload);
    }

    [Fact]
    public void I2cFramePreservesCommandByteOrderAndPayload()
    {
        var payload = Convert.FromHexString("00A1FF");
        var originalPayload = payload.ToArray();

        var frame = BuildFrame(0x12345678, payload);

        Assert.Equal(Convert.FromHexString("A10700007856341200A1FFA4"), frame);
        Assert.Equal(payload.Length + 9, frame.Length);
        Assert.Equal(0, frame.Sum(value => (int)value) & 0xFF);
        Assert.Equal(originalPayload, payload);
    }

    [Fact]
    public void I2cEnvelopeExtractsDeclaredPayloadAndOwnsCopy()
    {
        var response = Convert.FromHexString("A10700007856341200A1FFA45566");
        var originalResponse = response.ToArray();

        Assert.True(TryUnwrap(response, out var payload));

        Assert.Equal(Convert.FromHexString("00A1FF"), payload);
        Assert.Equal(originalResponse, response);
        Assert.NotSame(response, payload);
        response[8] = 0x42;
        Assert.Equal((byte)0x00, payload[0]);
        payload[1] = 0x17;
        Assert.Equal((byte)0xA1, response[9]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("A1")]
    [InlineData("A105")]
    [InlineData("A10500001C000000")]
    [InlineData("A10400001C0000003F")]
    [InlineData("A10300001C00000040")]
    [InlineData("A10600001C00000052")]
    [InlineData("A20500001C000000003D")]
    public void I2cEnvelopeRejectsInvalidPayloads(string? responseHex)
    {
        Assert.False(TryUnwrap(ParseOptionalBytes(responseHex), out var payload));
        Assert.Empty(payload);
    }

    [Theory]
    [InlineData("00", 0x00)]
    [InlineData("A1", 0xA1)]
    [InlineData("FF", 0xFF)]
    [InlineData("001122", 0x00)]
    [InlineData("42A10500001C000000", 0x42)]
    public void I2cValuePreservesRawReplies(string responseHex, int expected)
    {
        Assert.True(TryExtract(Convert.FromHexString(responseHex), out var value));
        Assert.Equal((byte)expected, value);
    }

    [Theory]
    [InlineData(0x00)]
    [InlineData(0xA1)]
    [InlineData(0xFF)]
    public void I2cValueAcceptsFramedAndAlreadyUnwrappedOriginalBytes(int expected)
    {
        var frame = BuildFrame(0x1C, new[] { (byte)expected });

        Assert.True(TryExtract(frame, out var framedValue));
        Assert.Equal((byte)expected, framedValue);
        Assert.True(TryUnwrap(frame, out var payload));
        Assert.True(TryExtract(payload, out var rawValue));
        Assert.Equal((byte)expected, rawValue);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("A105")]
    [InlineData("A1050000")]
    [InlineData("A10500001C000000")]
    [InlineData("A10400001C0000003F")]
    [InlineData("A10300001C00000040")]
    [InlineData("A10600001C00000052")]
    public void I2cValueRejectsUnreadableAndMalformedMarkedReplies(string? responseHex)
    {
        Assert.False(TryExtract(ParseOptionalBytes(responseHex), out var value));
        Assert.Equal((byte)0, value);
    }

    [Theory]
    [InlineData(1, 0x1234L, "34")]
    [InlineData(2, 0x1234L, "3412")]
    [InlineData(4, 0x12345678L, "78563412")]
    [InlineData(4, 0x112345678L, "78563412")]
    [InlineData(1, -1L, "FF")]
    [InlineData(2, -1L, "FFFF")]
    [InlineData(4, -1L, "FFFFFFFF")]
    public void ExperimentPayloadWidthsPreserveEncodedValues(int width, long value, string expectedHex)
    {
        Assert.Equal(Convert.FromHexString(expectedHex), BuildExperimentPayload(width, value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(8)]
    public void ExperimentPayloadRejectsUnsupportedWidths(int width)
    {
        var error = Assert.Throws<TargetInvocationException>(() => BuildExperimentPayload(width, 1));
        var inner = Assert.IsType<InvalidOperationException>(error.InnerException);
        Assert.Equal($"Unsupported payload width {width}.", inner.Message);
    }

    [Theory]
    [InlineData("Byte", "00", 0L)]
    [InlineData("Byte", "FF", 255L)]
    [InlineData("Byte", "A1FF", 161L)]
    [InlineData("Int16", "FF7F", 32767L)]
    [InlineData("Int16", "0080", -32768L)]
    [InlineData("Int16", "FEFFCAFE", -2L)]
    [InlineData("Int32", "FFFFFF7F", 2147483647L)]
    [InlineData("Int32", "00000080", -2147483648L)]
    [InlineData("Int32", "FFFFFFFF", -1L)]
    [InlineData("Int32", "78563412AABB", 305419896L)]
    public void ExperimentDecodePreservesTypedValuesAndRawEvidence(string kind, string payloadHex, long expected)
    {
        var payload = Convert.FromHexString(payloadHex);
        var result = Decode(kind, payload);
        var typedValue = Property(result, "TypedValue");
        var expectedType = kind switch
        {
            "Byte" => typeof(byte),
            "Int16" => typeof(short),
            "Int32" => typeof(int),
            _ => throw new InvalidOperationException($"Unexpected fixture kind {kind}.")
        };

        Assert.NotNull(typedValue);
        Assert.Equal(expectedType, typedValue.GetType());
        Assert.Equal(expected, Convert.ToInt64(typedValue, CultureInfo.InvariantCulture));
        Assert.Equal(expected.ToString(CultureInfo.InvariantCulture), Property(result, "DisplayValue"));
        Assert.Equal("Baseline", Property(result, "Label"));
        Assert.Same(payload, Property(result, "Payload"));
    }

    [Fact]
    public void ExperimentDecodeUsesInvariantNumericDisplay()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NegativeSign = "~";
        try
        {
            CultureInfo.CurrentCulture = culture;
            var result = Decode("Int16", Convert.FromHexString("FFFF"));
            Assert.Equal("-1", Property(result, "DisplayValue"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Theory]
    [InlineData("Byte", null, "unavailable")]
    [InlineData("Byte", "", "unavailable")]
    [InlineData("Int16", "01", "01")]
    [InlineData("Int32", "01", "01")]
    [InlineData("Int32", "3412", "34-12")]
    [InlineData("Int32", "123456", "12-34-56")]
    public void ExperimentDecodeRejectsUnavailableAndShortPayloads(string kind, string? payloadHex, string expectedDisplay)
    {
        var payload = ParseOptionalBytes(payloadHex);
        var result = Decode(kind, payload);

        Assert.Null(Property(result, "TypedValue"));
        Assert.Equal(expectedDisplay, Property(result, "DisplayValue"));
        Assert.Same(payload, Property(result, "Payload"));
    }

    [Theory]
    [InlineData("Byte", "00", 0L, 4)]
    [InlineData("Byte", "A1", 161L, 4)]
    [InlineData("Int16", "FEFFCAFE", -2L, 1)]
    [InlineData("Int32", "000000800102", -2147483648L, 2)]
    public void RestoreTargetRetainsExactOriginalPayloadAndOwnsCopy(string kind, string payloadHex, long expected, int setterWidth)
    {
        var payload = Convert.FromHexString(payloadHex);
        var originalPayload = payload.ToArray();
        var before = Readbacks(Decode(kind, payload));

        Assert.True(TryBuildRestoreTarget(before, setterWidth, out var target));

        Assert.Equal(expected, Assert.IsType<long>(Property(target, "Value")));
        var restorePayload = Assert.IsType<byte[]>(Property(target, "Payload"));
        Assert.Equal(originalPayload, restorePayload);
        Assert.NotSame(payload, restorePayload);
        payload[0] ^= 0xFF;
        Assert.Equal(originalPayload, restorePayload);
    }

    [Fact]
    public void RestoreTargetRejectsMissingReadback()
    {
        var before = Readbacks();
        before.Add(ReadbackCommand + 1, Decode("Byte", new byte[] { 0 }));

        AssertRestoreTargetRejected(before);
    }

    [Theory]
    [InlineData("Byte", null)]
    [InlineData("Byte", "")]
    [InlineData("Int16", "01")]
    [InlineData("Int32", "000102")]
    public void RestoreTargetRejectsUndecodableOriginal(string kind, string? payloadHex)
    {
        AssertRestoreTargetRejected(Readbacks(Decode(kind, ParseOptionalBytes(payloadHex))));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void RestoreTargetRejectsTypedValuesWithoutRawEvidence(string? payloadHex)
    {
        var baseline = Activator.CreateInstance(
            ProbeType("AtReadResult"),
            "Baseline", ParseOptionalBytes(payloadHex), "7", (byte)7)!;

        AssertRestoreTargetRejected(Readbacks(baseline));
    }

    private static byte[]? ParseOptionalBytes(string? hex)
        => hex == null ? null : Convert.FromHexString(hex);

    private static Type ProbeType(string name)
        => ToolFormatterTestAssembly.Load(global::Program.NativeXuAudioProbeAssemblyRelativePath)
            .GetType(name, throwOnError: true)!;

    private static MethodInfo Method(string typeName, string name)
        => ProbeType(typeName).GetMethod(name, StaticMethods)
            ?? throw new InvalidOperationException($"Probe method {typeName}.{name} was not found.");

    private static object? Property(object target, string name)
        => (target.GetType().GetProperty(name)
            ?? throw new InvalidOperationException($"Probe property {target.GetType().Name}.{name} was not found."))
            .GetValue(target);

    private static byte[] BuildFrame(int command, byte[] payload)
        => Assert.IsType<byte[]>(Method("NativeXuProbeI2cTransport", "BuildAtFrameWithPayload")
            .Invoke(null, new object?[] { command, payload }));

    private static bool TryUnwrap(byte[]? response, out byte[] payload)
    {
        var arguments = new object?[] { response, new byte[] { 0x7E } };
        var success = Assert.IsType<bool>(Method("NativeXuProbeI2cTransport", "TryUnwrapAtEnvelopePayload")
            .Invoke(null, arguments));
        payload = Assert.IsType<byte[]>(arguments[1]);
        return success;
    }

    private static bool TryExtract(byte[]? response, out byte value)
    {
        var arguments = new object?[] { response, (byte)0x7E };
        var success = Assert.IsType<bool>(Method("NativeXuProbeI2cCommands", "TryExtractI2cValue")
            .Invoke(null, arguments));
        value = Assert.IsType<byte>(arguments[1]);
        return success;
    }

    private static byte[] BuildExperimentPayload(int width, long value)
        => Assert.IsType<byte[]>(Method("NativeXuProbeDefaultExperiment", "BuildPayload")
            .Invoke(null, new object?[] { width, value }));

    private static object Decode(string kind, byte[]? payload)
    {
        var getter = Activator.CreateInstance(
            ProbeType("GetterSpec"),
            "Baseline", ReadbackCommand, Enum.Parse(ProbeType("ValueKind"), kind))!;
        return Method("NativeXuProbeDefaultExperiment", "Decode")
            .Invoke(null, new object?[] { getter, payload })!;
    }

    private static IDictionary Readbacks(object? baseline = null)
    {
        var dictionaryType = typeof(Dictionary<,>).MakeGenericType(typeof(int), ProbeType("AtReadResult"));
        var before = (IDictionary)Activator.CreateInstance(dictionaryType)!;
        if (baseline != null)
        {
            before.Add(ReadbackCommand, baseline);
        }
        return before;
    }

    private static bool TryBuildRestoreTarget(IDictionary before, int setterWidth, out object target)
    {
        var setter = Activator.CreateInstance(
            ProbeType("SetterSpec"), "Setter", 0x44, ReadbackCommand, setterWidth)!;
        var arguments = new object?[] { before, setter, null };
        var success = Assert.IsType<bool>(Method("NativeXuProbeDefaultExperiment", "TryBuildRestoreTarget")
            .Invoke(null, arguments));
        target = arguments[2] ?? throw new InvalidOperationException("The restore target out value was not assigned.");
        return success;
    }

    private static void AssertRestoreTargetRejected(IDictionary before)
    {
        Assert.False(TryBuildRestoreTarget(before, 1, out var target));
        Assert.Equal(0L, Assert.IsType<long>(Property(target, "Value")));
        Assert.Null(Property(target, "Payload"));
    }
}