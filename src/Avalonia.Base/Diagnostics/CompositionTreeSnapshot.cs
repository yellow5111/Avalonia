using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Drawing.Nodes;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Utilities;

namespace Avalonia.Diagnostics;

[PrivateApi]
public class CompositionTreeSnapshot
{
    public Compositor Compositor { get; }

    public CompositionTreeSnapshotItem Root { get; }

    private CompositionTreeSnapshot(Compositor compositor, ServerCompositionVisual root)
    {
        Compositor = compositor;
        Root = new CompositionTreeSnapshotItem(this, root);
    }

    public bool IsDisposed { get; private set; }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return new ValueTask(Compositor.InvokeServerJobAsync(() =>
        {
            Root.Destroy();
        }));
    }

    public static Task<CompositionTreeSnapshot?> TakeAsync(CompositionVisual visual)
    {
        return visual.Compositor.InvokeServerJobAsync(() =>
        {
            if (visual.Root == null)
                return null;

            return new CompositionTreeSnapshot(visual.Compositor, visual.Server);
        });
    }

    public CompositionTreeSnapshotItem? HitTest(Point pos)
    {
        return null!;
    }
}

[PrivateApi]
public class CompositionTreeSnapshotItem
{
    private readonly RenderTargetBitmap? _renderTargetBitmap;
    private readonly CompositionTreeSnapshot _snapshot;
    public string? Name { get; }

    internal CompositionTreeSnapshotItem(CompositionTreeSnapshot snapshot, ServerCompositionVisual visual)
    {
        _snapshot = snapshot;
        Name = visual.GetType().Name;

        if ((visual as ServerCompositionDrawListVisual)?.RenderData?.Items is { } renderDataItems)
        {
            _renderTargetBitmap = new RenderTargetBitmap(new PixelSize((int)visual.Size.X, (int)visual.Size.Y));
            using (var impl = _renderTargetBitmap.PlatformImpl.Item.CreateDrawingContext(true))
            {
                var operations = new DrawingContextWalkerImpl(impl);

                var renderContext = new RenderDataNodeRenderContext(operations);
                foreach (var item in renderDataItems)
                {
                    item.Invoke(ref renderContext);
                }

                DrawOperations = operations.OperationsLog;
            }
        }

        visual.PopulateDiagnosticProperties(Properties);
        Transform = visual.CombinedTransformMatrix;
        ClipToBounds = visual.ClipToBounds;
        GeometryClip = visual.Clip;
        Size = visual.Size;
        if (visual is ServerCompositionContainerVisual container)
            Children = container.Children.List.Select(x => new CompositionTreeSnapshotItem(snapshot, x)).ToList();
        else
            Children = Array.Empty<CompositionTreeSnapshotItem>();
    }

    public IGeometryImpl? GeometryClip { get; }

    public bool ClipToBounds { get; }

    public Matrix Transform { get; }
    
    public Vector Size { get; }

    public IReadOnlyList<IReadOnlyDictionary<string, object?>>? DrawOperations { get; }
    
    public IReadOnlyList<CompositionTreeSnapshotItem> Children { get; }

    public Dictionary<string, object?> Properties { get; } = new();

    public Task<Bitmap?> RenderToBitmapAsync(int? drawOperationIndex)
    {
        if (_renderTargetBitmap is null) return Task.FromResult<Bitmap?>(null);

        using (var ms = new MemoryStream())
        {
            _renderTargetBitmap.Save(ms);
            ms.Position = 0;
            return Task.FromResult(new Bitmap(ms))!;
        }
    }

    internal void Destroy()
    {
        _renderTargetBitmap?.Dispose();
        foreach (var ch in Children)
            ch.Destroy();
    }

    private class DrawingContextWalkerImpl(IDrawingContextImpl inner) : IDrawingContextImpl, IDrawingContextImplWithEffects
    {
        private readonly List<IReadOnlyDictionary<string, object?>> _operationsLog = new();

        public IReadOnlyList<IReadOnlyDictionary<string, object?>> OperationsLog => _operationsLog;

        public void Dispose()
        {
        }

        public Matrix Transform
        {
            get => inner.Transform;
            set => inner.Transform = value;
        }

        private void AddItem(Dictionary<string, object?> dictionary, [CallerMemberName] string? caller = null)
        {
            dictionary["Operation"] = caller;
            _operationsLog.Add(dictionary);
        }

        public void Clear(Color color)
        {
            AddItem(new()
            {
                ["Color"] = color
            });
            inner.Clear(color);
        }

        public void DrawBitmap(IBitmapImpl source, double opacity, Rect sourceRect, Rect destRect)
        {
            AddItem(new()
            {
                ["Bitmap"] = source.ToString(),
                ["Opacity"] = opacity,
                ["SourceRect"] = sourceRect,
                ["DestRect"] = destRect,
            });
            inner.DrawBitmap(source, opacity, sourceRect, destRect);
        }

        public void DrawBitmap(IBitmapImpl source, IBrush opacityMask, Rect opacityMaskRect, Rect destRect)
        {
            AddItem(new()
            {
                ["Bitmap"] = source.ToString(),
                ["OpacityMark"] = opacityMask.ToString(),
                ["SourceRect"] = opacityMaskRect,
                ["DestRect"] = destRect,
            });
            inner.DrawBitmap(source, opacityMask, opacityMaskRect, destRect);
        }

