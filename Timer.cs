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
        private int SignalQualityCheckTimerIntervalSeconds { get; } = Properties.Settings.Default.SignalQualityCheckTimerIntervalSeconds;
        private int SignalQualityWarningPause { get; } = Properties.Settings.Default.SignalQualityWarningPauseMinutes;

        DispatcherTimer _SignalQualityCheckTimer;

        public void StartSignalQualityCheckTimer()
        {
            _SignalQualityCheckTimer = new DispatcherTimer();

            //Signalqualität prüfen
            _SignalQualityCheckTimer.Tick += CheckGsmSignalQuality;
            _SignalQualityCheckTimer.Interval = new TimeSpan(0, 0, 0, SignalQualityCheckTimerIntervalSeconds);
            _SignalQualityCheckTimer.Start();
        }

        DispatcherTimer _SignalQualityWarningPause;

        public void StartSignalQualityWarningTimer()
        {
            _SignalQualityWarningPause = new DispatcherTimer();

            //Pause zwischen Signalqualität Warnungen
            _SignalQualityWarningPause.Tick += CheckGsmSignalQuality;
            _SignalQualityWarningPause.Interval = new TimeSpan(0, 0, SignalQualityWarningPause, 0);
            _SignalQualityWarningPause.Start();
        }
    }
}
