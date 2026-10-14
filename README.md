# 🔊 AlarmaDecibeles

**Una alarma que te avisa cuando estás hablando muy fuerte.**

¿Reuniones en las que terminas gritando sin darte cuenta? ¿Llamadas nocturnas con gente durmiendo al lado? AlarmaDecibeles escucha tu micrófono y suena apenas pasas el volumen que tú elijas.

<p align="center">
  <img src="docs/screenshot.png" alt="Ventana de AlarmaDecibeles: medidor LED, fader de Nivel Rojo y botón Iniciar" width="260">
</p>

## ✨ Cómo funciona

| | |
|---|---|
| 🎚️ **Fader "Nivel Rojo"** | Arrastra la perilla para elegir el máximo de decibeles permitido. Todo lo que quede sobre ese nivel es la zona roja. |
| 📊 **Medidor LED** | Muestra en vivo el volumen del micrófono. La línea verde marca tu límite y los segmentos rojos se encienden cuando te pasas. |
| 🔔 **Alarma** | Suena un beep apenas pasas el límite. Si sigues en rojo, vuelve a sonar cada 10 segundos. |
| 🎤 **Micrófono** | Elige qué entrada de audio escuchar, o deja **"Por defecto seleccionado en Windows"** para usar la que tengas configurada en Windows. Pasa el mouse sobre el botón para ver cuál está activa. |
| ▶️ **Iniciar / Detener** | La app solo escucha cuando tú se lo pides. |
| 💾 **Recuerda tu configuración** | El micrófono y el nivel rojo quedan guardados para la próxima vez. |

## 🚀 Cómo usarla

1. Instala el [SDK de .NET 10](https://aka.ms/dotnet/download). Solo funciona en Windows.
2. Clona el repo y córrelo:

   ```powershell
   git clone https://github.com/juanes1eban/AlarmaDecibeles.git
   cd AlarmaDecibeles
   dotnet run
   ```

3. Aprieta **▶ Iniciar** y habla normal para ver dónde queda tu nivel.
4. Sube o baja el fader hasta dejar el límite un poco por encima de tu voz normal. Listo.

> 💡 **Tip:** parte con el límite alrededor de **−20 dB** y ajusta desde ahí. Cada micrófono capta distinto.

### ⚙️ Configuración

Tus preferencias se guardan en `%APPDATA%\AlarmaDecibeles\settings.json`:

```json
{
  "MicName": "Micrófono (Realtek(R) Audio)",
  "ThresholdDb": -20
}
```

El micrófono se guarda cuando aprietas **Guardar** en el selector, y el nivel rojo al cerrar la app. `"MicName": null` significa "por defecto de Windows", que también es lo que se usa si el micrófono guardado ya no está conectado. Para volver a los valores de fábrica, borra el archivo.

## 📏 Sobre los decibeles

Los valores que ves son **dBFS**: decibeles relativos al máximo que puede captar tu micrófono. Van de **−60 dB** (silencio) a **0 dB** (el tope). No equivalen a los dB de un sonómetro, así que el mismo número puede sonar distinto en otro micrófono o con otra ganancia. Lo que importa es calibrar el fader con tu propio equipo.

## 🏷️ Versiones

La versión actual aparece bajo el botón **Micrófono**, con el formato `v1.00.001`, `v1.00.002`, etc.

## 🛠️ Hecho con

- [WPF](https://learn.microsoft.com/dotnet/desktop/wpf/) sobre .NET 10
- [NAudio](https://github.com/naudio/NAudio) para capturar el micrófono

## 📄 Licencia

[GPL-3.0](LICENSE)
