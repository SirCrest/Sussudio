using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;

static partial class Program
{
    private enum ConfigSetterExpectation
    {
        Set,
        InitOnly,
        None
    }

    private enum ConfigNullability
    {
        NotApplicable,
        NotNull,
        Nullable
    }

    private enum ConfigPropertyScope
    {
        Instance,
        Static
    }

    private sealed record ConfigPropertySpec(
        string Name,
        Type Type,
        ConfigSetterExpectation Setter,
        ConfigNullability Nullability = ConfigNullability.NotApplicable,
        ConfigNullability ElementNullability = ConfigNullability.NotApplicable,
        ConfigPropertyScope Scope = ConfigPropertyScope.Instance,
        bool IsRequired = false);

    private static ConfigPropertySpec ConfigProperty(
        string name,
        Type type,
        ConfigSetterExpectation setter,
        ConfigNullability Nullability = ConfigNullability.NotApplicable,
        ConfigNullability ElementNullability = ConfigNullability.NotApplicable,
        ConfigPropertyScope scope = ConfigPropertyScope.Instance,
        bool isRequired = false)
        => new(name, type, setter, Nullability, ElementNullability, scope, isRequired);

    private static ConfigPropertySpec ConfigString(
        string name,
        ConfigSetterExpectation setter,
        ConfigNullability nullability)
        => ConfigProperty(name, typeof(string), setter, nullability);

    private static ConfigPropertySpec RequiredConfigString(
        string name,
        ConfigSetterExpectation setter)
        => ConfigProperty(name, typeof(string), setter, ConfigNullability.NotNull, isRequired: true);

    private static ConfigPropertySpec RequiredConfigProperty(
        string name,
        Type type,
        ConfigSetterExpectation setter)
        => ConfigProperty(name, type, setter, isRequired: true);

    private static void AssertDeclaredConfigProperties(Type type, ConfigPropertySpec[] expectedProperties)
    {
        var instanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;
        var staticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly;
        var actualNames = type.GetProperties(instanceFlags | staticFlags)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var expectedNames = expectedProperties
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        if (!actualNames.SequenceEqual(expectedNames))
        {
            throw new InvalidOperationException(
                $"{type.Name} public property set changed. Expected: {string.Join(", ", expectedNames)}; actual: {string.Join(", ", actualNames)}.");
        }

        foreach (var expected in expectedProperties)
        {
            var flags = expected.Scope == ConfigPropertyScope.Static ? staticFlags : instanceFlags;
            var property = type.GetProperty(expected.Name, flags)
                ?? throw new InvalidOperationException($"{type.Name}.{expected.Name} was not found.");
            AssertEqual(expected.Type, property.PropertyType, $"{type.Name}.{expected.Name} property type");
            AssertEqual(
                expected.IsRequired,
                property.GetCustomAttribute<RequiredMemberAttribute>() != null,
                $"{type.Name}.{expected.Name} required-member metadata");
            if (property.GetMethod == null || !property.GetMethod.IsPublic)
            {
                throw new InvalidOperationException($"{type.Name}.{expected.Name} must expose a public getter.");
            }

            if (expected.Setter == ConfigSetterExpectation.None)
            {
                if (property.SetMethod != null)
                {
                    throw new InvalidOperationException($"{type.Name}.{expected.Name} must not expose a setter.");
                }
            }
            else
            {
                if (property.SetMethod == null || !property.SetMethod.IsPublic)
                {
                    throw new InvalidOperationException($"{type.Name}.{expected.Name} must expose a public setter.");
                }

                var isInitOnly = IsInitOnlySetter(property);
                AssertEqual(
                    expected.Setter == ConfigSetterExpectation.InitOnly,
                    isInitOnly,
                    $"{type.Name}.{expected.Name} init-only setter");
            }

            if (expected.Nullability != ConfigNullability.NotApplicable)
            {
                var nullability = new NullabilityInfoContext().Create(property);
                var expectedState = expected.Nullability == ConfigNullability.Nullable
                    ? NullabilityState.Nullable
                    : NullabilityState.NotNull;
                AssertEqual(expectedState, nullability.ReadState, $"{type.Name}.{expected.Name} read nullability");
                if (expected.Setter != ConfigSetterExpectation.None)
                {
                    AssertEqual(expectedState, nullability.WriteState, $"{type.Name}.{expected.Name} write nullability");
                }

                if (expected.ElementNullability != ConfigNullability.NotApplicable)
                {
                    var elementNullability = property.PropertyType.IsArray
                        ? nullability.ElementType
                        : nullability.GenericTypeArguments.FirstOrDefault();
                    if (elementNullability == null)
                    {
                        throw new InvalidOperationException($"{type.Name}.{expected.Name} did not expose element nullability.");
                    }

                    var expectedElementState = expected.ElementNullability == ConfigNullability.Nullable
                        ? NullabilityState.Nullable
                        : NullabilityState.NotNull;
                    AssertEqual(expectedElementState, elementNullability.ReadState, $"{type.Name}.{expected.Name} element read nullability");
                    AssertEqual(expectedElementState, elementNullability.WriteState, $"{type.Name}.{expected.Name} element write nullability");
                }
            }
        }
    }

