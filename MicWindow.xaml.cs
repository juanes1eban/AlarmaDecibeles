using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using AlarmaDecibeles.Controls;

namespace AlarmaDecibeles
{
    /// <summary>
    /// Diálogo para elegir la fuente de audio de entrada.
    /// </summary>
    public partial class MicWindow : Window
    {
        private const string WindowsDefaultLabel = "Por defecto seleccionado en Windows";

        /// <summary>Micrófono elegido, o null si se eligió el por defecto de Windows.</summary>
        public string? SelectedMic => MicList.SelectedIndex > 0 ? (string)MicList.SelectedItem : null;

        public MicWindow(IReadOnlyList<string> devices, string? current)
        {
            InitializeComponent();
            MicList.ItemsSource = devices.Prepend(WindowsDefaultLabel).ToList();
            MicList.SelectedIndex = current == null ? 0 : Math.Max(0, devices.ToList().IndexOf(current) + 1);

            string? windowsMic = SoundControl.GetWindowsDefaultMicName();
            WindowsDefaultText.Text = windowsMic == null
                ? "Windows no tiene un micrófono seleccionado."
                : $"Windows tiene seleccionado: {windowsMic}";
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (MicList.SelectedIndex < 0) return;
            DialogResult = true;
        }
    }
}
