namespace Sussudio.Controllers;

internal sealed partial class AudioControlBindingController
{
    private readonly AudioControlBindingControllerContext _context;

    public AudioControlBindingController(AudioControlBindingControllerContext context)
    {
        _context = context;
    }
}
