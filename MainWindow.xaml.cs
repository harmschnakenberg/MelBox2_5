using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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

        #region Events
        public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged;

        private static void NotifyStaticPropertyChanged([CallerMemberName] string propertyName = null)
        {
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            StatusClass status = new StatusClass();
            Status_DockPanel.DataContext = status;
            Ticker_DataGrid_LastMessages.DataContext = status;
            Log_DataGrid_LastLogentries.DataContext = status;

            InitializeSerialPort();
            SpManager.StartListening();
            SerialSettingsGrid.DataContext = SpManager.SerialPort;// new SerialSettings();

            StartSignalQualityCheckTimer();

            //TODO:Prüfe, ob SIM eingelegt ist

            CheckGsmSignalQuality(this, null);
           
            SubscribeForIncomingSms();

            Status_TextBlock_StartUpTime.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            Log(Topic.General, Prio.Info, 2003181727, "Neustart MelBox2");
            Messages.Create_StartupMessage();
            StatusClass.MessagesInDb = Sql.CountMessagesInDb();
        }



        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SpManager.Dispose();
        }


    }
}
