using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace MelBox2_5
{
    public partial class MainWindow : Window
    {
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

            this.Gsm_TextBox_SerialPortResponse.Dispatcher.Invoke(DispatcherPriority.Normal,
                    new Action(() => { textBoxContent = this.Gsm_TextBox_SerialPortResponse.Text; }));

            if (textBoxContent.Length > maxTextLength)
                textBoxContent = textBoxContent.Remove(0, textBoxContent.Length - maxTextLength);

            // This application is connected to a GPS sending ASCCI characters, so data is converted to text
            string str = Encoding.ASCII.GetString(e.Data);

            this.Gsm_TextBox_SerialPortResponse.Dispatcher.Invoke(DispatcherPriority.Normal,
                new Action(() => { this.Gsm_TextBox_SerialPortResponse.Text = textBoxContent + str; }));

            //Antworten protokollieren
            string dir = System.IO.Path.GetDirectoryName(TextLogPath);
            if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);

            //using (System.IO.StreamWriter file = System.IO.File.AppendText(TextLogPath))
            //{
            //    file.WriteLine("\r\n" + DateTime.Now.ToShortTimeString() + ":\r\n" + str);
            //}
        }

        /// <summary>
        /// Triggert die Abfrage der momentanen Netzqualität mit ShowGsmSignalQuality(...)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void GetGsmSignalQuality(object sender, EventArgs e)
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

                    //this.Gsm_TextBox_SerialPortResponse.Dispatcher.Invoke(DispatcherPriority.Normal,
                    //    new Action(() => { this.Gsm_ProgressBar_SignalQuality.Value = signalQuality; }));

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


    }
}
