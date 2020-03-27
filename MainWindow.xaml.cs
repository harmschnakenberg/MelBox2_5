using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

            SerialSettingsGrid.DataContext = new SerialSettings();

            InitializeSerialPort();
            SpManager.StartListening();
            StartSignalQualityCheckTimer();

            Log(Topic.General, Prio.Info, 2003181727, "Neustart MelBox2");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SpManager.Dispose();
        }
    }
}
