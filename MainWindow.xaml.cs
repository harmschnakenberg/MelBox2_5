using System;
using System.Windows;

namespace MelBox2_5
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// Regionen zuklappen: Ctrl+M, Ctrl+S
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly Sql sql = new Sql();

        internal static Sql Sql { get => sql; }

        public MainWindow()
        {
            InitializeComponent();

            InitializeSerialPort();
            SpManager.StartListening();

            SerialSettingsGrid.DataContext = SpManager.SerialPort;// new SerialSettings();
            Status_DockPanel.DataContext = new StatusClass();

            StartSignalQualityCheckTimer();

            Status_TextBlock_StartUpTime.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            Log(Topic.General, Prio.Info, 2003181727, "Neustart MelBox2");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SpManager.Dispose();
        }



    }
}
