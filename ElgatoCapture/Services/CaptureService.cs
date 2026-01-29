using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;
using Windows.Media.Audio;
using Windows.Media.Render;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;

namespace ElgatoCapture.Services;

// COM interface for accessing raw audio buffer bytes
[ComImport]
[Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
unsafe interface IMemoryBufferByteAccess
{
    void GetBuffer(out byte* buffer, out uint capacity);
}

public class CaptureService : IDisposable
{
    private MediaCapture? _mediaCapture;
    private MediaFrameReader? _frameReader;
    private MediaFrameReader? _recordingFrameReader; // Separate reader for recording
    private StorageFile? _recordingFile;
    private AviWriter? _aviWriter;
    private bool _isRecording;
    private bool _isInitialized;
    private readonly object _lockObject = new();

    // FFmpeg encoder for CFR output
    private FFmpegEncoderService? _ffmpegEncoder;
    private AudioGraph? _recordingAudioGraph; // Separate graph for recording audio capture
    private AudioFrameOutputNode? _audioFrameOutputNode;

    // Audio preview
    private AudioGraph? _audioGraph;
    private AudioDeviceInputNode? _audioInputNode;
    private AudioDeviceOutputNode? _audioOutputNode;
    private bool _isAudioPreviewActive;
    private string? _audioDeviceId;

    // Stored device info for reinitialization
    private CaptureDevice? _currentDevice;
    private CaptureSettings? _currentSettings;
    private bool _wasAudioPreviewActive;

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<Exception>? ErrorOccurred;
    public event EventHandler<ulong>? FrameCaptured;
    public event EventHandler? RequestPreviewStop;
    public event EventHandler? PreviewNeedsRestart;

    public bool IsRecording => _isRecording;
    public bool IsInitialized => _isInitialized;
    public bool IsAudioPreviewActive => _isAudioPreviewActive;
    public MediaCapture? MediaCapture => _mediaCapture;

    public async Task InitializeAsync(CaptureDevice device, CaptureSettings settings)
    {
        await CleanupAsync();

        // Store device and settings for potential reinitialization during recording
        _currentDevice = device;
        _currentSettings = settings;

        _mediaCapture = new MediaCapture();

        Logger.Log($"Audio enabled: {settings.AudioEnabled}");
        Logger.Log($"Audio device ID: {device.AudioDeviceId ?? "(none)"}");
        Logger.Log($"Audio device name: {device.AudioDeviceName ?? "(none)"}");

        // Initialize MediaCapture with AudioAndVideo mode from the start if audio is available
        // This avoids needing to reinitialize when recording starts, making it seamless
        // AudioGraph handles audio PREVIEW (to speakers), MediaCapture handles audio RECORDING
        var initSettings = new MediaCaptureInitializationSettings
        {
            VideoDeviceId = device.Id
        };

        if (settings.AudioEnabled && !string.IsNullOrEmpty(device.AudioDeviceId))
        {
            _audioDeviceId = device.AudioDeviceId;
            initSettings.AudioDeviceId = device.AudioDeviceId;
            initSettings.StreamingCaptureMode = StreamingCaptureMode.AudioAndVideo;
            Logger.Log($"✓ Initializing with AudioAndVideo mode for seamless recording");
            Logger.Log($"  Audio device: {device.AudioDeviceName}");
            Logger.Log($"  (AudioGraph handles preview, MediaCapture handles recording)");
        }
        else if (settings.AudioEnabled)
        {
            _audioDeviceId = null;
            initSettings.StreamingCaptureMode = StreamingCaptureMode.Video;
            Logger.Log($"✗ Audio enabled but no audio device available - using Video mode");
        }
        else
        {
            _audioDeviceId = null;
            initSettings.StreamingCaptureMode = StreamingCaptureMode.Video;
            Logger.Log("Audio disabled by user - using Video mode");
        }

        Logger.Log($"Streaming mode: {initSettings.StreamingCaptureMode}");

        _mediaCapture.Failed += MediaCapture_Failed;

        try
        {
            await _mediaCapture.InitializeAsync(initSettings);

            // Set the video format on the device
            Logger.Log($"=== Setting device format to {settings.Width}x{settings.Height}@{settings.FrameRate}fps ===");
            var videoController = _mediaCapture.VideoDeviceController;
            var availableProperties = videoController.GetAvailableMediaStreamProperties(MediaStreamType.VideoRecord);

            Logger.Log($"Device has {availableProperties.Count} available formats");

            var matchingFormat = availableProperties
                .OfType<VideoEncodingProperties>()
                .FirstOrDefault(p =>
                    p.Width == settings.Width &&
                    p.Height == settings.Height &&
                    Math.Abs((double)p.FrameRate.Numerator / p.FrameRate.Denominator - settings.FrameRate) < 1);

            if (matchingFormat != null)
            {
                var fps = (double)matchingFormat.FrameRate.Numerator / matchingFormat.FrameRate.Denominator;
                Logger.Log($"✓ Found matching format: {matchingFormat.Width}x{matchingFormat.Height}@{fps:F1}fps ({matchingFormat.Subtype})");
                await videoController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoRecord, matchingFormat);
                Logger.Log($"✓ Device format set successfully");
            }
            else
            {
                Logger.Log($"✗ No matching format found for {settings.Width}x{settings.Height}@{settings.FrameRate}fps");
                Logger.Log("Available formats:");
                foreach (var prop in availableProperties.OfType<VideoEncodingProperties>().Take(10))
                {
                    var fps = (double)prop.FrameRate.Numerator / prop.FrameRate.Denominator;
                    Logger.Log($"  - {prop.Width}x{prop.Height}@{fps:F1}fps ({prop.Subtype})");
                }
            }

            _isInitialized = true;
            StatusChanged?.Invoke(this, "Initialized");
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex);
            throw;
        }
    }

    private void MediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
    {
        ErrorOccurred?.Invoke(this, new Exception($"MediaCapture failed: {errorEventArgs.Message}"));
    }

    public async Task StartRecordingAsync(CaptureSettings settings)
    {
        if (!_isInitialized || _mediaCapture == null || _currentDevice == null)
        {
            throw new InvalidOperationException("Capture not initialized");
        }

        if (_isRecording)
        {
            return;
        }

        try
        {
            Logger.Log("=== Starting recording (seamless - no reinitialization needed) ===");
            Logger.Log($"Audio preview active: {_isAudioPreviewActive}");
            Logger.Log($"Audio in recording: {(settings.AudioEnabled && !string.IsNullOrEmpty(_audioDeviceId) ? "Yes" : "No")}");

            var folder = await StorageFolder.GetFolderFromPathAsync(settings.OutputPath);
            _recordingFile = await folder.CreateFileAsync(
                settings.GetOutputFileName(),
                CreationCollisionOption.GenerateUniqueName);

            if (settings.Format == RecordingFormat.UncompressedAvi)
            {
                await StartUncompressedRecordingAsync(settings);
            }
            else
            {
                await StartCompressedRecordingAsync(settings);
            }

            _isRecording = true;
            StatusChanged?.Invoke(this, "Recording");
            Logger.Log("✓ Recording started successfully");
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex);
            throw;
        }
    }

    private async Task StartCompressedRecordingAsync(CaptureSettings settings)
    {
        if (_mediaCapture == null || _recordingFile == null) return;

        Logger.Log("=== Starting FFmpeg-based recording for CFR output ===");

        // Initialize FFmpeg encoder
        _ffmpegEncoder = new FFmpegEncoderService();
        _ffmpegEncoder.StatusChanged += (s, msg) => Logger.Log($"[FFmpegEncoder] {msg}");
        _ffmpegEncoder.ErrorOccurred += (s, err) => Logger.Log($"[FFmpegEncoder] ERROR: {err}");
        _ffmpegEncoder.FrameEncoded += (s, count) => FrameCaptured?.Invoke(this, count);

        // Start FFmpeg encoder with output path
        await _ffmpegEncoder.StartEncodingAsync(settings, _recordingFile.Path);

        // Set up frame reader for recording (uses existing MediaCapture)
        await SetupRecordingFrameReaderAsync(settings);

        // Set up audio capture for recording
        if (settings.AudioEnabled && !string.IsNullOrEmpty(_audioDeviceId))
        {
            await SetupRecordingAudioCaptureAsync();
        }

        Logger.Log("FFmpeg recording started - frames will be piped to encoder");
    }

    private async Task SetupRecordingFrameReaderAsync(CaptureSettings settings)
    {
        if (_mediaCapture == null) return;

        // Find video frame source
        var frameSourceGroups = await MediaFrameSourceGroup.FindAllAsync();
        MediaFrameSourceGroup? matchingGroup = null;
        MediaFrameSourceInfo? videoSourceInfo = null;

        foreach (var group in frameSourceGroups)
        {
            var videoSource = group.SourceInfos.FirstOrDefault(si =>
                si.MediaStreamType == MediaStreamType.VideoRecord &&
                si.SourceKind == MediaFrameSourceKind.Color);

            if (videoSource != null)
            {
                matchingGroup = group;
                videoSourceInfo = videoSource;
                break;
            }
        }

        if (matchingGroup == null || videoSourceInfo == null)
        {
            Logger.Log("WARNING: Could not find frame source for FFmpeg recording");
            // Try to use VideoPreview stream instead
            var previewSources = _mediaCapture.FrameSources.Values
                .Where(fs => fs.Info.MediaStreamType == MediaStreamType.VideoPreview ||
                             fs.Info.MediaStreamType == MediaStreamType.VideoRecord)
                .ToList();

            if (previewSources.Count > 0)
            {
                Logger.Log($"Using alternative frame source: {previewSources[0].Info.MediaStreamType}");
                _recordingFrameReader = await _mediaCapture.CreateFrameReaderAsync(previewSources[0]);
            }
            else
            {
                throw new InvalidOperationException("No suitable frame source available for FFmpeg recording");
            }
        }
        else
        {
            // Use the identified frame source
            if (_mediaCapture.FrameSources.TryGetValue(videoSourceInfo.Id, out var frameSource))
            {
                _recordingFrameReader = await _mediaCapture.CreateFrameReaderAsync(frameSource);
            }
            else
            {
                // Fallback: try any available frame source
                var anySource = _mediaCapture.FrameSources.Values.FirstOrDefault();
                if (anySource != null)
                {
                    _recordingFrameReader = await _mediaCapture.CreateFrameReaderAsync(anySource);
                }
                else
                {
                    throw new InvalidOperationException("No frame sources available");
                }
            }
        }

        if (_recordingFrameReader != null)
        {
            _recordingFrameReader.FrameArrived += RecordingFrameReader_FrameArrived;
            var result = await _recordingFrameReader.StartAsync();
            Logger.Log($"Recording frame reader started: {result}");
        }
    }

    private async void RecordingFrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        if (_ffmpegEncoder == null || !_ffmpegEncoder.IsEncoding) return;

        using var frame = sender.TryAcquireLatestFrame();
        if (frame?.VideoMediaFrame == null) return;

        try
        {
            SoftwareBitmap? softwareBitmap = null;

            // Try to get SoftwareBitmap from frame
            if (frame.VideoMediaFrame.SoftwareBitmap != null)
            {
                softwareBitmap = frame.VideoMediaFrame.SoftwareBitmap;
            }
            else if (frame.VideoMediaFrame.Direct3DSurface != null)
            {
                // Convert from GPU memory (hardware formats like NV12/YUY2)
                softwareBitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(
                    frame.VideoMediaFrame.Direct3DSurface,
                    BitmapAlphaMode.Premultiplied);
            }

            if (softwareBitmap == null) return;

            // Convert to BGRA8 if needed (FFmpeg expects BGRA)
            if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8)
            {
                var converted = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                if (softwareBitmap != frame.VideoMediaFrame.SoftwareBitmap)
                {
                    softwareBitmap.Dispose();
                }
                softwareBitmap = converted;
            }

            // Send to FFmpeg encoder
            _ffmpegEncoder.EnqueueVideoFrame(softwareBitmap);

            // Dispose converted bitmap (FFmpeg has copied the data)
            if (softwareBitmap != frame.VideoMediaFrame.SoftwareBitmap)
            {
                softwareBitmap.Dispose();
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
        }
    }

    private async Task SetupRecordingAudioCaptureAsync()
    {
        if (string.IsNullOrEmpty(_audioDeviceId)) return;

        try
        {
            Logger.Log("=== Setting up audio capture for FFmpeg recording ===");

            // Create a separate AudioGraph for recording (the preview graph outputs to speakers)
            var graphSettings = new AudioGraphSettings(AudioRenderCategory.Media)
            {
                QuantumSizeSelectionMode = QuantumSizeSelectionMode.SystemDefault,
                EncodingProperties = AudioEncodingProperties.CreatePcm(48000, 2, 16) // s16le format
            };

            var graphResult = await AudioGraph.CreateAsync(graphSettings);
            if (graphResult.Status != AudioGraphCreationStatus.Success)
            {
                Logger.Log($"Failed to create recording AudioGraph: {graphResult.Status}");
                return;
            }

            _recordingAudioGraph = graphResult.Graph;

            // Create frame output node (captures audio samples)
            _audioFrameOutputNode = _recordingAudioGraph.CreateFrameOutputNode();
            _recordingAudioGraph.QuantumStarted += RecordingAudioGraph_QuantumStarted;

            // Find and connect the audio input device
            var audioDevices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(
                Windows.Devices.Enumeration.DeviceClass.AudioCapture);
            var captureDevice = audioDevices.FirstOrDefault(d => d.Id == _audioDeviceId);

            if (captureDevice == null)
            {
                Logger.Log($"Audio capture device not found: {_audioDeviceId}");
                return;
            }

            var inputResult = await _recordingAudioGraph.CreateDeviceInputNodeAsync(
                Windows.Media.Capture.MediaCategory.Media,
                _recordingAudioGraph.EncodingProperties,
                captureDevice);

            if (inputResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                Logger.Log($"Failed to create audio input node: {inputResult.Status}");
                return;
            }

            // Connect input to frame output
            inputResult.DeviceInputNode.AddOutgoingConnection(_audioFrameOutputNode);

            // Start the graph
            _recordingAudioGraph.Start();
            Logger.Log("Recording audio graph started");
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
        }
    }

    private void RecordingAudioGraph_QuantumStarted(AudioGraph sender, object args)
    {
        if (_ffmpegEncoder == null || !_ffmpegEncoder.IsEncoding || _audioFrameOutputNode == null) return;

        try
        {
            var frame = _audioFrameOutputNode.GetFrame();
            if (frame == null) return;

            using (var buffer = frame.LockBuffer(AudioBufferAccessMode.Read))
            using (var reference = buffer.CreateReference())
            {
                // Get the raw audio bytes
                unsafe
                {
                    byte* dataInBytes;
                    uint capacityInBytes;
                    ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacityInBytes);

                    if (capacityInBytes > 0)
                    {
                        var audioData = new byte[capacityInBytes];
                        System.Runtime.InteropServices.Marshal.Copy((IntPtr)dataInBytes, audioData, 0, (int)capacityInBytes);
                        _ffmpegEncoder.EnqueueAudioSamples(audioData);
                    }
                }
            }

            frame.Dispose();
        }
        catch (Exception ex)
        {
            // Don't log every frame error to avoid spam
            System.Diagnostics.Debug.WriteLine($"Audio frame error: {ex.Message}");
        }
    }

    private async Task StartUncompressedRecordingAsync(CaptureSettings settings)
    {
        if (_mediaCapture == null || _recordingFile == null) return;

        // Find a frame source for raw frames
        var frameSourceGroups = await MediaFrameSourceGroup.FindAllAsync();
        MediaFrameSourceGroup? matchingGroup = null;
        MediaFrameSourceInfo? videoSourceInfo = null;

        foreach (var group in frameSourceGroups)
        {
            var videoSource = group.SourceInfos.FirstOrDefault(si =>
                si.MediaStreamType == MediaStreamType.VideoRecord &&
                si.SourceKind == MediaFrameSourceKind.Color);

            if (videoSource != null)
            {
                matchingGroup = group;
                videoSourceInfo = videoSource;
                break;
            }
        }

        if (matchingGroup == null || videoSourceInfo == null)
        {
            // Fallback to using compressed recording for uncompressed - use very high bitrate
            var profile = MediaEncodingProfile.CreateAvi(VideoEncodingQuality.HD1080p);
            if (profile.Video != null)
            {
                profile.Video.Width = settings.Width;
                profile.Video.Height = settings.Height;
                profile.Video.FrameRate.Numerator = (uint)settings.FrameRate;
                profile.Video.FrameRate.Denominator = 1;
                profile.Video.Bitrate = 200_000_000; // 200 Mbps for near-lossless
            }

            if (!settings.AudioEnabled)
            {
                profile.Audio = null;
            }

            // Use standard recording API for CFR output
            await _mediaCapture.StartRecordToStorageFileAsync(profile, _recordingFile);
            Logger.Log("Started AVI fallback recording with standard API (CFR mode)");
            return;
        }

        // Initialize AVI writer for true uncompressed
        _aviWriter = new AviWriter(
            await _recordingFile.OpenAsync(FileAccessMode.ReadWrite),
            settings.Width,
            settings.Height,
            (uint)settings.FrameRate);

        // Reinitialize MediaCapture with frame source
        await _mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
        {
            SourceGroup = matchingGroup,
            SharingMode = MediaCaptureSharingMode.ExclusiveControl,
            MemoryPreference = MediaCaptureMemoryPreference.Cpu,
            StreamingCaptureMode = StreamingCaptureMode.Video
        });

        var frameSource = _mediaCapture.FrameSources[videoSourceInfo.Id];
        _frameReader = await _mediaCapture.CreateFrameReaderAsync(frameSource);
        _frameReader.FrameArrived += FrameReader_FrameArrived;

        await _frameReader.StartAsync();
    }

    private ulong _frameCount;

    private async void FrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        using var frame = sender.TryAcquireLatestFrame();
        if (frame?.VideoMediaFrame?.SoftwareBitmap == null) return;

        var softwareBitmap = frame.VideoMediaFrame.SoftwareBitmap;

        try
        {
            // Convert to BGRA8 if needed
            if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8)
            {
                softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8);
            }

            if (_aviWriter != null)
            {
                await _aviWriter.WriteFrameAsync(softwareBitmap);
            }
            _frameCount++;
            FrameCaptured?.Invoke(this, _frameCount);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex);
        }
    }

    public async Task StopRecordingAsync()
    {
        if (!_isRecording) return;

        try
        {
            Logger.Log("=== Stopping recording ===");

            // Stop recording frame reader (FFmpeg recording)
            if (_recordingFrameReader != null)
            {
                _recordingFrameReader.FrameArrived -= RecordingFrameReader_FrameArrived;
                await _recordingFrameReader.StopAsync();
                _recordingFrameReader.Dispose();
                _recordingFrameReader = null;
                Logger.Log("Recording frame reader stopped");
            }

            // Stop recording audio graph
            if (_recordingAudioGraph != null)
            {
                _recordingAudioGraph.QuantumStarted -= RecordingAudioGraph_QuantumStarted;
                _recordingAudioGraph.Stop();
                _audioFrameOutputNode?.Dispose();
                _audioFrameOutputNode = null;
                _recordingAudioGraph.Dispose();
                _recordingAudioGraph = null;
                Logger.Log("Recording audio graph stopped");
            }

            // Stop FFmpeg encoder
            if (_ffmpegEncoder != null)
            {
                await _ffmpegEncoder.StopEncodingAsync();
                _ffmpegEncoder.Dispose();
                _ffmpegEncoder = null;
                Logger.Log("FFmpeg encoder stopped");
            }

            // Legacy: Stop AVI frame reader
            if (_frameReader != null)
            {
                _frameReader.FrameArrived -= FrameReader_FrameArrived;
                await _frameReader.StopAsync();
                _frameReader.Dispose();
                _frameReader = null;
            }

            // Legacy: Finalize AVI writer
            if (_aviWriter != null)
            {
                await _aviWriter.FinalizeAsync();
                _aviWriter.Dispose();
                _aviWriter = null;
            }

            _isRecording = false;
            _frameCount = 0;

            Logger.Log($"✓ Recording stopped. Audio preview status: {(_isAudioPreviewActive ? "still active" : "inactive")}");
            StatusChanged?.Invoke(this, "Stopped");
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex);
            throw;
        }
    }

    public async Task StartAudioPreviewAsync()
    {
        if (string.IsNullOrEmpty(_audioDeviceId))
        {
            Logger.Log("Cannot start audio preview - no audio device configured");
            return;
        }

        if (_isAudioPreviewActive)
        {
            Logger.Log("Audio preview already active");
            return;
        }

        try
        {
            Logger.Log("=== Starting Audio Preview ===");
            Logger.Log($"Audio device ID: {_audioDeviceId}");

            // Create AudioGraph settings
            // Use SystemDefault for stable audio - LowestLatency can cause skipping under CPU load
            var graphSettings = new AudioGraphSettings(AudioRenderCategory.Media);
            graphSettings.QuantumSizeSelectionMode = QuantumSizeSelectionMode.SystemDefault;

            var graphResult = await AudioGraph.CreateAsync(graphSettings);

            if (graphResult.Status != AudioGraphCreationStatus.Success)
            {
                Logger.Log($"Failed to create AudioGraph: {graphResult.Status}");
                ErrorOccurred?.Invoke(this, new Exception($"Failed to create AudioGraph: {graphResult.Status}"));
                return;
            }

            _audioGraph = graphResult.Graph;
            Logger.Log($"AudioGraph created: {_audioGraph.EncodingProperties.SampleRate}Hz, {_audioGraph.EncodingProperties.ChannelCount} channels, {_audioGraph.EncodingProperties.BitsPerSample} bits");

            // Create output node (default audio device - speakers/headphones)
            var outputResult = await _audioGraph.CreateDeviceOutputNodeAsync();
            if (outputResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                Logger.Log($"Failed to create output node: {outputResult.Status}");
                ErrorOccurred?.Invoke(this, new Exception($"Failed to create audio output node: {outputResult.Status}"));
                _audioGraph?.Dispose();
                _audioGraph = null;
                return;
            }

            _audioOutputNode = outputResult.DeviceOutputNode;
            Logger.Log("Audio output node created successfully");

            // Find the audio capture device
            var audioDevices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(
                Windows.Devices.Enumeration.DeviceClass.AudioCapture);

            var captureDevice = audioDevices.FirstOrDefault(d => d.Id == _audioDeviceId);
            if (captureDevice == null)
            {
                Logger.Log($"Audio capture device not found: {_audioDeviceId}");
                ErrorOccurred?.Invoke(this, new Exception("Audio capture device not found"));
                await StopAudioPreviewAsync();
                return;
            }

            Logger.Log($"Found audio capture device: {captureDevice.Name}");

            // Create input node from the capture device
            // AudioGraph handles all format conversion internally
            var inputResult = await _audioGraph.CreateDeviceInputNodeAsync(
                Windows.Media.Capture.MediaCategory.Media,
                _audioGraph.EncodingProperties,
                captureDevice);

            if (inputResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                Logger.Log($"Failed to create input node: {inputResult.Status}, ExtendedError: {inputResult.ExtendedError?.Message}");
                ErrorOccurred?.Invoke(this, new Exception($"Failed to create audio input node: {inputResult.Status}"));
                await StopAudioPreviewAsync();
                return;
            }

            _audioInputNode = inputResult.DeviceInputNode;
            Logger.Log("Audio input node created successfully");

            // Connect input to output - AudioGraph handles format conversion
            _audioInputNode.AddOutgoingConnection(_audioOutputNode);
            Logger.Log("Audio nodes connected");

            // Start the graph
            _audioGraph.Start();

            _isAudioPreviewActive = true;
            Logger.Log("✓ Audio preview started successfully");
            StatusChanged?.Invoke(this, "Audio preview active");
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            ErrorOccurred?.Invoke(this, ex);
            await StopAudioPreviewAsync();
        }
    }

    public async Task StopAudioPreviewAsync()
    {
        if (!_isAudioPreviewActive)
        {
            return;
        }

        try
        {
            Logger.Log("=== Stopping Audio Preview ===");

            if (_audioInputNode != null)
            {
                _audioInputNode.Dispose();
                _audioInputNode = null;
            }

            if (_audioOutputNode != null)
            {
                _audioOutputNode.Dispose();
                _audioOutputNode = null;
            }

            if (_audioGraph != null)
            {
                _audioGraph.Stop();
                _audioGraph.Dispose();
                _audioGraph = null;
            }

            _isAudioPreviewActive = false;
            Logger.Log("✓ Audio preview stopped");
            StatusChanged?.Invoke(this, "Audio preview stopped");
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            ErrorOccurred?.Invoke(this, ex);
        }

        await Task.CompletedTask;
    }

    public async Task CleanupAsync()
    {
        if (_isRecording)
        {
            await StopRecordingAsync();
        }

        if (_isAudioPreviewActive)
        {
            await StopAudioPreviewAsync();
        }

        if (_mediaCapture != null)
        {
            _mediaCapture.Failed -= MediaCapture_Failed;
            _mediaCapture.Dispose();
            _mediaCapture = null;
        }

        _isInitialized = false;
    }

    public void Dispose()
    {
        CleanupAsync().GetAwaiter().GetResult();
    }
}

