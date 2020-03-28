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
        private readonly int SignalQualityCheckTimerIntervalSeconds = Properties.Settings.Default.SignalQualityCheckTimerIntervalSeconds;

        DispatcherTimer _SignalQualityCheckTimer;

        public void StartSignalQualityCheckTimer()
        {
            _SignalQualityCheckTimer = new DispatcherTimer();

            //Signalqualität prüfen
            _SignalQualityCheckTimer.Tick += CheckGsmSignalQuality;
            _SignalQualityCheckTimer.Interval = new TimeSpan(0, 0, 0, SignalQualityCheckTimerIntervalSeconds);
            _SignalQualityCheckTimer.Start();
        }
    }
}
