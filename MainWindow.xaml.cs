using System;
using System.IO;
using System.Linq;
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
        private string? _selectedAlarmPath;

        public MainWindow()
        {
            InitializeComponent();
            SizeChanged += MainWindow_SizeChanged;
            Loaded += MainWindow_Loaded;

            // Create donut animator (fixed grid size - viewbox handles scaling)
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
            TestButton.Click += TestButton_Click;
            AlarmSelector.SelectionChanged += AlarmSelector_SelectionChanged;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshAlarmList();
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

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            StopAlarm();
            _timer.Stop();
            _animator.Stop();

            _remaining = TimeSpan.FromSeconds(10);
            UpdateTimerDisplay();

            if (_remaining.TotalSeconds > 0)
            {
                _timer.Start();
                _animator.Start();
                _isRunning = true;
            }
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

            string? alarmFile = GetAlarmFileToUse();
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

        private string? GetAlarmFileToUse()
        {
            if (!string.IsNullOrWhiteSpace(_selectedAlarmPath) && File.Exists(_selectedAlarmPath))
                return _selectedAlarmPath;

            RefreshAlarmList();
            return _selectedAlarmPath;
        }

        private bool IsAllowedAlarmFile(string filePath)
        {
            string extension = Path.GetExtension(filePath);
            return AlarmExtensions.Any(ext => string.Equals(extension, ext, StringComparison.OrdinalIgnoreCase));
        }

        private void RefreshAlarmList()
        {
            Directory.CreateDirectory(_alarmFolder);

            var files = Directory
                .EnumerateFiles(_alarmFolder)
                .Where(IsAllowedAlarmFile)
                .OrderBy(Path.GetFileName)
                .ToList();

            AlarmSelector.SelectionChanged -= AlarmSelector_SelectionChanged;
            AlarmSelector.Items.Clear();

            if (files.Count == 0)
            {
                AlarmSelector.Items.Add(new ComboBoxItem
                {
                    Content = "No alarm files found",
                    IsEnabled = false
                });
                AlarmSelector.SelectedIndex = 0;
                AlarmSelector.IsEnabled = false;
                _selectedAlarmPath = null;

                AlarmSelector.SelectionChanged += AlarmSelector_SelectionChanged;
                return;
            }

            AlarmSelector.IsEnabled = true;

            string desiredSelection = _selectedAlarmPath ?? files[0];
            if (!files.Any(f => string.Equals(f, desiredSelection, StringComparison.OrdinalIgnoreCase)))
                desiredSelection = files[0];

            foreach (var file in files)
            {
                var item = new ComboBoxItem
                {
                    Content = Path.GetFileName(file),
                    Tag = Path.GetFullPath(file)
                };

                AlarmSelector.Items.Add(item);

                if (string.Equals(file, desiredSelection, StringComparison.OrdinalIgnoreCase))
                    AlarmSelector.SelectedItem = item;
            }

            _selectedAlarmPath = (AlarmSelector.SelectedItem as ComboBoxItem)?.Tag as string ?? files[0];

            AlarmSelector.SelectionChanged += AlarmSelector_SelectionChanged;
        }

        private void AlarmPlayer_MediaEnded(object? sender, EventArgs e)
        {
            if (!_alarmActive)
                return;

            _alarmPlayer.Position = TimeSpan.Zero;
            _alarmPlayer.Play();
        }

        private void AlarmSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AlarmSelector.SelectedItem is ComboBoxItem item && item.Tag is string path)
            {
                _selectedAlarmPath = path;
            }
        }
    }
}
