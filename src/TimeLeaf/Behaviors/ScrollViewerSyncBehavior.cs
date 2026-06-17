using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace TimeLeaf.Behaviors
{
    /// <summary>
    /// 指定した ScrollViewer のスクロール位置と同期させる Behavior。
    /// </summary>
    public class ScrollViewerSyncBehavior : Behavior<ScrollViewer>
    {
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            nameof(Source),
            typeof(ScrollViewer),
            typeof(ScrollViewerSyncBehavior),
            new PropertyMetadata(null, OnSourceChanged)
        );

        public ScrollViewer Source
        {
            get => (ScrollViewer)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
            nameof(Orientation),
            typeof(Orientation),
            typeof(ScrollViewerSyncBehavior),
            new PropertyMetadata(Orientation.Vertical)
        );

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewerSyncBehavior behavior)
            {
                if (e.OldValue is ScrollViewer oldSource)
                {
                    oldSource.ScrollChanged -= behavior.OnSourceScrollChanged;
                }
                if (e.NewValue is ScrollViewer newSource)
                {
                    newSource.ScrollChanged += behavior.OnSourceScrollChanged;
                    behavior.UpdateOffset();
                }
            }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            if (Source != null)
            {
                Source.ScrollChanged += OnSourceScrollChanged;
                UpdateOffset();
            }
        }

        protected override void OnDetaching()
        {
            if (Source != null)
            {
                Source.ScrollChanged -= OnSourceScrollChanged;
            }
            base.OnDetaching();
        }

        private void OnSourceScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            UpdateOffset();
        }

        private void UpdateOffset()
        {
            if (Source == null || AssociatedObject == null)
                return;

            if (Orientation == Orientation.Horizontal)
            {
                AssociatedObject.ScrollToHorizontalOffset(Source.HorizontalOffset);
            }
            else
            {
                AssociatedObject.ScrollToVerticalOffset(Source.VerticalOffset);
            }
        }
    }
}
