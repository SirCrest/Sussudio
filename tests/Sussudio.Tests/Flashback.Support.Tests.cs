using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task FlashbackSuppressedExceptionsUseAppLogs()
    {
        var decoderText = ReadFlashbackDecoderSource();
        var d3d11Text = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.D3D11.cs").Replace("\r\n", "\n");
        var d3d11DiscoveryText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.D3D11Discovery.cs").Replace("\r\n", "\n");

        var openFileBlock = ExtractTextBetween(
            decoderText,
            "public void OpenFile(string filePath)",
            "    /// <summary>\n    /// Closes the currently open file");
        AssertContains(openFileBlock, "FLASHBACK_DECODER_OPEN_WARN");
        AssertContains(openFileBlock, "CloseFileCore();\n            throw;");
        AssertContains(decoderText, "var closedPath = _currentFilePath;\n        CloseFileCore();\n        Logger.Log($\"FLASHBACK_DECODER_CLOSE path='{closedPath}'\");");
        AssertContains(decoderText, "_currentPosition = TimeSpan.Zero;\n        _currentFilePath = null;\n        _needsConvert = false;");
        AssertDoesNotContain(openFileBlock, "System.Diagnostics.Trace.TraceWarning");
        AssertContains(decoderText, "FLASHBACK_DECODER_INIT d3d11va=false reason=exception type={ex.GetType().Name} msg='{ex.Message}'");
        AssertContains(d3d11Text, "var codec = FindD3D11VADecoder(codecPar->codec_id, out var codecName);");
        AssertContains(d3d11Text, "FLASHBACK_DECODER_D3D11VA_SKIP reason=no_d3d11_device_ctx_decoder");
        AssertContains(d3d11Text, "FLASHBACK_DECODER_D3D11VA_SKIP reason=exception type={ex.GetType().Name} msg='{ex.Message}'");
        AssertDoesNotContain(d3d11Text, "private static string DescribeHardwareConfigs");
        AssertContains(d3d11DiscoveryText, "private static AVCodec* FindD3D11VADecoder(AVCodecID codecId, out string codecName)");
        AssertContains(d3d11DiscoveryText, "ffmpeg.avcodec_find_decoder_by_name(preferredName)");
        AssertContains(d3d11DiscoveryText, "AVCodecID.AV_CODEC_ID_AV1 => \"av1\"");
        AssertContains(d3d11DiscoveryText, "FLASHBACK_DECODER_D3D11VA_SELECT source=preferred codec={codecName}");
        AssertContains(d3d11DiscoveryText, "FLASHBACK_DECODER_D3D11VA_CANDIDATE source={source} codec={codecName} configs=[{hardwareConfigSummary}] d3d11_device_ctx={hasD3D11DeviceConfig}");
        AssertContains(d3d11DiscoveryText, "private static string DescribeHardwareConfigs(AVCodec* codec, out bool hasD3D11DeviceConfig)");
        AssertContains(d3d11DiscoveryText, "ffmpeg.avcodec_get_hw_config(codec, i)");
        AssertContains(d3d11DiscoveryText, "pixelFormat == AVPixelFormat.AV_PIX_FMT_D3D11");
        AssertContains(d3d11DiscoveryText, "deviceType == AVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA");
        AssertContains(d3d11DiscoveryText, "AvCodecHwConfigMethodHwDeviceCtx");
        AssertContains(d3d11DiscoveryText, "private static string FormatHardwareConfigMethods(int methods)");
        AssertContains(d3d11DiscoveryText, "private static string GetPixelFormatName(AVPixelFormat pixelFormat)");
        AssertContains(d3d11DiscoveryText, "private static string GetHardwareDeviceName(AVHWDeviceType deviceType)");

        return Task.CompletedTask;
    }

}
