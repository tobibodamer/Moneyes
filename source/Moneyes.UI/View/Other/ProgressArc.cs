using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Moneyes.UI.View
{
    public class ProgressArc
    {
        private Point _startPosition = new (50, 0);
        private Point _endPosition = new (100, 0);
        private Size _radius;
        private double _thickness = 2;
        private double _angle;

        public Point StartPosition
        {
            get => _startPosition; set
            {
                _startPosition = value;
            }
        }
        public Point EndPosition
        {
            get => _endPosition; set
            {
                _endPosition = value;
            }
        }
        public Size Radius
        {
            get => _radius; set
            {
                _radius = value;
            }
        }
        public double Thickness
        {
            get => _thickness; set
            {
                _thickness = value;
            }
        }
        public double Angle
        {
            get => _angle; set
            {
                _angle = value;
            }
        }
    }
}
