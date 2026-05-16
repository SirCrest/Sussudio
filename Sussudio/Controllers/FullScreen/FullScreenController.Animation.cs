using System;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;

namespace Sussudio.Controllers;

internal sealed partial class FullScreenController
{
    private Task AnimateFullScreenRectAsync(
        Windows.Foundation.Point prePos, double preW, double preH,
        Windows.Foundation.Point postPos, double postW, double postH,
        Action onCompleted)
    {
        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var scaleX = (float)(preW / postW);
        var scaleY = (float)(preH / postH);
        var offsetX = (float)(prePos.X - postPos.X + (preW - postW) / 2);
        var offsetY = (float)(prePos.Y - postPos.Y + (preH - postH) / 2);

        var visual = ElementCompositionPreview.GetElementVisual(_context.PreviewBorder);
        var compositor = visual.Compositor;

        visual.CenterPoint = new Vector3((float)(postW / 2), (float)(postH / 2), 0);

        var props = compositor.CreatePropertySet();
        props.InsertScalar("Progress", 0f);

        var duration = TimeSpan.FromMilliseconds(350);
        var easing = compositor.CreateCubicBezierEasingFunction(
            new Vector2(0.2f, 0f), new Vector2(0f, 1f));

        var progressAnim = compositor.CreateScalarKeyFrameAnimation();
        progressAnim.InsertKeyFrame(1f, 1f, easing);
        progressAnim.Duration = duration;

        var scaleExpr = compositor.CreateExpressionAnimation(
            "Vector3(s.X + (1 - s.X) * p.Progress, s.Y + (1 - s.Y) * p.Progress, 1)");
        scaleExpr.SetVector3Parameter("s", new Vector3(scaleX, scaleY, 1));
        scaleExpr.SetReferenceParameter("p", props);

        var offsetExpr = compositor.CreateExpressionAnimation(
            "Vector3(o.X * (1 - p.Progress), o.Y * (1 - p.Progress), 0)");
        offsetExpr.SetVector3Parameter("o", new Vector3(offsetX, offsetY, 0));
        offsetExpr.SetReferenceParameter("p", props);

        var batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
        props.StartAnimation("Progress", progressAnim);
        batch.End();

        visual.StartAnimation("Scale", scaleExpr);
        visual.StartAnimation("Offset", offsetExpr);

        batch.Completed += (_, _) =>
        {
            if (!_context.DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    visual.StopAnimation("Scale");
                    visual.StopAnimation("Offset");
                    visual.Scale = Vector3.One;
                    visual.Offset = Vector3.Zero;
                    visual.CenterPoint = Vector3.Zero;
                    onCompleted();
                    completion.TrySetResult(null);
                }
                catch (Exception ex)
                {
                    completion.TrySetException(ex);
                }
            }))
            {
                completion.TrySetException(new InvalidOperationException("Failed to enqueue full-screen animation completion on the UI thread."));
            }
        };

        return completion.Task;
    }

    private static Task WaitForSizeChangedAsync(FrameworkElement element, int timeoutMs)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        SizeChangedEventHandler? handler = null;
        handler = (_, _) =>
        {
            element.SizeChanged -= handler;
            tcs.TrySetResult(true);
        };
        element.SizeChanged += handler;

        _ = Task.Delay(timeoutMs).ContinueWith(_ =>
        {
            element.DispatcherQueue.TryEnqueue(() =>
            {
                element.SizeChanged -= handler;
                tcs.TrySetResult(false);
            });
        });

        return tcs.Task;
    }
}
