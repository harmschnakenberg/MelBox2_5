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

            //TODO:Prüfe, ob SIM eingelegt ist

            CheckGsmSignalQuality(this, null);

            SubscribeNotifyIncomingSms();

            Status_TextBlock_StartUpTime.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            Log(Topic.General, Prio.Info, 2003181727, "Neustart MelBox2");
            Messages.Create_StartupMessage();
            StatusClass.MessagesInDb = Sql.CountMessagesInDb();
        }

        private void SubscribeNotifyIncomingSms()
        {
            //Setzte Textmodus in GSM-Modem
            PortComandExe("AT+CMGF=1");
            System.Threading.Thread.Sleep(500);

            //Setzte Speicherbereich im GSM-Modem "SM" SIM, "PM" Phone-Memory, "MT" + "SM" + "PM"
            PortComandExe("AT+CPMS=\"MT\"");
            System.Threading.Thread.Sleep(500);

            //TODO: funktioniert nur sporadisch - warum?
            //Aktiviere Benachrichtigung von GSM-Modem, wenn neue SMS ankommt

            PortComandExe("AT+CNMI=?");
            System.Threading.Thread.Sleep(500);


            PortComandExe("AT+CNMI=2,1,2,0,1");
            System.Threading.Thread.Sleep(500);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SpManager.Dispose();
        }



    }
}
