namespace Sussudio.Controllers;

internal sealed partial class CaptureOptionBindingController
{
    public void AttachShowAllCaptureOptionsBinding()
    {
        _context.ShowAllCaptureOptionsToggle.Click += (s, e) =>
            _context.ViewModel.ShowAllCaptureOptions = _context.ShowAllCaptureOptionsToggle.IsChecked == true;
    }

    public void HandleShowAllCaptureOptionsChanged()
    {
        if ((_context.ShowAllCaptureOptionsToggle.IsChecked == true) != _context.ViewModel.ShowAllCaptureOptions)
        {
            _context.ShowAllCaptureOptionsToggle.IsChecked = _context.ViewModel.ShowAllCaptureOptions;
        }
    }
}
