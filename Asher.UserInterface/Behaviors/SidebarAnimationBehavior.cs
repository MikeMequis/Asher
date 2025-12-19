using System.Windows;
using System.Windows.Media.Animation;

namespace Asher.UserInterface.Behaviors
{
    public static class SidebarAnimationBehavior
    {
        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.RegisterAttached(
                "IsExpanded",
                typeof(bool),
                typeof(SidebarAnimationBehavior),
                new PropertyMetadata(false, OnIsExpandedChanged));

        public static readonly DependencyProperty AnimationDurationProperty =
            DependencyProperty.RegisterAttached(
                "AnimationDuration",
                typeof(double),
                typeof(SidebarAnimationBehavior),
                new PropertyMetadata(400.0));

        public static bool GetIsExpanded(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsExpandedProperty);
        }

        public static void SetIsExpanded(DependencyObject obj, bool value)
        {
            obj.SetValue(IsExpandedProperty, value);
        }

        public static double GetAnimationDuration(DependencyObject obj)
        {
            return (double)obj.GetValue(AnimationDurationProperty);
        }

        public static void SetAnimationDuration(DependencyObject obj, double value)
        {
            obj.SetValue(AnimationDurationProperty, value);
        }

        private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element)
            {
                var isExpanded = (bool)e.NewValue;
                var duration = GetAnimationDuration(d);
                AnimateSidebar(element, isExpanded, duration);
            }
        }

        private static void AnimateSidebar(FrameworkElement element, bool isExpanded, double durationMs)
        {
            var targetWidth = isExpanded ? 250.0 : 60.0;
            
            var animation = new DoubleAnimation
            {
                To = targetWidth,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            // Stop any existing animation to prevent conflicts
            element.BeginAnimation(FrameworkElement.WidthProperty, null);
            
            // Start the new animation
            element.BeginAnimation(FrameworkElement.WidthProperty, animation);
        }
    }
} 