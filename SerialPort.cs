﻿using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace MelBox2_5
{
    public partial class MainWindow : Window
    {
        public static SerialPortManager SpManager { get; set; }

        private static readonly string TextLogPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Log", string.Format("Log{0:000}.txt", DateTime.Now.DayOfYear));

        private void InitializeSerialPort()
        {
            SpManager = new SerialPortManager();

            SpManager.NewSerialDataRecieved += new EventHandler<SerialDataEventArgs>(GsmTrafficLogger);
            //SpManager.NewSerialDataRecieved += new EventHandler<SerialDataEventArgs>(CheckForIncomingSms);
        }

        /// <summary>
        /// Schreibt ein AT-Command an das Modem
        /// </summary>
        /// <param name="command"></param>
        private void PortComandExe(string command)
        {
            System.IO.Ports.SerialPort port = SpManager.SerialPort;
            //Port bereit?
            if (port == null)
            {
                MessageBox.Show("2003111425 serieller Port ist unbestimmt. Befehl an GSM-Modem wird abgebrochen.");                
                return;
            }

            if (!SerialPort.GetPortNames().ToList().Contains(port.PortName))
            {
                MessageBox.Show("2003111551 Der Port >" + port.PortName + " < existiert nicht.");
                return;
            }

            try
            {
                if (!port.IsOpen)
                {
                   // MessageBox.Show("2003110953 Port >" + port.PortName + "< ist nicht offen. Versuche zu öffnen");

                    SpManager.SerialPort.Open();
                    System.Threading.Thread.Sleep(500);
                }

                port.DiscardOutBuffer();
                port.DiscardInBuffer();

                SpManager.SerialPort.Write(command + "\r");
                //MessageBox.Show("PortComandExe(" + command + ")");
            }
            catch (System.IO.IOException ex_io)
            {
                MessageBox.Show("2003110956 Konnte Befehl nicht an COM-Port senden.\r\n" + ex_io.Message);
                return;
            }

        }

    }

    public class SerialSettings
    {
        public static string PortName => SerialPort.GetPortNames().Contains(Properties.Resources.ComPort) ? Properties.Resources.ComPort : SerialPort.GetPortNames().LastOrDefault();

        public static int BaudRate => int.Parse(Properties.Resources.BaudRate);

        public static Parity Parity => (Parity)Enum.Parse(typeof(Parity), Properties.Resources.Parity);
     
        public static int DataBits => int.Parse(Properties.Resources.DataBits);

        public static StopBits StopBits => (StopBits) Enum.Parse(typeof(StopBits), Properties.Resources.StopBits);      
    }

    public class SerialPortManager : IDisposable
    {
        #region Fields


        #endregion

        #region Properties
        public SerialPort SerialPort { get; set; } = new SerialPort();

 
        #endregion

        public SerialPortManager()
        {
           
        }

        public void StartListening()
        {
            if (!SerialPort.GetPortNames().Contains(SerialSettings.PortName))
            {
                string msg = ">" + SerialSettings.PortName + "< ist kein gültiger COM-Port-Name.";
                MessageBox.Show(msg, "ACHTUNG", MessageBoxButton.OK, MessageBoxImage.Error);
                MainWindow.Log(MainWindow.Topic.COM, MainWindow.Prio.Fehler, 2003262224, msg);
                return;
            }

            try
            {
                // Closing serial port if it is open
                if (SerialPort != null && SerialPort.IsOpen)
                {
                    SerialPort.Close();
                    System.Threading.Thread.Sleep(500);
                }

                // Setting serial port settings
                SerialPort = new SerialPort(
                SerialSettings.PortName,
                SerialSettings.BaudRate,
                SerialSettings.Parity,
                SerialSettings.DataBits,
                SerialSettings.StopBits                
                    )
                {

                    //ergänzt am 11.03.2020
                    ReadTimeout = 300,
                    WriteTimeout = 500,
                    DtrEnable = true,
                    RtsEnable = true
                };

                // Subscribe to event and open serial port for data
                SerialPort.DataReceived += new SerialDataReceivedEventHandler(SerialPort_DataReceived);
                SerialPort.Open();
            }
            catch (ArgumentException ex_arg)
            {
                string msg = "Die COM-Port-Einstellungen für >" + SerialSettings.PortName + "< sind fehlerhaft. Config-Datei prüfen. " + ex_arg.Message;
                MessageBox.Show(msg, "ACHTUNG", MessageBoxButton.OK, MessageBoxImage.Error);
                MainWindow.Log(MainWindow.Topic.COM, MainWindow.Prio.Fehler, 2003262224, msg);
            }
            catch (System.IO.IOException ex_io)
            {
                string msg = "Das Gsm-Modem konnte nicht erreicht werden. " + ex_io.Message;
                MessageBox.Show(msg, "ACHTUNG", MessageBoxButton.OK, MessageBoxImage.Error);
                MainWindow.Log(MainWindow.Topic.COM, MainWindow.Prio.Fehler, 2003262224, msg);
            }
        }

        #region Port schließen
        ~SerialPortManager()
        {
            Dispose(false);
        }

        public void StopListening()
        {
            SerialPort.Close();
            System.Threading.Thread.Sleep(300);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Part of basic design pattern for implementing Dispose
        protected virtual void Dispose(bool disposing)
        {
            if (SerialPort == null) return;

            if (disposing)
            {
                SerialPort.DataReceived -= new SerialDataReceivedEventHandler(SerialPort_DataReceived);
            }
            // Releasing serial port (and other unmanaged objects)
            if (SerialPort != null)
            {
                if (SerialPort.IsOpen)
                    SerialPort.Close();

                SerialPort.Dispose();
                Thread.Sleep(500);
            }
        }

        #endregion

      

        #region Event Handlers

        public event EventHandler<SerialDataEventArgs> NewSerialDataRecieved;

        void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {            
            int dataLength = SerialPort.BytesToRead;
            byte[] data = new byte[dataLength];
            int nbrDataRead = SerialPort.Read(data, 0, dataLength);
            if (nbrDataRead == 0)
                return;

            // Send data to whom ever interested
            NewSerialDataRecieved?.Invoke(this, new SerialDataEventArgs(data));
        }

        #endregion

    }

    /// <summary>
    /// EventArgs used to send bytes recieved on serial port
    /// </summary>
    public class SerialDataEventArgs : EventArgs
    {
        public SerialDataEventArgs(byte[] dataInByteArray)
        {
            Data = dataInByteArray;
        }

        /// <summary>
        /// Byte array containing data from serial port
        /// </summary>
        public byte[] Data;
    }
}