    private static bool IsInitOnlySetter(PropertyInfo property)
        => property.SetMethod?.ReturnParameter.GetRequiredCustomModifiers()
            .Any(modifier => modifier.FullName == "System.Runtime.CompilerServices.IsExternalInit") == true;

    private static void AssertEqual<T>(T expected, T actual, string fieldName)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"Assertion failed for {fieldName}: expected '{expected}', actual '{actual}'.");
        }
    }

    private static void AssertNearlyEqual(double expected, double actual, double tolerance, string fieldName)
    {
        if (Math.Abs(expected - actual) > tolerance)
        {
            throw new InvalidOperationException(
                $"Assertion failed for {fieldName}: expected '{expected}', actual '{actual}', tolerance '{tolerance}'.");
        }
    }

    private static void AssertContains(string value, string token)
    {
        var normalizedValue = NormalizeLineEndings(value);
        var normalizedToken = NormalizeLineEndings(token);
        if (normalizedValue.IndexOf(normalizedToken, StringComparison.OrdinalIgnoreCase) < 0)
        {
            throw new InvalidOperationException(
                $"Assertion failed: expected '{value}' to contain '{token}'.");
        }
    }

    private static void AssertDoesNotContain(string value, string token)
    {
        var normalizedValue = NormalizeLineEndings(value);
        var normalizedToken = NormalizeLineEndings(token);
        if (normalizedValue.IndexOf(normalizedToken, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            throw new InvalidOperationException(
                $"Assertion failed: expected value not to contain '{token}'.");
        }
    }

    private static void AssertNotNull(object? value, string fieldName)
    {
        if (value == null)
        {
            throw new InvalidOperationException($"Assertion failed for {fieldName}: value was null.");
        }
    }

    private static void AssertSequenceEqual<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual, string fieldName)
    {
        if (!expected.SequenceEqual(actual))
        {
            throw new InvalidOperationException(
                $"Assertion failed for {fieldName}. Expected: {string.Join(", ", expected)}; actual: {string.Join(", ", actual)}.");
        }
    }

    private static void AssertEnumValues(Type enumType, params (string Name, int Value)[] expectedValues)
    {
        AssertEqual(expectedValues.Length, Enum.GetNames(enumType).Length, $"{enumType.Name} value count");
        foreach (var (name, value) in expectedValues)
        {
            var parsed = Enum.Parse(enumType, name);
            AssertEqual(value, Convert.ToInt32(parsed), $"{enumType.Name}.{name}");
        }
    }

    private static string NormalizeLineEndings(string value)
        => value.Replace("\r\n", "\n").Replace('\r', '\n');

    private static HashSet<string> ExtractSnapshotFields(string sourceText)
    {
        var fields = new HashSet<string>(StringComparer.Ordinal);
        foreach (var callPrefix in new[]
        {
            "Get(snapshot,",
            "GetInt(snapshot,",
            "GetDouble(snapshot,",
            "GetLong(snapshot,",
            "GetNullableLong(snapshot,",
            "GetBool(snapshot,",
            "GetString(snapshot,",
            "FormatFrameBudgetMs(snapshot,",
            "FormatIntervalMs(snapshot,"
        })
        {
            ExtractSnapshotFieldsFromCalls(sourceText, callPrefix, fields);
        }

        return fields;
    }

    private static void ExtractSnapshotFieldsFromCalls(string sourceText, string callPrefix, HashSet<string> fields)
    {
        var index = 0;
        while (index < sourceText.Length)
        {
            var callIdx = sourceText.IndexOf(callPrefix, index, StringComparison.Ordinal);
            if (callIdx < 0)
                break;

            var afterComma = callIdx + callPrefix.Length;
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
    }

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

    private static string ReadFlashbackPlaybackControllerSource()
    {
        var parts = new[]
        {
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.Positioning.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.CommandQueue.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.AudioRouting.cs").Replace("\r\n", "\n")
        };

        return string.Join("\n", parts);
    }

    private static string ReadFlashbackEncoderSinkSource()
    {
        var parts = new[]
        {
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.EncodingLoop.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.ForceRotate.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Queueing.cs").Replace("\r\n", "\n")
        };

        return string.Join("\n", parts);
    }

    private static string ReadFlashbackExporterSource()
    {
        var parts = new[]
        {
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Lifecycle.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.Execution.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SingleFilePacketReadLoop.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs").Replace("\r\n", "\n")
        };

        return string.Join("\n", parts);
    }

    private static string ReadFlashbackDecoderSource()
    {
        var parts = new[]
        {
            // Keep playback before video output so source-shape checks still see
            // inline audio delivery before the frame-conversion section marker.
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.Playback.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.VideoSetup.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.VideoOutput.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs").Replace("\r\n", "\n")
        };

        return string.Join("\n", parts);
    }

    private static string ReadFlashbackBufferManagerSource()
    {
        var parts = new[]
        {
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.Segments.cs").Replace("\r\n", "\n")
        };

        return string.Join("\n", parts);
    }

    private static (int, int) GetTupleValues(object tuple)
    {
        var item1 = tuple.GetType().GetField("Item1")?.GetValue(tuple);
        var item2 = tuple.GetType().GetField("Item2")?.GetValue(tuple);
        return (Convert.ToInt32(item1), Convert.ToInt32(item2));
    }

    private static (int?, int?) GetNullableTupleValues(object tuple)
    {
        var item1 = tuple.GetType().GetField("Item1")?.GetValue(tuple);
        var item2 = tuple.GetType().GetField("Item2")?.GetValue(tuple);
        return (item1 == null ? null : Convert.ToInt32(item1), item2 == null ? null : Convert.ToInt32(item2));
    }

    private static object CreateInitializedBufferManager(string tempDir)
    {
        var optionsType = RequireType("Sussudio.Models.FlashbackBufferOptions");
        var options = RuntimeHelpers.GetUninitializedObject(optionsType);
        SetPropertyBackingField(options, "BufferDuration", TimeSpan.FromMinutes(5));
        SetPropertyBackingField(options, "TempDirectory", tempDir);
        SetPropertyBackingField(options, "SegmentDuration", TimeSpan.FromMinutes(10));

        var managerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var manager = RuntimeHelpers.GetUninitializedObject(managerType);
        SetPrivateField(manager, "_options", options);
        SetPrivateField(manager, "_indexLock", new object());
        SetPrivateField(manager, "_sessionId", "test-session");
        SetPrivateField(manager, "_sessionDirectory", tempDir);
        SetPrivateField(manager, "_activeSegmentPath", Path.Combine(tempDir, "fb_test_0003.ts"));
        SetPrivateField(manager, "_activeSegmentStartPtsTicks", -1L);
        SetPrivateField(manager, "_nextSegmentIndex", 4);

        var listField = managerType.GetField("_completedSegments", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var list = listField.GetValue(manager);
        if (list == null)
        {
            var completedSegmentType = managerType.GetNestedType("CompletedSegment", BindingFlags.NonPublic)!;
            var listGenericType = typeof(List<>).MakeGenericType(completedSegmentType);
            list = Activator.CreateInstance(listGenericType)!;
            listField.SetValue(manager, list);
        }

        return manager;
    }

    private static void AddCompletedSegment(object manager, string path, TimeSpan startPts, TimeSpan endPts, long sizeBytes)
    {
        var managerType = manager.GetType();
        var completedSegmentType = managerType.GetNestedType("CompletedSegment", BindingFlags.NonPublic)!;
        var listField = managerType.GetField("_completedSegments", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var list = listField.GetValue(manager)!;
        var addMethod = list.GetType().GetMethod("Add")!;

        var countProperty = list.GetType().GetProperty("Count")!;
        var sequenceNumber = (int)countProperty.GetValue(list)!;

        var segment = Activator.CreateInstance(completedSegmentType, path, sequenceNumber, startPts, endPts, sizeBytes)!;
        addMethod.Invoke(list, new[] { segment });
    }

    private static void WriteSizedFile(string path, int byteCount)
    {
        File.WriteAllBytes(path, Enumerable.Repeat((byte)0x47, byteCount).ToArray());
    }

    private static void SeedCommandFailure(object controller, string failure)
        => InvokeNonPublicInstanceMethod(controller, "SetLastCommandFailure", new object[] { failure });

    private static bool ValidateFinalOutputFailureAfterMove(string outputPath, out long outputBytes, out string failureMessage)
    {
        if (outputPath.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
        {
            outputBytes = new FileInfo(outputPath).Length;
            failureMessage = string.Empty;
            return outputBytes > 0;
        }

        outputBytes = -1;
        failureMessage = $"forced final validation failure for '{outputPath}'";
        return false;
    }

    private static Dictionary<string, string> ReadMainViewModelCodeFiles()
    {
        return Directory
            .GetFiles(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels"), "MainViewModel*.cs")
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                path => Path.GetFileName(path),
                path => StripCSharpCommentsAndStringContents(File.ReadAllText(path).Replace("\r\n", "\n")),
                StringComparer.OrdinalIgnoreCase);
    }

    private static string ReadRepoCodeWithoutCommentsOrStrings(string relativePath)
        => StripCSharpCommentsAndStringContents(ReadRepoFile(relativePath).Replace("\r\n", "\n"));

    private static void AssertMemberContains(string source, string memberName, string token)
        => AssertContains(ExtractMemberCode(source, memberName), token);

    private static void AssertMemberDoesNotContain(string source, string memberName, string token)
        => AssertDoesNotContain(ExtractMemberCode(source, memberName), token);

    private static string ExtractMemberCode(string source, string memberName)
    {
        var match = Regex.Match(
            source,
            @"(?m)^\s*(?:(?:public|private|protected|internal|static|async|partial|override|virtual|sealed)\s+)*(?:[\w<>,\?\[\]\.]+\s+)+" +
            Regex.Escape(memberName) +
            @"\s*\(");
        if (!match.Success)
        {
            throw new InvalidOperationException($"Member '{memberName}' was not found.");
        }

        var openBrace = source.IndexOf('{', match.Index);
        var arrow = source.IndexOf("=>", match.Index, StringComparison.Ordinal);
        var semicolon = source.IndexOf(';', match.Index);
        if (arrow >= 0 && semicolon >= 0 && (openBrace < 0 || arrow < openBrace))
        {
            return source.Substring(match.Index, semicolon - match.Index + 1);
        }

        if (openBrace < 0)
        {
            throw new InvalidOperationException($"Member '{memberName}' has no body.");
        }

        var closeBrace = FindMatchingBrace(source, openBrace);
        return source.Substring(match.Index, closeBrace - match.Index + 1);
    }

    private static string ExtractTextBetween(string source, string startToken, string endToken)
    {
        var start = source.IndexOf(startToken, StringComparison.Ordinal);
        if (start < 0)
        {
            throw new InvalidOperationException($"Start token '{startToken}' was not found.");
        }

        var end = source.IndexOf(endToken, start + startToken.Length, StringComparison.Ordinal);
        if (end < 0)
        {
            throw new InvalidOperationException($"End token '{endToken}' was not found after '{startToken}'.");
        }

        return source.Substring(start, end - start);
    }

    private static int FindMatchingBrace(string source, int openBraceIndex)
    {
        var depth = 0;
        for (var i = openBraceIndex; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
            }
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return i;
                }
            }
        }

        throw new InvalidOperationException("Matching brace was not found.");
    }

    private static void AssertRegex(string value, string pattern, string fieldName)
    {
        if (!Regex.IsMatch(value, pattern, RegexOptions.Singleline))
        {
            throw new InvalidOperationException(
                $"Assertion failed for {fieldName}: pattern '{pattern}' was not found.");
        }
    }

    private static void AssertNoRegex(string value, string pattern, string fieldName)
    {
        if (Regex.IsMatch(value, pattern, RegexOptions.Singleline))
        {
            throw new InvalidOperationException(
                $"Assertion failed for {fieldName}: forbidden pattern '{pattern}' was found.");
        }
    }

    private static void AssertOccursBefore(string value, string earlierToken, string laterToken)
    {
        var normalizedValue = NormalizeLineEndings(value);
        var normalizedEarlierToken = NormalizeLineEndings(earlierToken);
        var normalizedLaterToken = NormalizeLineEndings(laterToken);
        var earlier = normalizedValue.IndexOf(normalizedEarlierToken, StringComparison.Ordinal);
        var later = normalizedValue.IndexOf(normalizedLaterToken, StringComparison.Ordinal);
        if (earlier < 0 || later < 0 || earlier >= later)
        {
            throw new InvalidOperationException(
                $"Assertion failed: expected '{earlierToken}' to occur before '{laterToken}'.");
        }
    }

    private static string StripCSharpCommentsAndStringContents(string source)
    {
        var builder = new StringBuilder(source.Length);
        for (var i = 0; i < source.Length; i++)
        {
            var current = source[i];
            var next = i + 1 < source.Length ? source[i + 1] : '\0';

            if (current == '/' && next == '/')
            {
                builder.Append(' ');
                builder.Append(' ');
                i += 2;
                while (i < source.Length && source[i] != '\n')
                {
                    builder.Append(' ');
                    i++;
                }
                if (i < source.Length)
                {
                    builder.Append('\n');
                }
                continue;
            }

            if (current == '/' && next == '*')
            {
                builder.Append(' ');
                builder.Append(' ');
                i += 2;
                while (i < source.Length)
                {
                    if (source[i] == '*' && i + 1 < source.Length && source[i + 1] == '/')
                    {
                        builder.Append(' ');
                        builder.Append(' ');
                        i++;
                        break;
                    }

                    builder.Append(source[i] == '\n' ? '\n' : ' ');
                    i++;
                }
                continue;
            }

            if (current == '"')
            {
                var verbatim = i > 0 && source[i - 1] == '@';
                builder.Append('"');
                i++;
                while (i < source.Length)
                {
                    if (!verbatim && source[i] == '\\' && i + 1 < source.Length)
                    {
                        builder.Append(' ');
                        builder.Append(' ');
                        i += 2;
                        continue;
                    }

                    if (source[i] == '"')
                    {
                        builder.Append('"');
                        if (verbatim && i + 1 < source.Length && source[i + 1] == '"')
                        {
                            builder.Append('"');
                            i += 2;
                            continue;
                        }
                        break;
                    }

                    builder.Append(source[i] == '\n' ? '\n' : ' ');
                    i++;
                }
                continue;
            }

            if (current == '\'')
            {
                builder.Append('\'');
                i++;
                while (i < source.Length)
                {
                    if (source[i] == '\\' && i + 1 < source.Length)
                    {
                        builder.Append(' ');
                        builder.Append(' ');
                        i += 2;
                        continue;
                    }

                    if (source[i] == '\'')
                    {
                        builder.Append('\'');
                        break;
                    }

                    builder.Append(source[i] == '\n' ? '\n' : ' ');
                    i++;
                }
                continue;
            }

            builder.Append(current);
        }

        return builder.ToString();
    }

    private static string ReadRepoFile(string relativePath)
        => File.ReadAllText(Path.Combine(GetRepoRoot(), relativePath));

    private static string ReadMainWindowCompositionSource()
        => ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");

    private static string ReadMainWindowCaptureSelectionBindingsAdapterSource()
        => ReadMainWindowAdapterSource("Sussudio/MainWindow.ControlBindings.cs");

    private static string ReadMainWindowFlashbackAdapterSource()
        => ReadMainWindowAdapterSource("Sussudio/MainWindow.Flashback.Interactions.cs");

    private static string ReadMainWindowPreviewRendererAdapterSource()
        => ReadMainWindowAdapterSource("Sussudio/MainWindow.PreviewLifecycle.Composition.cs");

    private static string ReadMainWindowPreviewStartupAdapterSource()
        => ReadMainWindowAdapterSource("Sussudio/MainWindow.PreviewLifecycle.Composition.cs");

    private static string ReadMainWindowPreviewTransitionsAdapterSource()
        => ReadMainWindowAdapterSource("Sussudio/MainWindow.PreviewLifecycle.Composition.cs");

    private static string ReadMainWindowPropertyChangedPreviewAdapterSource()
        => ReadMainWindowAdapterSource("Sussudio/MainWindow.PreviewLifecycle.Composition.cs");

    private static string ReadMainWindowShellChromeAdapterSource()
        => ReadMainWindowAdapterSource("Sussudio/MainWindow.ShellChrome.Composition.cs");

    private static string ReadMainWindowAdapterSource(params string[] files)
        => string.Join(
            "\n",
            files.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));

    private static string ReadAutomationSnapshotFamilyText()
    {
        return ReadRepoFile("Sussudio/Models/Automation/AutomationSnapshot.cs")
            .Replace("\r\n", "\n");
    }

    private static string ReadAutomationSnapshotFlatteningFamilyText()
        => string.Join(
            "\n",
            ReadAutomationSnapshotFlatteningOrchestrationText(),
            ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.AutomationSnapshot.cs"))
            .Replace("\r\n", "\n");

    private static string ReadAutomationSnapshotFlatteningOrchestrationText()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        const string startToken = "private static AutomationSnapshotFlattenedProjectionSet BuildAutomationSnapshotFlattenedProjectionSet(";
        const string endToken = "private SnapshotStatusProjection BuildSnapshotStatusProjection(";
        var startIndex = snapshotProjectionText.IndexOf(startToken, StringComparison.Ordinal);
        var endIndex = snapshotProjectionText.IndexOf(endToken, StringComparison.Ordinal);
        if (startIndex < 0 || endIndex <= startIndex)
        {
            throw new InvalidOperationException("Unable to locate automation snapshot flattening orchestration in the root snapshot projection file.");
        }

        return snapshotProjectionText[startIndex..endIndex];
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

    private static object CreateConfigInstance(Type type)
        => Activator.CreateInstance(type, nonPublic: true)
           ?? throw new InvalidOperationException($"Failed to create {type.Name}.");

    private static object CreateResolutionFormatDictionary(Type mediaFormatType)
        => Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(
               typeof(string),
               typeof(List<>).MakeGenericType(mediaFormatType)))
           ?? throw new InvalidOperationException("Failed to create resolution format dictionary.");

    private static void AddResolutionFormats(
        object formatsByResolution,
        Type mediaFormatType,
        string resolutionKey,
        params object[] formats)
        => ((IDictionary)formatsByResolution).Add(
            resolutionKey,
            CreateMediaFormatList(mediaFormatType, formats));

    private static object CreateMediaFormatList(Type mediaFormatType, params object[] formats)
    {
        var list = (IList)(Activator.CreateInstance(typeof(List<>).MakeGenericType(mediaFormatType))
                           ?? throw new InvalidOperationException("Failed to create media format list."));
        foreach (var format in formats)
        {
            list.Add(format);
        }

        return list;
    }

    private static object CreateTestMediaFormat(
        Type mediaFormatType,
        uint width,
        uint height,
        double frameRate,
        string pixelFormat,
        bool isHdr)
    {
        var format = CreateConfigInstance(mediaFormatType);
        SetPropertyOrBackingField(format, "Width", width);
        SetPropertyOrBackingField(format, "Height", height);
        SetPropertyOrBackingField(format, "FrameRate", frameRate);
        SetPropertyOrBackingField(format, "PixelFormat", pixelFormat);
        SetPropertyOrBackingField(format, "IsHdr", isHdr);
        return format;
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
                var assemblyDirectory = Path.GetDirectoryName(_assembly.Location);
                var referencePath = assemblyDirectory == null
                    ? null
                    : Path.Combine(assemblyDirectory, reference.Name + ".dll");
                if (referencePath == null || !File.Exists(referencePath))
                {
                    continue;
                }

                var referencedAssembly = Assembly.LoadFrom(referencePath);
                type = referencedAssembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
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

    private static readonly Dictionary<string, Assembly> ToolAssemblyCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Assembly> IsolatedToolAssemblyCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, AssemblyLoadContext> IsolatedToolAssemblyContexts = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object ToolAssemblyCacheLock = new();

    private static Assembly LoadToolAssembly(string relativeAssemblyPath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(GetRepoRoot(), relativeAssemblyPath));
        lock (ToolAssemblyCacheLock)
        {
            if (ToolAssemblyCache.TryGetValue(fullPath, out var cached))
            {
                return cached;
            }

            RequireFreshToolAssembly(relativeAssemblyPath, fullPath);
            var assemblyDirectory = Path.GetDirectoryName(fullPath)
                                    ?? throw new InvalidOperationException($"Tool assembly directory not found for '{fullPath}'.");

            Assembly? ResolveToolAssemblyDependency(AssemblyLoadContext context, AssemblyName assemblyName)
            {
                var dependencyPath = Path.Combine(assemblyDirectory, $"{assemblyName.Name}.dll");
                return File.Exists(dependencyPath)
                    ? context.LoadFromAssemblyPath(dependencyPath)
                    : null;
            }

            AssemblyLoadContext.Default.Resolving += ResolveToolAssemblyDependency;
            try
            {
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(fullPath);
                ToolAssemblyCache[fullPath] = assembly;
                return assembly;
            }
            finally
            {
                AssemblyLoadContext.Default.Resolving -= ResolveToolAssemblyDependency;
            }
        }
    }

    private static Assembly LoadToolAssemblyIsolated(string relativeAssemblyPath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(GetRepoRoot(), relativeAssemblyPath));
        lock (ToolAssemblyCacheLock)
        {
            if (IsolatedToolAssemblyCache.TryGetValue(fullPath, out var cached))
            {
                return cached;
            }

            RequireFreshToolAssembly(relativeAssemblyPath, fullPath);
            var loadContext = new ToolAssemblyLoadContext(fullPath);
            var assembly = loadContext.LoadFromAssemblyPath(fullPath);
            IsolatedToolAssemblyCache[fullPath] = assembly;
            IsolatedToolAssemblyContexts[fullPath] = loadContext;
            return assembly;
        }
    }

    private sealed class ToolAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public ToolAssemblyLoadContext(string mainAssemblyToLoadPath)
            : base(isCollectible: false)
        {
            _resolver = new AssemblyDependencyResolver(mainAssemblyToLoadPath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
        }
    }

    private static void RequireFreshToolAssembly(string relativeAssemblyPath, string fullPath)
    {
        if (!File.Exists(fullPath))
        {
            throw new InvalidOperationException(
                $"Required tool assembly was not found: {relativeAssemblyPath}. Build it first with: {GetToolBuildCommand(relativeAssemblyPath)}");
        }

        var assemblyWriteTime = File.GetLastWriteTimeUtc(fullPath);
        var newestInputWriteTime = GetNewestToolInputWriteTimeUtc(relativeAssemblyPath);
        if (newestInputWriteTime > assemblyWriteTime)
        {
            throw new InvalidOperationException(
                $"Required tool assembly is stale: {relativeAssemblyPath}. Build it again with: {GetToolBuildCommand(relativeAssemblyPath)}");
        }
    }

    private static DateTime GetNewestToolInputWriteTimeUtc(string relativeAssemblyPath)
    {
        var root = GetRepoRoot();
        var projectDirectory = GetToolProjectDirectory(relativeAssemblyPath);
        var inputDirectories = EnumerateToolInputDirectories(projectDirectory)
            .Concat(UsesCommonToolSources(projectDirectory)
                ? EnumerateToolInputDirectories(Path.Combine(root, "tools", "Common"))
                : Array.Empty<string>())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var inputFiles = inputDirectories
            .SelectMany(Directory.EnumerateFiles)
            .Concat(EnumerateToolProjectCompileIncludes(projectDirectory))
            .Concat(Directory.EnumerateFiles(Path.Combine(root, "Sussudio.Automation.Contracts"), "*.cs"))
            .Where(file => File.Exists(file) && IsToolInputFile(file))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var newest = DateTime.MinValue;
        foreach (var file in inputFiles)
        {
            var writeTime = File.GetLastWriteTimeUtc(file);
            if (writeTime > newest)
            {
                newest = writeTime;
            }
        }

        foreach (var directory in inputDirectories)
        {
            var writeTime = Directory.GetLastWriteTimeUtc(directory);
            if (writeTime > newest)
            {
                newest = writeTime;
            }
        }

        return newest;
    }

    private static IEnumerable<string> EnumerateToolProjectCompileIncludes(string projectDirectory)
    {
        foreach (var projectFile in Directory.EnumerateFiles(projectDirectory, "*.csproj"))
        {
            XDocument project;
            try
            {
                project = XDocument.Load(projectFile);
            }
            catch
            {
                continue;
            }

            var projectFileDirectory = Path.GetDirectoryName(projectFile)
                                       ?? throw new InvalidOperationException($"Project directory not found for '{projectFile}'.");
            foreach (var include in project.Descendants()
                         .Where(element => string.Equals(element.Name.LocalName, "Compile", StringComparison.OrdinalIgnoreCase))
                         .Select(element => element.Attribute("Include")?.Value)
                         .Where(value => !string.IsNullOrWhiteSpace(value)))
            {
                var expanded = include!.Replace('\\', Path.DirectorySeparatorChar);
                if (expanded.Contains('*'))
                {
                    continue;
                }

                yield return Path.GetFullPath(Path.Combine(projectFileDirectory, expanded));
            }
        }
    }

    private static bool UsesCommonToolSources(string projectDirectory)
    {
        foreach (var projectFile in Directory.EnumerateFiles(projectDirectory, "*.csproj"))
        {
            XDocument project;
            try
            {
                project = XDocument.Load(projectFile);
            }
            catch
            {
                continue;
            }

            foreach (var include in project.Descendants()
                         .Where(element => string.Equals(element.Name.LocalName, "Compile", StringComparison.OrdinalIgnoreCase))
                         .Select(element => element.Attribute("Include")?.Value)
                         .Where(value => !string.IsNullOrWhiteSpace(value)))
            {
                var normalized = include!.Replace('\\', '/');
                if (normalized.Contains("../Common/", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static string GetToolProjectDirectory(string relativeAssemblyPath)
    {
        var root = GetRepoRoot();
        var normalized = relativeAssemblyPath.Replace('\\', '/');
        if (normalized.StartsWith("tools/ssctl/", StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(root, "tools", "ssctl");
        }

        if (normalized.StartsWith("tools/McpServer/", StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(root, "tools", "McpServer");
        }

        if (normalized.StartsWith("tools/AutomationClient/", StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(root, "tools", "AutomationClient");
        }

        if (normalized.StartsWith("tools/NativeXuAudioProbe/", StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(root, "tools", "NativeXuAudioProbe");
        }

        throw new InvalidOperationException($"No tool project mapping is configured for '{relativeAssemblyPath}'.");
    }

    private static string GetToolBuildCommand(string relativeAssemblyPath)
    {
        var normalized = relativeAssemblyPath.Replace('\\', '/');
        if (normalized.StartsWith("tools/ssctl/", StringComparison.OrdinalIgnoreCase))
        {
            return "dotnet build tools/ssctl/ssctl.csproj -c Debug --no-restore";
        }

        if (normalized.StartsWith("tools/McpServer/", StringComparison.OrdinalIgnoreCase))
        {
            return "dotnet build tools/McpServer/McpServer.csproj -c Debug --no-restore";
        }

        if (normalized.StartsWith("tools/AutomationClient/", StringComparison.OrdinalIgnoreCase))
        {
            return "dotnet build tools/AutomationClient/AutomationClient.csproj -c Debug --no-restore";
        }

        if (normalized.StartsWith("tools/NativeXuAudioProbe/", StringComparison.OrdinalIgnoreCase))
        {
            return "dotnet build tools/NativeXuAudioProbe/NativeXuAudioProbe.csproj -c Debug --no-restore";
        }

        return "dotnet build";
    }

    private static bool IsToolInputFile(string file)
    {
        var extension = Path.GetExtension(file);
        return extension.Equals(".cs", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".props", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".targets", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> EnumerateToolInputDirectories(string directory)
    {
        if (!Directory.Exists(directory))
        {
            yield break;
        }

        yield return directory;
        foreach (var childDirectory in Directory.EnumerateDirectories(directory))
        {
            var name = Path.GetFileName(childDirectory);
            if (name.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("obj", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var nestedDirectory in EnumerateToolInputDirectories(childDirectory))
            {
                yield return nestedDirectory;
            }
        }
    }
}

namespace Sussudio.Tests
{
    internal static class MainWindowCompositionSource
    {
        public static string Read()
            => RuntimeContractSource.ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
    }

    internal static class MainWindowStatsOverlaySource
    {
        public static string Read()
            => RuntimeContractSource.ReadRepoFile("Sussudio/MainWindow.ShellChrome.Composition.cs").Replace("\r\n", "\n");
    }
}