// Simple AVI writer for uncompressed frames
internal class AviWriter : IDisposable
{
    private readonly IRandomAccessStream _stream;
    private readonly DataWriter _writer;
    private readonly uint _width;
    private readonly uint _height;
    private readonly uint _frameRate;
    private readonly List<uint> _frameOffsets = new();
    private uint _frameCount;
    private ulong _moviStartPosition;
    private readonly byte[] _frameBuffer;

    public AviWriter(IRandomAccessStream stream, uint width, uint height, uint frameRate)
    {
        _stream = stream;
        _writer = new DataWriter(stream);
        _width = width;
        _height = height;
        _frameRate = frameRate;
        _frameBuffer = new byte[width * height * 4]; // BGRA
    }

    public async Task WriteHeaderAsync()
    {
        // Simplified AVI header - will be updated on finalize
        // RIFF header
        WriteString("RIFF");
        _writer.WriteUInt32(0); // File size placeholder
        WriteString("AVI ");

        // hdrl LIST
        WriteString("LIST");
        _writer.WriteUInt32(0); // hdrl size placeholder
        WriteString("hdrl");

        // avih (main AVI header)
        WriteString("avih");
        _writer.WriteUInt32(56); // avih size
        _writer.WriteUInt32(1000000 / _frameRate); // microseconds per frame
        _writer.WriteUInt32(0); // max bytes per sec
        _writer.WriteUInt32(0); // padding granularity
        _writer.WriteUInt32(0x10); // flags (AVIF_HASINDEX)
        _writer.WriteUInt32(0); // total frames placeholder
        _writer.WriteUInt32(0); // initial frames
        _writer.WriteUInt32(1); // streams
        _writer.WriteUInt32(_width * _height * 4); // suggested buffer size
        _writer.WriteUInt32(_width);
        _writer.WriteUInt32(_height);
        _writer.WriteUInt32(0); // reserved
        _writer.WriteUInt32(0);
        _writer.WriteUInt32(0);
        _writer.WriteUInt32(0);

        // strl LIST (stream list)
        WriteString("LIST");
        _writer.WriteUInt32(116); // strl size
        WriteString("strl");

        // strh (stream header)
        WriteString("strh");
        _writer.WriteUInt32(56); // strh size
        WriteString("vids"); // fccType
        WriteString("DIB "); // fccHandler (uncompressed)
        _writer.WriteUInt32(0); // flags
        _writer.WriteUInt16(0); // priority
        _writer.WriteUInt16(0); // language
        _writer.WriteUInt32(0); // initial frames
        _writer.WriteUInt32(1); // scale
        _writer.WriteUInt32(_frameRate); // rate
        _writer.WriteUInt32(0); // start
        _writer.WriteUInt32(0); // length placeholder
        _writer.WriteUInt32(_width * _height * 4); // suggested buffer
        _writer.WriteUInt32(0); // quality
        _writer.WriteUInt32(0); // sample size
        _writer.WriteInt16(0); // left
        _writer.WriteInt16(0); // top
        _writer.WriteInt16((short)_width); // right
        _writer.WriteInt16((short)_height); // bottom

        // strf (stream format - BITMAPINFOHEADER)
        WriteString("strf");
        _writer.WriteUInt32(40); // strf size
        _writer.WriteUInt32(40); // biSize
        _writer.WriteInt32((int)_width);
        _writer.WriteInt32(-(int)_height); // negative for top-down
        _writer.WriteUInt16(1); // planes
        _writer.WriteUInt16(32); // bits per pixel
        _writer.WriteUInt32(0); // compression (BI_RGB)
        _writer.WriteUInt32(_width * _height * 4); // image size
        _writer.WriteInt32(0);
        _writer.WriteInt32(0);
        _writer.WriteUInt32(0);
        _writer.WriteUInt32(0);

        // movi LIST
        WriteString("LIST");
        _writer.WriteUInt32(0); // movi size placeholder
        WriteString("movi");

        await _writer.StoreAsync();
        _moviStartPosition = _stream.Position;
    }

