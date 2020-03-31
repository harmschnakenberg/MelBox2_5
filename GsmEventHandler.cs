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

                textBoxContent = textBoxContent.Replace("\r\n\r\n", "\r\n").Replace("\r\r","\r");

                if (textBoxContent.Length > maxTextLength)
                    textBoxContent = textBoxContent.Remove(0, textBoxContent.Length - maxTextLength);

                // This application is connected to a GPS sending ASCCI characters, so data is converted to text
                string str = Encoding.ASCII.GetString(e.Data);

                if (str.Contains("ERROR"))
                {
                    Log(Topic.SMS, Prio.Fehler, 2003272054, "Bei der Kommunikation mit dem GSM-Modem ist ein Fehler aufgetreten: " + str.Replace("\r\n", string.Empty));
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
            PortComandExe("AT+CSQ");
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
                        new Action(() => { this.Gsm_ProgressBar_SignalQuality.Value = signalQuality; }));

                    StatusClass.GsmSignalQuality = signalQuality;

                    //Wenn signalQuality < 10 Nachricht an Admin
                    if (signalQuality < 10)
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

            var t = Task.Run(() =>
            {
                System.Threading.Thread.Sleep(300);
                //Setzte Textmodus in GSM-Modem
                PortComandExe("AT+CMGF=1");
                System.Threading.Thread.Sleep(300);

                //Setzte Speicherbereich im GSM-Modem "SM" SIM, "PM" Phone-Memory, "MT" + "SM" + "PM"
                PortComandExe("AT+CPMS=\"MT\"");
                System.Threading.Thread.Sleep(300);

                //TODO: funktioniert nur sporadisch - warum?
                //Aktiviere Benachrichtigung von GSM-Modem, wenn neue SMS ankommt

                PortComandExe("AT+CNMI=?");
                System.Threading.Thread.Sleep(300);


                PortComandExe("AT+CNMI=2,1,2,0,1");
                System.Threading.Thread.Sleep(300);
            });
            _ = t.Wait(2000);
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
                        PortComandExe("AT+CMGF=1");
                        System.Threading.Thread.Sleep(300);

                        PortComandExe("AT+CPMS=\"MT\"");
                        System.Threading.Thread.Sleep(300);

                        SpManager.NewSerialDataRecieved += ReadIncomingMessagePart1;
                        PortComandExe("AT+CMGR=" + smsId);
                        System.Threading.Thread.Sleep(300);
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
                    
                    Sql.CreateMessageEntry(IncomingMessage);

                    IncomingMessage = null;
                    WaitForSmsContent = false;
                    SpManager.NewSerialDataRecieved -= ReadIncomingMessagePart1;
                }
            }
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

            PortComandExe("AT+CMGF=1");
            System.Threading.Thread.Sleep(500);

            //FUNKTIONIERT!
            PortComandExe("AT+CMGS=\"+" + phone + "\"\r");
            System.Threading.Thread.Sleep(1000);
            PortComandExe(content + ctrlz);

        }

        private void Gsm_Button_SubscribeWaitForSms_Click(object sender, RoutedEventArgs e)
        {
            SubscribeForIncomingSms();
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
