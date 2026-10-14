using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace AlarmaDecibeles
{
    /// <summary>
    /// Configuración persistente en %APPDATA%\AlarmaDecibeles\settings.json.
    /// </summary>
    public class Settings
    {
        public static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AlarmaDecibeles", "settings.json");

        // Se guarda por nombre y no por índice: los índices cambian al enchufar/desenchufar dispositivos.
        // null = el micrófono por defecto de Windows.
        public string? MicName { get; set; }
        public double ThresholdDb { get; set; } = -20;

        public static Settings Load()
        {
            try
            {
                return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath)) ?? new Settings();
            }
            catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
            {
                // Sin archivo o archivo corrupto: partir con los valores por defecto.
                return new Settings();
            }
        }

        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            // Encoder relajado para que el archivo sea legible a mano ("Micrófono", no "Micrófono").
            var options = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, options));
        }
    }
}
