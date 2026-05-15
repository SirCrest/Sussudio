using System.Text.Json;
using System.Threading.Tasks;

static partial class Program
{
    private static Task ResponseFormatter_IsSuccess_ParsesSuccessAndFailureJson()
    {
        var formatterType = RequireSharedToolType("Sussudio.Tools.AutomationSnapshotFormatter");
        var isSuccess = RequireNonPublicStaticMethod(formatterType, "IsSuccess");

        using (var docTrue = JsonDocument.Parse("{\"Success\": true, \"Message\": \"ok\"}"))
        {
            AssertEqual(true, (bool)isSuccess.Invoke(null, new object[] { docTrue.RootElement })!, "IsSuccess with Success=true");
        }

        using (var docFalse = JsonDocument.Parse("{\"Success\": false, \"Message\": \"fail\"}"))
        {
            AssertEqual(false, (bool)isSuccess.Invoke(null, new object[] { docFalse.RootElement })!, "IsSuccess with Success=false");
        }

        using (var docMissing = JsonDocument.Parse("{\"Message\": \"no success field\"}"))
        {
            AssertEqual(false, (bool)isSuccess.Invoke(null, new object[] { docMissing.RootElement })!, "IsSuccess with missing Success property");
        }

        return Task.CompletedTask;
    }

    private static Task ResponseFormatter_Get_HandlesAllJsonValueKinds()
    {
        var formatterType = RequireSharedToolType("Sussudio.Tools.AutomationSnapshotFormatter");
        var get = RequireNonPublicStaticMethod(formatterType, "Get");

        var json = @"{
            ""str"": ""hello"",
            ""num"": 42,
            ""boolTrue"": true,
            ""boolFalse"": false,
            ""nullVal"": null,
            ""emptyArr"": [],
            ""nonEmptyArr"": [1, 2],
            ""obj"": { ""nested"": true },
            ""emptyStr"": """"
        }";

        using var doc = JsonDocument.Parse(json);
        var el = doc.RootElement;

        AssertEqual("hello", (string)get.Invoke(null, new object[] { el, "str", "N/A" })!, "Get string value");
        AssertEqual("42", (string)get.Invoke(null, new object[] { el, "num", "N/A" })!, "Get number value");
        AssertEqual("true", (string)get.Invoke(null, new object[] { el, "boolTrue", "N/A" })!, "Get bool true");
        AssertEqual("false", (string)get.Invoke(null, new object[] { el, "boolFalse", "N/A" })!, "Get bool false");
        AssertEqual("N/A", (string)get.Invoke(null, new object[] { el, "nullVal", "N/A" })!, "Get null value");
        AssertEqual("N/A", (string)get.Invoke(null, new object[] { el, "nonExistent", "N/A" })!, "Get missing property");
        AssertEqual("custom", (string)get.Invoke(null, new object[] { el, "nonExistent", "custom" })!, "Get missing with custom fallback");
        AssertEqual("N/A", (string)get.Invoke(null, new object[] { el, "emptyArr", "N/A" })!, "Get empty array");
        AssertEqual("", (string)get.Invoke(null, new object[] { el, "emptyStr", "N/A" })!, "Get empty string");

        return Task.CompletedTask;
    }
}
