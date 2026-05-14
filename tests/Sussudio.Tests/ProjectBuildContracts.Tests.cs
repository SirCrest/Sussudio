using System.Threading.Tasks;

static partial class Program
{
    private static Task ProjectFile_PreservesEnglishOnlyPublishLocalePolicy()
    {
        var projectText = ReadRepoFile("Sussudio/Sussudio.csproj").Replace("\r\n", "\n");
        AssertContains(projectText, "<SatelliteResourceLanguages>en-US</SatelliteResourceLanguages>");
        AssertContains(projectText, "AfterTargets=\"Build;Publish\"");
        AssertContains(projectText, "$_.Name.ToLowerInvariant() -ne 'en-us'");
        AssertContains(projectText, "'$(PublishDir)' != ''");
        AssertContains(projectText, "^[A-Za-z]{2,3}(-[A-Za-z]+)+$");
        return Task.CompletedTask;
    }
}
