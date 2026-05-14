using System;
using System.IO;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Recording;

public sealed partial class LibAvRecordingSink
{
    private static bool TryValidateStoppedOutputFile(string outputPath, out long outputBytes, out string failureMessage)
    {
        outputBytes = 0;
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            failureMessage = "output path is empty";
            return false;
        }

        try
        {
            if (!File.Exists(outputPath))
            {
                failureMessage = "output file is missing";
                return false;
            }

            outputBytes = new FileInfo(outputPath).Length;
            if (outputBytes <= 0)
            {
                failureMessage = "output file is empty";
                return false;
            }
        }
        catch (Exception ex)
        {
            failureMessage = $"output file length unavailable: {ex.Message}";
            Logger.Log($"LIBAV_SINK_STOP_OUTPUT_VALIDATE_WARN output='{outputPath}' type={ex.GetType().Name} msg={ex.Message}");
            return false;
        }

        failureMessage = string.Empty;
        return true;
    }
}
