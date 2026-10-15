using System.Reflection;
using System.Text.Json;
using Xunit;

namespace Sussudio.Tests;

public sealed class SettingsServiceTests
{
    [Fact]
    public void SaveAndLoadPreserveRequestedValuesAndNullablePreferences()
    {
        using var files = new SettingsFiles();
        var path = Path.Combine(files.DirectoryPath, "nested", "settings.json");
        var settings = CreateSettings(
            ("SelectedDeviceId", "capture-1"),
            ("OutputPath", "D:\\Clips\\雪"),
            ("SelectedRecordingFormat", "AV1"),
            ("SelectedPreset", "P7"),
            ("SelectedSplitEncodeMode", "Auto"),
            ("CustomBitrateMbps", 42.5d),
            ("IsHdrEnabled", true),
            ("IsAudioEnabled", false),
            ("SelectedDeviceAudioMode", "Analog"),
            ("FlashbackBufferMinutes", 7),
            ("SelectedVideoFormat", "3840x2160 P010"));

        Assert.True(Save(settings, path, out var failure), failure);
        Assert.Empty(failure);
        Assert.False(File.Exists(path + ".tmp"));
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        Assert.Equal("AV1", document.RootElement.GetProperty("SelectedRecordingFormat").GetString());
        Assert.True(document.RootElement.GetProperty("IsHdrEnabled").GetBoolean());
        Assert.False(document.RootElement.GetProperty("IsAudioEnabled").GetBoolean());

        var loaded = Load(path);
        AssertPreference(loaded, "SelectedDeviceId", "capture-1");
        AssertPreference(loaded, "OutputPath", "D:\\Clips\\雪");
        AssertPreference(loaded, "SelectedRecordingFormat", "AV1");
        AssertPreference(loaded, "SelectedPreset", "P7");
        AssertPreference(loaded, "SelectedSplitEncodeMode", "Auto");
        AssertPreference(loaded, "CustomBitrateMbps", 42.5d);
        AssertPreference(loaded, "IsHdrEnabled", true);
        AssertPreference(loaded, "IsAudioEnabled", false);
        AssertPreference(loaded, "IsAudioPreviewEnabled", null);
        AssertPreference(loaded, "SelectedDeviceAudioMode", "Analog");
        AssertPreference(loaded, "FlashbackBufferMinutes", 7);
        AssertPreference(loaded, "SelectedVideoFormat", "3840x2160 P010");
    }

    [Fact]
    public void ReplacementPublishesOnlyTheNewSettings()
    {
        using var files = new SettingsFiles();
        var path = files.SettingsPath;
        var original = CreateSettings(("SelectedRecordingFormat", "HEVC"), ("IsHdrEnabled", true), ("OutputPath", "old"));
        Assert.True(Save(original, path, out var initialFailure), initialFailure);

        var replacement = CreateSettings(("SelectedRecordingFormat", "AV1"), ("IsHdrEnabled", false));
        Assert.True(Save(replacement, path, out var failure), failure);

        var loaded = Load(path);
        AssertPreference(loaded, "SelectedRecordingFormat", "AV1");
        AssertPreference(loaded, "IsHdrEnabled", false);
        AssertPreference(loaded, "OutputPath", null);
        Assert.False(File.Exists(path + ".tmp"));
        Assert.Single(Directory.GetFiles(files.DirectoryPath));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("null")]
    [InlineData("{")]
    [InlineData("{\"IsHdrEnabled\":\"not-a-boolean\"}")]
    public void UnavailableOrUnreadableDocumentsReturnUnsetPreferences(string? content)
    {
        using var files = new SettingsFiles();
        if (content != null)
            File.WriteAllText(files.SettingsPath, content);

        var loaded = Load(files.SettingsPath);

        AssertUnsetPreferences(loaded);
        if (content == null)
            Assert.False(File.Exists(files.SettingsPath));
        else
            Assert.Equal(content, File.ReadAllText(files.SettingsPath));
        Assert.False(File.Exists(files.SettingsPath + ".tmp"));
    }

