using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GoLine
{
    public class NavigationHelper
    {
        public static bool GetIsHome(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsHomeProperty);
        }

        public static void SetIsHome(DependencyObject obj, bool value)
        {
            obj.SetValue(IsHomeProperty, value);
        }
        
        public static readonly DependencyProperty IsHomeProperty =
            DependencyProperty.RegisterAttached("IsHome", typeof(bool), typeof(NavigationHelper), new PropertyMetadata(false));
    }
}
