using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

// COM-free balance tests for the ownership transfer used by both FFmpeg callers.
public sealed class FfmpegD3D11OwnershipTransferTests
{
    private static readonly IntPtr Destination = new(0x10);
    private static readonly IntPtr Device = new(0x20);
    private static readonly IntPtr DeviceContext = new(0x30);

    [Fact]
    public void SuccessfulTransfer_LeavesBothReferencesForFfmpegFinalRelease()
    {
        var balances = new Dictionary<IntPtr, int>();
        var releases = new Dictionary<IntPtr, int>();
        var committed = false;

        Action<IntPtr> addRef = pointer => Adjust(balances, pointer, 1);
        Action<IntPtr> release = pointer =>
        {
            Adjust(balances, pointer, -1);
            Adjust(releases, pointer, 1);
        };
        Action<IntPtr, IntPtr, IntPtr> commit = (destination, device, context) =>
        {
            Assert.Equal(Destination, destination);
            Assert.Equal(Device, device);
            Assert.Equal(DeviceContext, context);
            committed = true;
        };

        InvokeTransferCore(addRef, release, commit);

        Assert.True(committed);
        Assert.Equal(1, balances[Device]);
        Assert.Equal(1, balances[DeviceContext]);
        Assert.Empty(releases);

        // Model the backend uninit reached by the final owning av_buffer_unref.
        release(DeviceContext);
        release(Device);

        Assert.Equal(0, balances[Device]);
        Assert.Equal(0, balances[DeviceContext]);
        Assert.Equal(1, releases[Device]);
        Assert.Equal(1, releases[DeviceContext]);
    }

    [Fact]
    public void SecondAddRefFailure_RollsBackTheDeviceExactlyOnce()
    {
        var balances = new Dictionary<IntPtr, int>();
        var releases = new Dictionary<IntPtr, int>();
        var committed = false;

        Action<IntPtr> addRef = pointer =>
        {
            if (pointer == DeviceContext)
            {
                throw new InvalidOperationException("context AddRef failed");
            }

            Adjust(balances, pointer, 1);
        };
        Action<IntPtr> release = pointer =>
        {
            Adjust(balances, pointer, -1);
            Adjust(releases, pointer, 1);
        };

        var failure = Assert.Throws<TargetInvocationException>(() =>
            InvokeTransferCore(addRef, release, (_, _, _) => committed = true));

        Assert.IsType<InvalidOperationException>(failure.InnerException);
        Assert.False(committed);
        Assert.Equal(0, balances[Device]);
        Assert.False(balances.ContainsKey(DeviceContext));
        Assert.Equal(1, releases[Device]);
        Assert.False(releases.ContainsKey(DeviceContext));
    }

    [Fact]
    public void CommitFailure_RollsBackBothReferencesExactlyOnce()
    {
        var balances = new Dictionary<IntPtr, int>();
        var releases = new Dictionary<IntPtr, int>();

        Action<IntPtr> addRef = pointer => Adjust(balances, pointer, 1);
        Action<IntPtr> release = pointer =>
        {
            Adjust(balances, pointer, -1);
            Adjust(releases, pointer, 1);
        };

        var failure = Assert.Throws<TargetInvocationException>(() =>
            InvokeTransferCore(
                addRef,
                release,
                (_, _, _) => throw new InvalidOperationException("commit failed")));

        Assert.IsType<InvalidOperationException>(failure.InnerException);
        Assert.Equal(0, balances[Device]);
        Assert.Equal(0, balances[DeviceContext]);
        Assert.Equal(1, releases[Device]);
        Assert.Equal(1, releases[DeviceContext]);
    }

    private static void InvokeTransferCore(
        Action<IntPtr> addRef,
        Action<IntPtr> release,
        Action<IntPtr, IntPtr, IntPtr> commit)
    {
        var ownershipType = SussudioAssembly.Load().GetType(
            "Sussudio.Services.Gpu.FfmpegD3D11Ownership",
            throwOnError: true)!;
        var transferCore = ownershipType.GetMethod(
            "TransferBorrowedReferencesCore",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FfmpegD3D11Ownership.TransferBorrowedReferencesCore was not found.");

        transferCore.Invoke(
            obj: null,
            parameters: new object[] { Destination, Device, DeviceContext, addRef, release, commit });
    }

    private static void Adjust(Dictionary<IntPtr, int> values, IntPtr pointer, int delta)
    {
        values.TryGetValue(pointer, out var current);
        values[pointer] = current + delta;
    }
}
