using System.Collections;
using System.Reflection;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace Sussudio.Tests;

// Executes the bridge linked into the built NativeXuAudioProbe with per-call
// fake KS IO. Only TryOpen uses native IO, against privately owned ordinary files.
public sealed class KsExtensionUnitNativeTests
{
    private const BindingFlags StaticMethods = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly Guid PropertySet = new("961073C7-49F7-44F2-AB42-E940405940C2");
    private static readonly Guid DevSpecific = new("941C7AC0-C559-11D0-8A2B-00A0C9255AC1");
    private static Type Bridge => ToolFormatterTestAssembly.Load(global::Program.NativeXuAudioProbeAssemblyRelativePath)
        .GetType("Sussudio.Services.NativeXu.KsExtensionUnitNative", throwOnError: true)!;

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void TopologyUsesExactPropertyAndBoundedBufferGrowth(int retryCount)
    {
        using var handle = FakeHandle();
        var nodeTypes = new[] { Guid.Empty, DevSpecific, PropertySet };
        var io = new FakeIo((actualHandle, input, output, call) =>
        {
            Assert.Same(handle, actualHandle);
            Assert.Equal(Convert.FromHexString("C04A0D723375D011A5D628DB04C100000100000001000000"), input);
            Assert.Equal(4096 << (call - 1), output.Length);
            Assert.All(output, value => Assert.Equal((byte)0, value));
            if (call <= retryCount)
                return new IoResult(false, 0, call % 2 == 0 ? 234 : 122);
            WriteTopology(output, nodeTypes);
            return new IoResult(true, 8 + nodeTypes.Length * 16, 0);
        });

        var result = ReadTopology(handle, io);

        Assert.True(result.Succeeded);
        Assert.Null(result.Error);
        Assert.Equal(retryCount + 1, io.Calls);
        Assert.Collection(result.Nodes!,
            node => AssertNode(node, 0, false, Guid.Empty),
            node => AssertNode(node, 1, true, DevSpecific),
            node => AssertNode(node, 2, false, PropertySet));
    }

