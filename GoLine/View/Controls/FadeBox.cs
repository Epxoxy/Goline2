using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace GoLine
{
    public class FadeBox : ContentControl
    {
        static FadeBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FadeBox), new FrameworkPropertyMetadata(typeof(FadeBox)));
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(FadeBox), new FrameworkPropertyMetadata(KeyboardNavigationMode.Cycle));
            VisibilityProperty.OverrideMetadata(typeof(FadeBox), new FrameworkPropertyMetadata(Visibility.Collapsed,OnVisibilityChanged));
        }

        public bool ClickOutsideClose
        {
            get { return (bool)GetValue(ClickOutsideCloseProperty); }
            set { SetValue(ClickOutsideCloseProperty, value); }
        }
        
        public static readonly DependencyProperty ClickOutsideCloseProperty =
            DependencyProperty.Register("ClickOutsideClose", typeof(bool), typeof(FadeBox), new PropertyMetadata(false));
        
        public bool IsOpen
        {
            get { return (bool)GetValue(IsOpenProperty); }
            set { SetValue(IsOpenProperty, value); }
        }
        
        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register("IsOpen", typeof(bool), typeof(FadeBox), new PropertyMetadata(false, OnIsOpenChanged));
        
        private static void OnVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as FadeBox;
        }

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as FadeBox;
            if (control != null) control.UpdateOpenState();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var rect = GetTemplateChild("DismissLayer") as FrameworkElement;
            var OpenStoryboard = GetTemplateChild("OpenStoryboard") as Storyboard;
            var CloseStoryboard = GetTemplateChild("CloseStoryboard") as Storyboard;
            RegisterAnimation(ref OpenStoryboard);
            RegisterAnimation(ref CloseStoryboard);
            rect.MouseDown -= OnRectMouseDown;
            rect.MouseDown += OnRectMouseDown;
            UpdateOpenState(false);
        }
        
        private void RegisterAnimation(ref Storyboard storybard)
        {
            if (storybard != null)
            {
                storybard.Completed -= OnAnimationCompleted;
                storybard.Completed += OnAnimationCompleted;
            }
        }

        private void OnAnimationCompleted(object sender, EventArgs e)
        {
            if (!IsOpen) Visibility = Visibility.Collapsed;
        }

        private void OnRectMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ClickOutsideClose && IsOpen) IsOpen = false;
        }
        
        private void UpdateOpenState(bool useTransition = true)
        {
            if (IsOpen)
            {
                if (Visibility != Visibility.Visible) Visibility = Visibility.Visible;
                VisualStateManager.GoToState(this, "Open", useTransition);
                Keyboard.Focus(this);
            }
            else
            {
                Keyboard.ClearFocus();
                VisualStateManager.GoToState(this, "Close", useTransition);
            }
        }

    }
}
