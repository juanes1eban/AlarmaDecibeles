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

## Releases

Al subir un tag `vX.Y.Z` se dispara `.github/workflows/release.yml`, que hace lo siguiente:
1. `dotnet publish` self-contained win-x64, con `-p:Version` tomado del tag (pisa el `<Version>` del csproj);
2. arma el instalador con Inno Setup (`installer.iss`, que copia toda la carpeta `publish`);
3. crea el Release con `Output/AlarmaDecibeles-Setup.exe`.

El nombre del asset no lleva versión a propósito: el link del README `releases/latest/download/AlarmaDecibeles-Setup.exe` depende de eso. No cambiar el `AppId` de `installer.iss`, porque Windows lo usa para reconocer las actualizaciones.

## Versión e ícono

- La versión vive solo en `<Version>` del `.csproj` (`1.0.1`), y `MainWindow` la muestra como `v1.00.001` (`Major.Minor:00.Build:000`). Para sacar una versión nueva, se sube el último número: `1.0.2` se ve como `v1.00.002`.
- `mic.ico` es el `<ApplicationIcon>`. Las ventanas WPF sin `Icon` propio heredan el del `.exe`, así que no hace falta asignarlo en el XAML.

## Arquitectura

- **`Controls/SoundControl.cs`**: crea un `WaveInEvent` nuevo (44.1 kHz, 16-bit, mono) en cada `Start()` y lo libera en `Stop()`, para que un error o un dispositivo desconectado no deje una instancia que ya no se pueda reiniciar. Por cada buffer calcula el RMS, lo pasa a dBFS (`20·log10(rms)`, de −∞ a 0) y dispara `LevelChanged(db)`. Si el nivel supera `ThresholdDb`, también dispara `AlarmTriggered`, con un cooldown de 5 s para que los beeps no se solapen. `Start()` lanza `MmException` si no puede abrir el dispositivo. `CaptureLost` avisa si la captura se cae sola.
- **Hilos:** `LevelChanged` y `AlarmTriggered` llegan en el hilo de NAudio. La UI usa `Dispatcher.BeginInvoke` (nunca `Invoke`) para no frenar la captura, y el beep se toca directo desde ese hilo.
- **Micrófono perdido:** WinMM no siempre avisa al desenchufar un dispositivo. Por eso `MainWindow` tiene además un watchdog (`DispatcherTimer`): si pasan 3 s sin datos mientras escucha, detiene la captura y avisa.
- **`Settings.cs`**: JSON en `%APPDATA%\AlarmaDecibeles\settings.json` con `MicName` (`null` = por defecto de Windows) y `ThresholdDb`. Si falta el archivo o está corrupto, `Load()` vuelve a los valores por defecto. El micrófono se guarda por nombre, no por índice, porque los índices cambian al enchufar o desenchufar dispositivos. Ojo: los nombres de WinMM vienen truncados a 31 caracteres.
- **`MicWindow`**: diálogo modal para elegir el micrófono. La primera opción, "Por defecto seleccionado en Windows", se guarda como `MicName = null` y usa `DeviceNumber = -1` (`WAVE_MAPPER`, que sigue al predeterminado de Windows). El nombre que se muestra sale de `MMDeviceEnumerator` (NAudio.Wasapi). El índice del micrófono se resuelve por nombre (`ResolveMic()`) justo antes de cada `Start()`, porque los índices de WinMM cambian al enchufar o desenchufar. Si se cambia de micrófono mientras escucha, la captura se reinicia con el nuevo; guardar el mismo micrófono no hace nada.
- **`App.xaml`**: estilos compartidos entre ventanas (`PanelButton`).
- **`MainWindow`**: UI oscura estilo mixer, todo en code-behind (sin MVVM).
  - Los LEDs del medidor (`Meter`, un `UniformGrid` de 2 columnas) se crean en el constructor. `UpdateMeter()` los repinta según el nivel actual y el umbral.
  - El fader vertical "Nivel Rojo" es un `Slider` con template propio en `Window.Resources`: su valor es el umbral de alerta.
- Los niveles son dBFS relativos al micrófono, no dB SPL calibrados.

## Ojo

No le pongas `Value` al `Slider` en el XAML. El umbral por defecto vive solo en `Settings.ThresholdDb`, y el constructor lo asigna al slider. Además, un `Value` en el XAML dispara `ValueChanged` *durante* `InitializeComponent()`, cuando los elementos hermanos con `x:Name` todavía son `null`: así se originó el crash que se arregló al principio. `UpdateMeter` igual lo tolera, porque `_leds` aún está vacía en ese momento.
