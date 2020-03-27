using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MelBox2_5
{
    class StatusClass
    {
        private static double _GsmSignalQuality = 0;
        private static int _ErrorCount = 0;
        private static int _WarningCount = 0;
        private static uint _MessagesInDb = 0;

        public static double GsmSignalQuality
        {
            get => _GsmSignalQuality;
            set
            {
                _GsmSignalQuality = value;
                NotifyStaticPropertyChanged();
            }
        }

        public static int ErrorCount { 
            get => _ErrorCount;
            set
            {
                _ErrorCount = value;
                NotifyStaticPropertyChanged();
            }
        }

        public static int WarningCount
        {
            get => _WarningCount;
            set
            {
                _WarningCount = value;
                NotifyStaticPropertyChanged();
            }
        }

        public static uint MessagesInDb
        {
            get => _MessagesInDb;
            set
            {
                _MessagesInDb = value;
                NotifyStaticPropertyChanged();
            }
        }

        #region Events

        public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged;

        private static void NotifyStaticPropertyChanged([CallerMemberName] string propertyName = null)
        {
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }


        #endregion

    }
}
