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

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ModeOptionsRestoreIncomingStateWithoutChangingReinitializeSuppression(bool rebuilding, bool suppressed)
    {
        var owner = new SelectionOwner(suppressed, rebuilding);

        owner.ApplyModeOptions(() =>
        {
            Assert.True(owner.IsRebuilding);
            Assert.Equal(suppressed, owner.IsSuppressed);
        });

        Assert.Equal(rebuilding, owner.IsRebuilding);
        Assert.Equal(suppressed, owner.IsSuppressed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedModeOptionsRestoreIncomingState(bool rebuilding)
    {
        var owner = new SelectionOwner(initiallySuppressed: true, initiallyRebuilding: rebuilding);
        var failure = new InvalidOperationException("mode options failed");

        var observed = Assert.Throws<InvalidOperationException>(() => owner.ApplyModeOptions(() =>
        {
            Assert.True(owner.IsRebuilding);
            throw failure;
        }));

        Assert.Same(failure, observed);
        Assert.Equal(rebuilding, owner.IsRebuilding);
        Assert.True(owner.IsSuppressed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NestedModeOptionsKeepOuterStateAfterSuccessOrCaughtFailure(bool failInner)
    {
        var owner = new SelectionOwner(initiallySuppressed: false);
        var failure = new InvalidOperationException("inner mode options failed");

        owner.ApplyModeOptions(() =>
        {
            var observed = Record.Exception(() => owner.ApplyModeOptions(() =>
            {
                Assert.True(owner.IsRebuilding);
                if (failInner)
                {
                    throw failure;
                }
            }));
            Assert.Same(failInner ? failure : null, observed);
            Assert.True(owner.IsRebuilding);
            owner.ApplyModeOptions(() => Assert.True(owner.IsRebuilding));
            Assert.True(owner.IsRebuilding);
            Assert.False(owner.IsSuppressed);
        });

        Assert.False(owner.IsRebuilding);
        Assert.False(owner.IsSuppressed);
    }

    [Fact]
    public void ModeOptionsAndReinitializeSuppressionHaveIndependentLifetimes()
    {
        var owner = new SelectionOwner(initiallySuppressed: false);

        owner.Apply(() =>
        {
            owner.ApplyModeOptions(() => Assert.True(owner.IsSuppressed && owner.IsRebuilding));
            Assert.True(owner.IsSuppressed);
            Assert.False(owner.IsRebuilding);
        });
        owner.ApplyModeOptions(() =>
        {
            owner.Apply(() => Assert.True(owner.IsSuppressed && owner.IsRebuilding));
            Assert.True(owner.IsRebuilding);
            Assert.False(owner.IsSuppressed);
        });

        Assert.False(owner.IsRebuilding);
        Assert.False(owner.IsSuppressed);
    }

    private sealed class SelectionOwner
    {
        private readonly object _viewModel;
        private readonly FieldInfo _suppression;
        private readonly FieldInfo _rebuilding;

        public SelectionOwner(bool initiallySuppressed, bool initiallyRebuilding = false)
        {
            var type = SussudioAssembly.Load().GetType("Sussudio.ViewModels.MainViewModel", throwOnError: true)!;
            _viewModel = RuntimeHelpers.GetUninitializedObject(type);
            _suppression = type.GetField("_suppressFormatChangeReinitialize", BindingFlags.Instance | BindingFlags.NonPublic)!;
            _suppression.SetValue(_viewModel, initiallySuppressed);
            _rebuilding = type.GetField("_isRebuildingModeOptions", BindingFlags.Instance | BindingFlags.NonPublic)!;
            _rebuilding.SetValue(_viewModel, initiallyRebuilding);
            Apply = type.GetMethod("ApplyCaptureSelectionWithoutReinitialize", BindingFlags.Instance | BindingFlags.NonPublic)!
                .CreateDelegate<Action<Action>>(_viewModel);
            ApplyModeOptions = type.GetMethod("ApplyCaptureModeOptions", BindingFlags.Instance | BindingFlags.NonPublic)!
                .CreateDelegate<Action<Action>>(_viewModel);
        }

        public Action<Action> Apply { get; }
        public Action<Action> ApplyModeOptions { get; }
        public bool IsSuppressed => (bool)_suppression.GetValue(_viewModel)!;
        public bool IsRebuilding => (bool)_rebuilding.GetValue(_viewModel)!;
    }
}
