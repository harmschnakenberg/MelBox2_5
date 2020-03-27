using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace MelBox2_5
{
    public partial class MainWindow : Window
    {
       // private const int SignalQualityCheckTimerIntervalMinutes = 5;

        DispatcherTimer _SignalQualityCheckTimer;

        public void StartSignalQualityCheckTimer()
        {
            _SignalQualityCheckTimer = new DispatcherTimer();

            //Signalqualität prüfen
            _SignalQualityCheckTimer.Tick += GetGsmSignalQuality;
            _SignalQualityCheckTimer.Interval = new TimeSpan(0, 0, 0, 5);
            _SignalQualityCheckTimer.Start();
        }
    }
}
