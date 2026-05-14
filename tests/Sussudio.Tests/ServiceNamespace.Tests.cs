using System.Threading.Tasks;

// Tests that prevent app service code from drifting into stale namespaces.
static partial class Program
{
    private static Task ServiceNamespaces_FollowServiceFolders()
    {
        var repoRoot = GetRepoRoot();
        AssertServiceNamespaceFolderRules(repoRoot);
        AssertServiceNamespaceNativeXuProbeOwnership(repoRoot);
        AssertServiceNamespaceSourceOwnership(repoRoot);

        return Task.CompletedTask;
    }
}
