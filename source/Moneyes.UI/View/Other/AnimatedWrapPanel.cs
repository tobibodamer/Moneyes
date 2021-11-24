using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Moneyes.UI.View
{
    class AnimatedWrapPanel : Panel
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            Size infiniteSize = new Size(double.PositiveInfinity, double.PositiveInfinity);

            double curX = 0, curY = 0;  // The top left (x,y) coordiantes of each child
            double curLineHeight = 0;   // The current height of the Panel

            foreach (UIElement child in Children)
            {
                // Give the child maximum space to expand to. This will set its DesiredSize
                child.Measure(infiniteSize);

                // Wrap child to next line if it doesn't fit
                if (curX + child.DesiredSize.Width > availableSize.Width)
                {
                    curY += curLineHeight;
                    curX = 0;
                    curLineHeight = 0;
                }

                // Next child will be placed next to this child
                curX += child.DesiredSize.Width;

                // Record the maximum Height the current row will require
                if (child.DesiredSize.Height > curLineHeight)
                    curLineHeight = child.DesiredSize.Height;

            }

            // The final height the Panel will require
            curY += curLineHeight;


            Size resultSize = new Size();

            // Should not return infinity as DesiredSize of the Panel
            resultSize.Width = double.IsPositiveInfinity(availableSize.Width) ? curX : availableSize.Width;
            resultSize.Height = double.IsPositiveInfinity(availableSize.Height) ? curY : availableSize.Height;

            return resultSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (this.Children == null || this.Children.Count == 0)
                return finalSize;

            TranslateTransform trans = null;
            double curX = 0, curY = 0, curLineHeight = 0;

            foreach (UIElement child in Children)
            {
                trans = child.RenderTransform as TranslateTransform;
                if (trans == null)
                {
                    child.RenderTransformOrigin = new Point(0, 0);
                    trans = new TranslateTransform();
                    child.RenderTransform = trans;
                }

                if (curX + child.DesiredSize.Width > finalSize.Width)
                { //Wrap to next line
                    curY += curLineHeight;
                    curX = 0;
                    curLineHeight = 0;
                }

                child.Arrange(new Rect(0, 0, child.DesiredSize.Width,
                              child.DesiredSize.Height));

                DoubleAnimation animation = new DoubleAnimation(0, TimeSpan.FromMilliseconds(1000));
                animation.EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut };

                animation.To = curX;
                trans.BeginAnimation(TranslateTransform.XProperty, animation, HandoffBehavior.Compose);

                animation.To = curY;
                trans.BeginAnimation(TranslateTransform.YProperty, animation, HandoffBehavior.Compose);

                curX += child.DesiredSize.Width;
                if (child.DesiredSize.Height > curLineHeight)
                    curLineHeight = child.DesiredSize.Height;
            }

            return finalSize;
        }

    }
}
