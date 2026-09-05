using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sussudio.Models;
using Sussudio.Tools;
using Xunit;

namespace Sussudio.Tests;

/// <summary>
/// Executes <see cref="AutomationCommandCatalog"/> and the wire-contract helpers on
/// <see cref="AutomationPipeProtocol"/> directly — Sussudio.Automation.Contracts is
/// project-referenced, so no reflection is involved.
///
/// These cover the parts of the contract that existing suites do not: the numeric ID
/// table that must never be renumbered, the manifest-revision rule that is currently
/// only a comment, and the executing behaviour of ValidatePath.
/// </summary>
public sealed class AutomationCommandCatalogTests
{
    // The numeric IDs are serialized on the wire. AutomationCommandKind's maintainer
    // rules say: append only, never renumber, never reuse a freed value — because a
    // stale ssctl/MCP/StreamDeck client would otherwise silently misroute commands
    // (e.g. SetRecordingFormat -> SetQuality) and corrupt user recording profiles.
    // Nothing enforced that until this table existed.
    private static readonly IReadOnlyDictionary<int, string> FrozenCommandIds = new Dictionary<int, string>
    {
        [0] = "Authenticate",
        [1] = "GetSnapshot",
        [2] = "GetDiagnostics",
        [3] = "RefreshDevices",
        [4] = "SelectDevice",
        [5] = "SelectAudioInputDevice",
        [6] = "SetCustomAudioInput",
        [7] = "SetResolution",
        [8] = "SetFrameRate",
        [9] = "SetRecordingFormat",
        [10] = "SetQuality",
        [11] = "SetCustomBitrate",
        [12] = "SetHdrEnabled",
        [13] = "SetAudioEnabled",
        [14] = "SetAudioPreviewEnabled",
        [15] = "SetOutputPath",
        [16] = "SetPreviewEnabled",
        [17] = "SetRecordingEnabled",
        [18] = "ArmClose",
        [19] = "WindowAction",
        [20] = "WaitForCondition",
        [21] = "VerifyLastRecording",
        [22] = "AssertSnapshot",
        [23] = "SetTrueHdrPreviewEnabled",
        [24] = "ProbeVideoSource",
        [25] = "ProbePreviewColor",
        [26] = "CapturePreviewFrame",
        [27] = "CaptureWindowScreenshot",
        [28] = "SetVideoFormat",
        [29] = "GetCaptureOptions",
        [30] = "SetPreset",
        [31] = "SetSplitEncodeMode",
        [32] = "SetMjpegDecoderCount",
        [33] = "SetShowAllCaptureOptions",
        [34] = "SetPreviewVolume",
        [35] = "SetStatsVisible",
        [36] = "SetDeviceAudioMode",
        [37] = "GetPerformanceTimeline",
        [38] = "SetStatsSectionVisible",
        [39] = "SetAnalogAudioGain",
        [40] = "SetSettingsVisible",
        [41] = "FlashbackAction",
        [42] = "FlashbackExport",
        [43] = "FlashbackGetSegments",
        [44] = "VerifyFile",
        [45] = "RestartFlashback",
        [46] = "SetMicrophoneEnabled",
        [47] = "SetFlashbackEnabled",
        [48] = "GetAudioRampTrace",
        [49] = "SetFrameTimeOverlayVisible",
        [50] = "SetFlashbackTimelineVisible",
        [51] = "GetAutomationManifest",
        [52] = "SetFullScreenEnabled",
        [53] = "OpenRecordingsFolder",
        [54] = "SelectMicrophoneDevice",
        [55] = "SetMicrophoneVolume",
        [56] = "SetFlashbackBufferMinutes",
        [57] = "SetFlashbackGpuDecode",
    };

    // ── Wire-contract stability ──────────────────────────────────────────────

    [Fact]
    public void ExistingCommandIdsAreNeverRenumbered()
    {
        foreach (var (id, name) in FrozenCommandIds)
        {
            Assert.True(
                Enum.IsDefined(typeof(AutomationCommandKind), id),
                $"Command id {id} ({name}) disappeared. Freed ids must be reserved, never removed or reused — "
                + "old clients still encode them and would be routed to the wrong handler.");

            Assert.Equal(name, ((AutomationCommandKind)id).ToString());
        }
    }

