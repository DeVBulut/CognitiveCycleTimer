using System;
using System.IO;
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
        private readonly MediaPlayer _alarmPlayer;
        private readonly string _alarmFolder;

        private static readonly string[] AlarmExtensions = { ".mp3", ".wav", ".wma", ".aac", ".m4a" };

        private TimeSpan _remaining;
        private bool _isRunning;
        private bool _alarmActive;

        public MainWindow()
        {
            InitializeComponent();
            SizeChanged += MainWindow_SizeChanged;

            // Create donut animator (fixed grid size – viewbox handles scaling)
            _animator = new DonutAnimator(DonutTextBlock, width: 60, height: 35);

            // Timer setup
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;

            // Alarm setup
            _alarmPlayer = new MediaPlayer();
            _alarmPlayer.MediaEnded += AlarmPlayer_MediaEnded;

            _alarmFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "alarms");
            Directory.CreateDirectory(_alarmFolder);

            _remaining = TimeSpan.FromMinutes(25);
            UpdateTimerDisplay();

            // Button bindings
            StartButton.Click += StartButton_Click;
            PauseButton.Click += PauseButton_Click;
            ClearButton.Click += ClearButton_Click;
        }


        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            StopAlarm();

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
            StopAlarm();
            _isRunning = false;
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            _animator.Stop();
            StopAlarm();
            _animator.Reset();              
            _isRunning = false;

            _remaining = TimeSpan.FromMinutes(25);
            UpdateTimerDisplay();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_remaining.TotalSeconds <= 0)
            {
                HandleTimerCompleted();
                return;
            }

            _remaining = _remaining.Subtract(TimeSpan.FromSeconds(1));

            if (_remaining.TotalSeconds <= 0)
            {
                _remaining = TimeSpan.Zero;
                UpdateTimerDisplay();
                HandleTimerCompleted();
            }
            else
            {
                UpdateTimerDisplay();
            }
        }

        private void HandleTimerCompleted()
        {
            _timer.Stop();
            _animator.Stop();
            _isRunning = false;
            StartAlarm();
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

        private void StartAlarm()
        {
            if (_alarmActive)
                return;

            Directory.CreateDirectory(_alarmFolder);

            string? alarmFile = FindFirstAlarmSound();
            if (alarmFile == null)
                return;

            try
            {
                _alarmPlayer.Open(new Uri(alarmFile));
                _alarmPlayer.Position = TimeSpan.Zero;
                _alarmPlayer.Play();
                _alarmActive = true;
            }
            catch
            {
                _alarmActive = false; // Ignore playback errors silently
            }
        }

        private void StopAlarm()
        {
            if (!_alarmActive)
                return;

            _alarmPlayer.Stop();
            _alarmPlayer.Close();
            _alarmActive = false;
        }

        private string? FindFirstAlarmSound()
        {
            foreach (var file in Directory.EnumerateFiles(_alarmFolder))
            {
                string extension = Path.GetExtension(file);
                foreach (var allowedExt in AlarmExtensions)
                {
                    if (string.Equals(extension, allowedExt, StringComparison.OrdinalIgnoreCase))
                        return Path.GetFullPath(file);
                }
            }

            return null;
        }

        private void AlarmPlayer_MediaEnded(object? sender, EventArgs e)
        {
            if (!_alarmActive)
                return;

            _alarmPlayer.Position = TimeSpan.Zero;
            _alarmPlayer.Play();
        }
    }
}
