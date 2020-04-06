using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MelBox2_5
{
    class StatusClass
    {
        public StatusClass()
        {
            #region MessageCount

            InBox.CollectionChanged += (sender, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    InMsgsSinceStartup++;

                    for (int i = 0; i < InBox.Count; i++)
                    {
                        Sql Sql = new Sql();
                        if (Sql.CreateMessageEntry(InBox[i]))
                        {
                            Sql.ShowLastMessages();
                            OutBox.Add(InBox[i]);
                            InBox.RemoveAt(i);                            
                        }
                    }

                }
            };

            OutBox.CollectionChanged += (sender, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    OutMsgsSinceStartup++;
                    System.Windows.MessageBox.Show(OutBox[OutBox.Count - 1].Content);

                    
                }
            };
        }

        #endregion

        #region Fields
        private static double _GsmSignalQuality = 0;
        private static int _ErrorCount = 0;
        private static int _WarningCount = 0;
        private static uint _MessagesInDb = 0;
        private static uint _InMsgsSinceStartup = 0;
        private static uint _OutMsgsSinceStartup = 0;

        private static readonly ObservableCollection<Message> _InBox = new ObservableCollection<Message>();
        private static readonly ObservableCollection<Message> _OutBox = new ObservableCollection<Message>();

        private static DataTable _LastMessages;
        private static DataTable _LastLogEntries;
        #endregion

        #region Properties
        public static DataTable LastMessages
        {
            get { return _LastMessages; }
            set
            {
                _LastMessages = value;
                NotifyStaticPropertyChanged();
            }
        }

        public static DataTable LastLogEntries
        {
            get { return _LastLogEntries; }
            set
            {
                _LastLogEntries = value;
                NotifyStaticPropertyChanged();
            }
        }

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

        public static uint InMsgsSinceStartup
        {
            get => _InMsgsSinceStartup;
            set
            {
                _InMsgsSinceStartup = value;
                NotifyStaticPropertyChanged();
            }
        }

        public static uint OutMsgsSinceStartup
        {
            get => _OutMsgsSinceStartup;
            set
            {
                _OutMsgsSinceStartup = value;
                NotifyStaticPropertyChanged();
            }
        }

        public static ObservableCollection<Message> OutBox
        {
            get => _OutBox;
        }


        public static ObservableCollection<Message> InBox
        { 
            get => _InBox;
        }

        #endregion

        #region Events

        public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged;

        private static void NotifyStaticPropertyChanged([CallerMemberName] string propertyName = null)
        {
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }


        #endregion

    }
}
