using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace SpinningDonut
{
    public partial class MainWindow : Window
    {
        private DonutAnimator _animator;

        // Timer fields
        private readonly DispatcherTimer _timer;
        private TimeSpan _remaining;
        private bool _isRunning;

        public MainWindow()
        {
            InitializeComponent();
            SizeChanged += MainWindow_SizeChanged;

            // Create donut animator (fixed grid size – viewbox handles scaling)
            _animator = new DonutAnimator(DonutTextBlock, width: 60, height: 35);

            // Timer setup
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;

            _remaining = TimeSpan.FromMinutes(25);
            UpdateTimerDisplay();

            // Button bindings
            StartButton.Click += StartButton_Click;
            PauseButton.Click += PauseButton_Click;
            ClearButton.Click += ClearButton_Click;
        }


        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (_remaining.TotalSeconds > 0)
            {
                _timer.Start();
                _animator.Start();
                _isRunning = true;
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            _animator.Stop();
            _isRunning = false;
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            _animator.Stop();
            _animator.Reset();              
            _isRunning = false;

            _remaining = TimeSpan.FromMinutes(25);
            UpdateTimerDisplay();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_remaining.TotalSeconds > 0)
            {
                _remaining = _remaining.Subtract(TimeSpan.FromSeconds(1));
                UpdateTimerDisplay();
            }
            else
            {
                _timer.Stop();
                _animator.Stop();
                _isRunning = false;
            }
        }

        private void UpdateTimerDisplay()
        {
            TimerDisplay.Text = _remaining.ToString(@"mm\:ss");
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (TopViewbox == null)
                return;

            var transform = TopViewbox.LayoutTransform as ScaleTransform
                            ?? new ScaleTransform(1, 1);

            TopViewbox.LayoutTransform = transform;

            double naturalScale = GetViewboxScale(TopViewbox);

            double minScale = 0.7;   // never smaller than 70%
            double maxScale = 1.0;   // never larger than 100%

            double finalScale = Math.Clamp(naturalScale, minScale, maxScale);

            transform.ScaleX = finalScale;
            transform.ScaleY = finalScale;
        }


        private double GetViewboxScale(Viewbox vb)
        {
            if (vb.Child == null)
                return 1.0;

            double sx = vb.ActualWidth / vb.Child.DesiredSize.Width;
            double sy = vb.ActualHeight / vb.Child.DesiredSize.Height;

            return Math.Min(sx, sy); // Uniform scaling
        }
    }
}
