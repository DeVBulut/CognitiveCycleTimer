using System.Windows;

namespace CognitiveCycleTimer;

public partial class MainWindow : Window
{
    private DonutAnimator? _animator;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        Unloaded += MainWindow_Unloaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Adjust width/height as needed to fit your TextBlock size
        _animator = new DonutAnimator(DonutText, width: 40, height: 24, fps: 30);
        _animator.Start();
    }

    private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
    {
        _animator?.Stop();
    }
}