    [Theory]
    [InlineData(122)]
    [InlineData(234)]
    public void TopologyExhaustedRetryRemainsFailureEvenWithParseableReply(int errorCode)
    {
        using var handle = FakeHandle();
        var io = new FakeIo((_, _, output, call) =>
        {
            Assert.Equal(4096 << (call - 1), output.Length);
            WriteTopology(output, new[] { DevSpecific });
            return new IoResult(false, 24, errorCode);
        });

        var result = ReadTopology(handle, io);

        Assert.False(result.Succeeded);
        Assert.Null(result.Nodes);
        Assert.StartsWith($"topology-query-failed win32={errorCode} (", result.Error);
        Assert.Equal(5, io.Calls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void TopologyStopsOnNonRetryableError(int retriesBeforeFailure)
    {
        using var handle = FakeHandle();
        var io = new FakeIo((_, _, _, call) => new IoResult(false, 0, call <= retriesBeforeFailure ? 122 : 5));

        var result = ReadTopology(handle, io);

        Assert.False(result.Succeeded);
        Assert.Null(result.Nodes);
        Assert.StartsWith("topology-query-failed win32=5 (", result.Error);
        Assert.Equal(retriesBeforeFailure + 1, io.Calls);
    }

    [Theory]
    [InlineData(1L, -1)]
    [InlineData(1L, 0)]
    [InlineData(1L, 7)]
    [InlineData(0L, 8)]
    [InlineData(1L, 8)]
    [InlineData(2L, 24)]
    [InlineData(256L, 4096)]
    [InlineData(256L, 4097)]
    [InlineData(256L, 4104)]
    [InlineData(1L, int.MaxValue)]
    [InlineData(268435456L, 24)]
    [InlineData(2147483648L, 24)]
    [InlineData(4294967295L, 24)]
    public void TopologyTreatsMalformedSuccessfulReplyAsEmpty(long advertisedCount, int returnedLength)
    {
        using var handle = FakeHandle();
        var io = new FakeIo((_, _, output, _) =>
        {
            WriteTopology(output, new[] { DevSpecific });
            BitConverter.TryWriteBytes(output.AsSpan(4, 4), (uint)advertisedCount);
            return new IoResult(true, returnedLength, 0);
        });

        var result = ReadTopology(handle, io);

        Assert.True(result.Succeeded);
        Assert.Null(result.Error);
        Assert.Empty(result.Nodes!);
        Assert.Equal(1, io.Calls);
    }

    [Fact]
    public void TopologyRejectsReportedLengthOutsideItsOwnedBuffer()
    {
        using var handle = FakeHandle();
        var io = new FakeIo((_, _, output, _) =>
        {
            WriteTopology(output, new[] { DevSpecific });
            return new IoResult(true, output.Length + 1, 0);
        });

        var result = ReadTopology(handle, io);

        Assert.True(result.Succeeded);
        Assert.Null(result.Error);
        Assert.Empty(result.Nodes!);
    }

    [Fact]
    public void TopologyAcceptsMaximumWholeNodeCountAndIgnoresTrailingBytes()
    {
        using var handle = FakeHandle();
        const int nodeCount = (65536 - 8) / 16;
        var io = new FakeIo((_, _, output, _) =>
        {
            if (output.Length < 65536)
                return new IoResult(false, 0, 234);
            BitConverter.TryWriteBytes(output.AsSpan(0, 4), (uint)output.Length);
            BitConverter.TryWriteBytes(output.AsSpan(4, 4), (uint)nodeCount);
            DevSpecific.TryWriteBytes(output.AsSpan(8 + (nodeCount - 1) * 16, 16));
            return new IoResult(true, output.Length, 0);
        });

        var result = ReadTopology(handle, io);

        Assert.True(result.Succeeded);
        Assert.Equal(nodeCount, result.Nodes!.Length);
        AssertNode(result.Nodes[0], 0, false, Guid.Empty);
        AssertNode(result.Nodes[^1], nodeCount - 1, true, DevSpecific);
        Assert.Equal(5, io.Calls);
    }

    [Theory]
    [InlineData(-10, "")]
    [InlineData(0, "")]
    [InlineData(1, "11")]
    [InlineData(4, "11223344")]
    [InlineData(6, "11223344")]
    [InlineData(int.MaxValue, "11223344")]
    public void GetPreservesFramingAndReportedLengthButOwnsBoundedData(int returnedLength, string expectedHex)
    {
        using var handle = FakeHandle();
        byte[]? nativeOutput = null;
        var io = new FakeIo((actualHandle, input, output, _) =>
        {
            Assert.Same(handle, actualHandle);
            Assert.Equal(Convert.FromHexString("C7731096F749F244AB42E940405940C278563412010000104030201000000000"), input);
            Assert.Equal(4, output.Length);
            Convert.FromHexString("11223344").CopyTo(output, 0);
            nativeOutput = output;
            return new IoResult(true, returnedLength, 0);
        });

        var result = Get(handle, io, 4);

        Assert.True(result.Succeeded);
        Assert.Equal(returnedLength, result.BytesReturned);
        Assert.Null(result.ErrorCode);
        Assert.Equal(Convert.FromHexString(expectedHex), result.Data);
        Assert.NotSame(nativeOutput, result.Data);
        nativeOutput![0] = 0xFF;
        Assert.Equal(Convert.FromHexString(expectedHex), result.Data);
        if (result.Data.Length > 0)
        {
            result.Data[0] = 0xAA;
            Assert.Equal((byte)0xFF, nativeOutput[0]);
        }
        Assert.Equal(1, io.Calls);
    }

    [Fact]
    public void GetAllowsEmptyRequestedBuffer()
    {
        using var handle = FakeHandle();
        var io = new FakeIo((_, _, output, _) =>
        {
            Assert.Empty(output);
            return new IoResult(true, 5, 0);
        });

        var result = Get(handle, io, 0);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Data);
        Assert.Equal(5, result.BytesReturned);
        Assert.Null(result.ErrorCode);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(0)]
    public void GetFailurePreservesNativeCodeAndReportedLengthWithoutReturningData(int errorCode)
    {
        using var handle = FakeHandle();
        var io = new FakeIo((_, _, output, _) =>
        {
            Array.Fill(output, (byte)0xFF);
            return new IoResult(false, 42, errorCode);
        });

        var result = Get(handle, io, 4);

        Assert.False(result.Succeeded);
        Assert.Empty(result.Data);
        Assert.Equal(42, result.BytesReturned);
        Assert.Equal(errorCode, result.ErrorCode);
    }

    [Theory]
    [InlineData("TryXuSetViaOutputCore", "")]
    [InlineData("TryXuSetViaOutputCore", "01A5FF")]
    [InlineData("TryXuSetViaInputCore", "")]
    [InlineData("TryXuSetViaInputCore", "01A5FF")]
    public void SetPreservesBothNativePayloadLayouts(string operation, string payloadHex)
    {
        using var handle = FakeHandle();
        var value = Convert.FromHexString(payloadHex);
        var expectedHeader = Convert.FromHexString("C7731096F749F244AB42E940405940C278563412020000104030201000000000");
        var io = new FakeIo((actualHandle, input, output, _) =>
        {
            Assert.Same(handle, actualHandle);
            if (operation == "TryXuSetViaOutputCore")
            {
                Assert.Equal(expectedHeader, input);
                Assert.Same(value, output);
                Assert.Equal(Convert.FromHexString(payloadHex), output);
            }
            else
            {
                Assert.Equal(expectedHeader.Concat(value).ToArray(), input);
                Assert.Empty(output);
                Assert.NotSame(value, input);
                if (value.Length > 0)
                {
                    input[expectedHeader.Length] = 0xEE;
                    Assert.Equal(Convert.FromHexString(payloadHex), value);
                }
            }
            return new IoResult(true, 7, 0);
        });

        var result = Set(operation, handle, io, value);

        Assert.True(result.Succeeded);
        Assert.Null(result.ErrorCode);
        Assert.Equal(Convert.FromHexString(payloadHex), value);
        Assert.Equal(1, io.Calls);
    }

    [Theory]
    [InlineData("TryXuSetViaOutputCore", 5)]
    [InlineData("TryXuSetViaOutputCore", 0)]
    [InlineData("TryXuSetViaInputCore", 5)]
    [InlineData("TryXuSetViaInputCore", 0)]
    public void SetFailurePreservesNativeError(string operation, int errorCode)
    {
        using var handle = FakeHandle();
        var io = new FakeIo((_, _, _, _) => new IoResult(false, 0, errorCode));

        var result = Set(operation, handle, io, new byte[] { 0x10 });

        Assert.False(result.Succeeded);
        Assert.Equal(errorCode, result.ErrorCode);
        Assert.Equal(1, io.Calls);
    }

    [Theory]
    [InlineData(-1, int.MinValue)]
    [InlineData(int.MinValue, -1)]
    public void RequestsPreserveUnsignedNodeAndSelectorBitPatterns(int nodeId, int selector)
    {
        using var handle = FakeHandle();
        var io = new FakeIo((_, input, _, _) =>
        {
            Assert.Equal(unchecked((uint)selector), BitConverter.ToUInt32(input, 16));
            Assert.Equal(unchecked((uint)nodeId), BitConverter.ToUInt32(input, 24));
            Assert.Equal(0U, BitConverter.ToUInt32(input, 28));
            return new IoResult(true, 0, 0);
        });

        Assert.True(Get(handle, io, 0, nodeId, selector).Succeeded);
        Assert.True(Set("TryXuSetViaOutputCore", handle, io, Array.Empty<byte>(), nodeId, selector).Succeeded);
        Assert.True(Set("TryXuSetViaInputCore", handle, io, Array.Empty<byte>(), nodeId, selector).Succeeded);
        Assert.Equal(3, io.Calls);
    }

    [Fact]
    public void OpenReturnsOwnedReadWriteHandleForOrdinaryFile()
    {
        using var file = new PrivateFile();
        var result = Open(file.Path);
        using (result.Handle)
        {
            Assert.NotNull(result.Handle);
            Assert.False(result.Handle.IsInvalid);
            Assert.Null(result.ErrorCode);
            RandomAccess.Write(result.Handle, new byte[] { 4, 5, 6 }, 0);
            var read = new byte[3];
            Assert.Equal(3, RandomAccess.Read(result.Handle, read, 0));
            Assert.Equal(new byte[] { 4, 5, 6 }, read);
        }
        Assert.True(result.Handle.IsClosed);
        using var exclusive = File.Open(file.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        Assert.Equal(3, exclusive.Length);
    }

    [Fact]
    public void OpenFallsBackToReadOnlyWhenSharingRejectsWriteAccess()
    {
        using var file = new PrivateFile();
        using var reader = File.Open(file.Path, FileMode.Open, FileAccess.Read, FileShare.Read);
        Assert.Throws<IOException>(() =>
        {
            using var rejected = File.Open(file.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        });

        var result = Open(file.Path);
        using (result.Handle)
        {
            Assert.NotNull(result.Handle);
            Assert.False(result.Handle.IsInvalid);
            Assert.Null(result.ErrorCode);
            var read = new byte[3];
            Assert.Equal(3, RandomAccess.Read(result.Handle, read, 0));
            Assert.Equal(new byte[] { 1, 2, 3 }, read);
            Assert.Throws<UnauthorizedAccessException>(() => RandomAccess.Write(result.Handle, new byte[] { 9 }, 0));
        }
        Assert.True(result.Handle.IsClosed);
    }

    [Fact]
    public void OpenReportsMissingOrdinaryFileWithoutCreatingIt()
    {
        using var file = new PrivateFile();
        var missing = System.IO.Path.Combine(file.DirectoryPath, "missing.bin");

        var result = Open(missing);

        Assert.Null(result.Handle);
        Assert.Equal(2, result.ErrorCode);
        Assert.False(File.Exists(missing));
    }

    [Fact]
    public void OpenReportsBothAccessAttemptsRejectedAndRecoversAfterOwnerReleasesFile()
    {
        using var file = new PrivateFile();
        using (var owner = File.Open(file.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            var result = Open(file.Path);
            Assert.Null(result.Handle);
            Assert.Equal(32, result.ErrorCode);
        }

        var recovered = Open(file.Path);
        using (recovered.Handle)
        {
            Assert.NotNull(recovered.Handle);
            Assert.False(recovered.Handle.IsInvalid);
            Assert.Null(recovered.ErrorCode);
        }
    }

    private static SafeFileHandle FakeHandle() => new(new IntPtr(-1), ownsHandle: false);

    private static (bool Succeeded, object[]? Nodes, string? Error) ReadTopology(SafeFileHandle handle, FakeIo io)
    {
        object?[] arguments = { handle, null, null, io.Bind() };
        var succeeded = (bool)Method("TryReadTopologyNodesCore").Invoke(null, arguments)!;
        return (succeeded, (arguments[1] as IEnumerable)?.Cast<object>().ToArray(), (string?)arguments[2]);
    }

    private static (bool Succeeded, byte[] Data, int BytesReturned, int? ErrorCode) Get(
        SafeFileHandle handle, FakeIo io, int bufferSize, int nodeId = 0x10203040, int selector = 0x12345678)
    {
        object?[] arguments = { handle, nodeId, PropertySet, selector, bufferSize, null, 0, null, io.Bind() };
        var succeeded = (bool)Method("TryXuGetDirectCore").Invoke(null, arguments)!;
        return (succeeded, (byte[])arguments[5]!, (int)arguments[6]!, (int?)arguments[7]);
    }

    private static (bool Succeeded, int? ErrorCode) Set(
        string operation, SafeFileHandle handle, FakeIo io, byte[] value, int nodeId = 0x10203040, int selector = 0x12345678)
    {
        object?[] arguments = { handle, nodeId, PropertySet, selector, value, null, io.Bind() };
        var succeeded = (bool)Method(operation).Invoke(null, arguments)!;
        return (succeeded, (int?)arguments[5]);
    }

    private static (SafeFileHandle? Handle, int? ErrorCode) Open(string path)
    {
        object?[] arguments = { path, null };
        var handle = (SafeFileHandle?)Method("TryOpen").Invoke(null, arguments);
        return (handle, (int?)arguments[1]);
    }

    private static MethodInfo Method(string name) => Bridge.GetMethod(name, StaticMethods)!;

    private static void WriteTopology(byte[] output, Guid[] nodeTypes)
    {
        BitConverter.TryWriteBytes(output.AsSpan(0, 4), (uint)(8 + nodeTypes.Length * 16));
        BitConverter.TryWriteBytes(output.AsSpan(4, 4), (uint)nodeTypes.Length);
        for (var index = 0; index < nodeTypes.Length; index++)
            nodeTypes[index].TryWriteBytes(output.AsSpan(8 + index * 16, 16));
    }

    private static void AssertNode(object node, int id, bool isDevSpecific, Guid type)
    {
        Assert.Equal(id, node.GetType().GetProperty("NodeId")!.GetValue(node));
        Assert.Equal(isDevSpecific, node.GetType().GetProperty("IsDevSpecific")!.GetValue(node));
        Assert.Equal(type, node.GetType().GetProperty("NodeType")!.GetValue(node));
    }

    private readonly record struct IoResult(bool Succeeded, int BytesReturned, int ErrorCode);

    private sealed class FakeIo(Func<SafeFileHandle, byte[], byte[], int, IoResult> response)
    {
        public int Calls { get; private set; }

        public Delegate Bind() => Delegate.CreateDelegate(
            Bridge.GetNestedType("KsPropertyIo", BindingFlags.NonPublic)!, this,
            typeof(FakeIo).GetMethod(nameof(Invoke))!);

        public bool Invoke(SafeFileHandle handle, byte[] input, byte[] output, out int bytesReturned, out int errorCode)
        {
            var result = response(handle, input, output, ++Calls);
            bytesReturned = result.BytesReturned;
            errorCode = result.ErrorCode;
            return result.Succeeded;
        }
    }

    private sealed class PrivateFile : IDisposable
    {
        public string DirectoryPath { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Sussudio-KsNativeTests-" + Guid.NewGuid().ToString("N"));
        public string Path => System.IO.Path.Combine(DirectoryPath, "ordinary-file.bin");

        public PrivateFile()
        {
            Directory.CreateDirectory(DirectoryPath);
            File.WriteAllBytes(Path, new byte[] { 1, 2, 3 });
        }

        public void Dispose()
        {
            File.Delete(Path);
            Directory.Delete(DirectoryPath);
        }
    }
}
