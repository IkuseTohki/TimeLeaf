using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace TimeLeaf.Behaviors
{
    /// <summary>
    /// マウスホイールイベントを別の要素に転送する Behavior。
    /// </summary>
    public class RedirectMouseWheelBehavior : Behavior<UIElement>
    {
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register(
            nameof(Target),
            typeof(UIElement),
            typeof(RedirectMouseWheelBehavior),
            new PropertyMetadata(null)
        );

        public UIElement Target
        {
            get => (UIElement)GetValue(TargetProperty);
            set => SetValue(TargetProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewMouseWheel += OnPreviewMouseWheel;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewMouseWheel -= OnPreviewMouseWheel;
            base.OnDetaching();
        }

        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Target == null || e.Handled)
                return;

            // イベントを完了としてマークし、ターゲットに対して新しいイベントを発生させる
            e.Handled = true;
            var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = UIElement.MouseWheelEvent,
                Source = sender,
            };
            Target.RaiseEvent(eventArg);
        }
    }
}
