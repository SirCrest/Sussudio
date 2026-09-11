using Xunit;

namespace Sussudio.Tests;

// Tests that change process-wide paths or environment variables must run alone.
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class RecoveryEnvironmentCollection
{
    public const string Name = "Recovery environment";
}
