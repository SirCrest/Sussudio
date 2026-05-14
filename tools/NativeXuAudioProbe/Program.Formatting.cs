static class NativeXuProbeFormatting
{
    public static string FormatRaw(byte[]? payload) => payload == null ? "null" : BitConverter.ToString(payload);
}
