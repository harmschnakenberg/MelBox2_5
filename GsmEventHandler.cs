using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Controls;

namespace MelBox2_5
{

    public partial class MainWindow : Window
    {
        public int MinRequieredSignalQuality { get; } = Properties.Settings.Default.MinRequieredSignalQuality;

        #region Protokollierung GSM-Datenverkehr
        private void Gsm_TextBox_SerialPortResponse_TextChanged(object sender, TextChangedEventArgs e)
        {
            Gsm_ScrollViewer_SerialPortResponse.ScrollToEnd();
        }

        /// <summary>
        /// Liest die vom COM-Port erhaltenden Daten und schreibt sie in eine TextBox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">Empfangene Daten</param>
        void GsmTrafficLogger(object sender, SerialDataEventArgs e)
        {
            //Quelle: https://stackoverflow.com/questions/4016921/wpf-invoke-a-control

            int maxTextLength = 1000; // maximum text length in text box
            string textBoxContent = string.Empty; //für Inhalt der TextBox

            var t = Task.Run(() => {

                this.Gsm_TextBox_SerialPortResponse.Dispatcher.Invoke(DispatcherPriority.Background,
                        new Action(() => { textBoxContent = this.Gsm_TextBox_SerialPortResponse.Text; }));

                textBoxContent = textBoxContent.Replace("\r\n\r\n", "\r\n").Replace("\r\r", "\r");

                if (textBoxContent.Length > maxTextLength)
                    textBoxContent = textBoxContent.Remove(0, textBoxContent.Length - maxTextLength);

                // This application is connected to a GPS sending ASCCI characters, so data is converted to text
                string str = Encoding.ASCII.GetString(e.Data);

                if (str.Contains("ERROR"))
                {
                    Log(Topic.SMS, Prio.Fehler, 2003272054, "Fehler Antwort GSM-Modem: " + str.Replace("\r\n", " "));
                }

                this.Gsm_TextBox_SerialPortResponse.Dispatcher.Invoke(DispatcherPriority.Background,
                    new Action(() => { this.Gsm_TextBox_SerialPortResponse.Text = textBoxContent + str; }));

                //Antworten protokollieren
                //string dir = System.IO.Path.GetDirectoryName(TextLogPath);
                //if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);

                //using (System.IO.StreamWriter file = System.IO.File.AppendText(TextLogPath))
                //{
                //    file.WriteLine("\r\n" + DateTime.Now.ToShortTimeString() + ":\r\n" + str);
                //}

            });
            t.Wait(2000);
        }

        #endregion

        #region Signalqualitätscheck
        /// <summary>
        /// Triggert die Abfrage der momentanen Netzqualität mit ShowGsmSignalQuality(...)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void CheckGsmSignalQuality(object sender, EventArgs e)
        {
            //Ereignis abbonieren
            SpManager.NewSerialDataRecieved += ShowGsmSignalQuality;

            //Signalqualität abfragen    
            GsmCommandQueue.Add("AT+CSQ");
            //PortComandExe("AT+CSQ");
        }

        /// <summary>
        /// Ermittelt die momentane Netzqualität für das GSM-Netz und schreibt den Wert in eine ProgressBar
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">Antwort aus GSM-Modem</param>
        private void ShowGsmSignalQuality(object sender, SerialDataEventArgs e)
        {
            string portResponse = Encoding.ASCII.GetString(e.Data);

            //Antwort OK?
            if (portResponse.Contains("CSQ:"))
            {
                try
                {
                    //Finde Zahlenwerte
                    Regex rS = new Regex(@"CSQ:\s[0-9]+,");
                    Match m = rS.Match(portResponse);
                    if (!m.Success)
                    {
                        Log(Topic.SMS, Prio.Warnung, 2003230749, "Antwort von GSM-Modem auf Signalqualität konnte nicht verarbeitet werden:\r\n" + portResponse);
                        return;
                    }

                    //Mögliche Werte: 2 - 9 marginal, 10 - 14 OK, 15 - 19 Good, 20 - 30 Excellent, 99 = kein Signal
                    int.TryParse(m.Value.Remove(0, 4).Trim(','), out int signalQuality);

                    this.Gsm_TextBox_SerialPortResponse.Dispatcher.Invoke(DispatcherPriority.Normal,
                        new Action(() => { this.Status_ProgressBar_SignalQuality.Value = signalQuality; }));

                    StatusClass.GsmSignalQuality = signalQuality;

                    //Wenn signalQuality zu gering ist: Nachricht an Admin
                    if (signalQuality < MinRequieredSignalQuality)
                        Messages.Create_SignalQualityMessage(signalQuality);
                }
                finally
                {
                    //Ereignisabbonement kündigen.
                    SpManager.NewSerialDataRecieved -= ShowGsmSignalQuality;
                }
            }
        }

