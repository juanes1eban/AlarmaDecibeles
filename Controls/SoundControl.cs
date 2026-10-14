using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace AlarmaDecibeles.Controls
{
    /// <summary>
    /// Escucha el micrófono y avisa cuando el volumen supera un umbral (en dBFS).
    /// </summary>
    public class SoundControl : IDisposable
    {
        private static readonly TimeSpan AlarmCooldown = TimeSpan.FromSeconds(10);

        private readonly WaveInEvent _waveIn;
        private DateTime _lastAlarm = DateTime.MinValue;

        public double ThresholdDb { get; set; } = -20;

        public event Action<double>? LevelChanged;
        public event Action? AlarmTriggered;

        public SoundControl()
        {
            _waveIn = new WaveInEvent { WaveFormat = new WaveFormat(44100, 16, 1) };
            _waveIn.DataAvailable += OnDataAvailable;
        }

        /// <summary>WAVE_MAPPER: WinMM enruta al micrófono por defecto de Windows.</summary>
        public const int WindowsDefaultDevice = -1;

        /// <summary>Índice del dispositivo de entrada (o WindowsDefaultDevice); se aplica en el próximo Start().</summary>
        public int DeviceNumber
        {
            get => _waveIn.DeviceNumber;
            set => _waveIn.DeviceNumber = value;
        }

        public static List<string> GetInputDevices()
        {
            var names = new List<string>();
            for (int i = 0; i < WaveInEvent.DeviceCount; i++)
                names.Add(WaveInEvent.GetCapabilities(i).ProductName);
            return names;
        }

        /// <summary>Nombre del micrófono que Windows tiene como predeterminado, o null si no hay ninguno.</summary>
        public static string? GetWindowsDefaultMicName()
        {
            try
            {
                using var enumerator = new MMDeviceEnumerator();
                using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
                return device.FriendlyName;
            }
            catch (COMException)
            {
                // Windows lanza esto cuando no hay ningún dispositivo de captura activo.
                return null;
            }
        }

        public void Start() => _waveIn.StartRecording();
        public void Stop() => _waveIn.StopRecording();

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            int sampleCount = e.BytesRecorded / 2;
            if (sampleCount == 0) return;

            double sumSquares = 0;
            for (int i = 0; i < e.BytesRecorded; i += 2)
            {
                short sample = BitConverter.ToInt16(e.Buffer, i);
                double normalized = sample / 32768.0;
                sumSquares += normalized * normalized;
            }

            double rms = Math.Sqrt(sumSquares / sampleCount);
            double db = 20 * Math.Log10(rms > 0 ? rms : 1e-9);

            LevelChanged?.Invoke(db);
            // Suena al entrar en rojo y, si sigue en rojo, de nuevo recién tras el cooldown.
            if (db > ThresholdDb && DateTime.UtcNow - _lastAlarm >= AlarmCooldown)
            {
                _lastAlarm = DateTime.UtcNow;
                AlarmTriggered?.Invoke();
            }
        }

        public void Dispose()
        {
            _waveIn.DataAvailable -= OnDataAvailable;
            _waveIn.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
