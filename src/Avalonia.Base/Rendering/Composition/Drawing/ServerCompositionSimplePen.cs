using System;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Server;

internal partial class ServerCompositionSimplePen : IPen
{
    IDashStyle? IPen.DashStyle => DashStyle;

    public ImmutablePen ToImmutable()
    {
        return new ImmutablePen(
            Brush?.ToImmutable(),
            Thickness,
            DashStyle?.ToImmutable(),
            LineCap,
            LineJoin,
            MiterLimit);
    }
}
