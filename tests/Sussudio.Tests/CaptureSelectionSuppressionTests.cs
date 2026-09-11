using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace Sussudio.Tests;

public sealed class CaptureSelectionSuppressionTests
{
    public CaptureSelectionSuppressionTests()
        => global::Program.EnsureTargetAssemblyLoadedForXUnit();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SelectionMutationRestoresIncomingSuppression(bool initiallySuppressed)
    {
        var owner = new SelectionOwner(initiallySuppressed);
        var applied = false;

        owner.Apply(() =>
        {
            Assert.True(owner.IsSuppressed);
            applied = true;
        });

        Assert.True(applied);
        Assert.Equal(initiallySuppressed, owner.IsSuppressed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedMutationRestoresIncomingSuppression(bool initiallySuppressed)
    {
        var owner = new SelectionOwner(initiallySuppressed);
        var failure = new InvalidOperationException("selection failed");

        var observed = Assert.Throws<InvalidOperationException>(() => owner.Apply(() =>
        {
            Assert.True(owner.IsSuppressed);
            throw failure;
        }));

        Assert.Same(failure, observed);
        Assert.Equal(initiallySuppressed, owner.IsSuppressed);
    }

    [Fact]
    public void NestedSelectionKeepsOuterSuppressionUntilOuterCompletes()
    {
        var owner = new SelectionOwner(initiallySuppressed: false);

        owner.Apply(() =>
        {
            owner.Apply(() => Assert.True(owner.IsSuppressed));
            Assert.True(owner.IsSuppressed);
        });

        Assert.False(owner.IsSuppressed);
    }

    [Fact]
    public void CaughtNestedFailureKeepsOuterSelectionSuppressed()
    {
        var owner = new SelectionOwner(initiallySuppressed: false);

        owner.Apply(() =>
        {
            Assert.Throws<InvalidOperationException>(() => owner.Apply(() =>
                throw new InvalidOperationException("inner selection failed")));
            Assert.True(owner.IsSuppressed);

            owner.Apply(() => Assert.True(owner.IsSuppressed));
            Assert.True(owner.IsSuppressed);
        });

        Assert.False(owner.IsSuppressed);
    }

    private sealed class SelectionOwner
    {
        private readonly object _viewModel;
        private readonly FieldInfo _suppression;

        public SelectionOwner(bool initiallySuppressed)
        {
            var type = SussudioAssembly.Load().GetType("Sussudio.ViewModels.MainViewModel", throwOnError: true)!;
            _viewModel = RuntimeHelpers.GetUninitializedObject(type);
            _suppression = type.GetField("_suppressFormatChangeReinitialize", BindingFlags.Instance | BindingFlags.NonPublic)!;
            _suppression.SetValue(_viewModel, initiallySuppressed);
            Apply = type.GetMethod("ApplyCaptureSelectionWithoutReinitialize", BindingFlags.Instance | BindingFlags.NonPublic)!
                .CreateDelegate<Action<Action>>(_viewModel);
        }

        public Action<Action> Apply { get; }
        public bool IsSuppressed => (bool)_suppression.GetValue(_viewModel)!;
    }
}
