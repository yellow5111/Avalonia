﻿using System;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// Viewbox is used to scale single child to fit in the available space.
    /// </summary>
    public class Viewbox : Control
    {
        private Decorator _containerVisual;

        /// <summary>
        /// Defines the <see cref="Stretch"/> property.
        /// </summary>
        public static readonly StyledProperty<Stretch> StretchProperty =
            AvaloniaProperty.Register<Image, Stretch>(nameof(Stretch), Stretch.Uniform);

        /// <summary>
        /// Defines the <see cref="StretchDirection"/> property.
        /// </summary>
        public static readonly StyledProperty<StretchDirection> StretchDirectionProperty =
            AvaloniaProperty.Register<Viewbox, StretchDirection>(nameof(StretchDirection), StretchDirection.Both);

        /// <summary>
        /// Defines the <see cref="Child"/> property
        /// </summary>
        public static readonly StyledProperty<IControl?> ChildProperty =
            Decorator.ChildProperty.AddOwner<Viewbox>();

        static Viewbox()
        {
            ClipToBoundsProperty.OverrideDefaultValue<Viewbox>(true);
            UseLayoutRoundingProperty.OverrideDefaultValue<Viewbox>(true);
            AffectsMeasure<Viewbox>(StretchProperty, StretchDirectionProperty);
        }

        public Viewbox()
        {
            _containerVisual = new Decorator();
            _containerVisual.RenderTransformOrigin = RelativePoint.TopLeft;
            AddVisualChild(_containerVisual);
            ((ISetLogicalParent)_containerVisual).SetParent(this);
        }

        /// <summary>
        /// Gets or sets the stretch mode, 
        /// which determines how child fits into the available space.
        /// </summary>
        public Stretch Stretch
        {
            get => GetValue(StretchProperty);
            set => SetValue(StretchProperty, value);
        }

        /// <summary>
        /// Gets or sets a value controlling in what direction contents will be stretched.
        /// </summary>
        public StretchDirection StretchDirection
        {
            get => GetValue(StretchDirectionProperty);
            set => SetValue(StretchDirectionProperty, value);
        }

        /// <summary>
        /// Gets or sets the child of the Viewbox
        /// </summary>
        [Content]
        public IControl? Child
        {
            get => GetValue(ChildProperty);
            set => SetValue(ChildProperty, value);
        }

        protected override int LogicalChildrenCount => 1;
        protected override int VisualChildrenCount => 1;

        /// <summary>
        /// Gets or sets the transform applied to the container visual that
        /// hosts the child of the Viewbox
        /// </summary>
        protected internal ITransform? InternalTransform
        {
            get => _containerVisual.RenderTransform;
            set => _containerVisual.RenderTransform = value;
        }

        protected override ILogical GetLogicalChild(int index) => index == 0 ?
            _containerVisual : throw new ArgumentOutOfRangeException();

        protected override IVisual GetVisualChild(int index) => index == 0 ?
            _containerVisual : throw new ArgumentOutOfRangeException();

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ChildProperty)
            {
                _containerVisual.Child = change.NewValue.GetValueOrDefault<IControl>();
                InvalidateMeasure();
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var child = _containerVisual;

            if (child != null)
            {
                child.Measure(Size.Infinity);

                var childSize = child.DesiredSize;

                var size = Stretch.CalculateSize(availableSize, childSize, StretchDirection);

                return size;
            }

            return new Size();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var child = _containerVisual;

            if (child != null)
            {
                var childSize = child.DesiredSize;
                var scale = Stretch.CalculateScaling(finalSize, childSize, StretchDirection);

                InternalTransform = new ScaleTransform(scale.X, scale.Y);

                child.Arrange(new Rect(childSize));

                return childSize * scale;
            }

            return finalSize;
        }
    }
}
