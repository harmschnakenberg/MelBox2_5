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
using System.Windows;

namespace MelBox2_5
{
    class StatusClass
    {
        public StatusClass()
        {
            #region MessageCount

            //InBox.CollectionChanged += (sender, e) =>
            //{
            //    if (e.Action == NotifyCollectionChangedAction.Add)
            //    {
            //        MessageBox.Show(InBox.Count + " | " + InBox.Last().Content);

                    
            //    }
            //};

            //OutBox.CollectionChanged += (sender, e) =>
            //{
            //    if (e.Action == NotifyCollectionChangedAction.Add)
            //    {
            //        //System.Windows.MessageBox.Show(OutBox[OutBox.Count - 1].Content);

            //        //Zähle den Gesmatzähler hoch
            //        OutMsgsSinceStartup++;

            //        //TODO: Nachrichten senden
            //    }
            //};
        }

        #endregion

        #region Fields
        private static double _GsmSignalQuality = 0;
        private static int _ErrorCount = 0;
        private static int _WarningCount = 0;
        private static uint _MessagesInDb = 0;
        private static uint _InMsgsSinceStartup = 0;
        private static uint _OutMsgsSinceStartup = 0;

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

        public static List<Message> OutBox { get; set; } = new List<Message>();
        //{
        //    get => _OutBox;
        //}


        //public static List<Message> InBox { get; set; } = new List<Message>();
        //{ 
        //    get => _InBox;
        //}

       
        #endregion

        #region Events

        public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged;

        private static void NotifyStaticPropertyChanged([CallerMemberName] string propertyName = null)
        {
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }


        #endregion

        #region Methods

        public static void InBoxAdd(Message message)
        {
            InMsgsSinceStartup++;
           
            Sql sql = new Sql();

            if (sql.CreateMessageEntry(message))
            {
                
                OutBox.Add(message);
            }
            else
            {
                //InBox.Add(message);

                MainWindow.Log(MainWindow.Topic.SQL, MainWindow.Prio.Fehler, 2004131107,
                    "Eingegangene Nachricht konnte nicht in DB gespeichert werden; " + message.Content);
            }

            sql.ShowLastMessages();

        }


        #endregion
    }
}
