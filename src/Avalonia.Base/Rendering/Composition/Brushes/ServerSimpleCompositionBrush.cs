using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Transport;

// ReSharper disable CheckNamespace

namespace Avalonia.Rendering.Composition.Server
{
    internal abstract partial class ServerCompositionSimpleBrush : IBrush
    {
        ITransform? IBrush.Transform => Transform;
    }

    internal abstract class ServerCompositionSimpleGradientBrush : ServerCompositionSimpleBrush, IGradientBrush, IMutableBrush
    {
        public abstract IImmutableBrush ToImmutable();

        internal ServerCompositionSimpleGradientBrush(ServerCompositor compositor) : base(compositor)
        {
            
        }

        private readonly List<IGradientStop> _gradientStops = new();
        public IReadOnlyList<IGradientStop> GradientStops => _gradientStops;
        public GradientSpreadMethod SpreadMethod { get; private set; }

        protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
        {
            base.DeserializeChangesCore(reader, committedAt);
            SpreadMethod = reader.Read<GradientSpreadMethod>();
            _gradientStops.Clear();
            var count = reader.Read<int>();
            for (var c = 0; c < count; c++)
                _gradientStops.Add(reader.ReadObject<ImmutableGradientStop>());
        }
    }

    partial class ServerCompositionSimpleConicGradientBrush : IConicGradientBrush, IMutableBrush
    {
        public override IImmutableBrush ToImmutable()
        {
            return new ImmutableConicGradientBrush(
                Avalonia.Media.GradientStops.AsImmutable(GradientStops), Opacity, Transform?.ToImmutable(), TransformOrigin,
                SpreadMethod, Center, Angle);
        }
    }
    
    partial class ServerCompositionSimpleLinearGradientBrush : ILinearGradientBrush, IMutableBrush
    {
        public override IImmutableBrush ToImmutable()
        {
            return new ImmutableLinearGradientBrush(
                Avalonia.Media.GradientStops.AsImmutable(GradientStops), Opacity, Transform?.ToImmutable(), TransformOrigin,
                SpreadMethod, StartPoint, EndPoint);
        }
    }
    
    partial class ServerCompositionSimpleRadialGradientBrush : IRadialGradientBrush, IMutableBrush
    {
        public double Radius => RadiusX.Scalar;
        public override IImmutableBrush ToImmutable()
        {
            return new ImmutableRadialGradientBrush(
                Avalonia.Media.GradientStops.AsImmutable(GradientStops), Opacity, Transform?.ToImmutable(), TransformOrigin,
                SpreadMethod, Center, GradientOrigin, Radius);
        }
    }
    
    partial class ServerCompositionSimpleSolidColorBrush : ISolidColorBrush, IMutableBrush
    {
        public IImmutableBrush ToImmutable()
        {
            return new ImmutableSolidColorBrush(Color, Opacity, Transform?.ToImmutable());
        }
    }
}
