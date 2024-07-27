using System;
using System.Collections.Generic;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Drawing.Nodes;

class RenderDataBitmapNode : IRenderDataItem, IDisposable
{
    public IRef<IBitmapImpl>? Bitmap { get; set; }
    public double Opacity { get; set; }
    public Rect SourceRect { get; set; }
    public Rect DestRect { get; set; }
    
    public bool HitTest(Point p) => DestRect.Contains(p);

    public void Invoke(ref RenderDataNodeRenderContext context)
    {
        if (Bitmap != null)
            context.Context.DrawBitmap(Bitmap.Item, Opacity, SourceRect, DestRect);
    }

    public Rect? Bounds => DestRect;
    public void Dispose()
    {
        Bitmap?.Dispose();
        Bitmap = null;
    }

    public void PopulateDiagnosticProperties(Dictionary<string, object?> properties)
    {
        properties[nameof(Bounds)] = Bounds;
        properties[nameof(Bitmap)] = Bitmap?.Item?.ToString();
        properties[nameof(Opacity)] = Opacity;
        properties[nameof(SourceRect)] = SourceRect;
        properties[nameof(DestRect)] = DestRect;
    }
}
