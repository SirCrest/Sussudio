using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackEncoderSink
{
    private sealed class ForceRotateRequest
    {
        private const int StatePending = 0;
        private const int StateCommitting = 1;
        private const int StateCompleted = 2;
        private const int StateCanceled = 3;

        private int _state = StatePending;

        private readonly TaskCompletionSource<IReadOnlyList<string>> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<IReadOnlyList<string>> Task => _completion.Task;

        public bool IsCompleted
        {
            get
            {
                var state = Volatile.Read(ref _state);
                return state == StateCompleted ||
                       state == StateCanceled ||
                       _completion.Task.IsCompleted;
            }
        }

        public bool TryBeginCommit()
            => Interlocked.CompareExchange(ref _state, StateCommitting, StatePending) == StatePending;

        public bool TryCancel()
        {
            if (Interlocked.CompareExchange(ref _state, StateCanceled, StatePending) != StatePending)
            {
                return false;
            }

            _completion.TrySetResult(Array.Empty<string>());
            return true;
        }

        public void Complete(IReadOnlyList<string> paths)
        {
            while (true)
            {
                var state = Volatile.Read(ref _state);
                if (state == StateCompleted || state == StateCanceled)
                {
                    return;
                }

                if (Interlocked.CompareExchange(ref _state, StateCompleted, state) == state)
                {
                    _completion.TrySetResult(paths);
                    return;
                }
            }
        }

        public void CompleteEmpty()
            => Complete(Array.Empty<string>());
    }
}
