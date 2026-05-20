using System.Threading.Tasks;

// Tests that prevent app service code from drifting into stale namespaces.
static partial class Program
{
    internal static Task ServiceNamespaces_FollowServiceFolders()
    {
        var repoRoot = GetRepoRoot();
        AssertServiceNamespaceFolderRules(repoRoot);
        AssertServiceNamespaceNativeXuProbeOwnership(repoRoot);
        AssertServiceNamespaceSourceOwnership(repoRoot);
        AssertServiceContractsBoundaryOwnership(repoRoot);

        return Task.CompletedTask;
    }
}
