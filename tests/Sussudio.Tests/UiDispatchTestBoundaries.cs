using System;
using System.Collections.Generic;

// Boundary substitutes for the linked production UiDispatchControllers.cs file.
// They deliberately contain no invocation, cancellation, or completion policy.
namespace Microsoft.UI.Dispatching
{
    public delegate void DispatcherQueueHandler();

    public sealed partial class DispatcherQueue
    {
        private readonly Queue<DispatcherQueueHandler> _callbacks = new();

        public bool HasThreadAccess { get; set; }
        public bool AcceptEnqueue { get; set; } = true;
        public int PendingCount => _callbacks.Count;

        public bool TryEnqueue(DispatcherQueueHandler callback)
        {
            if (!AcceptEnqueue)
            {
                return false;
            }

            _callbacks.Enqueue(callback);
            return true;
        }

        public void RunNext() => _callbacks.Dequeue()();
    }
}

namespace Sussudio.ViewModels
{
    public sealed partial class MainViewModel
    {
        public string StatusText { get; set; } = string.Empty;
    }
}

namespace Sussudio
{
    internal static class Logger
    {
        public static void LogException(Exception exception) { }
    }
}
