using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Channels;
using Xunit;

namespace Sussudio.Tests;

public sealed class FlashbackPlaybackMailboxTests
{
    [Theory]
    [InlineData("Seek")]
    [InlineData("UpdateScrub")]
    public void RemovedIntentIsAccountedForWhenTheWriterCompletesBeforeReplacement(string kind)
    {
        var mailbox = new MailboxHarness();
        mailbox.UseSingleSlotChannel(completeAfterRead: true);
        Assert.Equal("Enqueued", mailbox.EnqueueIntent(kind, TimeSpan.FromSeconds(1)));

        Assert.False(mailbox.EnqueueControl("Play"));

        Assert.Equal(1L, mailbox.CommandsEnqueued);
        Assert.Equal(2L, mailbox.CommandsDropped);
        Assert.Equal(0, mailbox.PendingCommands);
        Assert.Equal("Rejected", mailbox.EnqueueIntent(kind, TimeSpan.FromSeconds(2)));
        Assert.Equal(3L, mailbox.CommandsDropped);
        Assert.Equal(0, mailbox.PendingCommands);
    }

    [Fact]
    public void SuccessfulReplacementAccountsForOnlyTheRemovedCommand()
    {
        var mailbox = new MailboxHarness();
        mailbox.UseSingleSlotChannel(completeAfterRead: false);
        Assert.Equal("Enqueued", mailbox.EnqueueSeek(TimeSpan.FromSeconds(1)));

        Assert.True(mailbox.EnqueueControl("Play"));

        Assert.Equal(2L, mailbox.CommandsEnqueued);
        Assert.Equal(1L, mailbox.CommandsDropped);
        Assert.Equal(1, mailbox.PendingCommands);
        Assert.Equal("Play", mailbox.Kind(mailbox.ReadCurrent()));
        Assert.False(mailbox.TryReadCurrent(out _));
    }

    [Theory]
    [InlineData("Seek", " pos_ms=1250")]
    [InlineData("BeginScrub", " pos_ms=1250")]
    [InlineData("UpdateScrub", " pos_ms=1250")]
    [InlineData("EndScrub", " pos_ms=1250")]
    [InlineData("Nudge", " delta_ms=-250")]
    [InlineData("Play", "")]
    [InlineData("Pause", "")]
    [InlineData("GoLive", "")]
    [InlineData("Stop", "")]
    [InlineData("Warm", "")]
    public void CommandDetailsDescribeOnlyTheRelevantPayload(string kind, string expected)
    {
        var mailbox = new MailboxHarness();
        var details = mailbox.FormatDetails(kind, TimeSpan.FromMilliseconds(1250), TimeSpan.FromMilliseconds(-250));
        Assert.Equal(expected, details.Mailbox);
        Assert.Equal(expected, details.Controller);
    }

    [Theory]
    [InlineData(0, " delta_ms=0")]
    [InlineData(0.125, " delta_ms=0.125")]
    public void NudgeDetailsPreserveZeroAndFractionalMilliseconds(double deltaMs, string expected)
    {
        var details = new MailboxHarness().FormatDetails("Nudge", TimeSpan.Zero, TimeSpan.FromMilliseconds(deltaMs));
        Assert.Equal(expected, details.Mailbox);
        Assert.Equal(expected, details.Controller);
    }

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
        internal long CommandsEnqueued => (long)GetProperty(_mailbox, "CommandsEnqueued");
        internal long CommandsDropped => (long)GetProperty(_mailbox, "CommandsDropped");
        internal int PendingCommands => (int)GetProperty(_mailbox, "PendingCommands");

        internal void UseSingleSlotChannel(bool completeAfterRead)
        {
            var channel = Activator.CreateInstance(
                typeof(CompletingReadChannel<>).MakeGenericType(_commandType),
                new object[] { completeAfterRead })!;
            var generationType = _mailboxType.GetNestedType("Generation", BindingFlags.NonPublic)!;
            var generation = Activator.CreateInstance(
                generationType, InstanceNonPublic, binder: null, args: new[] { channel }, culture: null)!;
            _mailboxType.GetField("_currentGeneration", InstanceNonPublic)!.SetValue(_mailbox, generation);
        }

        internal string EnqueueIntent(string kind, TimeSpan position)
            => kind == "Seek" ? EnqueueSeek(position) : EnqueueScrub(position);

        internal (string Mailbox, string Controller) FormatDetails(string kind, TimeSpan position, TimeSpan delta)
        {
            var command = Activator.CreateInstance(_commandType)!;
            SetPropertyOrBackingField(command, "Kind", Enum.Parse(_commandKindType, kind));
            SetPropertyOrBackingField(command, "Position", position);
            SetPropertyOrBackingField(command, "Delta", delta);
            var mailboxDetail = (string)_mailboxType.GetMethod(
                "FormatCommandDetail", BindingFlags.Static | BindingFlags.NonPublic,
                binder: null, types: new[] { _commandType }, modifiers: null)!.Invoke(null, new[] { command })!;
            var controllerDetail = (string)RequireRuntimeType("Sussudio.Services.Flashback.FlashbackPlaybackController").GetMethod(
                "FormatCommandDetail", BindingFlags.Static | BindingFlags.NonPublic,
                binder: null, types: new[] { _commandType }, modifiers: null)!.Invoke(null, new[] { command })!;
            return (mailboxDetail, controllerDetail);
        }

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

    // Force the completion race at the read/write boundary without sleeps or
    // a production hook. Capacity one exercises the same full-channel branch.
    private sealed class CompletingReadChannel<T> : Channel<T>
    {
        public CompletingReadChannel(bool completeAfterRead)
        {
            var channel = Channel.CreateBounded<T>(1);
            Reader = new CompletingReader(channel, completeAfterRead);
            Writer = channel.Writer;
        }

        private sealed class CompletingReader(Channel<T> channel, bool completeAfterRead) : ChannelReader<T>
        {
            public override Task Completion => channel.Reader.Completion;

            public override bool TryRead([MaybeNullWhen(false)] out T item)
            {
                if (!channel.Reader.TryRead(out item))
                {
                    return false;
                }

                if (completeAfterRead)
                {
                    channel.Writer.TryComplete();
                }

                return true;
            }

            public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default)
                => channel.Reader.WaitToReadAsync(cancellationToken);
        }
    }
}
