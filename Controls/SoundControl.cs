using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NAudio;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace AlarmaDecibeles.Controls
{
    /// <summary>
    /// Escucha el micrófono y avisa cuando el volumen supera un umbral (en dBFS).
    /// </summary>
    public class SoundControl : IDisposable
    {
        /// <summary>WAVE_MAPPER: WinMM enruta al micrófono por defecto de Windows.</summary>
        public const int WindowsDefaultDevice = -1;

        private static readonly TimeSpan AlarmCooldown = TimeSpan.FromSeconds(5);

        // Se crea uno nuevo en cada Start(): así un error o un dispositivo desconectado
        // no deja la instancia en un estado del que no se puede volver a iniciar.
        private WaveInEvent? _waveIn;
        private DateTime _lastAlarm = DateTime.MinValue;

        /// <summary>Lo fija la UI desde la configuración guardada.</summary>
        public double ThresholdDb { get; set; }

        /// <summary>Índice del dispositivo de entrada (o WindowsDefaultDevice); se aplica en el próximo Start().</summary>
        public int DeviceNumber { get; set; } = WindowsDefaultDevice;

        public event Action<double>? LevelChanged;
        public event Action? AlarmTriggered;
        /// <summary>La captura se detuvo sola (error o dispositivo desconectado). Llega con el motivo.</summary>
        public event Action<string>? CaptureLost;

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

        /// <summary>Empieza a capturar. Lanza MmException si el dispositivo no existe o no se puede abrir.</summary>
        public void Start()
        {
            Stop();
            var waveIn = new WaveInEvent { DeviceNumber = DeviceNumber, WaveFormat = new WaveFormat(44100, 16, 1) };
            waveIn.DataAvailable += OnDataAvailable;
            waveIn.RecordingStopped += OnRecordingStopped;
            try
            {
                waveIn.StartRecording();
            }
            catch
            {
                Release(waveIn);
                throw;
            }
            _waveIn = waveIn;
        }

        public void Stop()
        {
            if (_waveIn == null) return;
            Release(_waveIn);
            _waveIn = null;
        }

        private void Release(WaveInEvent waveIn)
        {
            // Se desuscribe primero: cualquier RecordingStopped que llegue después es inesperado.
            waveIn.DataAvailable -= OnDataAvailable;
            waveIn.RecordingStopped -= OnRecordingStopped;
            try
            {
                waveIn.Dispose();
            }
            catch (MmException)
            {
                // El dispositivo ya no existe (p. ej. se desenchufó): no hay nada más que cerrar.
            }
        }

        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            if (sender != _waveIn) return;
            Stop();
            CaptureLost?.Invoke(e.Exception?.Message ?? "la grabación se detuvo");
        }

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
            Stop();
            GC.SuppressFinalize(this);
        }
    }
}
