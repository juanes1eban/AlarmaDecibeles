using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using NAudio;
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
        // WinMM entrega un buffer cada ~100 ms; tanto rato sin datos significa que el micrófono se perdió.
        private static readonly TimeSpan SilenceTimeout = TimeSpan.FromSeconds(3);

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
        private readonly DispatcherTimer _watchdog = new() { Interval = TimeSpan.FromSeconds(1) };
        private double _level = MinDb;
        private DateTime _lastData;
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
            // Settings es la única fuente del umbral: el slider no trae Value en el XAML.
            ThresholdSlider.Value = _settings.ThresholdDb;
            _soundControl.ThresholdDb = ThresholdSlider.Value;
            UpdateMeter();

            _soundControl.LevelChanged += OnLevelChanged;
            // MessageBeep no necesita el hilo de UI: se llama directo desde el hilo de captura.
            _soundControl.AlarmTriggered += SystemSounds.Beep.Play;
            _soundControl.CaptureLost += reason => Dispatcher.BeginInvoke(() => OnCaptureLost(reason));
            _watchdog.Tick += (_, _) =>
            {
                if (_running && DateTime.UtcNow - _lastData > SilenceTimeout)
                    OnCaptureLost("el micrófono dejó de enviar audio");
            };
            Closing += (_, _) =>
            {
                try
                {
                    SaveSettings();
                }
                finally
                {
                    _soundControl.Dispose();
                }
            };
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e) => SetRunning(!_running);

        private void MicButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new MicWindow(SoundControl.GetInputDevices(), _settings.MicName) { Owner = this };
            if (dialog.ShowDialog() != true || dialog.SelectedMic == _settings.MicName) return;

            _settings.MicName = dialog.SelectedMic;
            SaveSettings();
            // Si estaba escuchando, se reinicia con el micrófono nuevo en vez de quedar apagado.
            if (_running)
            {
                SetRunning(false);
                SetRunning(true);
            }
        }

        /// <summary>
        /// Índice actual del micrófono guardado. Se resuelve justo antes de cada Start() porque los índices
        /// de WinMM cambian al enchufar o desenchufar dispositivos. Si no existe, se usa el de Windows.
        /// </summary>
        private int ResolveMic()
        {
            int index = SoundControl.GetInputDevices().IndexOf(_settings.MicName ?? "");
            return index >= 0 ? index : SoundControl.WindowsDefaultDevice;
        }

        // Se calcula al abrir el tooltip para reflejar cambios hechos en Windows mientras la app corre.
        private void MicButton_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            MicButton.ToolTip = ResolveMic() == SoundControl.WindowsDefaultDevice
                ? $"Por defecto de Windows: {SoundControl.GetWindowsDefaultMicName() ?? "ninguno"}"
                : _settings.MicName;
        }

        private void SaveSettings()
        {
            _settings.ThresholdDb = ThresholdSlider.Value;
            try
            {
                _settings.Save();
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                ShowWarning($"No se pudo guardar la configuración en {Settings.FilePath}.\n\nDetalle: {e.Message}");
            }
        }

        private void SetRunning(bool running)
        {
            if (running == _running) return;
            if (running)
            {
                _soundControl.DeviceNumber = ResolveMic();
                try
                {
                    _soundControl.Start();
                }
                catch (MmException e)
                {
                    ShowWarning("No se pudo abrir el micrófono. Revisa que esté conectado o elige otro en 🎤 Micrófono."
                                + $"\n\nDetalle: {e.Message}");
                    return;
                }
                _running = true;
                _lastData = DateTime.UtcNow;
                _watchdog.Start();
                ToggleButton.Content = "■ Detener";
                ToggleButton.BorderBrush = ToggleButton.Foreground = ActiveBrush;
            }
            else
            {
                _running = false;
                _watchdog.Stop();
                _soundControl.Stop();
                ToggleButton.Content = "▶ Iniciar";
                ToggleButton.ClearValue(BorderBrushProperty);
                ToggleButton.ClearValue(ForegroundProperty);
                // Sin esto el medidor queda congelado en el último nivel (a veces en rojo).
                _level = MinDb;
                LevelText.Text = "--- dB";
                UpdateMeter();
            }
        }

        private void OnCaptureLost(string reason)
        {
            if (!_running) return;
            SetRunning(false);
            ShowWarning($"Se dejó de escuchar el micrófono: {reason}.\n\nRevisa que esté conectado y aprieta Iniciar de nuevo.");
        }

        private void ShowWarning(string message) =>
            MessageBox.Show(this, message, "Alarma Decibeles", MessageBoxButton.OK, MessageBoxImage.Warning);

        private void ThresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _soundControl.ThresholdDb = e.NewValue;
            UpdateMeter();
        }

        // BeginInvoke y no Invoke: el hilo de captura no debe quedar esperando a la UI.
        private void OnLevelChanged(double db)
        {
            Dispatcher.BeginInvoke(() =>
            {
                // Un buffer que llega después de Detener no debe volver a pintar el medidor.
                if (!_running) return;
                _lastData = DateTime.UtcNow;
                _level = db;
                LevelText.Text = $"{db:0.0} dB";
                UpdateMeter();
            });
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
