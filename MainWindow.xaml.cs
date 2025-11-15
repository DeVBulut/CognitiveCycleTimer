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
        private DispatcherTimer _timer;
        private TimeSpan _remaining;
        private bool _isRunning;

        public MainWindow()
        {
            InitializeComponent();
            SizeChanged += MainWindow_SizeChanged;

            _animator = new DonutAnimator(DonutTextBlock, width: 60, height: 35);

            Loaded += (_, __) => _animator.Start();
            Unloaded += (_, __) => _animator.Stop();
            Closed += (_, __) => _animator.Stop();

            // Timer setup
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;
            _remaining = TimeSpan.FromMinutes(25);
            UpdateTimerDisplay();

            StartButton.Click += StartButton_Click;
            PauseButton.Click += PauseButton_Click;
            ClearButton.Click += ClearButton_Click;
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (_remaining.TotalSeconds > 0)
            {
                _timer.Start();
                _isRunning = true;
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            _isRunning = false;
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
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

            // Get actual scale the Viewbox wants to apply
            var transform = TopViewbox.LayoutTransform as ScaleTransform;

            if (transform == null)
            {
                transform = new ScaleTransform(1, 1);
                TopViewbox.LayoutTransform = transform;
            }

            // Extract the natural scale that Viewbox computed internally
            double naturalScale = GetViewboxScale(TopViewbox);

            // Enforce minimum scale
            double minScale = 0.7; // <--- adjust here (0.7 feels great)

            double finalScale = Math.Max(naturalScale, minScale);

            transform.ScaleX = finalScale;
            transform.ScaleY = finalScale;
        }

        private double GetViewboxScale(Viewbox vb)
        {
            if (vb.Child == null)
                return 1.0;

            // How much width scale is needed
            double sx = vb.ActualWidth / vb.Child.DesiredSize.Width;
            double sy = vb.ActualHeight / vb.Child.DesiredSize.Height;

            // Viewbox Stretch="Uniform" means scale = min(sx, sy)
            return Math.Min(sx, sy);
        }


    }
}
