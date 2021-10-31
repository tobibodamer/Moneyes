using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Moneyes.UI.View
{
    /// <summary>
    /// Interaktionslogik für CircularProgressBar.xaml
    /// </summary>
    public partial class CircularProgressBar : UserControl
    {
        public CircularProgressBar()
        {
            InitializeComponent();
            BackgroundCircle.Angle = 360;
            RefreshControl();
            this.SizeChanged += (sender, args) =>
            {
                RefreshControl();
            };
            this.LayoutUpdated += (sender, args) =>
            {
                RefreshControl();
            };

            this.Loaded += (sender, args) =>
            {
                RefreshControl();
            };

        }
        
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            RefreshControl();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            RefreshControl();
        }

        private double GetOrigin()
        {
            return Math.Min(Height, Width) / 2.0;
        }
        private void RefreshControl()
        {
            ArcCalculatorBase arcCalculator = OverlayMode switch
            {
                OverlayMode.Centered => new CenteredArcCalculator(BackgroundCircle.Thickness, ValueCircle.Thickness) { ORIGIN = GetOrigin() },
                OverlayMode.InnerCircle => new InsetArcCalculator(BackgroundCircle.Thickness, ValueCircle.Thickness) { ORIGIN = GetOrigin() },
                OverlayMode.OuterCircle => new OutsetArcCalculator(BackgroundCircle.Thickness, ValueCircle.Thickness) { ORIGIN = GetOrigin() },
                _ => new OutsetArcCalculator(BackgroundCircle.Thickness, ValueCircle.Thickness) { ORIGIN = GetOrigin() },
            };

            arcCalculator.Calculate(Minimum, Maximum, Math.Min(CurrentValue, Maximum));

            BackgroundCircle.Radius = arcCalculator.BackgroundCircleRadius;
            BackgroundCircle.StartPosition = arcCalculator.BackgroundCircleStartPosition;
            BackgroundCircle.EndPosition = arcCalculator.BackgroundCircleEndPosition;

            ValueCircle.Radius = arcCalculator.ValueCircleRadius;
            ValueCircle.StartPosition = arcCalculator.ValueCircleStartPosition;
            ValueCircle.EndPosition = arcCalculator.ValueCircleEndPosition;
            ValueCircle.Angle = arcCalculator.ValueAngle;
        }

        public ProgressArc BackgroundCircle { get; set; } = new ProgressArc();
        public ProgressArc ValueCircle { get; set; } = new ProgressArc();
        //public double MinValue { get; set; } = 10;
        //public double MaxValue { get; set; } = 120;
        //public double CurrentValue { get; set; } = 60;
        //public OverlayMode SelectedOverlayMode { get; set; }


        private static void OnPropChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is not CircularProgressBar circularProgressBar)
            {
                return;
            }

            circularProgressBar.RefreshControl();
        }
        public double CurrentValue
        {
            get { return (double)GetValue(CurrentValueProperty); }
            set
            {
                SetValue(CurrentValueProperty, value);
                OnPropertyChanged();
            }
        }

        public static readonly DependencyProperty CurrentValueProperty =
            DependencyProperty.Register("CurrentValue", typeof(double), typeof(CircularProgressBar),
                new PropertyMetadata(50.0, new PropertyChangedCallback(OnPropChanged)));

        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set
            {
                SetValue(MaximumProperty, value);
                OnPropertyChanged();
            }
        }

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(double), typeof(CircularProgressBar),
                new PropertyMetadata(100.0, new PropertyChangedCallback(OnPropChanged)));



        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); OnPropertyChanged(); }
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(double), typeof(CircularProgressBar),
                new PropertyMetadata(0.0, new PropertyChangedCallback(OnPropChanged)));


        public OverlayMode OverlayMode
        {
            get { return (OverlayMode)GetValue(OverlayModeProperty); }
            set { SetValue(OverlayModeProperty, value); OnPropertyChanged(); }
        }

        // Using a DependencyProperty as the backing store for OverlayMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OverlayModeProperty =
            DependencyProperty.Register("OverlayMode", typeof(OverlayMode), typeof(CircularProgressBar),
                new PropertyMetadata(new PropertyChangedCallback(OnPropChanged)));



        public Brush BackgroundStroke
        {
            get { return (Brush)GetValue(BackgroundStrokeProperty); }
            set { SetValue(BackgroundStrokeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BackgroundStroke.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BackgroundStrokeProperty =
            DependencyProperty.Register("BackgroundStroke", typeof(Brush), typeof(CircularProgressBar), 
                new PropertyMetadata(new SolidColorBrush(Colors.LightGray)));


        public Brush ForegroundStroke
        {
            get { return (Brush)GetValue(ForegroundStrokeProperty); }
            set { SetValue(ForegroundStrokeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BackgroundStroke.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ForegroundStrokeProperty =
            DependencyProperty.Register("ForegroundStroke", typeof(Brush), typeof(CircularProgressBar),
                new PropertyMetadata(new SolidColorBrush(Colors.Purple)));

        public double InnerThickness
        {
            get { return BackgroundCircle.Thickness; }
            set { BackgroundCircle.Thickness = value; OnPropertyChanged(); }
        }

        public double OuterThickness
        {
            get { return ValueCircle.Thickness; }
            set { ValueCircle.Thickness = value; OnPropertyChanged(); }
        }

        public static readonly DependencyProperty ProgressContentProperty =
            DependencyProperty.Register("ProgressContent", typeof(object), typeof(CircularProgressBar));

        public object ProgressContent
        {
            get { return (object)GetValue(ProgressContentProperty); }
            set { SetValue(ProgressContentProperty, value); }
        }
    }

    public enum OverlayMode
    {
        InnerCircle,
        OuterCircle,
        Centered
    }
}
