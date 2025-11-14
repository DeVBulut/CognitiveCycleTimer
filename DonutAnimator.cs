using System;
using System.Text;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SpinningDonut
{
    public class DonutAnimator
    {
        private readonly TextBlock _target;
        private readonly DispatcherTimer _timer;

        private readonly int _width;
        private readonly int _height;

        private double _A;
        private double _B;

        // Aspect ratio correction for Consolas (width:height ≈ 1 : 1.8)
        private const double ASPECT = 0.55;

        // I found the sweeet spot to be 70x40 for Consolas at 12pt font size
        public DonutAnimator(TextBlock target, int width = 80, int height = 24)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _width = width;
            _height = height;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000.0 / 60.0) // ~60 FPS
            };

            _timer.Tick += OnTick;
        }

        public void Start()
        {
            if (!_timer.IsEnabled)
            {
                _A = 0;
                _B = 0;
                _timer.Start();
            }
        }

        public void Stop()
        {
            if (_timer.IsEnabled)
            {
                _timer.Stop();
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            _target.Text = RenderFrame();
        }

        private string RenderFrame()
        {
            int bufferSize = _width * _height;

            char[] output = new char[bufferSize];
            double[] zbuffer = new double[bufferSize];

            for (int i = 0; i < bufferSize; i++)
            {
                output[i] = ' ';
                zbuffer[i] = 0;
            }

            double cosA = Math.Cos(_A);
            double sinA = Math.Sin(_A);
            double cosB = Math.Cos(_B);
            double sinB = Math.Sin(_B);

            const double R1 = 1.0;
            const double R2 = 2.0;
            const double K2 = 5.0;

            double K1 = _width * K2 * 3.0 / (8.0 * (R1 + R2));

            const string luminanceChars = ".,-~:;=!*#$@";

            // Donut geometry loops
            for (double theta = 0; theta < 2 * Math.PI; theta += 0.07)
            {
                double costheta = Math.Cos(theta);
                double sintheta = Math.Sin(theta);

                for (double phi = 0; phi < 2 * Math.PI; phi += 0.02)
                {
                    double cosphi = Math.Cos(phi);
                    double sinphi = Math.Sin(phi);

                    double circleX = R2 + R1 * costheta;
                    double circleY = R1 * sintheta;

                    double x = circleX * (cosB * cosphi + sinA * sinB * sinphi) - circleY * cosA * sinB;
                    double y = circleX * (sinB * cosphi - sinA * cosB * sinphi) + circleY * cosA * cosB;
                    double z = K2 + cosA * circleX * sinphi + circleY * sinA;
                    double ooz = 1.0 / z;

                    int xp = (int)(_width / 2 + K1 * ooz * x);
                    int yp = (int)(_height / 2 - K1 * ooz * y * ASPECT);

                    if (xp < 0 || xp >= _width || yp < 0 || yp >= _height)
                        continue;

                    int idx = xp + _width * yp;

                    double L =
                        cosphi * costheta * sinB
                        - cosA * costheta * sinphi
                        - sinA * sintheta
                        + cosB * (cosA * sintheta - costheta * sinA * sinphi);

                    if (L > 0)
                    {
                        if (ooz > zbuffer[idx])
                        {
                            zbuffer[idx] = ooz;
                            int lumIndex = (int)(L * 8.0);
                            if (lumIndex < 0) lumIndex = 0;
                            if (lumIndex >= luminanceChars.Length)
                                lumIndex = luminanceChars.Length - 1;

                            output[idx] = luminanceChars[lumIndex];
                        }
                    }
                }
            }

            _A += 0.04;
            _B += 0.02;

            StringBuilder sb = new StringBuilder(bufferSize + _height);

            for (int row = 0; row < _height; row++)
            {
                sb.Append(output, row * _width, _width);
                sb.Append('\n');
            }

            return sb.ToString();
        }
    }
}
