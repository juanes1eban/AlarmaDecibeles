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
| 🔔 **Alarma** | Mientras estés sobre el límite, suena un beep del sistema. |
| ▶️ **Iniciar / Detener** | La app solo escucha cuando tú se lo pides. |

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

## 📏 Sobre los decibeles

Los valores que ves son **dBFS**: decibeles relativos al máximo que puede captar tu micrófono. Van de **−60 dB** (silencio) a **0 dB** (el tope). No equivalen a los dB de un sonómetro, así que el mismo número puede sonar distinto en otro micrófono o con otra ganancia. Lo que importa es calibrar el fader con tu propio equipo.

## 🛠️ Hecho con

- [WPF](https://learn.microsoft.com/dotnet/desktop/wpf/) sobre .NET 10
- [NAudio](https://github.com/naudio/NAudio) para capturar el micrófono

## 📄 Licencia

[GPL-3.0](LICENSE)