    private void WriteString(string s)
    {
        foreach (char c in s)
        {
            _writer.WriteByte((byte)c);
        }
    }

    public async Task WriteFrameAsync(SoftwareBitmap bitmap)
    {
        var buffer = new Windows.Storage.Streams.Buffer((uint)_frameBuffer.Length);
        bitmap.CopyToBuffer(buffer);

        // Get the raw pixel data
        var reader = DataReader.FromBuffer(buffer);
        reader.ReadBytes(_frameBuffer);

        // Write frame chunk
        _frameOffsets.Add((uint)(_stream.Position - 4));
        WriteString("00dc"); // video chunk
        _writer.WriteUInt32((uint)_frameBuffer.Length);
        _writer.WriteBytes(_frameBuffer);
        _frameCount++;

        await _writer.StoreAsync();
    }

    public async Task FinalizeAsync()
    {
        // Write index
        var indexStart = _stream.Position;
        WriteString("idx1");
        _writer.WriteUInt32((uint)(_frameCount * 16));

        for (int i = 0; i < _frameOffsets.Count; i++)
        {
            WriteString("00dc");
            _writer.WriteUInt32(0x10); // AVIIF_KEYFRAME
            _writer.WriteUInt32(_frameOffsets[i] - (uint)_moviStartPosition);
            _writer.WriteUInt32((uint)_frameBuffer.Length);
        }

        await _writer.StoreAsync();

        // Update file size in RIFF header
        var fileSize = _stream.Position;
        _stream.Seek(4);
        var sizeWriter = new DataWriter(_stream);
        sizeWriter.WriteUInt32((uint)(fileSize - 8));
        await sizeWriter.StoreAsync();

        await _stream.FlushAsync();
    }

    public void Dispose()
    {
        _writer.Dispose();
        _stream.Dispose();
    }
}
