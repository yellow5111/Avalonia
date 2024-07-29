using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Media.Immutable;

namespace Avalonia.Media
{
    /// <summary>
    /// A collection of <see cref="GradientStop"/>s.
    /// </summary>
    public class GradientStops : AvaloniaList<GradientStop>
    {
        public GradientStops()
        {
            ResetBehavior = ResetBehavior.Remove;
        }

        public IReadOnlyList<ImmutableGradientStop> ToImmutable()
        {
            return AsImmutable(this);
        }

        internal static IReadOnlyList<ImmutableGradientStop> AsImmutable(IReadOnlyList<IGradientStop> stops)
        {
            var immutableGradientStops = new ImmutableGradientStop[stops.Count];
            for (int i = 0; i < stops.Count; i++)
            {
                var stop = stops[i];
                immutableGradientStops[i] = new ImmutableGradientStop(stop.Offset, stop.Color);
            }
            return immutableGradientStops;
        }
    }
}