        #endregion

        #region SMS Empfangen
        void SubscribeForIncomingSms()
        {
            Gsm_TextBox_SerialPortResponse.Text += "Abboniere SMS-Benachrichtigungen.\r\n";

            //Setzte Textmodus in GSM-Modem
            GsmCommandQueue.Add("AT+CMGF=1");
            //Setzte Speicherbereich im GSM-Modem "SM" SIM, "PM" Phone-Memory, "MT" + "SM" + "PM"
            GsmCommandQueue.Add("AT+CPMS=\"MT\"");

            //Abfrage, welche Optionen für Empfangsindiaktor von GSM-Modem unterstützt werden
            //GsmCommandQueue.Add("AT+CNMI=?");

            //Setze Empfangsindiaktor von GSM-Modem senden
            GsmCommandQueue.Add("AT+CNMI=2,1,2,0,1");
        }

        void CheckForIncomingSmsIndication(object sender, SerialDataEventArgs e)
        {
            Task.Run(() =>
            {
                string str = Encoding.ASCII.GetString(e.Data);

                //Empfangshinweis für eingehende Nachricht ?
                if (str.Contains("+CMTI:"))
                {
                    string smsIdStr = System.Text.RegularExpressions.Regex.Match(str, @"\d+").Value;

                    if (int.TryParse(smsIdStr, out int smsId))
                    {
                        GsmCommandQueue.Add("AT+CMGF=1");
                        GsmCommandQueue.Add("AT+CPMS=\"MT\"");
                        SpManager.NewSerialDataRecieved += ReadIncomingMessagePart1;
                        GsmCommandQueue.Add("AT+CMGR=" + smsId);
                    }
                    else
                    {
                        Log(Topic.SMS, Prio.Fehler, 2003221714, "Die ID der eingegangenen SMS konnte nicht ausgelesen werden aus >" + str + "< ");
                    }

                }
            });
        }

        bool WaitForSmsContent = false;

        Message IncomingMessage;

        void ReadIncomingMessagePart1(object sender, SerialDataEventArgs e)
        {
            string str = Encoding.ASCII.GetString(e.Data);

            //Empfange Nachricht ?
            if (str.Contains("+CMGR:"))
            {
                //+CMGR: "REC READ","+4915142265412",,"20/03/30,13:44:56+08"\r\n
                string[] smsHeader = str.Remove(0, 7).Replace("\"", string.Empty).Replace("\r\n", string.Empty).Split(',');

                Contact contact = Sql.GetContactFromDb(0, "", "", smsHeader[1], "");

                DateTime recDate = DateTime.ParseExact(smsHeader[3], "yy/MM/dd", CultureInfo.InvariantCulture);
                TimeSpan recDateTime = TimeSpan.Parse(smsHeader[4].Substring(0, 7));

                IncomingMessage = new Message()
                {
                    From = contact,
                    RecieveTime = recDate.Add(recDateTime),
                    Status = MessageType.RecievedFromSms
                };

                WaitForSmsContent = true;
                return;
            }

            if (WaitForSmsContent)
            {
                MessageBox.Show(str);

                //Nachrichteninhalt?
                if (!str.StartsWith("AT+") && !str.StartsWith("+") && !str.StartsWith("\r\nOK")) //&& str.Contains("\r\n\r\n")
                {
                    string messageContent = str.Replace("\r", string.Empty).Replace("\n", string.Empty);

                    if (messageContent.EndsWith("OK")) messageContent = messageContent.Substring(0, messageContent.Length - 3);

                    IncomingMessage.Content = messageContent;

                    if (str.EndsWith("OK\r\n"))
                    {
                        Sql.CreateMessageEntry(IncomingMessage);

                        //TODO: Nachricht weiterleiten
                        MessageBox.Show("SMS Empfangen");

                        IncomingMessage = null;
                        WaitForSmsContent = false;
                        SpManager.NewSerialDataRecieved -= ReadIncomingMessagePart1;
                    }
                }
            }
        }

        #endregion

        #region SMS Senden

