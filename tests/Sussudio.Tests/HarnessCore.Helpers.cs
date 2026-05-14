using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

static partial class Program
{
    private static HashSet<string> ExtractSnapshotFields(string sourceText)
    {
        var fields = new HashSet<string>(StringComparer.Ordinal);
        var index = 0;
        while (index < sourceText.Length)
        {
            var getIdx = sourceText.IndexOf("Get(snapshot,", index, StringComparison.Ordinal);
            if (getIdx < 0)
                break;

            var afterComma = getIdx + "Get(snapshot,".Length;
            var quoteIdx = sourceText.IndexOf('"', afterComma);
            if (quoteIdx < 0 || quoteIdx - afterComma > 10)
            {
                index = afterComma;
                continue;
            }

            var endQuoteIdx = sourceText.IndexOf('"', quoteIdx + 1);
            if (endQuoteIdx < 0)
            {
                index = quoteIdx + 1;
                continue;
            }

            var fieldName = sourceText.Substring(quoteIdx + 1, endQuoteIdx - quoteIdx - 1);
            if (fieldName.Length > 0)
                fields.Add(fieldName);

            index = endQuoteIdx + 1;
        }

        return fields;
    }

    private static object BuildRecordingContext(
        bool usePostMuxAudio,
        string? videoPath = null,
        string? audioTempPath = null,
        string? finalPath = null)
    {
        var settings = BuildSettings(hdrEnabled: false);
        var contextType = RequireType("Sussudio.Services.Contracts.RecordingContext");
        var context = RuntimeHelpers.GetUninitializedObject(contextType);
        SetPropertyBackingField(context, "Settings", settings);
        SetPropertyBackingField(context, "UsePostMuxAudio", usePostMuxAudio);
        SetPropertyBackingField(context, "EffectiveFrameRate", 60.0);
        SetPropertyBackingField(context, "FrameRateArg", "60");
        SetPropertyBackingField(context, "EffectiveWidth", 1920u);
        SetPropertyBackingField(context, "EffectiveHeight", 1080u);
        SetPropertyBackingField(context, "VideoInputPixelFormat", "nv12");
        SetPropertyBackingField(context, "VideoOutputPath", videoPath ?? "/tmp/video.mp4");
        SetPropertyBackingField(context, "FinalOutputPath", finalPath ?? "/tmp/final.mp4");
        SetPropertyBackingField(context, "AudioTempPath", audioTempPath);
        SetPropertyBackingField(context, "HdrPipelineActive", false);
        return context;
    }

    private static void SetPropertyBackingField(object instance, string propertyName, object? value)
    {
        var field = instance.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null)
        {
            field.SetValue(instance, value);
            return;
        }

