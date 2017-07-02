using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace GoLine
{
    public class FadeExtension
    {
        private static Storyboard fadeScale;
        private static Storyboard fadeOpacity;
        private static Storyboard fadeOutScale;
        private static Storyboard fadeOutOpacity;
        private static CubicEase cubicEase = new CubicEase()
        {
            EasingMode = EasingMode.EaseOut
        };
        private static Storyboard FadeScale
        {
            get
            {
                if (fadeScale == null)
                {
                    fadeScale = new Storyboard();
                    var doubleanimaitonx = new DoubleAnimationUsingKeyFrames();
                    doubleanimaitonx.KeyFrames.Add(new DiscreteDoubleKeyFrame(0.9d, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0d))));
                    doubleanimaitonx.KeyFrames.Add(new EasingDoubleKeyFrame(1d, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.3d)))
                    {
                        EasingFunction = cubicEase
                    });
                    var doubleanimaitony = new DoubleAnimationUsingKeyFrames();
                    doubleanimaitony.KeyFrames.Add(new DiscreteDoubleKeyFrame(0.9d, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0d))));
                    doubleanimaitony.KeyFrames.Add(new EasingDoubleKeyFrame(1d, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.3d)))
                    {
                        EasingFunction = cubicEase
                    });
                    Storyboard.SetTargetProperty(doubleanimaitonx, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                    Storyboard.SetTargetProperty(doubleanimaitony, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                    fadeScale.Children.Add(doubleanimaitonx);
                    fadeScale.Children.Add(doubleanimaitony);
                }
                return fadeScale;
            }
        }
        private static Storyboard FadeOpacity
        {
            get
            {
                if(fadeOpacity == null)
                {
                    fadeOpacity = new Storyboard();
                    var doubleanimaitonOpacity = new DoubleAnimationUsingKeyFrames();
                    doubleanimaitonOpacity.KeyFrames.Add(new DiscreteDoubleKeyFrame(0d, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0d))));
                    doubleanimaitonOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(1d, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.2d)))
                    {
                        EasingFunction = cubicEase
                    });
                    Storyboard.SetTargetProperty(doubleanimaitonOpacity, new PropertyPath("Opacity"));
                    fadeOpacity.Children.Add(doubleanimaitonOpacity);
                }
                return fadeOpacity;
            }
        }

        private static Storyboard FadeOutScale
        {
            get
            {
                if (fadeOutScale == null)
                {
                    fadeOutScale = new Storyboard();
                    var doubleanimaitonx = new DoubleAnimationUsingKeyFrames();
                    doubleanimaitonx.KeyFrames.Add(new DiscreteDoubleKeyFrame(1d, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0d))));
                    doubleanimaitonx.KeyFrames.Add(new EasingDoubleKeyFrame(0.9d, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.3d)))
                    {
                        EasingFunction = cubicEase
                    });
                    var doubleanimaitony = new DoubleAnimationUsingKeyFrames();
                    doubleanimaitony.KeyFrames.Add(new DiscreteDoubleKeyFrame(1d, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0d))));
                    doubleanimaitony.KeyFrames.Add(new EasingDoubleKeyFrame(0.9d, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.3d)))
                    {
                        EasingFunction = cubicEase
                    });
                    Storyboard.SetTargetProperty(doubleanimaitonx, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                    Storyboard.SetTargetProperty(doubleanimaitony, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                    fadeOutScale.Children.Add(doubleanimaitonx);
                    fadeOutScale.Children.Add(doubleanimaitony);
                }
                return fadeOutScale;
            }
        }
        private static Storyboard FadeOutOpacity
        {
            get
            {
                if (fadeOutOpacity == null)
                {
                    fadeOutOpacity = new Storyboard();
                    var doubleanimaitonOpacity = new DoubleAnimationUsingKeyFrames();
                    doubleanimaitonOpacity.KeyFrames.Add(new DiscreteDoubleKeyFrame(1d, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0d))));
                    doubleanimaitonOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(0d, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.2d)))
                    {
                        EasingFunction = cubicEase
                    });
                    Storyboard.SetTargetProperty(doubleanimaitonOpacity, new PropertyPath("Opacity"));
                    fadeOutOpacity.Children.Add(doubleanimaitonOpacity);
                }
                return fadeOutOpacity;
            }
        }

        public static void FadeInBoxOf(UIElement element, UIElement content)
        {
            if (element.Visibility != Visibility.Visible) element.Visibility = Visibility.Visible;
            if (content.Visibility != Visibility.Visible) content.Visibility = Visibility.Visible;
            var fadeopacityCopy = FadeOpacity.Clone();
            Storyboard.SetTarget(fadeopacityCopy, element);
            var fadescaleCopy = FadeScale.Clone();
            Storyboard.SetTarget(fadescaleCopy, content);
            fadeopacityCopy.Begin();
            fadescaleCopy.Begin();
        }
        public static void FadeScaleOf(UIElement element)
        {
            if (element.Visibility != Visibility.Visible) element.Visibility = Visibility.Visible;
            var fadeopacityCopy = FadeOpacity.Clone();
            Storyboard.SetTarget(fadeopacityCopy, element);
            fadeopacityCopy.Begin();
        }
        public static void FadeOpacityOf(UIElement element)
        {
            if (element.Visibility != Visibility.Visible) element.Visibility = Visibility.Visible;
            var fadescaleCopy = FadeScale.Clone();
            Storyboard.SetTarget(fadescaleCopy, element);
            fadescaleCopy.Begin();
        }

        public static void FadeOutBoxOf(UIElement element, UIElement content)
        {
            var fadeopacityCopy = FadeOutOpacity.Clone();
            Storyboard.SetTarget(fadeopacityCopy, element);
            var fadescaleCopy = FadeOutScale.Clone();
            Storyboard.SetTarget(fadescaleCopy, content);
            EventHandler fadeopacityCompletedHandler = null;
            fadeopacityCompletedHandler = (sender, e) =>
            {
                fadeopacityCopy.Completed -= fadeopacityCompletedHandler;
                if (element.Visibility != Visibility.Collapsed) element.Visibility = Visibility.Collapsed;
            };
            EventHandler fadescaleCompletedHandler = null;
            fadescaleCompletedHandler = (sender, e) =>
            {
                fadescaleCopy.Completed -= fadescaleCompletedHandler;
                if (content.Visibility != Visibility.Collapsed) content.Visibility = Visibility.Collapsed;
            };
            fadeopacityCopy.Completed += fadeopacityCompletedHandler;
            fadescaleCopy.Completed += fadescaleCompletedHandler;
            fadeopacityCopy.Begin();
            fadescaleCopy.Begin();
        }
        public static void FadeOutScaleOf(UIElement element)
        {
            var fadeopacityCopy = FadeOutOpacity.Clone();
            Storyboard.SetTarget(fadeopacityCopy, element);
            EventHandler fadeopacityCompletedHandler = null;
            fadeopacityCompletedHandler = (sender, e) =>
            {
                fadeopacityCopy.Completed -= fadeopacityCompletedHandler;
                if (element.Visibility != Visibility.Collapsed) element.Visibility = Visibility.Collapsed;
            };
            fadeopacityCopy.Completed += fadeopacityCompletedHandler;
            fadeopacityCopy.Begin();
        }
        public static void FadeOutOpacityOf(UIElement element)
        {
            var fadescaleCopy = FadeOutScale.Clone();
            Storyboard.SetTarget(fadescaleCopy, element);
            EventHandler fadescaleCompletedHandler = null;
            fadescaleCompletedHandler = (sender, e) =>
            {
                fadescaleCopy.Completed -= fadescaleCompletedHandler;
                if (element.Visibility != Visibility.Collapsed) element.Visibility = Visibility.Collapsed;
            };
            fadescaleCopy.Completed += fadescaleCompletedHandler;
            fadescaleCopy.Begin();
        }
        
    }
}
