using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

static partial class Program
{
    private static Task FlashbackSuppressedExceptionsUseAppLogs()
    {
        var decoderText = ReadFlashbackDecoderSource();
        var sinkText = ReadFlashbackEncoderSinkSource();

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
        AssertContains(decoderText, "var codec = FindD3D11VADecoder(codecPar->codec_id, out var codecName);");
        AssertContains(decoderText, "FLASHBACK_DECODER_D3D11VA_SKIP reason=no_d3d11_device_ctx_decoder");
        AssertContains(decoderText, "private static AVCodec* FindD3D11VADecoder(AVCodecID codecId, out string codecName)");
        AssertContains(decoderText, "ffmpeg.avcodec_find_decoder_by_name(preferredName)");
        AssertContains(decoderText, "AVCodecID.AV_CODEC_ID_AV1 => \"av1\"");
        AssertContains(decoderText, "FLASHBACK_DECODER_D3D11VA_SELECT source=preferred codec={codecName}");
        AssertContains(decoderText, "FLASHBACK_DECODER_D3D11VA_CANDIDATE source={source} codec={codecName} configs=[{hardwareConfigSummary}] d3d11_device_ctx={hasD3D11DeviceConfig}");
        AssertContains(decoderText, "private static string DescribeHardwareConfigs(AVCodec* codec, out bool hasD3D11DeviceConfig)");
        AssertContains(decoderText, "ffmpeg.avcodec_get_hw_config(codec, i)");
        AssertContains(decoderText, "pixelFormat == AVPixelFormat.AV_PIX_FMT_D3D11");
        AssertContains(decoderText, "deviceType == AVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA");
        AssertContains(decoderText, "AvCodecHwConfigMethodHwDeviceCtx");
        AssertContains(decoderText, "private static string FormatHardwareConfigMethods(int methods)");
        AssertContains(decoderText, "private static string GetPixelFormatName(AVPixelFormat pixelFormat)");
        AssertContains(decoderText, "private static string GetHardwareDeviceName(AVHWDeviceType deviceType)");
        AssertContains(decoderText, "FLASHBACK_DECODER_D3D11VA_SKIP reason=exception type={ex.GetType().Name} msg='{ex.Message}'");

        var fileSizeBlock = ExtractTextBetween(
            sinkText,
            "private static long GetFileSize(string path)",
            "    private static string CreateSessionId()");
        AssertContains(fileSizeBlock, "FLASHBACK_SINK_FILE_SIZE_WARN");
        AssertContains(fileSizeBlock, "return 0;");
        AssertDoesNotContain(fileSizeBlock, "System.Diagnostics.Trace.TraceWarning");

        return Task.CompletedTask;
    }

}
