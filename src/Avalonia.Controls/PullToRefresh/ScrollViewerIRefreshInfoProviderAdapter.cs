﻿using System;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Controls.PullToRefresh
{
    public class ScrollViewerIRefreshInfoProviderAdapter
    {
        private const int MaxSearchDepth = 10;
        private const int InitialOffsetThreshold = 1;

        private PullDirection _refreshPullDirection;
        private ScrollViewer _scrollViewer;
        private RefreshInfoProvider _refreshInfoProvider;
        private PullGestureRecognizer _pullGestureRecognizer;
        private InputElement? _interactionSource;
        private bool _isVisualizerInteractionSourceAttached;

        public ScrollViewerIRefreshInfoProviderAdapter(PullDirection pullDirection)
        {
            _refreshPullDirection = pullDirection;
        }

        public RefreshInfoProvider AdaptFromTree(IVisual root, Size refreshVIsualizerSize)
        {
            if (root is ScrollViewer scrollViewer)
            {
                return Adapt(scrollViewer, refreshVIsualizerSize);
            }
            else
            {
                int depth = 0;
                while (depth < MaxSearchDepth)
                {
                    var scroll = AdaptFromTreeRecursiveHelper(root, depth);

                    if (scroll != null)
                    {
                        return Adapt(scroll, refreshVIsualizerSize);
                    }

                    depth++;
                }
            }

            ScrollViewer AdaptFromTreeRecursiveHelper(IVisual root, int depth)
            {
                if (depth == 0)
                {
                    foreach (var child in root.VisualChildren)
                    {
                        if (child is ScrollViewer viewer)
                        {
                            return viewer;
                        }
                    }
                }
                else
                {
                    foreach (var child in root.VisualChildren)
                    {
                        var viewer = AdaptFromTreeRecursiveHelper(child, depth - 1);
                        if (viewer != null)
                        {
                            return viewer;
                        }
                    }
                }

                return null;
            }

            return null;
        }

        public RefreshInfoProvider Adapt(ScrollViewer adaptee, Size refreshVIsualizerSize)
        {
            if (adaptee == null)
            {
                throw new ArgumentNullException(nameof(adaptee), "Adaptee cannot be null");
            }

            if (_scrollViewer != null)
            {
                CleanUpScrollViewer();
            }

            if (_refreshInfoProvider != null && _interactionSource != null)
            {
                _interactionSource.RemoveHandler(Gestures.PullGestureEvent, _refreshInfoProvider.InteractingStateEntered);
                _interactionSource.RemoveHandler(Gestures.PullGestureEndedEvent, _refreshInfoProvider.InteractingStateExited);
            }

            _refreshInfoProvider = null;
            _scrollViewer = adaptee;

            if (_scrollViewer.Content == null)
            {
                throw new ArgumentException(nameof(adaptee), "Adaptee's content property cannot be null.");
            }

            var content = adaptee.Content as Visual;

            if (content == null)
            {
                throw new ArgumentException(nameof(adaptee), "Adaptee's content property must be a Visual");
            }

            if (content.GetVisualParent() == null)
            {
                _scrollViewer.Loaded += ScrollViewer_Loaded;
            }
            else
            {
                ScrollViewer_Loaded(null, null);

                if (content.Parent is not InputElement)
                {
                    throw new ArgumentException(nameof(adaptee), "Adaptee's content's parent must be a InputElement");
                }
            }

            _refreshInfoProvider = new RefreshInfoProvider(_refreshPullDirection, refreshVIsualizerSize, content);

            _pullGestureRecognizer = new PullGestureRecognizer(_refreshPullDirection);

            if (_interactionSource != null)
            {
                _interactionSource.GestureRecognizers.Add(_pullGestureRecognizer);
                _interactionSource.AddHandler(Gestures.PullGestureEvent, _refreshInfoProvider.InteractingStateEntered);
                _interactionSource.AddHandler(Gestures.PullGestureEndedEvent, _refreshInfoProvider.InteractingStateExited);
                _isVisualizerInteractionSourceAttached = true;
            }

            _scrollViewer.PointerPressed += ScrollViewer_PointerPressed;
            _scrollViewer.PointerReleased += ScrollViewer_PointerReleased;
            _scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;

            return _refreshInfoProvider;
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_isVisualizerInteractionSourceAttached && _refreshInfoProvider != null && _refreshInfoProvider.IsInteractingForRefresh)
            {
                if (!IsWithinOffsetThreashold())
                {
                    _refreshInfoProvider.IsInteractingForRefresh = false;
                }
            }
        }

        private void ScrollViewer_Loaded(object sender, RoutedEventArgs e)
        {
            var content = _scrollViewer.Content as Visual;
            if (content == null)
            {
                throw new ArgumentException(nameof(_scrollViewer), "Adaptee's content property must be a Visual");
            }

            if (content.Parent is not InputElement)
            {
                throw new ArgumentException(nameof(_scrollViewer), "Adaptee's content parent must be an InputElement");
            }

            MakeInteractionSource(content.Parent as InputElement);

            _scrollViewer.Loaded -= ScrollViewer_Loaded;
        }

        private void MakeInteractionSource(InputElement element)
        {
            _interactionSource = element;

            if (_pullGestureRecognizer != null)
            {
                element.GestureRecognizers.Add(_pullGestureRecognizer);
                _interactionSource.AddHandler(Gestures.PullGestureEvent, _refreshInfoProvider.InteractingStateEntered);
                _interactionSource.AddHandler(Gestures.PullGestureEndedEvent, _refreshInfoProvider.InteractingStateExited);
                _isVisualizerInteractionSourceAttached = true;
            }
        }

        private void ScrollViewer_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (_refreshInfoProvider != null)
            {
                _refreshInfoProvider.IsInteractingForRefresh = false;
            }
        }

        private void ScrollViewer_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            _refreshInfoProvider.PeekingMode = !IsWithinOffsetThreashold();
        }

        private bool IsWithinOffsetThreashold()
        {
            if (_scrollViewer != null)
            {
                var offset = _scrollViewer.Offset;

                switch (_refreshPullDirection)
                {
                    case PullDirection.TopToBottom:
                        return offset.Y < InitialOffsetThreshold;
                    case PullDirection.LeftToRight:
                        return offset.X < InitialOffsetThreshold;
                    case PullDirection.RightToLeft:
                        return offset.X > _scrollViewer.Extent.Width - _scrollViewer.Viewport.Width - InitialOffsetThreshold;
                    case PullDirection.BottomToTop:
                        return offset.Y > _scrollViewer.Extent.Height - _scrollViewer.Viewport.Height - InitialOffsetThreshold;
                }
            }

            return false;
        }

        private void CleanUpScrollViewer()
        {
            _scrollViewer.PointerPressed -= ScrollViewer_PointerPressed;
            _scrollViewer.PointerReleased -= ScrollViewer_PointerReleased;
            _scrollViewer.ScrollChanged -= ScrollViewer_ScrollChanged;
        }
    }
}
