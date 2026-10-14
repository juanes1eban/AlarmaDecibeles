using System;
using NAudio.Wave;

namespace AlarmaDecibeles.Controls
{
    /// <summary>
    /// Escucha el micrófono y avisa cuando el volumen supera un umbral (en dBFS).
    /// </summary>
    public class SoundControl : IDisposable
    {
        private readonly WaveInEvent _waveIn;

        public double ThresholdDb { get; set; } = -20;

        public event Action<double>? LevelChanged;
        public event Action? AlarmTriggered;

        public SoundControl()
        {
            _waveIn = new WaveInEvent { WaveFormat = new WaveFormat(44100, 16, 1) };
            _waveIn.DataAvailable += OnDataAvailable;
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
            if (db > ThresholdDb) AlarmTriggered?.Invoke();
        }

        public void Dispose()
        {
            _waveIn.DataAvailable -= OnDataAvailable;
            _waveIn.Dispose();
        }
    }
}
