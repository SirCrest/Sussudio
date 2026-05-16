namespace Sussudio.Controllers;

internal sealed partial class CaptureOptionBindingController
{
    private void AttachHdrToggleBindings()
    {
        _context.HdrToggle.Click += (s, e) =>
            _context.ViewModel.IsHdrEnabled = _context.HdrToggle.IsChecked == true;
        _context.TrueHdrPreviewToggle.Click += (s, e) =>
            _context.ViewModel.IsTrueHdrPreviewEnabled = _context.TrueHdrPreviewToggle.IsChecked == true;
    }

    public void HandleHdrEnabledChanged()
    {
        if (_context.HdrToggle.IsChecked != _context.ViewModel.IsHdrEnabled)
        {
            _context.HdrToggle.IsChecked = _context.ViewModel.IsHdrEnabled;
        }

        _context.ApplyHdrToggleEnabledState();
    }

    public void HandleTrueHdrPreviewEnabledChanged()
    {
        if (_context.TrueHdrPreviewToggle.IsChecked != _context.ViewModel.IsTrueHdrPreviewEnabled)
        {
            _context.TrueHdrPreviewToggle.IsChecked = _context.ViewModel.IsTrueHdrPreviewEnabled;
        }

        _context.SetHdrPassthroughEnabled(_context.ViewModel.IsTrueHdrPreviewEnabled);
    }
}
