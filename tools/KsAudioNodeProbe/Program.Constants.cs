static class KsAudioNodeProbeConstants
{
    public const uint IoctlKsProperty = 0x002F0003;
    public const uint DigcfPresent = 0x00000002;
    public const uint DigcfDeviceInterface = 0x00000010;
    public const uint GenericRead = 0x80000000;
    public const uint GenericWrite = 0x40000000;
    public const uint FileShareRead = 0x00000001;
    public const uint FileShareWrite = 0x00000002;
    public const uint OpenExisting = 3;
    public const uint KsPropertyTypeGet = 0x00000001;
    public const uint KsPropertyTypeSet = 0x00000002;
    public const uint KsPropertyTypeTopology = 0x10000000;
    public const int ErrorNoMoreItems = 259;
    public const int ErrorInsufficientBuffer = 122;
    public const int ErrorMoreData = 234;
}