        internal static int TimeOutSendSMSResponseMilliSeconds = Properties.Settings.Default.TimeOutSendSMSResponseMilliSeconds;
        internal static bool BlockSmsSending = false;

        /// <summary>
        /// Sendet die Nachrichten aus OutBox, getriggert durch Timer alle X Minuten
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void SendMessageFromOutBox(object sender, EventArgs e)
        {
            if (StatusClass.OutBox.Count == 0) return;


            Sql sql = new Sql();

            MainWindow.CurrentRecievers = sql.GetCurrentRecievers();

            //gehe durch ausstehende Nachrichten      
            for (int i = 0; i < StatusClass.OutBox.Count; i++)
            {
                StatusClass.OutBox[i].SentTime = DateTime.UtcNow;
                ++StatusClass.OutBox[i].SendingApproches;

                //Entferne Nachrichten aus OutBox nach Überschreitung Max. Sendeversuche
                if (StatusClass.OutBox[i].SendingApproches > Messages.MaxSendingApproches)
                {
                    MainWindow.Log(MainWindow.Topic.General, MainWindow.Prio.Fehler, 2004111307,
                        "Max. Sendeversuche (" + Messages.MaxSendingApproches + ") überschritten. Verwerfe Nachricht: " + StatusClass.OutBox[i].Content.Substring(0, 32) + "...");
                    StatusClass.OutBox.RemoveAt(i);
                    continue;
                }

                // Nur wenn keine Empfänger vorgemerkt sind
                if (StatusClass.OutBox[i].To.Count == 0)
                {
                    StatusClass.OutBox[i].To.AddRange(MainWindow.CurrentRecievers);
                }

                //gehe durch Empfänger
                foreach (Contact reciever in StatusClass.OutBox[i].To)
                {
                    if (reciever.DeliverSms)
                    {
                        //Sende SMS
                        MainWindow.SendSms(reciever.Phone, StatusClass.OutBox[i].Content);
                    }

                    if (reciever.DeliverEmail)
                    {
                        //TODO:Email versenden
                        MainWindow.Log(MainWindow.Topic.Email, MainWindow.Prio.Warnung, 2004111230,
                            "Senden von Email ist noch nicht implementiert. ID " + StatusClass.OutBox[i].Id + " " + StatusClass.OutBox[i].Content.Substring(0, 24) + 
                            "... Zieladresse war: " + reciever.EmailAddress);
                    }
                }

            }

            sql.ShowLastMessages();
        }

        public static void SendSms(ulong phone, string content)
        {
            if (phone == 0)
            {
                Log(Topic.SMS, Prio.Fehler, 2004111246, "SendSms() die übergebene Telefonnummer ist ungültig.");
            }

            var t = Task.Run(() =>
            {
                //Warte, solange SMS senden gesperrt ist. Siehe CheckForOutgoingSmsIndication()
                while (BlockSmsSending) { }
            });
            t.Wait(TimeOutSendSMSResponseMilliSeconds); //Timeout warten auf SMS sen

            if (BlockSmsSending)
            {
                MainWindow.Log(MainWindow.Topic.SMS, MainWindow.Prio.Fehler, 2004131250, "SMS senden; Zeitüberschreitung (" + TimeOutSendSMSResponseMilliSeconds + " ms) bei Sendebestätigung.");
            }

            BlockSmsSending = true;

            const string ctrlz = "\u001a";
            content = content.Replace("\r\n", " ");
            if (content.Length > 160) content = content.Substring(0, 160);

            GsmCommandQueue.Add("AT+CMGS=\"+" + phone + "\"\r");
            GsmCommandQueue.Add(content + ctrlz);

            SpManager.NewSerialDataRecieved += CheckForOutgoingSmsIndication;
        }

        internal static void CheckForOutgoingSmsIndication(object sender, SerialDataEventArgs e)
        {
            var t = Task.Run(() =>
            {
                string str = Encoding.ASCII.GetString(e.Data);

                //Sendehinweis für ausgegangene Nachricht ?
                if (str.Contains("+CMGS:") && str.Contains("\r\nOK\r\n"))
                {
                    MessageBox.Show("200411 1327 Rükmeldung SMS versendet.");

                    BlockSmsSending = false;
                }
                else if (str.Contains("+CMGS:") && str.Contains("ERROR"))
                {
                    MainWindow.Log(Topic.SMS, Prio.Fehler, 2004131211, "Fehler beim Senden einer SMS: " + str);
                    BlockSmsSending = false;
                }

            });
            
        }

        #endregion

