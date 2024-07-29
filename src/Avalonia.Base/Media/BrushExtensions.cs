using System;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Media
{
    /// <summary>
    /// Extension methods for brush classes.
    /// </summary>
    public static class BrushExtensions
    {
        /// <summary>
        /// Converts a brush to an immutable brush.
        /// </summary>
        /// <param name="brush">The brush.</param>
        /// <returns>
        /// The result of calling <see cref="IMutableBrush.ToImmutable"/> if the brush is mutable,
        /// otherwise <paramref name="brush"/>.
        /// </returns>
        public static IImmutableBrush ToImmutable(this IBrush brush)
        {
            return brush switch
            {
                IImmutableBrush immutableBrush => immutableBrush,
                IMutableBrush mutableBrush => mutableBrush.ToImmutable(),
                null => throw new ArgumentNullException(nameof(brush)),
                _ => throw new ArgumentOutOfRangeException(nameof(brush))
            };
        }

        /// <summary>
        /// Converts a dash style to an immutable dash style.
        /// </summary>
        /// <param name="style">The dash style.</param>
        /// <returns>
        /// The result of calling <see cref="DashStyle.ToImmutable"/> if the style is mutable,
        /// otherwise <paramref name="style"/>.
        /// </returns>
        public static ImmutableDashStyle ToImmutable(this IDashStyle style)
        {
            return style switch
            {
                DashStyle dashStyle => dashStyle.ToImmutable(),
                ImmutableDashStyle immutableDashStyle => immutableDashStyle,
                null => throw new ArgumentNullException(nameof(style)),
                _ => throw new ArgumentOutOfRangeException(nameof(style))
            };
        }

        /// <summary>
        /// Converts a pen to an immutable pen.
        /// </summary>
        /// <param name="pen">The pen.</param>
        /// <returns>
        /// The result of calling <see cref="Pen.ToImmutable"/> if the brush is mutable,
        /// otherwise <paramref name="pen"/>.
        /// </returns>
        public static ImmutablePen ToImmutable(this IPen pen)
        {
            return pen switch
            {
                ImmutablePen immutablePen => immutablePen,
                Pen clientPen => clientPen.ToImmutable(),
                ServerCompositionSimplePen serverPen => serverPen.ToImmutable(),
                null => throw new ArgumentNullException(nameof(pen)),
                _ => throw new ArgumentOutOfRangeException(nameof(pen))
            };
        }
    }
}
