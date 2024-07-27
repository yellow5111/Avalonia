using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
}

[PrivateApi]
public class CompositionTreeSnapshotItem
{
    private readonly CompositionTreeSnapshot _snapshot;
    public string? Name { get; }

    internal CompositionTreeSnapshotItem(CompositionTreeSnapshot snapshot, ServerCompositionVisual visual)
    {
        _snapshot = snapshot;
        Name = visual.GetType().Name;
        DrawOperations = BuildDrawOperations((visual as ServerCompositionDrawListVisual)?.RenderData?.Items);
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

    public IReadOnlyList<IReadOnlyDictionary<string, object?>> DrawOperations { get; }
    
    public IReadOnlyList<CompositionTreeSnapshotItem> Children { get; }

    public Dictionary<string, object?> Properties { get; } = new();

    private IReadOnlyList<IReadOnlyDictionary<string, object?>> BuildDrawOperations(IReadOnlyCollection<IRenderDataItem>? renderDataItems)
    {
        if (renderDataItems is null)
            return Array.Empty<IReadOnlyDictionary<string, object?>>();

        var items = new List<IReadOnlyDictionary<string, object?>>(renderDataItems.Count);
        foreach (var item in renderDataItems)
        {
            var dictionary = new Dictionary<string, object?>();
            dictionary[nameof(Type)] = item.GetType().Name;
            item.PopulateDiagnosticProperties(dictionary);
            if (item is RenderDataPushNode pushNode)
            {
                dictionary[nameof(pushNode.Children)] = BuildDrawOperations(pushNode.Children);
            }
            items.Add(dictionary);
        }

        return items;
    }

    internal void Destroy()
    {
        // Not used currently
        //foreach (var ch in Children)
        //    ch.Destroy();
    }
}
