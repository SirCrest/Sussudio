using Xunit;

namespace Sussudio.Tests;

// Recovery tests redirect a process-wide path and must not overlap other tests.
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class RecoveryEnvironmentCollection
{
    public const string Name = "Recovery environment";
}