    [Fact]
    public void NewCommandsAreAppendedAndForceAManifestRevisionBump()
    {
        var actual = Enum.GetValues<AutomationCommandKind>()
            .ToDictionary(kind => (int)kind, kind => kind.ToString());

        var added = actual.Keys.Except(FrozenCommandIds.Keys).OrderBy(id => id).ToArray();

        // Enforces maintainer rule 4, which until now lived only in a comment: any
        // change to the member set must bump AutomationPipeProtocol.CommandManifestRevision,
        // which the server uses to reject mismatched clients before they dispatch.
        Assert.True(
            added.Length == 0,
            $"AutomationCommandKind gained member(s) {string.Join(", ", added.Select(id => $"{id}={actual[id]}"))}. "
            + "Bump AutomationPipeProtocol.CommandManifestRevision by exactly +1 and extend FrozenCommandIds "
            + "in this test with the same id/name pairs.");

        if (added.Length == 0)
        {
            Assert.Equal(FrozenCommandIds.Count, actual.Count);
            Assert.Equal(2, AutomationPipeProtocol.CommandManifestRevision);
        }
    }

    [Fact]
    public void NewMembersAreAppendedAtTheEndRatherThanInsertedInTheMiddle()
    {
        var ids = Enum.GetValues<AutomationCommandKind>().Select(kind => (int)kind).OrderBy(id => id).ToArray();

        Assert.Equal(0, ids.First());
        Assert.Equal(ids.Length - 1, ids.Last());
        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void EntriesAreOrderedByNumericIdSoTheManifestIsStable()
    {
        var ids = AutomationCommandCatalog.Entries.Select(entry => (int)entry.Kind).ToArray();

        Assert.Equal(ids.OrderBy(id => id).ToArray(), ids);
    }

    [Fact]
    public void CommandMapRoundTripsEveryKindBetweenNameAndValue()
    {
        foreach (var kind in Enum.GetValues<AutomationCommandKind>())
        {
            Assert.True(AutomationPipeProtocol.TryGetCommandValue(kind.ToString(), out var value));
            Assert.Equal((int)kind, value);

            Assert.True(AutomationPipeProtocol.TryGetCommandName((int)kind, out var name));
            Assert.Equal(kind.ToString(), name);
        }
    }

    // ── Name resolution ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("SetRecordingEnabled")]
    [InlineData("setrecordingenabled")]
    [InlineData("SETRECORDINGENABLED")]
    [InlineData("set-recording-enabled")]
    [InlineData("set_recording_enabled")]
    [InlineData("set.recording.enabled")]
    [InlineData("17")]
    public void TryGetResolvesEveryCallerSpelling(string spelling)
    {
        // ssctl uses kebab-case, MCP uses snake_case, and the raw client sends
        // numeric ids. All three must land on the same catalog entry.
        Assert.True(AutomationCommandCatalog.TryGet(spelling, out var metadata));
        Assert.Equal(AutomationCommandKind.SetRecordingEnabled, metadata.Kind);
        Assert.Equal("SetRecordingEnabled", metadata.Name);
    }

    [Theory]
    [InlineData("not-a-command")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("SetRecording")]
    [InlineData("9999")]
    [InlineData("-1")]
    public void TryGetRejectsUnknownCommandNames(string spelling)
    {
        Assert.False(AutomationCommandCatalog.TryGet(spelling, out var metadata));
        Assert.Null(metadata);
    }

    [Fact]
    public void GetThrowsForAnUndefinedKind()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AutomationCommandCatalog.Get((AutomationCommandKind)9999));
    }

    [Fact]
    public void ResolveCommandAcceptsUndefinedNumericIdsUnchanged()
    {
        // Documents current behaviour rather than endorsing it: a numeric string is
        // returned verbatim without checking that the id is defined, so validation of
        // unknown numeric commands is the server's responsibility, not the client's.
        Assert.Equal(9999, AutomationPipeProtocol.ResolveCommand("9999"));
        Assert.Throws<ArgumentException>(() => AutomationPipeProtocol.ResolveCommand("nope"));
    }

    // ── Timeout policy ───────────────────────────────────────────────────────

    [Fact]
    public void LongRunningCommandsKeepTheirExtendedTimeouts()
    {
        // These three are the ones that genuinely outrun the default 15s budget;
        // regressing them to the default reintroduces spurious client timeouts.
        Assert.Equal(
            AutomationPipeProtocol.RecordingResponseTimeoutMs,
            AutomationCommandCatalog.Get(AutomationCommandKind.SetRecordingEnabled).ResponseTimeoutMs);
        Assert.Equal(
            AutomationPipeProtocol.FlashbackMutationResponseTimeoutMs,
            AutomationCommandCatalog.Get(AutomationCommandKind.FlashbackExport).ResponseTimeoutMs);
        Assert.Equal(
            AutomationPipeProtocol.ExtendedResponseTimeoutMs,
            AutomationCommandCatalog.Get(AutomationCommandKind.VerifyFile).ResponseTimeoutMs);
    }

    [Fact]
    public void EveryCommandHasAPositiveTimeout()
    {
        foreach (var entry in AutomationCommandCatalog.Entries)
        {
            Assert.True(entry.ResponseTimeoutMs > 0, $"{entry.Name} has a non-positive response timeout");
        }
    }

    [Fact]
    public void DefaultResponseTimeoutLookupCanonicalisesAliasSpellings()
    {
        var expected = AutomationCommandCatalog.Get(AutomationCommandKind.SetRecordingEnabled).ResponseTimeoutMs;

        Assert.Equal(expected, AutomationPipeProtocol.GetDefaultResponseTimeout("SetRecordingEnabled"));
        Assert.Equal(expected, AutomationPipeProtocol.GetDefaultResponseTimeout("set-recording-enabled"));
        Assert.Equal(expected, AutomationPipeProtocol.GetDefaultResponseTimeout("17"));
        Assert.Equal(expected, AutomationPipeProtocol.GetDefaultResponseTimeout(AutomationCommandKind.SetRecordingEnabled));
    }

    [Fact]
    public void UnknownCommandNamesFallBackToTheDefaultTimeout()
    {
        Assert.Equal(
            AutomationPipeProtocol.DefaultResponseTimeoutMs,
            AutomationPipeProtocol.GetDefaultResponseTimeout("no-such-command"));
    }

    // ── ValidatePath, executed rather than inspected ─────────────────────────

    [Fact]
    public void ValidatePathIsAPassThroughForCommandsWithNoPathPolicy()
    {
        Assert.Equal(
            AutomationCommandPathPolicy.None,
            AutomationCommandCatalog.Get(AutomationCommandKind.GetSnapshot).PathPolicy);

        // No policy means no validation at all, so even an empty value is returned.
        Assert.Equal("", AutomationCommandCatalog.ValidatePath(AutomationCommandKind.GetSnapshot, "anything", ""));
        Assert.Equal("junk", AutomationCommandCatalog.ValidatePath(AutomationCommandKind.GetSnapshot, "anything", "junk"));
    }

    [Theory]
    [InlineData(AutomationCommandKind.SetOutputPath, "outputPath")]
    [InlineData(AutomationCommandKind.FlashbackExport, "outputPath")]
    [InlineData(AutomationCommandKind.VerifyFile, "filePath")]
    public void ValidatePathRejectsBlankPathsForPathBearingCommands(AutomationCommandKind kind, string payloadKey)
    {
        Assert.Throws<InvalidOperationException>(() => AutomationCommandCatalog.ValidatePath(kind, payloadKey, ""));
        Assert.Throws<InvalidOperationException>(() => AutomationCommandCatalog.ValidatePath(kind, payloadKey, "   "));
        Assert.Throws<InvalidOperationException>(() => AutomationCommandCatalog.ValidatePath(kind, payloadKey, null!));
    }

    [Fact]
    public void ValidatePathCreatesTheParentDirectoryForWriteFileCommands()
    {
        var root = CreateScratchDirectory();
        try
        {
            var target = Path.Combine(root, "nested", "deeper", "clip.mp4");
            Assert.False(Directory.Exists(Path.GetDirectoryName(target)!));

            var returned = AutomationCommandCatalog.ValidatePath(
                AutomationCommandKind.FlashbackExport, "outputPath", target);

            // The destination directory is prepared so the export cannot fail late,
            // but the file itself must NOT be created — FlashbackExport refuses an
            // existing destination.
            Assert.True(Directory.Exists(Path.GetDirectoryName(target)!));
            Assert.False(File.Exists(target));
            Assert.Equal(target, returned);
        }
        finally
        {
            TryDelete(root);
        }
    }

    [Fact]
    public void ValidatePathCreatesTheDirectoryItselfForDirectoryCommands()
    {
        var root = CreateScratchDirectory();
        try
        {
            var target = Path.Combine(root, "recordings", "session");

            var returned = AutomationCommandCatalog.ValidatePath(
                AutomationCommandKind.SetOutputPath, "outputPath", target);

            Assert.True(Directory.Exists(target));
            Assert.Equal(target, returned);
        }
        finally
        {
            TryDelete(root);
        }
    }

    [Fact]
    public void ValidatePathRequiresAnExistingFileForReadFileCommands()
    {
        var root = CreateScratchDirectory();
        try
        {
            var missing = Path.Combine(root, "absent.mp4");
            var ex = Assert.Throws<InvalidOperationException>(
                () => AutomationCommandCatalog.ValidatePath(AutomationCommandKind.VerifyFile, "filePath", missing));
            Assert.Contains("VerifyFile", ex.Message, StringComparison.Ordinal);

            var present = Path.Combine(root, "present.mp4");
            File.WriteAllText(present, "not really an mp4");

            Assert.Equal(
                present,
                AutomationCommandCatalog.ValidatePath(AutomationCommandKind.VerifyFile, "filePath", present));
        }
        finally
        {
            TryDelete(root);
        }
    }

    [Fact]
    public void ValidatePathReturnsTheCallerSpellingRatherThanTheResolvedFullPath()
    {
        var root = CreateScratchDirectory();
        var previous = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(root);
            const string relative = "relative-output";

            var returned = AutomationCommandCatalog.ValidatePath(
                AutomationCommandKind.SetOutputPath, "outputPath", relative);

            // Callers persist and echo back exactly what they passed in; silently
            // rewriting it to an absolute path would change stored user settings.
            Assert.Equal(relative, returned);
            Assert.True(Directory.Exists(Path.Combine(root, relative)));
        }
        finally
        {
            Directory.SetCurrentDirectory(previous);
            TryDelete(root);
        }
    }

    [Fact]
    public void ValidatePathWrapsMalformedPathsAsInvalidOperation()
    {
        // Callers catch InvalidOperationException; letting ArgumentException or
        // IOException escape would bypass their error reporting.
        var invalid = "\0:<>|";

        var ex = Record.Exception(
            () => AutomationCommandCatalog.ValidatePath(AutomationCommandKind.SetOutputPath, "outputPath", invalid));

        Assert.IsType<InvalidOperationException>(ex);
    }

    [Fact]
    public void PathBearingCommandsDeclareTheirPathFieldAsAStringPayloadField()
    {
        var pathCommands = AutomationCommandCatalog.Entries
            .Where(entry => entry.PathPolicy != AutomationCommandPathPolicy.None)
            .ToArray();

        Assert.NotEmpty(pathCommands);

        foreach (var entry in pathCommands)
        {
            Assert.Contains(
                entry.PayloadFields,
                field => field.Type == AutomationPayloadFieldType.String
                         && (field.Name == "outputPath" || field.Name == "filePath"));
        }
    }

    // ── Manifest ─────────────────────────────────────────────────────────────

    [Fact]
    public void ManifestMirrorsTheCatalogEntryForEveryCommand()
    {
        var manifest = AutomationCommandCatalog.CreateManifest();

        Assert.Equal(1, manifest.SchemaVersion);
        Assert.Equal(AutomationCommandCatalog.Entries.Count, manifest.Commands.Count);

        foreach (var entry in AutomationCommandCatalog.Entries)
        {
            var command = manifest.Commands.Single(candidate => candidate.Id == (int)entry.Kind);

            Assert.Equal(entry.Name, command.Name);
            Assert.Equal(entry.PayloadShape, command.PayloadShape);
            Assert.Equal(entry.ResponseTimeoutMs, command.ResponseTimeoutMs);
            Assert.Equal(entry.RequiresReadyDevices, command.RequiresReadyDevices);
            Assert.Equal(entry.PathPolicy.ToString(), command.PathPolicy);
            Assert.Equal(entry.PayloadFields.Count, command.PayloadFields.Count);
        }
    }

    [Fact]
    public void ManifestJsonIsSerialisableAndCarriesCommandNames()
    {
        var json = AutomationCommandCatalog.CreateManifestJson();

        Assert.False(string.IsNullOrWhiteSpace(json));
        Assert.Contains("SetRecordingEnabled", json, StringComparison.Ordinal);
        Assert.Contains("FlashbackExport", json, StringComparison.Ordinal);
    }

    // ── Request envelope ─────────────────────────────────────────────────────

    [Fact]
    public void RequestEnvelopeStampsTheManifestRevisionSoTheServerCanRejectStaleClients()
    {
        var envelope = AutomationPipeProtocol.CreateRequestEnvelope(
            (int)AutomationCommandKind.GetSnapshot, authToken: "explicit");

        Assert.Equal((int)AutomationCommandKind.GetSnapshot, envelope["command"]);
        Assert.Equal(AutomationPipeProtocol.CommandManifestRevision, envelope["manifestRevision"]);
        Assert.Equal("explicit", envelope["authToken"]);
        Assert.NotNull(envelope["payload"]);
    }

    [Fact]
    public void EachRequestEnvelopeGetsAFreshCorrelationId()
    {
        var first = (string)AutomationPipeProtocol.CreateRequestEnvelope(1, authToken: "t")["correlationId"]!;
        var second = (string)AutomationPipeProtocol.CreateRequestEnvelope(1, authToken: "t")["correlationId"]!;

        Assert.NotEqual(first, second);
        // "N" format: 32 hex digits, no dashes — responses are matched on this.
        Assert.Equal(32, first.Length);
        Assert.All(first, c => Assert.True(Uri.IsHexDigit(c), $"'{c}' is not a hex digit"));
    }

    // ── Security fallback policy ─────────────────────────────────────────────

    [Fact]
    public void DefaultSecurityFallbackIsOnlyDisabledOnWindowsWithoutUsableSecurityAndWithoutAToken()
    {
        // Exhaustive over all 16 combinations rather than the sampled cases
        // elsewhere: this gate decides whether an unauthenticated pipe is exposed.
        foreach (var isWindows in new[] { false, true })
        foreach (var hasExplicit in new[] { false, true })
        foreach (var explicitFailed in new[] { false, true })
        foreach (var tokenRequired in new[] { false, true })
        {
            var expected = isWindows && (!hasExplicit || explicitFailed) && !tokenRequired;

            Assert.Equal(
                expected,
                AutomationPipeSecurityPolicy.ShouldDisableDefaultSecurityFallback(
                    isWindows, hasExplicit, explicitFailed, tokenRequired));
        }
    }

    [Fact]
    public void RequiringAnAuthTokenAlwaysKeepsTheDefaultSecurityFallback()
    {
        // The single most important row of the table above, stated on its own.
        foreach (var hasExplicit in new[] { false, true })
        foreach (var explicitFailed in new[] { false, true })
        {
            Assert.False(
                AutomationPipeSecurityPolicy.ShouldDisableDefaultSecurityFallback(
                    isWindows: true,
                    hasExplicitSecurityDescriptor: hasExplicit,
                    explicitSecurityFailed: explicitFailed,
                    authTokenRequired: true));
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string CreateScratchDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "sussudio-catalog-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
            // Scratch cleanup only; a leaked temp directory must not fail the test.
        }
        catch (UnauthorizedAccessException)
        {
            // Same.
        }
    }
}
