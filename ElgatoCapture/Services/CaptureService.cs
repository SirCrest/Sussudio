using System;
using System.Collections.Generic;
using System.Linq;
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

public class CaptureService : IDisposable
{
    private MediaCapture? _mediaCapture;
    private MediaFrameReader? _frameReader;
    private StorageFile? _recordingFile;
    private AviWriter? _aviWriter;
    private bool _isRecording;
    private bool _isInitialized;
    private readonly object _lockObject = new();

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

        MediaEncodingProfile profile;

        if (settings.Format == RecordingFormat.HevcMp4)
        {
            profile = MediaEncodingProfile.CreateHevc(
                settings.Height >= 2160 ? VideoEncodingQuality.Uhd2160p :
                settings.Height >= 1080 ? VideoEncodingQuality.HD1080p :
                VideoEncodingQuality.HD720p);
        }
        else
        {
            profile = MediaEncodingProfile.CreateMp4(
                settings.Height >= 2160 ? VideoEncodingQuality.Uhd2160p :
                settings.Height >= 1080 ? VideoEncodingQuality.HD1080p :
                VideoEncodingQuality.HD720p);
        }

        // Set explicit resolution and frame rate
        if (profile.Video != null)
        {
            profile.Video.Width = settings.Width;
            profile.Video.Height = settings.Height;
            profile.Video.FrameRate.Numerator = (uint)settings.FrameRate;
            profile.Video.FrameRate.Denominator = 1;

            // Apply quality-based bitrate (unless Auto, which uses encoder defaults)
            if (settings.Quality != VideoQuality.Auto)
            {
                var targetBitrate = settings.GetTargetBitrate();
                profile.Video.Bitrate = targetBitrate;
                Logger.Log($"Video bitrate set to {targetBitrate / 1_000_000.0:F1} Mbps (Quality: {settings.Quality})");
            }
            else
            {
                Logger.Log($"Using encoder default bitrate (Quality: Auto)");
            }
        }

        // Configure audio if enabled
        if (!settings.AudioEnabled)
        {
            profile.Audio = null;
        }
        else if (profile.Audio != null)
        {
            profile.Audio.Bitrate = 192000;
            profile.Audio.SampleRate = 48000;
            profile.Audio.ChannelCount = 2;
        }

        // Configure HDR if enabled and using HEVC
        if (settings.HdrEnabled && settings.Format == RecordingFormat.HevcMp4 && profile.Video != null)
        {
            profile.Video.Subtype = "HEVC";
            // HDR10 uses BT.2020 color space
        }

        // Use standard recording API (not low-lag) for better CFR (constant frame rate) output
        // Low-lag API prioritizes latency over frame timing consistency, resulting in VFR
        await _mediaCapture.StartRecordToStorageFileAsync(profile, _recordingFile);
        Logger.Log("Started recording with standard API (CFR mode)");
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
            Logger.Log("=== Stopping recording (seamless - no reinitialization needed) ===");

            if (_frameReader != null)
            {
                _frameReader.FrameArrived -= FrameReader_FrameArrived;
                await _frameReader.StopAsync();
                _frameReader.Dispose();
                _frameReader = null;
            }

            if (_aviWriter != null)
            {
                await _aviWriter.FinalizeAsync();
                _aviWriter.Dispose();
                _aviWriter = null;
            }
            else if (_mediaCapture != null)
            {
                // Stop standard recording (not low-lag)
                await _mediaCapture.StopRecordAsync();
                Logger.Log("Stopped recording with standard API");
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