        SetPropertyOrBackingField(instance, propertyName, value);
    }

    private static int GetCountProperty(object? collection)
    {
        if (collection == null)
            throw new InvalidOperationException("Collection is null");

        var countProp = collection.GetType().GetProperty("Count");
        if (countProp != null)
            return (int)(countProp.GetValue(collection) ?? 0);

        var iface = collection.GetType().GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>));
        if (iface != null)
        {
            var cp = iface.GetProperty("Count");
            return (int)(cp?.GetValue(collection) ?? 0);
        }

        throw new InvalidOperationException("No Count property found");
    }

    private static object BuildDevice(string id = "device-1")
    {
        var device = CreateInstance("Sussudio.Models.CaptureDevice");
        SetPropertyOrBackingField(device, "Id", id);
        SetPropertyOrBackingField(device, "Name", "Synthetic Capture Device");
        SetPropertyOrBackingField(device, "AudioDeviceId", "audio-1");
        SetPropertyOrBackingField(device, "AudioDeviceName", "Synthetic Audio");
        return device;
    }

    private static object BuildSettings(bool hdrEnabled)
    {
        var settings = CreateInstance("Sussudio.Models.CaptureSettings");
        SetPropertyOrBackingField(settings, "Width", 1920u);
        SetPropertyOrBackingField(settings, "Height", 1080u);
        SetPropertyOrBackingField(settings, "FrameRate", 60d);
        SetPropertyOrBackingField(settings, "RequestedFrameRateArg", "60/1");
        SetPropertyOrBackingField(settings, "RequestedFrameRateNumerator", 60u);
        SetPropertyOrBackingField(settings, "RequestedFrameRateDenominator", 1u);
        SetPropertyOrBackingField(settings, "RequestedPixelFormat", hdrEnabled ? "P010" : "NV12");
        SetPropertyOrBackingField(settings, "Format", ParseEnum("Sussudio.Models.RecordingFormat", "HevcMp4"));
        SetPropertyOrBackingField(settings, "Quality", ParseEnum("Sussudio.Models.VideoQuality", "High"));
        SetPropertyOrBackingField(settings, "HdrEnabled", hdrEnabled);
        SetPropertyOrBackingField(settings, "HdrOutputMode", ParseEnum("Sussudio.Models.HdrOutputMode", "Hdr10Pq"));
        SetPropertyOrBackingField(settings, "AudioEnabled", true);
        SetPropertyOrBackingField(settings, "OutputPath", Path.GetTempPath());
        return settings;
    }

    private static async Task InvokeInitializeAsync(object captureService, object device, object settings)
    {
        var initialize = captureService.GetType().GetMethod(
            "InitializeAsync",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: new[] { device.GetType(), settings.GetType(), typeof(CancellationToken) },
            modifiers: null);
        if (initialize == null)
        {
            throw new InvalidOperationException("CaptureService.InitializeAsync method not found.");
        }

        var task = initialize.Invoke(captureService, new[] { device, settings, CancellationToken.None }) as Task;
        if (task == null)
        {
            throw new InvalidOperationException("CaptureService.InitializeAsync did not return a Task.");
        }

        await task.ConfigureAwait(false);
    }

    private static async Task DisposeAsync(object captureService)
    {
        await DisposeValueTaskAsync(captureService).ConfigureAwait(false);
    }

    private static async Task DisposeValueTaskAsync(object instance)
    {
        var disposeAsync = instance.GetType().GetMethod("DisposeAsync", BindingFlags.Public | BindingFlags.Instance);
        if (disposeAsync == null)
        {
            return;
        }

        var valueTask = disposeAsync.Invoke(instance, null);
        if (valueTask == null)
        {
            return;
        }

        var asTaskMethod = valueTask.GetType().GetMethod("AsTask", BindingFlags.Public | BindingFlags.Instance);
        if (asTaskMethod?.Invoke(valueTask, null) is Task task)
        {
            await task.ConfigureAwait(false);
        }
    }

    private static async Task WaitForConditionAsync(Func<bool> condition, string description, int timeoutMs = 2000, int pollMs = 25)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(pollMs).ConfigureAwait(false);
        }

        throw new InvalidOperationException($"Timed out waiting for condition: {description}");
    }

    private static object CreateInstance(string typeName)
    {
        var type = RequireType(typeName);
        var instance = Activator.CreateInstance(type);
        if (instance == null)
        {
            throw new InvalidOperationException($"Failed to create instance of '{typeName}'.");
        }

        return instance;
    }

    private static object CreateUninitializedObject(Type type)
        => RuntimeHelpers.GetUninitializedObject(type);

    private static string GetRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Sussudio.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate repository root from '{AppContext.BaseDirectory}'.");
    }

    private static string ReadRepoFile(string relativePath)
        => File.ReadAllText(Path.Combine(GetRepoRoot(), relativePath));

    private static string ReadAutomationSnapshotFamilyText()
    {
        var files = new[]
        {
            "Sussudio/Models/Automation/AutomationSnapshot.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.UserSettings.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.Hdr.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.AudioIngest.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.Recording.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.CaptureFormat.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.SourceTelemetry.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.Preview.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.Mjpeg.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.SystemHealth.cs",
            "Sussudio/Models/Automation/AutomationSnapshot.Flashback.cs"
        };

        var parts = new List<string>();
        foreach (var file in files)
        {
            parts.Add(ReadRepoFile(file).Replace("\r\n", "\n"));
        }

        return string.Join("\n", parts);
    }

    private static Type RequireType(string typeName)
    {
        if (_assembly == null)
        {
            throw new InvalidOperationException("Target assembly is not loaded.");
        }

        var type = _assembly.GetType(typeName);
        if (type != null)
        {
            return type;
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(typeName);
            if (type != null)
            {
                return type;
            }
        }

        foreach (var reference in _assembly.GetReferencedAssemblies())
        {
            try
            {
                var referencedAssembly = Assembly.Load(reference);
                type = referencedAssembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }
            catch
            {
                // Keep the original missing-type error below; unrelated
                // platform references do not matter to this offline runner.
            }
        }

        throw new InvalidOperationException($"Type '{typeName}' not found in target assembly or referenced assemblies.");
    }

    private static object ParseEnum(string typeName, string value)
    {
        var type = RequireType(typeName);
        return Enum.Parse(type, value, ignoreCase: true);
    }

    private static object InvokeInstanceMethod(object instance, string methodName)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        if (method == null)
        {
            throw new InvalidOperationException($"Method '{methodName}' not found on '{instance.GetType().Name}'.");
        }

        return method.Invoke(instance, null)
               ?? throw new InvalidOperationException($"Method '{methodName}' returned null.");
    }

    private static object? InvokeNonPublicInstanceMethod(object instance, string methodName, object?[]? arguments)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
        {
            throw new InvalidOperationException($"Non-public method '{methodName}' not found on '{instance.GetType().Name}'.");
        }

        return method.Invoke(instance, arguments);
    }

    private static void SetPrivateField(object instance, string fieldName, object? value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new InvalidOperationException($"Missing private field '{fieldName}' on '{instance.GetType().Name}'.");
        }

        field.SetValue(instance, value);
    }

    private static void SeedPipelineStopFailureState(object pipeline, Type pipelineType)
    {
        SetPrivateField(pipeline, "_workQueue", CreateUnboundedChannelFieldValue(pipelineType, "_workQueue"));
        SetPrivateField(pipeline, "_workers", Array.Empty<Thread>());
        SetPrivateField(pipeline, "_decoders", CreateEmptyArrayFieldValue(pipelineType, "_decoders"));
        SetPrivateField(pipeline, "_reorderFrames", Activator.CreateInstance(typeof(SortedDictionary<,>).MakeGenericType(
            typeof(long),
            RequireType("Sussudio.Services.Gpu.ParallelMjpegDecodePipeline+DecodedFrame")))!);
        SetPrivateField(pipeline, "_knownMissingSequences", new SortedSet<long>());
        SetPrivateField(pipeline, "_reorderLock", new object());
        SetPrivateField(pipeline, "_emitSignal", new AutoResetEvent(false));
    }

    private static object CreateEmptyArrayFieldValue(Type declaringType, string fieldName)
    {
        var field = declaringType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing private field '{fieldName}' on '{declaringType.Name}'.");
        var elementType = field.FieldType.GetElementType()
            ?? throw new InvalidOperationException($"Field '{fieldName}' on '{declaringType.Name}' was not an array.");
        return Array.CreateInstance(elementType, 0);
    }

    private static object CreateUnboundedChannelFieldValue(Type declaringType, string fieldName)
    {
        var field = declaringType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing private field '{fieldName}' on '{declaringType.Name}'.");
        var itemType = field.FieldType.GetGenericArguments().SingleOrDefault()
            ?? throw new InvalidOperationException($"Field '{fieldName}' on '{declaringType.Name}' was not a generic channel.");
        var method = typeof(Channel).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(candidate =>
                candidate.Name == nameof(Channel.CreateUnbounded) &&
                candidate.IsGenericMethodDefinition &&
                candidate.GetParameters().Length == 0);
        return method.MakeGenericMethod(itemType).Invoke(null, null)
               ?? throw new InvalidOperationException($"Failed to create channel for '{fieldName}'.");
    }

    private static object CreateSizedArrayFieldValue(Type declaringType, string fieldName, int length)
    {
        var field = declaringType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing private field '{fieldName}' on '{declaringType.Name}'.");
        var elementType = field.FieldType.GetElementType()
            ?? throw new InvalidOperationException($"Field '{fieldName}' on '{declaringType.Name}' was not an array.");
        return Array.CreateInstance(elementType, length);
    }

    private static object CreateFieldInstance(Type declaringType, string fieldName)
    {
        var field = declaringType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing private field '{fieldName}' on '{declaringType.Name}'.");
        return Activator.CreateInstance(field.FieldType)
               ?? throw new InvalidOperationException($"Failed to create field instance for '{fieldName}'.");
    }

    private static object? GetPrivateField(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new InvalidOperationException($"Missing private field '{fieldName}' on '{instance.GetType().Name}'.");
        }

        return field.GetValue(instance);
    }

    private static void SetPropertyOrBackingField(object instance, string propertyName, object? value)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property?.SetMethod != null)
        {
            property.SetValue(instance, value);
            return;
        }

        var backingField = instance.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        if (backingField != null)
        {
            backingField.SetValue(instance, value);
            return;
        }

        throw new InvalidOperationException(
            $"Property '{propertyName}' is not writable and backing field was not found on '{instance.GetType().Name}'.");
    }

    private static string GetStringProperty(object instance, string propertyName)
    {
        var value = GetPropertyValue(instance, propertyName);
        return value?.ToString() ?? string.Empty;
    }

    private static long GetLongProperty(object instance, string propertyName)
    {
        var value = GetPropertyValue(instance, propertyName);
        return Convert.ToInt64(value);
    }

    private static int GetIntProperty(object instance, string propertyName)
    {
        var value = GetPropertyValue(instance, propertyName);
        return Convert.ToInt32(value);
    }

    private static double GetDoubleProperty(object instance, string propertyName)
    {
        var value = GetPropertyValue(instance, propertyName);
        return Convert.ToDouble(value);
    }

    private static bool GetBoolProperty(object instance, string propertyName)
    {
        var value = GetPropertyValue(instance, propertyName);
        return Convert.ToBoolean(value);
    }

    private static object? GetPropertyValue(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property == null)
        {
            throw new InvalidOperationException(
                $"Property '{propertyName}' not found on '{instance.GetType().Name}'.");
        }

        return property.GetValue(instance);
    }

    private static string NormalizeLineEndings(string value)
        => value.Replace("\r\n", "\n").Replace('\r', '\n');
}
