using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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

            Log(Topic.General, Prio.Info, 2003181727, "Neustart MelBox2");

            StatusClass status = new StatusClass();
            Status_DockPanel.DataContext = status;
            Ticker_DataGrid_LastMessages.DataContext = status;
            Log_DataGrid_LastLogentries.DataContext = status;
            Gsm_TextBox_SerialPortResponse.Text = "Programmstart\r\n";

            Contacts.PermanentSubscribers = Contacts.LoadContactsFromTextFile("PermanentRecievers.txt");

            Gsm_ListBox_PortNames.ItemsSource = System.IO.Ports.SerialPort.GetPortNames();

            #region GSM-Modem initialisieren
            InitializeSerialPort();
            SpManager.StartListening();
            SerialSettingsGrid.DataContext = SpManager.SerialPort;
            StartGsmWriteTimer();
            IsSimReady();
            #endregion

            SubscribeForIncomingSms();

            StartSignalQualityCheckTimer();
            StartSignalQualityWarningTimer();

            CheckGsmSignalQuality(this, null);
            StartSendMessageIntervallTimer();


            //Leite Sprachanrufe an das Bereitchaftshandy weiter

            //PortComandExe("ATD**61*+" + Contacts.Bereitschaftshandy.Phone + "**10#;");            
            //PortComandExe("ATD**61*+4916095285304**05#;");
            //GsmCommandQueue.Add("ATD**61*+4916095285304**05#;");
            GsmCommandQueue.Add("ATD**61*+4915142265412**05#;");

            #region Statusanzeige
            Gsm_TextBlock_SignalQualityCheckIntervall.Text = SignalQualityCheckTimerIntervalSeconds.ToString();
            Status_TextBlock_StartUpTime.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
           
            Messages.Create_StartupMessage();
            StatusClass.MessagesInDb = Sql.CountMessagesInDb();
            #endregion

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {           
            SpManager.Dispose();
        }

        private void Status_Button_ResetWarningsCount_Click(object sender, RoutedEventArgs e)
        {
            StatusClass.ErrorCount = 0;
            StatusClass.WarningCount = 0;
        }

    }
}
