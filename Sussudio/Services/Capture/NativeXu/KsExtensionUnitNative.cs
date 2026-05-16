using System;

namespace Sussudio.Services.Capture;

// Kernel Streaming extension-unit helper. It enumerates capture/video device
// interfaces and issues KSPROPERTY topology GET/SET calls used by the native XU
// telemetry and audio-control services.
internal static partial class KsExtensionUnitNative
{
    private static readonly Guid KsCategoryCapture = new("65E8773D-8F56-11D0-A3B9-00A0C9223196");
    private static readonly Guid KsCategoryVideo = new("6994AD05-93EF-11D0-A3CC-00A0C9223196");
    private static readonly Guid KsPropSetTopology = new("720D4AC0-7533-11D0-A5D6-28DB04C10000");
    private static readonly Guid KsNodeTypeDevSpecific = new("941C7AC0-C559-11D0-8A2B-00A0C9255AC1");

    private const int MaxTopologyBuffer = 64 * 1024;
    private const uint IoctlKsProperty = 0x002F0003;
    private const uint DigcfPresent = 0x00000002;
    private const uint DigcfDeviceInterface = 0x00000010;
    private const uint GenericRead = 0x80000000;
    private const uint GenericWrite = 0x40000000;
    private const uint FileShareRead = 0x00000001;
    private const uint FileShareWrite = 0x00000002;
    private const uint OpenExisting = 3;
    private const uint KsPropertyTypeGet = 0x00000001;
    private const uint KsPropertyTypeSet = 0x00000002;
    private const uint KsPropertyTypeTopology = 0x10000000;
    private const uint KsPropertyTopologyNodes = 1;
    private const int ErrorInsufficientBuffer = 122;
    private const int ErrorMoreData = 234;
    internal const int ErrorNotFound = 1168;
    internal const int ErrorSetNotFound = 1170;
    internal const int ErrorInvalidParameter = 87;
    internal const int ErrorInvalidFunction = 1;
    private const int ErrorNoMoreItems = 259;

    internal readonly record struct KsInterfacePath(string Path, Guid CategoryGuid);

    internal readonly record struct KsTopologyNode(int NodeId, bool IsDevSpecific, Guid NodeType);
}
