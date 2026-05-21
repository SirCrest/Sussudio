namespace Sussudio.Tools;

internal sealed partial class DiagnosticSessionRunContext
{
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        CommandChannel.Dispose();
        ScenarioCancellationSource.Dispose();
        _disposed = true;
    }
}
