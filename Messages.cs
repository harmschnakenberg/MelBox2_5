using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelBox2_5
{
    class Messages
    {

        public static int MaxSendingApproches { get; } = Properties.Settings.Default.MaxSendingApproches;

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

            StatusClass.InBoxAdd(notification);
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

            StatusClass.InBoxAdd(notification);
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

            StatusClass.InBoxAdd(notification);
        }


        #endregion

    }

}
