using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
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
        private readonly Settings _settings = Settings.Load();
        // Vacía mientras corre InitializeComponent, así UpdateMeter es no-op si el slider dispara temprano.
        private readonly List<(Rectangle Led, double Db)> _leds = new();
        private double _level = MinDb;
        private bool _running;

        public MainWindow()
        {
            InitializeComponent();
            var version = Assembly.GetExecutingAssembly().GetName().Version!;
            VersionText.Text = $"v{version.Major}.{version.Minor:00}.{version.Build:000}";

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
            ThresholdSlider.Value = _settings.ThresholdDb;
            UpdateMeter();
            ApplyMic();

            _soundControl.LevelChanged += OnLevelChanged;
            _soundControl.AlarmTriggered += OnAlarmTriggered;
            Closing += (_, _) =>
            {
                SaveSettings();
                _soundControl.Dispose();
            };
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e) => SetRunning(!_running);

        private void MicButton_Click(object sender, RoutedEventArgs e)
        {
            var devices = SoundControl.GetInputDevices();
            var current = _soundControl.DeviceNumber == SoundControl.WindowsDefaultDevice
                ? null
                : devices.ElementAtOrDefault(_soundControl.DeviceNumber);
            var dialog = new MicWindow(devices, current) { Owner = this };
            if (dialog.ShowDialog() != true) return;

            // El dispositivo solo cambia al iniciar de nuevo, así que se detiene la captura actual.
            SetRunning(false);
            _settings.MicName = dialog.SelectedMic;
            SaveSettings();
            ApplyMic();
        }

        private void ApplyMic()
        {
            // Sin micrófono guardado, o si el guardado ya no existe, se usa el por defecto de Windows.
            int index = SoundControl.GetInputDevices().IndexOf(_settings.MicName ?? "");
            _soundControl.DeviceNumber = index >= 0 ? index : SoundControl.WindowsDefaultDevice;
        }

        // Se calcula al abrir el tooltip para reflejar cambios hechos en Windows mientras la app corre.
        private void MicButton_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            MicButton.ToolTip = _soundControl.DeviceNumber == SoundControl.WindowsDefaultDevice
                ? $"Por defecto de Windows: {SoundControl.GetWindowsDefaultMicName() ?? "ninguno"}"
                : SoundControl.GetInputDevices().ElementAtOrDefault(_soundControl.DeviceNumber) ?? "Micrófono desconectado";
        }

        private void SaveSettings()
        {
            _settings.ThresholdDb = ThresholdSlider.Value;
            _settings.Save();
        }

        private void SetRunning(bool running)
        {
            if (running == _running) return;
            _running = running;
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
