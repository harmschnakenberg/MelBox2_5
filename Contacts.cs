using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelBox2_5
{
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

        //TODO: Permanent Shifts mit Leben füllen
        public static List<Contact> PermanentSubscribers { get; set; }

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

        #region Methods 

        /// <summary>
        /// Liest eine Instant von Contact aus einer Textdatei.
        /// Erstellt eine neue Muster-Textdatei wenn der angegbene Dateiname filename nicht vorhanden ist.
        /// </summary>
        /// <param name="filename">Dateiname mit Endung</param>
        /// <returns>Liste von Kontakten aus der Textdatei filename</returns>
        public static List<Contact> LoadContactsFromTextFile(string filename)
        {
            List<Contact> contacts = new List<Contact>();

            try
            {
                //Aufbau der Kontaktdatei:
                //[Name]
                //email=adresse@domain.de
                //phone=

                string dir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Contacts");

                if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);

                string path = System.IO.Path.Combine(dir, filename);

                if (!System.IO.File.Exists(path)) System.IO.File.AppendText(";Kontaktdatei\r\n;[Name]\r\n;Email=\r\n;Phone=\r\n");

                string[] lines = System.IO.File.ReadAllLines(path);
                Contact contact = null;

                foreach (string line in lines)
                {
                    if (line.StartsWith(";")) continue;

                    if (line.StartsWith("["))
                    {
                        if (contact != null)
                        {
                            contacts.Add(contact);
                        }

                        contact = new Contact
                        {
                            Name = line.Substring(1, line.Length - 2)
                        };
                    }

                    if (line.ToLower().StartsWith("email") && contact != null)
                    {
                        string[] addressLine = line.Split('=');
                        if (addressLine.Length > 1 && addressLine[1].Length > 5)
                        {
                            contact.Email = new System.Net.Mail.MailAddress(addressLine[1], contact.Name);
                            contact.DeliverEmail = true;
                        }
                    }

                    if (line.ToLower().StartsWith("phone") && contact != null)
                    {
                        string[] phoneLine = line.Split('=');
                        if (phoneLine.Length > 1 && phoneLine[1].Length > 5)
                        {
                            contact.PhoneString = phoneLine[1];
                            contact.DeliverSms = true;
                        }
                    }
                }

                //Letzten Kontakt in die Liste aufnehmen
                if (contact != null)
                {
                    contacts.Add(contact);
                }


            }
            catch
            {
                MainWindow.Log(MainWindow.Topic.IO, MainWindow.Prio.Fehler, 2004111139, "Die Kontakt-Datei konnte nicht eingelesen werden.");
                //throw new Exception("Die Kontakt-Datei >" + filename + "< konnte nicht eingelesen werden.");
            }

            return contacts;
        }

        #endregion
    }

}
