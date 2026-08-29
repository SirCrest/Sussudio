using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

public sealed class FlashbackPlaybackMailboxTests
{
    [Fact]
    public void ScrubUpdatesCoalesceWithoutCrossingTheFollowingControlCommand()
    {
        var mailbox = new MailboxHarness();

        Assert.Equal("Enqueued", mailbox.EnqueueScrub(TimeSpan.FromSeconds(1)));
        Assert.Equal("Coalesced", mailbox.EnqueueScrub(TimeSpan.FromSeconds(2)));
        Assert.True(mailbox.EnqueueControl("Play"));

        var scrub = mailbox.ReadCurrent();
        Assert.Equal("UpdateScrub", mailbox.Kind(scrub));
        Assert.Equal(TimeSpan.FromSeconds(2), mailbox.Position(mailbox.Resolve(scrub)));
        Assert.Equal("Play", mailbox.Kind(mailbox.ReadCurrent()));
        Assert.False(mailbox.TryReadCurrent(out _));
    }

    [Fact]
    public void SeeksCoalesceWithoutCrossingTheFollowingControlCommand()
    {
        var mailbox = new MailboxHarness();

        Assert.Equal("Enqueued", mailbox.EnqueueSeek(TimeSpan.FromSeconds(1)));
        Assert.Equal("Coalesced", mailbox.EnqueueSeek(TimeSpan.FromSeconds(2)));
        Assert.True(mailbox.EnqueueControl("Pause"));

        var seek = mailbox.ReadCurrent();
        Assert.Equal("Seek", mailbox.Kind(seek));
        Assert.Equal(TimeSpan.FromSeconds(2), mailbox.Position(mailbox.Resolve(seek)));
        Assert.Equal("Pause", mailbox.Kind(mailbox.ReadCurrent()));
        Assert.False(mailbox.TryReadCurrent(out _));
    }

    [Fact]
    public void RejectedSeekPreservesTheQueuedScrubSlot()
    {
        var mailbox = new MailboxHarness();
        Assert.Equal("Enqueued", mailbox.EnqueueScrub(TimeSpan.FromSeconds(1)));
        mailbox.CompleteCurrent();

        Assert.Equal("Rejected", mailbox.EnqueueSeek(TimeSpan.FromSeconds(2)));
        Assert.Equal("Coalesced", mailbox.EnqueueScrub(TimeSpan.FromSeconds(3)));
    }

    [Fact]
    public void RejectedScrubUpdatePreservesTheQueuedSeekSlot()
    {
        var mailbox = new MailboxHarness();
        Assert.Equal("Enqueued", mailbox.EnqueueSeek(TimeSpan.FromSeconds(1)));
        mailbox.CompleteCurrent();

        Assert.Equal("Rejected", mailbox.EnqueueScrub(TimeSpan.FromSeconds(2)));
        Assert.Equal("Coalesced", mailbox.EnqueueSeek(TimeSpan.FromSeconds(3)));
    }

    [Fact]
    public void RenewedGenerationAcceptsCommandsWithoutReadingTheCompletedGeneration()
    {
        var mailbox = new MailboxHarness();
        var oldGeneration = mailbox.CurrentGeneration;
        mailbox.Complete(oldGeneration);
        Assert.False(mailbox.EnqueueControl("Play"));

        var freshGeneration = mailbox.RenewGeneration();
        Assert.True(mailbox.EnqueueControl("Play"));
        Assert.False(mailbox.TryRead(oldGeneration, out _));
        Assert.Equal("Play", mailbox.Kind(mailbox.Read(freshGeneration)));
    }

    private sealed class MailboxHarness
    {
        private const BindingFlags InstanceNonPublic = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly Type _mailboxType = RequireRuntimeType("Sussudio.Services.Flashback.FlashbackPlaybackCommandMailbox");
        private readonly Type _commandType;
        private readonly Type _commandKindType;
        private readonly object _mailbox;

        internal MailboxHarness()
        {
            _commandType = _mailboxType.GetNestedType("Command", BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Mailbox command type not found.");
            _commandKindType = _mailboxType.GetNestedType("CommandKind", BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Mailbox command kind type not found.");
            _mailbox = Activator.CreateInstance(_mailboxType, nonPublic: true)
                ?? throw new InvalidOperationException("Mailbox construction failed.");
        }

        internal object CurrentGeneration => GetProperty(_mailbox, "CurrentGeneration");

        internal string EnqueueSeek(TimeSpan position)
            => Invoke("TryEnqueueSeek", position).ToString()!;

        internal string EnqueueScrub(TimeSpan position)
            => Invoke("TryEnqueueScrubUpdate", position).ToString()!;

        internal bool EnqueueControl(string kind)
        {
            var command = Activator.CreateInstance(_commandType)
                ?? throw new InvalidOperationException("Mailbox command construction failed.");
            SetPropertyOrBackingField(command, "Kind", Enum.Parse(_commandKindType, kind));
            return (bool)Invoke("TryEnqueue", command);
        }

        internal object Resolve(object command) => Invoke("ResolveLatestPosition", command);
        internal string Kind(object command) => GetProperty(command, "Kind").ToString()!;
        internal TimeSpan Position(object command) => (TimeSpan)GetProperty(command, "Position");
        internal object RenewGeneration() => Invoke("RenewGeneration");
        internal void CompleteCurrent() => Complete(CurrentGeneration);
        internal void Complete(object generation)
            => (_mailboxType.GetMethod("Complete", InstanceNonPublic)
                ?? throw new InvalidOperationException("Mailbox Complete method not found."))
                .Invoke(_mailbox, new[] { generation });

        internal object ReadCurrent() => Read(CurrentGeneration);

        internal object Read(object generation)
        {
            Assert.True(TryRead(generation, out var command));
            return command!;
        }

        internal bool TryReadCurrent(out object? command) => TryRead(CurrentGeneration, out command);

        internal bool TryRead(object generation, out object? command)
        {
            var reader = GetProperty(generation, "Reader");
            var method = reader.GetType().GetMethod("TryRead", new[] { _commandType.MakeByRefType() })
                ?? throw new InvalidOperationException("Mailbox reader TryRead missing.");
            var args = new object?[] { null };
            var result = (bool)method.Invoke(reader, args)!;
            command = args[0];
            return result;
        }

        private object Invoke(string name, params object[] arguments)
            => (_mailboxType.GetMethod(name, InstanceNonPublic)
                    ?? throw new InvalidOperationException($"Mailbox method '{name}' not found."))
                .Invoke(_mailbox, arguments)
                ?? throw new InvalidOperationException($"Mailbox method '{name}' returned null.");

        private static object GetProperty(object target, string name)
            => target.GetType().GetProperty(name, InstanceNonPublic | BindingFlags.Public)?.GetValue(target)
                ?? throw new InvalidOperationException($"Property '{name}' not found.");

        private static void SetPropertyOrBackingField(object target, string name, object value)
        {
            var property = target.GetType().GetProperty(name, InstanceNonPublic | BindingFlags.Public);
            if (property?.SetMethod is not null)
            {
                property.SetValue(target, value);
                return;
            }

            var field = target.GetType().GetField($"<{name}>k__BackingField", InstanceNonPublic)
                ?? throw new InvalidOperationException($"Writable property '{name}' not found.");
            field.SetValue(target, value);
        }

        private static Type RequireRuntimeType(string typeName)
            => (Type)(typeof(global::Program).GetMethod("RequireType", BindingFlags.Static | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("Program.RequireType not found."))
                .Invoke(null, new object[] { typeName })!;
    }
}
