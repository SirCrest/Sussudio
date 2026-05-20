using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewHarnessRegistrationXunitTests
{
    [Fact]
    public Task PresentationPreviewHarnessRegistrationCoversUiOwnershipChecks()
        => global::Program.PresentationPreviewHarnessRegistration_CoversUiOwnershipChecks();
}
