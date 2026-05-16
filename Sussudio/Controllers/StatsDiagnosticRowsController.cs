using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class StatsDiagnosticRowsControllerContext
{
    public required StatsDockRowChromeController RowChromeController { get; init; }
}

internal sealed class StatsDiagnosticRowsController
{
    private readonly StatsDiagnosticRowsControllerContext _context;

    public StatsDiagnosticRowsController(StatsDiagnosticRowsControllerContext context)
    {
        _context = context;
    }

    public void UpdateDiagnostics(StatsDiagnosticRowsPresentation presentation)
    {
        _context.RowChromeController.UpdateDiagnosticsRows(presentation);
    }
}