        #region SMS-Aktion
        private void Gsm_Button_SendTestSms_Click(object sender, RoutedEventArgs e)
        {
            ulong phone = HelperClass.ConvertStringToPhonenumber(Gsm_TextBox_TestSmsNumber_Reciever.Text);
            string content = Gsm_TextBox_TestSmsText.Text;
            const string ctrlz = "\u001a";

            if (phone == 0)
            {
                MessageBox.Show("Die Telefonnummer >" + Gsm_TextBox_TestSmsNumber_Reciever.Text + "< ist ungültig!", "MelBox2", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (content.Length < 5)
            {
                MessageBox.Show("SMS-Nachrichten müssen mindestens 5 Zeichen enthalten!", "MelBox2", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }


            //PortComandExe("AT+CMGF=1");
            //System.Threading.Thread.Sleep(500);

            ////FUNKTIONIERT!
            //PortComandExe("AT+CMGS=\"+" + phone + "\"\r");
            //System.Threading.Thread.Sleep(1000);
            //PortComandExe(content + ctrlz);

            GsmCommandQueue.Add("AT+CMGF=1");
            GsmCommandQueue.Add("AT+CMGS=\"+" + phone + "\"\r");
            GsmCommandQueue.Add(content + ctrlz);
        }

        private void Gsm_Button_SubscribeWaitForSms_Click(object sender, RoutedEventArgs e)
        {
            SubscribeForIncomingSms();
        }

        #endregion

        #region Sprachanrufe
        private void Gsm_Button_StartVoicCall_Click(object sender, RoutedEventArgs e)
        {
            ulong phone = HelperClass.ConvertStringToPhonenumber(Gsm_TextBox_TestSmsNumber_Reciever.Text);

            if (phone == 0)
            {
                MessageBox.Show("Die Telefonnummer >" + Gsm_TextBox_TestSmsNumber_Reciever.Text + "< ist ungültig!", "MelBox2", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            else
            {
                MessageBoxResult r = MessageBox.Show("Telefonnummer +" + phone + " anrufen?", "MelBox2", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (r == MessageBoxResult.Yes)
                {
                    PortComandExe("ATD+" + phone + ";");
                }

                System.Threading.Thread.Sleep(10000);
                //Auflegen
                PortComandExe("AT+CHUP");

            }
        }

        private void Gas_Button_RedirectVoiceCallsOn_Click(object sender, RoutedEventArgs e)
        {
            ulong phone = HelperClass.ConvertStringToPhonenumber(Gsm_TextBox_TestSmsNumber_Reciever.Text);
            int[] secondsToForward = { 5, 10, 15, 20, 25, 30 };

            if (phone == 0)
            {
                MessageBox.Show("Die Telefonnummer >" + Gsm_TextBox_TestSmsNumber_Reciever.Text + "< ist ungültig!", "MelBox2", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            else
            {
                MessageBoxResult r = MessageBox.Show("Sprachanrufe umleiten an Telefonnummer +" + phone + "?", "MelBox2", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (r == MessageBoxResult.Yes)
                {
                    GsmCommandQueue.Add("ATD**61*+" + phone + "**" + secondsToForward[1] + "#;");
                    //PortComandExe("ATD**61*+" + phone + "**" + secondsToForward[1] + "#;");
                }
            }
        }

        private void Gas_Button_RedirectVoiceCallsOff_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult r = MessageBox.Show("Umleitung Sprachanrufe deaktivieren?", "MelBox2", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (r == MessageBoxResult.Yes)
            {
                PortComandExe("ATD##61#;");
            }
        }

        #endregion
    }

    /// <summary>
    /// Ändert die Farbe der Progressbar in Abhänigkeit der Signalstärke
    /// </summary>
    public class ProgressForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double progress = (double)value;
            System.Windows.Media.Brush foreground = System.Windows.Media.Brushes.Green;

            //Mögliche Werte: 2 - 9 marginal, 10 - 14 OK, 15 - 19 Good, 20 - 30 Excellent, 99 = kein Signal

            if (progress < 3 || progress >= 30)
            {
                foreground = Brushes.Red;
            }
            else if (progress < 9)
            {
                foreground = Brushes.DarkOrange;
            }
            else if (progress < 15)
            {
                foreground = Brushes.YellowGreen;
            }
            else if (progress < 20)
            {
                foreground = Brushes.Green;
            }
            else if (progress < 30)
            {
                foreground = Brushes.DarkGreen;
            }

            return foreground;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
