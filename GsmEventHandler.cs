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

            this.Gsm_TextBox_SerialPortResponse.Dispatcher.Invoke(DispatcherPriority.Background,
                    new Action(() => { textBoxContent = this.Gsm_TextBox_SerialPortResponse.Text; }));

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
                    //if (signalQuality < 10)
                    //  Messages.Create_SignalQualityMessage(signalQuality);
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

        void CheckForIncomingSms(object sender, SerialDataEventArgs e)
        {
            string str = Encoding.ASCII.GetString(e.Data);

            //Empfangshinweis für eingehende Nachricht ?
            if (str.Contains("+CMTI:"))
            {
                string smsIdStr = System.Text.RegularExpressions.Regex.Match(str, @"\d+").Value;

                if (int.TryParse(smsIdStr, out int smsId))
                {
                    //PortComandExe("AT+CMGF=1");

                    PortComandExe("AT+CMGR=" + smsId);
                    MessageBox.Show(smsId.ToString());
                    //TODO: Eingegangene SMS in Datenbank schreiben

                    SpManager.NewSerialDataRecieved += ReadIncomingMessage;                    
                    PortComandExe("AT+CMGR=" + smsId);
                }
                else
                {
                    Log(Topic.SMS, Prio.Fehler, 2003221714, "Die ID der eingegangenen SMS konnte nicht ausgelesen werden aus >" + str + "< ");
                }

            }
        }

        void ReadIncomingMessage(object sender, SerialDataEventArgs e)
        {
            string str = Encoding.ASCII.GetString(e.Data);

            //Empfange Nachricht ?
            if (str.Contains("+CMGR:"))
            {
                PortComandExe("AT+CMGF=1");
                //TODO: SMS decodieren, in DB und dann in Sendeliste schreiben

                //System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex("\\+CMGL: (\\d+),\"(.+)\",\"(.+)\",(.*),\"(.+)\"\\r\\n(.+)"); // "\\+CMGL: (\\d+),\"(.+)\",\"(.+)\",(.*),\"(.+)\"\n(.+)\n\n\""
                //System.Text.RegularExpressions.Match m = r.Match(input);

                //while (m.Success)
                //{
                //    //string gr0 = m.Groups[0].Value; // <alles>
                //    string gr1 = m.Groups[1].Value; //6
                //    string gr2 = m.Groups[2].Value; //STO SENT
                //    string gr3 = m.Groups[3].Value; //+49123456789
                //                                    //string gr4 = m.Groups[4].Value; // -LEER-
                //    string gr5 = m.Groups[5].Value.Replace(',', ' '); //18/09/28,11:05:51 + 105
                //    string gr6 = m.Groups[6].Value; //Nachricht
                //    string gr7 = m.Groups[7].Value; //Nachricht (notwendig?)

                //    //MessageBox.Show(string.Format("0:{0}\r\n1:{1}\r\n2:{2}\r\n3:{3}\r\n4:{4}\r\n5:{5}\r\n6:{6}\r\n7:{7}\r\n", gr0, gr1, gr2, gr3, gr4, gr5, gr6, gr7), "Rohdaten");

                //    int.TryParse(gr1, out int smsId);
                //    DateTime.TryParse(gr5, out DateTime time);

                //    //MessageBox.Show("Zeit interpretiert: " + time.ToString("dd.MM.yyyy HH:mm:ss"));

                //    //Message Status zu MessageType
                //    MessageType type;
                //    switch (gr2)
                //    {
                //        case "REC UNREAD":
                //        case "REC READ":
                //            type = MessageType.RecievedFromSms;
                //            break;
                //        case "STO SENT":
                //            type = MessageType.SentToSms;
                //            break;
                //        default:
                //            type = MessageType.RecievedFromSms;
                //            break;
                //    }

                //    //Nachricht erstellen
                //    Message msg = new Message
                //    {
                //        Index = smsId,
                //        Status = gr2,
                //        //Alphabet -leer-
                //        Type = (ushort)type,
                //        Cellphone = HelperClass.ConvertStringToPhonenumber(gr3),
                //        SentTime = Sql.ConvertToUnixTime(time),
                //        CustomerKeyWord = GetKeyWords(gr6),
                //        Content = gr6 + gr7
                //    };

                //    messages.Add(msg);

                //    m = m.NextMatch();
                //}

                SpManager.NewSerialDataRecieved -= ReadIncomingMessage;
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
