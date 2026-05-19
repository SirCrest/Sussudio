using System;
using Sussudio.Models;

namespace Sussudio.Controllers;

internal static class PreviewRuntimeSnapshotController
{
    public static PreviewRuntimeSnapshot Build(PreviewRuntimeSnapshotInput input)
    {
        var d3dProjection = PreviewRuntimeD3DProjection.Build(input);
        var healthInput = PreviewRuntimeSnapshotHealthInputFactory.Build(
            input,
            d3dProjection,
            Environment.TickCount64,
            DateTimeOffset.UtcNow);
        var health = PreviewRuntimeSnapshotHealthPolicy.Evaluate(healthInput);

        return PreviewRuntimeSnapshotMapper.Build(input, d3dProjection, health, DateTimeOffset.UtcNow);
    }
}
