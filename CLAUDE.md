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

## Arquitectura

- **`Controls/SoundControl.cs`**: envuelve un `WaveInEvent` (44.1 kHz, 16-bit, mono). Por cada buffer calcula el RMS, lo pasa a dBFS (`20·log10(rms)`, de −∞ a 0) y dispara `LevelChanged(db)`. Si el nivel supera `ThresholdDb`, también dispara `AlarmTriggered`. Estos eventos llegan en el hilo de NAudio, así que la UI tiene que usar `Dispatcher.Invoke`.
- **`MainWindow`**: UI oscura estilo mixer, todo en code-behind (sin MVVM).
  - Los LEDs del medidor (`Meter`, un `UniformGrid` de 2 columnas) se crean en el constructor. `UpdateMeter()` los repinta según el nivel actual y el umbral.
  - El fader vertical "Nivel Rojo" es un `Slider` con template propio en `Window.Resources`: su valor es el umbral de alerta.
- Los niveles son dBFS relativos al micrófono, no dB SPL calibrados.

## Ojo

El `Slider` dispara `ValueChanged` *durante* `InitializeComponent()`, porque su `Value` en el XAML es distinto del default. En ese momento los elementos hermanos con `x:Name` todavía pueden ser `null`. Cualquier handler que corra en ese instante tiene que tolerarlo: `UpdateMeter` no falla porque `_leds` aún está vacía.
