using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Xunit;

namespace Sussudio.Tests;

// The MCP tools for the agent senses (listen_audio_output, get_ui_tree, invoke_ui_element) return failed calls WITH their
// payload: the candidate paths of an ambiguous target, the analysis of a silent capture. The query tools deliberately do
// not (their failures must not look like data), so the sense tools use their own factory method.
//
// Failure modes, written before the tests:
//   1. The failed response's payload is dropped (the pre-existing FromStructuredResponse behaviour), so the agent sees
//      "ambiguous" with nothing to disambiguate with.
//   2. The failure is returned as a success because it carries data, so the agent proceeds as if the call worked.
//   3. A failed response with no usable payload (validation error, null, array, scalar) returns empty structured content
//      or throws, instead of a plain error with the message and error code.
//   4. A successful response changes behaviour relative to the query tools (error flag, wrapper, missing-data message).
//   5. The result references the response document, so reading it after the document was disposed fails.
public sealed class McpFailureDataResultTests
{
    private const string Payload = "{\"Status\":\"ambiguous\",\"Candidates\":[{\"Path\":\"Grid/Button[2]\"}],\"CandidateTotal\":5}";

    private static (bool IsError, string? Structured, string Text) Call(string responseJson, string propertyName = "Data", string missing = "missing-data-message")
    {
        var assembly = ToolFormatterTestAssembly.Load(global::Program.McpServerAssemblyRelativePath);
        var factoryType = assembly.GetType("McpServer.Tools.McpToolResultFactory")
            ?? throw new InvalidOperationException("McpToolResultFactory not found.");
        var method = factoryType.GetMethod("FromStructuredResponseKeepingFailureData", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FromStructuredResponseKeepingFailureData not found.");

        // The MCP host resolves its own System.Text.Json, so the JsonElement must be built from the type the method
        // expects (the same approach as the existing structured-query contract test).
        var documentType = method.GetParameters()[0].ParameterType.Assembly.GetType("System.Text.Json.JsonDocument")
            ?? throw new InvalidOperationException("JsonDocument not found in the MCP host's System.Text.Json.");
        var parse = documentType.GetMethods().Single(candidate => candidate.Name == "Parse" &&
            candidate.GetParameters().Length == 2 && candidate.GetParameters()[0].ParameterType == typeof(string));
        object result;
        using (var document = (IDisposable)parse.Invoke(null, new[]
        {
            (object)responseJson,
            Activator.CreateInstance(parse.GetParameters()[1].ParameterType)!
        })!)
        {
            var root = documentType.GetProperty("RootElement")!.GetValue(document)!;
            result = method.Invoke(null, new[] { root, propertyName, missing })!;
        }

        // Read only after the source document is disposed: the result must own its data.
        var type = result.GetType();
        var isError = (bool?)type.GetProperty("IsError")!.GetValue(result) == true;
        var structured = type.GetProperty("StructuredContent")!.GetValue(result)?.ToString();
        var block = ((IEnumerable)type.GetProperty("Content")!.GetValue(result)!).Cast<object>().Single();
        var text = (string)block.GetType().GetProperty("Text")!.GetValue(block)!;
        return (isError, structured, text);
    }

    [Fact]
    public void FailedResponseWithData_StaysAnError_AndKeepsThePayloadAsStructuredContent()
    {
        var (isError, structured, text) = Call(
            "{\"Success\":false,\"Message\":\"'Save' matches 5 elements.\",\"ErrorCode\":\"InvalidRequest\",\"Data\":" + Payload + "}");

        Assert.True(isError);
        Assert.NotNull(structured);
        Assert.True(System.Text.Json.Nodes.JsonNode.DeepEquals(
            System.Text.Json.Nodes.JsonNode.Parse(Payload), System.Text.Json.Nodes.JsonNode.Parse(structured!)));
        Assert.Contains("'Save' matches 5 elements.", text, StringComparison.Ordinal);
        Assert.Contains("ErrorCode: InvalidRequest", text, StringComparison.Ordinal);
        Assert.Contains("Grid/Button[2]", text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData(",\"Data\":null")]
    [InlineData(",\"Data\":[]")]
    [InlineData(",\"Data\":\"text\"")]
    [InlineData(",\"Data\":42")]
    public void FailedResponseWithoutAnObjectPayload_IsAPlainErrorWithMessageAndCode(string dataMember)
    {
        var (isError, structured, text) = Call(
            "{\"Success\":false,\"Message\":\"seconds must be between 0.5 and 30.\",\"ErrorCode\":\"InvalidRequest\"" + dataMember + "}");

        Assert.True(isError);
        Assert.Null(structured);
        Assert.Contains("seconds must be between 0.5 and 30.", text, StringComparison.Ordinal);
        Assert.Contains("InvalidRequest", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SuccessfulResponse_ReturnsThePayloadUnwrapped_AndNotAsAnError()
    {
        var (isError, structured, text) = Call("{\"Success\":true,\"Message\":\"ok\",\"Data\":" + Payload + "}");

        Assert.False(isError);
        Assert.NotNull(structured);
        Assert.True(System.Text.Json.Nodes.JsonNode.DeepEquals(
            System.Text.Json.Nodes.JsonNode.Parse(Payload), System.Text.Json.Nodes.JsonNode.Parse(structured!)));
        Assert.True(System.Text.Json.Nodes.JsonNode.DeepEquals(
            System.Text.Json.Nodes.JsonNode.Parse(Payload), System.Text.Json.Nodes.JsonNode.Parse(text)));
    }

    [Fact]
    public void SuccessfulResponseWithoutData_IsAnErrorNamingTheMissingPayload()
    {
        var (isError, structured, text) = Call("{\"Success\":true,\"Message\":\"ok\"}");

        Assert.True(isError);
        Assert.Null(structured);
        Assert.Contains("missing-data-message", text, StringComparison.Ordinal);
    }
}