    [Fact]
    public void OlderDocumentsKeepOmittedSettingsUnsetAndIgnoreUnknownProperties()
    {
        using var files = new SettingsFiles();
        File.WriteAllText(files.SettingsPath,
            """{"SelectedRecordingFormat":"HEVC","IsAudioEnabled":false,"FuturePreference":{"enabled":true}}""");

        var loaded = Load(files.SettingsPath);

        AssertPreference(loaded, "SelectedRecordingFormat", "HEVC");
        AssertPreference(loaded, "IsAudioEnabled", false);
        AssertPreference(loaded, "IsHdrEnabled", null);
        AssertPreference(loaded, "FlashbackBufferMinutes", null);
        AssertPreference(loaded, "SelectedVideoFormat", null);
    }

    [Fact]
    public void ReadFailureUsesDefaultsWithoutChangingTheDocument()
    {
        using var files = new SettingsFiles();
        const string content = """{"SelectedRecordingFormat":"AV1","IsHdrEnabled":true}""";
        File.WriteAllText(files.SettingsPath, content);

        using (var exclusive = new FileStream(files.SettingsPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            AssertUnsetPreferences(Load(files.SettingsPath));

        Assert.Equal(content, File.ReadAllText(files.SettingsPath));
        AssertPreference(Load(files.SettingsPath), "IsHdrEnabled", true);
    }

    [Fact]
    public void FailedSaveKeepsTheLastGoodDocumentAndReportsTheCause()
    {
        using var files = new SettingsFiles();
        const string original = """{"SelectedRecordingFormat":"HEVC","IsHdrEnabled":true}""";
        File.WriteAllText(files.SettingsPath, original);
        Directory.CreateDirectory(files.SettingsPath + ".tmp");
        var replacement = CreateSettings(("SelectedRecordingFormat", "AV1"), ("IsHdrEnabled", false));

        Assert.False(Save(replacement, files.SettingsPath, out var failure));

        Assert.False(string.IsNullOrWhiteSpace(failure));
        Assert.Contains("Exception:", failure, StringComparison.Ordinal);
        Assert.Equal(original, File.ReadAllText(files.SettingsPath));
        AssertPreference(Load(files.SettingsPath), "SelectedRecordingFormat", "HEVC");
        Assert.True(Directory.Exists(files.SettingsPath + ".tmp"));
    }

    private static Type SettingsType => SussudioAssembly.Load().GetType("Sussudio.Services.Runtime.UserSettings", throwOnError: true)!;
    private static Type ServiceType => SussudioAssembly.Load().GetType("Sussudio.Services.Runtime.SettingsService", throwOnError: true)!;

    private static object CreateSettings(params (string Name, object Value)[] values)
    {
        var settings = Activator.CreateInstance(SettingsType)!;
        foreach (var (name, value) in values)
            SettingsType.GetProperty(name)!.SetValue(settings, value);
        return settings;
    }

    private static object Load(string path)
    {
        var load = ServiceType.GetMethod("LoadFromFile", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(load);
        return load.Invoke(null, new object[] { path })!;
    }

    private static bool Save(object settings, string path, out string failure)
    {
        var arguments = new object?[] { settings, path, null };
        var saved = (bool)ServiceType.GetMethod("SaveToFile", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, arguments)!;
        failure = Assert.IsType<string>(arguments[2]);
        return saved;
    }

    private static void AssertPreference(object settings, string name, object? expected)
        => Assert.Equal(expected, SettingsType.GetProperty(name)!.GetValue(settings));

    private static void AssertUnsetPreferences(object settings)
        => Assert.All(SettingsType.GetProperties(BindingFlags.Public | BindingFlags.Instance),
            property => Assert.Null(property.GetValue(settings)));

    private sealed class SettingsFiles : IDisposable
    {
        internal SettingsFiles()
        {
            DirectoryPath = Path.Combine(Path.GetTempPath(), $"sussudio-settings-behavior-{Guid.NewGuid():N}");
            Directory.CreateDirectory(DirectoryPath);
        }

        internal string DirectoryPath { get; }
        internal string SettingsPath => Path.Combine(DirectoryPath, "settings.json");
        public void Dispose() => Directory.Delete(DirectoryPath, recursive: true);
    }
}
