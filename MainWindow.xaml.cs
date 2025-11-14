using System.Windows;

namespace SpinningDonut
{
    public partial class MainWindow : Window
    {
        private DonutAnimator _animator;

        public MainWindow()
        {
            InitializeComponent();

            // Try smaller / larger sizes if needed (width: 60–100, height: 20–35)
            // I found the sweeet spot to be 70x40 for Consolas at 12pt font size
            _animator = new DonutAnimator(DonutTextBlock, width: 60, height: 35);

            Loaded += (_, __) => _animator.Start();
            Unloaded += (_, __) => _animator.Stop();
            Closed += (_, __) => _animator.Stop();
        }
    }
}