        public void DrawLine(IPen? pen, Point p1, Point p2)
        {
            AddItem(new()
            {
                ["Pen"] = pen?.ToImmutable(),
                ["Point1"] = p1,
                ["Point2"] = p2
            });
            inner.DrawLine(pen, p1, p2);
        }

        public void DrawGeometry(IBrush? brush, IPen? pen, IGeometryImpl geometry)
        {
            AddItem(new()
            {
                ["Brush"] = brush?.ToImmutable(),
                ["Pen"] = pen?.ToImmutable(),
                ["Geometry"] = geometry.ToString()
            });
            inner.DrawGeometry(brush, pen, geometry);
        }

        public void DrawRectangle(IBrush? brush, IPen? pen, RoundedRect rect, BoxShadows boxShadows = default)
        {
            AddItem(new()
            {
                ["Brush"] = brush?.ToImmutable(),
                ["Pen"] = pen?.ToImmutable(),
                ["Rect"] = rect,
                ["BoxShadows"] = boxShadows,
            });
            inner.DrawRectangle(brush, pen, rect, boxShadows);
        }

        public void DrawRegion(IBrush? brush, IPen? pen, IPlatformRenderInterfaceRegion region)
        {
            AddItem(new()
            {
                ["Brush"] = brush?.ToImmutable(),
                ["Pen"] = pen?.ToImmutable(),
                ["Region"] = region.ToString()
            });
            inner.DrawRegion(brush, pen, region);
        }

        public void DrawEllipse(IBrush? brush, IPen? pen, Rect rect)
        {
            AddItem(new()
            {
                ["Brush"] = brush?.ToImmutable(),
                ["Pen"] = pen?.ToImmutable(),
                ["Rect"] = rect
            });
            inner.DrawEllipse(brush, pen, rect);
        }

        public void DrawGlyphRun(IBrush? foreground, IGlyphRunImpl glyphRun)
        {
            AddItem(new()
            {
                ["Foreground"] = foreground?.ToImmutable(),
                ["GlyphRun"] = glyphRun.ToString()
            });
            inner.DrawGlyphRun(foreground, glyphRun);
        }

        public IDrawingContextLayerImpl CreateLayer(PixelSize size)
        {
            AddItem(new()
            {
                ["Size"] = size
            });
            return inner.CreateLayer(size);
        }

        public void PushClip(Rect clip)
        {
            AddItem(new()
            {
                ["Clip"] = clip
            });
            inner.PushClip(clip);
        }

        public void PushClip(RoundedRect clip)
        {
            AddItem(new()
            {
                ["Clip"] = clip
            });
            inner.PushClip(clip);
        }

        public void PushClip(IPlatformRenderInterfaceRegion region)
        {
            AddItem(new()
            {
                ["Clip"] = region.ToString()
            });
            inner.PushClip(region);
        }

        public void PopClip()
        {
            AddItem(new(){});
            inner.PopClip();
        }

        public void PushLayer(Rect bounds)
        {
            AddItem(new()
            {
                ["Bounds"] = bounds
            });
            inner.PushLayer(bounds);
        }

        public void PopLayer()
        {
            AddItem(new());
            inner.PopLayer();
        }

        public void PushOpacity(double opacity, Rect? bounds)
        {
            AddItem(new()
            {
                ["Opacity"] = opacity,
                ["Bounds"] = bounds
            });
            inner.PushOpacity(opacity, bounds);
        }

        public void PopOpacity()
        {
            AddItem(new());
            inner.PopOpacity();
        }

        public void PushOpacityMask(IBrush mask, Rect bounds)
        {
            AddItem(new()
            {
                ["OpacityMask"] = mask.ToImmutable(),
                ["Bounds"] = bounds
            });
            inner.PushOpacityMask(mask, bounds);
        }

        public void PopOpacityMask()
        {
            AddItem(new());
            inner.PopOpacityMask();
        }

        public void PushGeometryClip(IGeometryImpl clip)
        {
            AddItem(new()
            {
                ["Geometry"] = clip.ToString(),
            });
            inner.PushGeometryClip(clip);
        }

        public void PopGeometryClip()
        {
            AddItem(new());
            inner.PopGeometryClip();
        }

        public void PushRenderOptions(RenderOptions renderOptions)
        {
            AddItem(new()
            {
                ["RenderOptions"] = renderOptions,
            });
            inner.PushRenderOptions(renderOptions);
        }

        public void PopRenderOptions()
        {
            AddItem(new());
            inner.PopRenderOptions();
        }

        public void PushEffect(IEffect effect)
        {
            AddItem(new()
            {
                ["Effect"] = effect.ToImmutable(),
            });
            (inner as IDrawingContextImplWithEffects)?.PushEffect(effect);
        }

        public void PopEffect()
        {
            AddItem(new());
            (inner as IDrawingContextImplWithEffects)?.PopEffect();
        }

        public object? GetFeature(Type t) => inner.GetFeature(t);
    }
}
