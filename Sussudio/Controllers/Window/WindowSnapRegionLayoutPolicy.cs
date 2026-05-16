using Sussudio.Models;
using Windows.Graphics;

namespace Sussudio.Controllers;

internal static class WindowSnapRegionLayoutPolicy
{
    public static RectInt32? ResolveTargetBounds(
        AutomationWindowAction region,
        RectInt32 workArea,
        SizeInt32 currentSize)
    {
        return region switch
        {
            AutomationWindowAction.SnapLeft => new RectInt32(
                workArea.X,
                workArea.Y,
                workArea.Width / 2,
                workArea.Height),
            AutomationWindowAction.SnapRight => new RectInt32(
                workArea.X + workArea.Width / 2,
                workArea.Y,
                workArea.Width - workArea.Width / 2,
                workArea.Height),
            AutomationWindowAction.SnapTopLeft => new RectInt32(
                workArea.X,
                workArea.Y,
                workArea.Width / 2,
                workArea.Height / 2),
            AutomationWindowAction.SnapTopRight => new RectInt32(
                workArea.X + workArea.Width / 2,
                workArea.Y,
                workArea.Width - workArea.Width / 2,
                workArea.Height / 2),
            AutomationWindowAction.SnapBottomLeft => new RectInt32(
                workArea.X,
                workArea.Y + workArea.Height / 2,
                workArea.Width / 2,
                workArea.Height - workArea.Height / 2),
            AutomationWindowAction.SnapBottomRight => new RectInt32(
                workArea.X + workArea.Width / 2,
                workArea.Y + workArea.Height / 2,
                workArea.Width - workArea.Width / 2,
                workArea.Height - workArea.Height / 2),
            AutomationWindowAction.Center => new RectInt32(
                workArea.X + (workArea.Width - currentSize.Width) / 2,
                workArea.Y + (workArea.Height - currentSize.Height) / 2,
                currentSize.Width,
                currentSize.Height),
            _ => null
        };
    }
}
