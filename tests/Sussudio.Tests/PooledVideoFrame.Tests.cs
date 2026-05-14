using System.Buffers;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

// Shared reflection and frame factory helpers for pooled-frame tests.
static partial class Program
{
    private static object CreatePooledVideoFrame(
        Type frameType,
        object pixelFormat,
        long sequenceNumber,
        long arrivalTick,
        long decodedTick,
        int width,
        int height,
        int length,
        ArrayPool<byte> pool)
    {
        var constructor = frameType.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: new[]
            {
                typeof(long),
                typeof(long),
                typeof(long),
                typeof(int),
                typeof(int),
                pixelFormat.GetType(),
                typeof(int),
                typeof(ArrayPool<byte>)
            },
            modifiers: null)
            ?? throw new InvalidOperationException("PooledVideoFrame private constructor not found.");

        return constructor.Invoke(new object[] { sequenceNumber, arrivalTick, decodedTick, width, height, pixelFormat, length, pool })
            ?? throw new InvalidOperationException("PooledVideoFrame constructor returned null.");
    }

    private static object CreateUnstartedJitterBuffer(Type jitterType, int targetDepth)
    {
        var jitter = RuntimeHelpers.GetUninitializedObject(jitterType);
        var bufferedFrameType = RequireNestedType(jitterType, "BufferedFrame");
        var listType = typeof(List<>).MakeGenericType(bufferedFrameType);

        SetPrivateField(jitter, "_sync", new object());
        SetPrivateField(jitter, "_frames", Activator.CreateInstance(listType));
        SetPrivateField(jitter, "_signal", new AutoResetEvent(false));
        SetPrivateField(jitter, "_frameIntervalTicks", Math.Max(1L, Stopwatch.Frequency / 120L));
        SetPrivateField(jitter, "_minAdaptiveTargetDepth", 2);
        SetPrivateField(jitter, "_maxAdaptiveTargetDepth", 8);
        SetPrivateField(jitter, "_targetDepth", targetDepth);
        SetPrivateField(jitter, "_maxDepth", 12);
        SetPrivateField(jitter, "_nextPreviewSequence", -1L);
        SetPrivateField(jitter, "_lastAdaptiveIssueTick", Stopwatch.GetTimestamp());
        SetPrivateField(jitter, "_lastTargetDecreaseTick", Stopwatch.GetTimestamp());
        return jitter;
    }

    private static Type RequireNestedType(Type declaringType, string nestedTypeName)
        => declaringType.GetNestedType(nestedTypeName, BindingFlags.NonPublic)
           ?? throw new InvalidOperationException($"Nested type '{nestedTypeName}' not found on '{declaringType.Name}'.");

    private static object CreateRawBufferedFrame(Type bufferedFrameType, long enqueueTick)
    {
        var constructor = bufferedFrameType.GetConstructor(
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(byte[]), typeof(int), typeof(int), typeof(int), typeof(long), typeof(long) },
            modifiers: null)
            ?? throw new InvalidOperationException("Raw BufferedFrame constructor not found.");

        return constructor.Invoke(new object[] { ArrayPool<byte>.Shared.Rent(384), 384, 16, 16, 10L, enqueueTick })
            ?? throw new InvalidOperationException("Raw BufferedFrame constructor returned null.");
    }

    private static object CreateLeaseBufferedFrame(Type bufferedFrameType, object lease, long enqueueTick)
    {
        var constructor = bufferedFrameType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Single(ctor =>
            {
                var parameters = ctor.GetParameters();
                return parameters.Length == 2 &&
                       parameters[0].ParameterType.Name == "PooledVideoFrameLease" &&
                       parameters[1].ParameterType == typeof(long);
            });

        return constructor.Invoke(new[] { lease, enqueueTick })
            ?? throw new InvalidOperationException("Lease BufferedFrame constructor returned null.");
    }

    private static object? CreatePendingFrameArgument(ParameterInfo parameter, Type leaseType, object lease)
    {
        if (parameter.ParameterType == leaseType)
        {
            return lease;
        }

        if (!parameter.ParameterType.IsValueType)
        {
            return null;
        }

        if (parameter.ParameterType == typeof(int))
        {
            return parameter.Name is "width" or "height" ? 16 : 0;
        }

        if (parameter.ParameterType == typeof(long))
        {
            return 1L;
        }

        if (parameter.ParameterType == typeof(bool))
        {
            return false;
        }

        if (parameter.ParameterType == typeof(IntPtr))
        {
            return IntPtr.Zero;
        }

        return Activator.CreateInstance(parameter.ParameterType);
    }

    private static long GetLongPrivateField(object instance, string fieldName)
        => Convert.ToInt64(GetPrivateField(instance, fieldName));

    private static int GetIntPrivateField(object instance, string fieldName)
        => Convert.ToInt32(GetPrivateField(instance, fieldName));

    private static string GetStringPrivateField(object instance, string fieldName)
        => Convert.ToString(GetPrivateField(instance, fieldName), CultureInfo.InvariantCulture) ?? string.Empty;

    private static void AssertAddLeaseThrows(MethodInfo addLeaseMethod, object frame)
    {
        try
        {
            addLeaseMethod.Invoke(frame, Array.Empty<object>());
        }
        catch (TargetInvocationException ex) when (ex.InnerException is ObjectDisposedException)
        {
            return;
        }

        throw new InvalidOperationException("AddLease should throw ObjectDisposedException.");
    }

    private static void AssertPropertyThrowsObjectDisposed(object instance, string propertyName)
    {
        try
        {
            _ = GetPropertyValue(instance, propertyName);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is ObjectDisposedException)
        {
            return;
        }

        throw new InvalidOperationException($"{instance.GetType().Name}.{propertyName} should throw ObjectDisposedException.");
    }

    private sealed class TrackingArrayPool : ArrayPool<byte>
    {
        private byte[]? _rented;

        public int RentCount { get; private set; }
        public int ReturnCount { get; private set; }

        public override byte[] Rent(int minimumLength)
        {
            RentCount++;
            _rented = new byte[Math.Max(1, minimumLength)];
            return _rented;
        }

        public override void Return(byte[] array, bool clearArray = false)
        {
            if (!ReferenceEquals(array, _rented))
            {
                throw new InvalidOperationException("Unexpected array returned to pool.");
            }

            ReturnCount++;
        }
    }
}
