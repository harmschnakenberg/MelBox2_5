using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelBox2_5
{
    class Messages
    {

        public static bool BlockSignalQualityMessages { get; set; } = false;

        #region Methoden

        internal static void Create_SignalQualityMessage(int signalQuality)
        {
            if (BlockSignalQualityMessages) return;

                //Mögliche Werte: 2 - 9 marginal, 10 - 14 OK, 15 - 19 Good, 20 - 30 Excellent, 99 = kein Signal
            string signalStrength = "unbekannt";

            if (signalQuality < 3 || signalQuality >= 30)
            {
                signalStrength = "kein Signal";
            }
            else if (signalQuality < 9)
            {
                signalStrength = "marginal";
            }
            else if (signalQuality < 15)
            {
                signalStrength = "ok";
            }
            else if (signalQuality < 20)
            {
                signalStrength = "gut";
            }
            else if (signalQuality < 30)
            {
                signalStrength = "exzellent";
            }

            StringBuilder body = new StringBuilder();
            body.Append("MelBox2: Die Signalqualität am GSM-Modem für die Störungsweitermeldung wird eingestuft als -" + signalStrength + "-.");

            Message notification = new Message
            {
                Content = body.ToString(),
                From = Contacts.SmsCenter,
                RecieveTime = DateTime.UtcNow,
                Status = MessageType.System,
                Subject = "MelBox2 - Signalqualität " + signalStrength,
                To = new List<Contact>() { Contacts.MelBox2Admin }
            };

            StatusClass.InBox.Add(notification);
            BlockSignalQualityMessages = true;
        }

        //internal static void Create_NewUnknownContactMessage(Message recievedMessage, uint newContactId, string keyWord)
        //{
        //    if (recievedMessage is null)
        //    {
        //        throw new ArgumentNullException(nameof(recievedMessage));
        //    }

        //    StringBuilder body = new StringBuilder();
        //    body.Append("Es wurde ein neuer Absender in die Datenbank von MelBox2 eingetragen.\r\n\r\n");
        //    body.Append("Neue Nachricht empfangen am " + recievedMessage.SentTime.ToShortDateString() + " um " + recievedMessage.SentTime.ToLongTimeString() + "\r\n\r\n");

        //    body.Append("Benutzerschlüsselwort ist\t\t>" + keyWord + "<\r\n");
        //    body.Append("Empfangene Emailadresse war\t\t>" + recievedMessage.From.Email + "<\r\n");
        //    body.Append("Empfangene Telefonnummer war\t>+" + recievedMessage.From.Phone + "<\r\n\r\n");

        //    body.Append("Empfangenen Nachricht war [" + recievedMessage.ContentId + "]\t\t>" + recievedMessage.Content + "<\r\n\r\n");

        //    body.Append("Bitte die Absenderdaten in MelBox2 im Reiter >Stammdaten< für die ID [" + newContactId + "] vervollständigen .\r\n");
        //    body.Append("Dies ist eine automatische Nachricht von MelBox2.");

        //    Message notification = new Message
        //    {
        //        Content = body.ToString(),
        //        From = Contacts.SmsCenter,
        //        Status = MessageType.System,
        //        Subject = "MelBox2 - neuer Absender",
        //        To = new List<Contact>() { Contacts.MelBox2Admin }
        //    };

        //    StatusClass.InBox.Add(notification);
        //}

        internal static void Create_NewUnknownContactMessage(uint contactId, string email, ulong phone, string keyWord)
        {            
            StringBuilder body = new StringBuilder();
            body.Append("Es wurde ein neuer Absender in die Datenbank von MelBox2 eingetragen.\r\n\r\n");
            
            body.Append("Benutzerschlüsselwort ist\t\t>" + keyWord + "<\r\n");
            body.Append("Empfangene Emailadresse war\t\t>" + email + "<\r\n");
            body.Append("Empfangene Telefonnummer war\t>+" + phone + "<\r\n\r\n");

            body.Append("Bitte die Absenderdaten in MelBox2 im Reiter >Stammdaten< für die ID [" + contactId + "] vervollständigen .\r\n");
            body.Append("Dies ist eine automatische Nachricht von MelBox2.");

            Message notification = new Message
            {
                Content = body.ToString(),
                From = Contacts.SmsCenter,
                Status = MessageType.System,
                Subject = "MelBox2 - neuer Absender",
                To = new List<Contact>() { Contacts.MelBox2Admin }
            };

            StatusClass.InBox.Add(notification);
        }

        internal static void Create_StartupMessage()
        {

            StringBuilder body = new StringBuilder();
            body.Append("MelBox2: Die Anwendung wurde neu gestartet am " + DateTime.Now.ToString("dd.MM.yyyy HH:mm") + " Uhr");

            Message notification = new Message
            {
                Content = body.ToString(),
                RecieveTime = DateTime.UtcNow,
                From = Contacts.SmsCenter,
                Status = MessageType.System,
                Subject = "MelBox2 - Neustart " + DateTime.Now.ToString("dd.MM.yyyy HH:mm") + " Uhr",
                To = new List<Contact>() { Contacts.MelBox2Admin }
            };

            StatusClass.InBox.Add(notification);
        }


        #endregion

    }

    public static class Contacts
    {
        #region Felder

        private static Contact _SmsCenter = null;
        private static Contact _MelBox2Admin = null;
        private static Contact _Bereitschaftshandy = null;
        private static Contact _KreuService = null;


        #endregion

        #region Properties
        //public static ulong GsmModemPhoneNumber { get; set; } = 4915142265412;

        public static string UnknownName { get; } = "_UNBEKANNT_";

        public static IEnumerable<Contact> PermanentSubscribers { get; set; }


        public static Contact SmsCenter
        {
            get
            {
                if (_SmsCenter == null)
                    _SmsCenter = MainWindow.Sql.GetContactFromDb(1); //siehe Sql.CreateNewDataBase()
                return _SmsCenter;
            }
        }

        public static Contact MelBox2Admin
        {
            get
            {
                if (_MelBox2Admin == null)
                    _MelBox2Admin = MainWindow.Sql.GetContactFromDb(2); //siehe Sql.CreateNewDataBase()
                return _MelBox2Admin;
            }
        }

        public static Contact Bereitschaftshandy
        {
            get
            {
                if (_Bereitschaftshandy == null)
                {
                    Sql sql = new Sql();
                    _Bereitschaftshandy = sql.GetContactFromDb(3); //siehe Sql.CreateNewDataBase()
                }
                return _Bereitschaftshandy;
            }
        }

        public static Contact KreuService
        {
            get
            {
                if (_KreuService == null)
                    _KreuService = MainWindow.Sql.GetContactFromDb(4); //siehe Sql.CreateNewDataBase()
                return _KreuService;
            }
        }

        #endregion
    }
}
