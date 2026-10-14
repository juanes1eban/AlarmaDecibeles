using System;
using System.Collections.Generic;
using System.Media;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using AlarmaDecibeles.Controls;

namespace AlarmaDecibeles
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int Segments = 30;
        private const double MinDb = -60;
        private const double StepDb = -MinDb / Segments;

        private static readonly Brush LitBrush = Hex("#8FB8C4");
        private static readonly Brush OffBrush = Hex("#1F282F");
        private static readonly Brush RedBrush = Hex("#E84545");
        private static readonly Brush RedOffBrush = Hex("#3A1C20");
        private static readonly Brush MarkerBrush = Hex("#4CE07A");
        private static readonly Brush ActiveBrush = Hex("#7FD6A8");

        private readonly SoundControl _soundControl = new();
        // Vacía mientras corre InitializeComponent, así UpdateMeter es no-op si el slider dispara temprano.
        private readonly List<(Rectangle Led, double Db)> _leds = new();
        private double _level = MinDb;
        private bool _running;

        public MainWindow()
        {
            InitializeComponent();

            // UniformGrid llena de arriba hacia abajo: la fila 0 es el segmento más fuerte.
            for (int row = 0; row < Segments; row++)
            {
                double db = MinDb + (Segments - 1 - row) * StepDb;
                for (int col = 0; col < 2; col++)
                {
                    var led = new Rectangle { Margin = new Thickness(1) };
                    Meter.Children.Add(led);
                    _leds.Add((led, db));
                }
            }
            UpdateMeter();

            _soundControl.LevelChanged += OnLevelChanged;
            _soundControl.AlarmTriggered += OnAlarmTriggered;
            Closing += (_, _) => _soundControl.Dispose();
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _running = !_running;
            if (_running)
            {
                _soundControl.Start();
                ToggleButton.Content = "■ Detener";
                ToggleButton.BorderBrush = ToggleButton.Foreground = ActiveBrush;
            }
            else
            {
                _soundControl.Stop();
                ToggleButton.Content = "▶ Iniciar";
                ToggleButton.ClearValue(BorderBrushProperty);
                ToggleButton.ClearValue(ForegroundProperty);
            }
        }

        private void ThresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _soundControl.ThresholdDb = e.NewValue;
            UpdateMeter();
        }

        private void OnLevelChanged(double db)
        {
            Dispatcher.Invoke(() =>
            {
                _level = db;
                LevelText.Text = $"{db:0.0} dB";
                UpdateMeter();
            });
        }

        private void OnAlarmTriggered()
        {
            Dispatcher.Invoke(SystemSounds.Beep.Play);
        }

        private void UpdateMeter()
        {
            double threshold = _soundControl.ThresholdDb;
            foreach (var (led, db) in _leds)
            {
                bool red = db >= threshold;
                bool lit = db < _level;
                led.Fill = red && db < threshold + StepDb ? MarkerBrush
                         : lit ? (red ? RedBrush : LitBrush)
                         : (red ? RedOffBrush : OffBrush);
            }
        }

        private static Brush Hex(string hex)
        {
            var brush = (Brush)new BrushConverter().ConvertFromString(hex)!;
            brush.Freeze();
            return brush;
        }
    }
}
