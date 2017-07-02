using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;

namespace GoLine
{
    public class RingSlider : System.Windows.Controls.Slider
    {
        static RingSlider()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RingSlider), new FrameworkPropertyMetadata(typeof(RingSlider)));
        }

        public double Percent
        {
            get { return (double)GetValue(PercentProperty); }
            private set { SetValue(PercentProperty, value); }
        }
        public static readonly DependencyProperty PercentProperty =
            DependencyProperty.Register("Percent", typeof(double), typeof(RingSlider), new PropertyMetadata(0d));

        protected override void OnValueChanged(double oldValue, double newValue)
        {
            base.OnValueChanged(oldValue, newValue);
            var percent = Value / Maximum;
            if(percent != Percent) Percent = percent;
            updateArc();
        }

        private void updateArc()
        {
            if (processArc != null)
            {
                Storyboard storyboard = new Storyboard();
                storyboard.Children.Add(new DoubleAnimation() { To = 360 * Percent, Duration = TimeSpan.FromSeconds(0.1d) });
                Storyboard.SetTarget(storyboard, processArc);
                Storyboard.SetTargetProperty(storyboard, new PropertyPath("EndAngle"));
                storyboard.Begin();
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            processArc = GetTemplateChild("PART_Arc") as Microsoft.Expression.Shapes.Arc;
            updateArc();
        }
        private Microsoft.Expression.Shapes.Arc processArc;

    }
}
