using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace TimeLeaf.Behaviors
{
    /// <summary>
    /// 指定された水平オフセット位置まで自動的にスクロールする Behavior。
    /// </summary>
    public class AutoHorizontalScrollBehavior : Behavior<ScrollViewer>
    {
        public static readonly DependencyProperty ScrollOffsetProperty = DependencyProperty.Register(
            nameof(ScrollOffset),
            typeof(double),
            typeof(AutoHorizontalScrollBehavior),
            new PropertyMetadata(0.0, OnScrollOffsetChanged)
        );

        public double ScrollOffset
        {
            get => (double)GetValue(ScrollOffsetProperty);
            set => SetValue(ScrollOffsetProperty, value);
        }

        private static void OnScrollOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AutoHorizontalScrollBehavior behavior && behavior.AssociatedObject != null)
            {
                behavior.AssociatedObject.ScrollToHorizontalOffset((double)e.NewValue);
            }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            // 初期表示時にもスクロールを適用
            AssociatedObject.Loaded += OnLoaded;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Loaded -= OnLoaded;
            base.OnDetaching();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.ScrollToHorizontalOffset(ScrollOffset);
        }
    }
}
