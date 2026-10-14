# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Qué es esto

Alarma que avisa cuando hablas muy fuerte. App WPF en .NET 10 (`net10.0-windows`) que usa NAudio 2.0.1 para capturar el micrófono.

## Comandos

```powershell
dotnet build
dotnet run
```

No hay tests ni linter configurados.

## Versión e ícono

- La versión vive solo en `<Version>` del `.csproj` (`1.0.1`), y `MainWindow` la muestra como `v1.00.001` (`Major.Minor:00.Build:000`). Para sacar una versión nueva, se sube el último número: `1.0.2` se ve como `v1.00.002`.
- `mic.ico` es el `<ApplicationIcon>`. Las ventanas WPF sin `Icon` propio heredan el del `.exe`, así que no hace falta asignarlo en el XAML.

## Arquitectura

- **`Controls/SoundControl.cs`**: envuelve un `WaveInEvent` (44.1 kHz, 16-bit, mono). Por cada buffer calcula el RMS, lo pasa a dBFS (`20·log10(rms)`, de −∞ a 0) y dispara `LevelChanged(db)`. Si el nivel supera `ThresholdDb`, también dispara `AlarmTriggered`, con un cooldown de 5 s para que los beeps no se solapen. Estos eventos llegan en el hilo de NAudio, así que la UI tiene que usar `Dispatcher.Invoke`.
- **`Settings.cs`**: JSON en `%APPDATA%\AlarmaDecibeles\settings.json` con `MicName` (`null` = por defecto de Windows) y `ThresholdDb`. Si falta el archivo o está corrupto, `Load()` vuelve a los valores por defecto. El micrófono se guarda por nombre, no por índice, porque los índices cambian al enchufar o desenchufar dispositivos. Ojo: los nombres de WinMM vienen truncados a 31 caracteres.
- **`MicWindow`**: diálogo modal para elegir el micrófono. La primera opción, "Por defecto seleccionado en Windows", se guarda como `MicName = null` y usa `DeviceNumber = -1` (`WAVE_MAPPER`, que sigue al predeterminado de Windows). El nombre que se muestra sale de `MMDeviceEnumerator` (NAudio.Wasapi). Cambiar de dispositivo detiene la captura, porque `WaveInEvent.DeviceNumber` solo se aplica en el próximo `StartRecording()`.
- **`App.xaml`**: estilos compartidos entre ventanas (`PanelButton`).
- **`MainWindow`**: UI oscura estilo mixer, todo en code-behind (sin MVVM).
  - Los LEDs del medidor (`Meter`, un `UniformGrid` de 2 columnas) se crean en el constructor. `UpdateMeter()` los repinta según el nivel actual y el umbral.
  - El fader vertical "Nivel Rojo" es un `Slider` con template propio en `Window.Resources`: su valor es el umbral de alerta.
- Los niveles son dBFS relativos al micrófono, no dB SPL calibrados.

## Ojo

El `Slider` dispara `ValueChanged` *durante* `InitializeComponent()`, porque su `Value` en el XAML es distinto del default. En ese momento los elementos hermanos con `x:Name` todavía pueden ser `null`. Cualquier handler que corra en ese instante tiene que tolerarlo: `UpdateMeter` no falla porque `_leds` aún está vacía.
