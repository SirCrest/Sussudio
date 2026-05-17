using System.Collections;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task MainViewModelCaptureSettings_OwnsSettingsProjection()
    {
        var captureText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Capture.cs")
            .Replace("\r\n", "\n");
        var recordingLifecycleText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.RecordingLifecycle.cs")
            .Replace("\r\n", "\n");
        var recordingOperationsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.RecordingOperations.cs")
            .Replace("\r\n", "\n");
        var captureSettingsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureSettings.cs")
            .Replace("\r\n", "\n");
        var captureSettingsFrameRateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureSettingsFrameRate.cs")
            .Replace("\r\n", "\n");

        AssertContains(captureSettingsText, "private CaptureSettings BuildCaptureSettings()");
        AssertContains(captureSettingsText, "var runtime = _captureService.GetRuntimeSnapshot();");
        AssertContains(captureSettingsText, "var sourceTelemetry = _captureService.GetLatestSourceTelemetrySnapshot();");
        AssertContains(captureSettingsText, "var frameRateProjection = ProjectCaptureSettingsFrameRate(new CaptureSettingsFrameRateRequest(");
        AssertContains(captureSettingsText, "FrameRate = frameRateProjection.EffectiveFrameRate,");
        AssertContains(captureSettingsText, "RequestedFrameRateArg = frameRateProjection.RequestedFrameRateArg,");
        AssertContains(captureSettingsText, "RequestedFrameRateNumerator = frameRateProjection.RequestedFrameRateNumerator,");
        AssertContains(captureSettingsText, "RequestedFrameRateDenominator = frameRateProjection.RequestedFrameRateDenominator,");
        AssertContains(captureSettingsText, "RequestedPixelFormat = ResolveRequestedPixelFormat()");
        AssertContains(captureSettingsText, "ForceMjpegDecode = ShouldForceMjpegDecode()");
        AssertContains(captureSettingsText, "settings.UseCustomAudioInput = IsCustomAudioInputEnabled;");
        AssertContains(captureSettingsText, "settings.MicrophoneEnabled = IsMicrophoneEnabled;");
        AssertContains(captureSettingsText, "private string? ResolveRequestedPixelFormat()");
        AssertContains(captureSettingsText, "private bool ShouldForceMjpegDecode()");
        AssertContains(captureSettingsFrameRateText, "private readonly record struct CaptureSettingsFrameRateRequest");
        AssertContains(captureSettingsFrameRateText, "private readonly record struct CaptureSettingsFrameRateProjection");
        AssertContains(captureSettingsFrameRateText, "private CaptureSettingsFrameRateProjection ProjectCaptureSettingsFrameRate(CaptureSettingsFrameRateRequest request)");
        AssertContains(captureSettingsFrameRateText, "var selectedFrameRateOption = AvailableFrameRates");
        AssertContains(captureSettingsFrameRateText, "var effectiveFrameRate = IsAutoResolutionValue(SelectedResolution) && AutoResolvedFrameRate.HasValue && AutoResolvedFrameRate.Value > 0");
        AssertContains(captureSettingsFrameRateText, "runtimeMatchesResolution");
        AssertContains(captureSettingsFrameRateText, "request.Runtime.NegotiatedFrameRateNumerator");
        AssertContains(captureSettingsFrameRateText, "request.SourceTelemetry.HasFrameRate");
        AssertContains(captureSettingsFrameRateText, "TryParseFrameRateRational(requestedFrameRateArg");
        AssertContains(captureSettingsFrameRateText, "SelectedFormat?.FrameRateNumerator > 0 && SelectedFormat.FrameRateDenominator > 0");
        AssertContains(captureSettingsFrameRateText, "requestedFrameRateArg = effectiveFrameRate.ToString(\"0.###\");");
        AssertDoesNotContain(captureText, "private CaptureSettings BuildCaptureSettings()");
        AssertContains(captureText, "await _sessionCoordinator.StartVideoPreviewAsync(settings, cancellationToken)");
        AssertContains(recordingOperationsText, "await _sessionCoordinator.StartRecordingAsync(settings, cancellationToken);");
        AssertDoesNotContain(recordingLifecycleText, "await _sessionCoordinator.StartRecordingAsync(settings, cancellationToken);");
        AssertDoesNotContain(captureText, "await _sessionCoordinator.StartRecordingAsync(settings, cancellationToken);");

        return Task.CompletedTask;
    }

    private static Task MainViewModelCaptureSettingsFrameRate_PreservesProjectionPrecedence()
    {
        var projection = InvokeCaptureSettingsFrameRateProjection(
            selectedResolution: "1920x1080",
            selectedFrameRate: 60,
            autoResolvedFrameRate: null,
            selectedFormat: CreateMediaFormat(width: 1920, height: 1080, frameRate: 60, numerator: 60, denominator: 1),
            runtime: CreateRuntimeSnapshot(
                actualWidth: 1920,
                actualHeight: 1080,
                actualFrameRate: 60000d / 1001d,
                actualFrameRateArg: "60000/1001",
                negotiatedNumerator: 60000,
                negotiatedDenominator: 1001),
            sourceTelemetry: CreateSourceTelemetry(frameRateExact: 60, frameRateArg: "60/1"),
            CreateFrameRateOption(
                RequireType("Sussudio.Models.FrameRateOption"),
                60,
                60000d / 1001d,
                "60000/1001",
                isEnabled: true));

        AssertNearlyEqual(60, GetDoubleProperty(projection, "EffectiveFrameRate"), 0.001, "source-over-runtime effective frame rate");
        AssertEqual("60/1", GetStringProperty(projection, "RequestedFrameRateArg"), "source telemetry frame-rate arg wins after runtime");
        AssertEqual(60, Convert.ToInt32(GetPropertyValue(projection, "RequestedFrameRateNumerator")), "source telemetry numerator wins after runtime");
        AssertEqual(1, Convert.ToInt32(GetPropertyValue(projection, "RequestedFrameRateDenominator")), "source telemetry denominator wins after runtime");

        projection = InvokeCaptureSettingsFrameRateProjection(
            selectedResolution: "1920x1080",
            selectedFrameRate: 60,
            autoResolvedFrameRate: null,
            selectedFormat: CreateMediaFormat(width: 1920, height: 1080, frameRate: 59.94, numerator: 60000, denominator: 1001),
            runtime: CreateRuntimeSnapshot(),
            sourceTelemetry: CreateSourceTelemetry(),
            CreateFrameRateOption(
                RequireType("Sussudio.Models.FrameRateOption"),
                60,
                60,
                string.Empty,
                isEnabled: true));

        AssertNearlyEqual(60, GetDoubleProperty(projection, "EffectiveFrameRate"), 0.001, "selected frame-rate effective value");
        AssertEqual("60000/1001", GetStringProperty(projection, "RequestedFrameRateArg"), "selected format rational fallback");
        AssertEqual(60000, Convert.ToInt32(GetPropertyValue(projection, "RequestedFrameRateNumerator")), "selected format fallback numerator");
        AssertEqual(1001, Convert.ToInt32(GetPropertyValue(projection, "RequestedFrameRateDenominator")), "selected format fallback denominator");

        projection = InvokeCaptureSettingsFrameRateProjection(
            selectedResolution: "Source",
            selectedFrameRate: 0,
            autoResolvedFrameRate: 119.88,
            selectedFormat: null,
            runtime: CreateRuntimeSnapshot(),
            sourceTelemetry: CreateSourceTelemetry());

        AssertNearlyEqual(119.88, GetDoubleProperty(projection, "EffectiveFrameRate"), 0.001, "auto-resolved effective frame rate");
        AssertEqual("119.88", GetStringProperty(projection, "RequestedFrameRateArg"), "decimal frame-rate fallback");
        AssertEqual(null, GetPropertyValue(projection, "RequestedFrameRateNumerator"), "decimal fallback numerator remains unset");
        AssertEqual(null, GetPropertyValue(projection, "RequestedFrameRateDenominator"), "decimal fallback denominator remains unset");

        return Task.CompletedTask;
    }

    private static object InvokeCaptureSettingsFrameRateProjection(
        string selectedResolution,
        double selectedFrameRate,
        double? autoResolvedFrameRate,
        object? selectedFormat,
        object runtime,
        object sourceTelemetry,
        params object[] frameRateOptions)
    {
        var viewModelType = RequireType("Sussudio.ViewModels.MainViewModel");
        var requestType = RequireType("Sussudio.ViewModels.MainViewModel+CaptureSettingsFrameRateRequest");
        var viewModel = CreateUninitializedObject(viewModelType);
        var frameRateType = RequireType("Sussudio.Models.FrameRateOption");
        var availableFrameRates = (IList)(Activator.CreateInstance(typeof(System.Collections.ObjectModel.ObservableCollection<>).MakeGenericType(frameRateType))
            ?? throw new InvalidOperationException("Failed to create frame-rate option collection."));
        foreach (var option in frameRateOptions)
        {
            availableFrameRates.Add(option);
        }

        SetPropertyBackingField(viewModel, "AvailableFrameRates", availableFrameRates);
        SetPropertyBackingField(viewModel, "SelectedFrameRate", selectedFrameRate);
        SetPropertyBackingField(viewModel, "SelectedResolution", selectedResolution);
        SetPropertyBackingField(viewModel, "AutoResolvedFrameRate", autoResolvedFrameRate);
        SetPropertyBackingField(viewModel, "SelectedFormat", selectedFormat);

        var request = Activator.CreateInstance(
            requestType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object?[] { true, 1920u, 1080u, runtime, sourceTelemetry },
            culture: null)
            ?? throw new InvalidOperationException("Failed to create CaptureSettingsFrameRateRequest.");
        var project = viewModelType.GetMethod("ProjectCaptureSettingsFrameRate", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ProjectCaptureSettingsFrameRate was not found.");
        return project.Invoke(viewModel, new[] { request })
               ?? throw new InvalidOperationException("ProjectCaptureSettingsFrameRate returned null.");
    }

    private static object CreateRuntimeSnapshot(
        uint? actualWidth = null,
        uint? actualHeight = null,
        double? actualFrameRate = null,
        string? actualFrameRateArg = null,
        uint? negotiatedNumerator = null,
        uint? negotiatedDenominator = null)
    {
        var snapshot = CreateConfigInstance(RequireType("Sussudio.Models.CaptureRuntimeSnapshot"));
        SetPropertyOrBackingField(snapshot, "ActualWidth", actualWidth);
        SetPropertyOrBackingField(snapshot, "ActualHeight", actualHeight);
        SetPropertyOrBackingField(snapshot, "ActualFrameRate", actualFrameRate);
        SetPropertyOrBackingField(snapshot, "ActualFrameRateArg", actualFrameRateArg);
        SetPropertyOrBackingField(snapshot, "NegotiatedFrameRateNumerator", negotiatedNumerator);
        SetPropertyOrBackingField(snapshot, "NegotiatedFrameRateDenominator", negotiatedDenominator);
        return snapshot;
    }

    private static object CreateSourceTelemetry(double? frameRateExact = null, string? frameRateArg = null)
    {
        var snapshot = CreateConfigInstance(RequireType("Sussudio.Models.SourceSignalTelemetrySnapshot"));
        SetPropertyOrBackingField(snapshot, "FrameRateExact", frameRateExact);
        SetPropertyOrBackingField(snapshot, "FrameRateArg", frameRateArg);
        return snapshot;
    }

    private static object CreateMediaFormat(
        uint width,
        uint height,
        double frameRate,
        uint numerator,
        uint denominator)
    {
        var format = CreateConfigInstance(RequireType("Sussudio.Models.MediaFormat"));
        SetPropertyOrBackingField(format, "Width", width);
        SetPropertyOrBackingField(format, "Height", height);
        SetPropertyOrBackingField(format, "FrameRate", frameRate);
        SetPropertyOrBackingField(format, "FrameRateNumerator", numerator);
        SetPropertyOrBackingField(format, "FrameRateDenominator", denominator);
        return format;
    }
}
