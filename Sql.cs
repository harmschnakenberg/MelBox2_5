using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelBox2_5
{
    class Sql
    {

        #region Felder
       
        private readonly string Datasource = "Data Source=" + DbPath;

        #endregion

        #region Properties
        public static string DbPath { get; set; } = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "DB", "MelBox2.db");

        #endregion

        #region SQL- Basismethoden
        public Sql()
        {
            if (!System.IO.File.Exists(DbPath))
            {
                CreateNewDataBase();
            }
        }


        /// <summary>
        /// Erzeugt eine neue Datenbankdatei, erzeugt darin Tabellen, Füllt diverse Tabellen mit Defaultwerten.
        /// </summary>
        private void CreateNewDataBase()
        {
            //Erstelle Datenbank-Datei
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(DbPath));
            FileStream stream = File.Create(DbPath);
            stream.Close();

            //Erzeuge Tabellen in neuer Datenbank-Datei
            using (var con = new SQLiteConnection(Datasource))
            {
                con.Open();

                List<String> TableCreateQueries = new List<string>
                    {
                        "CREATE TABLE \"Log\"(\"ID\" INTEGER NOT NULL PRIMARY KEY UNIQUE,\"Time\" INTEGER NOT NULL, \"Topic\" TEXT , \"Prio\" INTEGER NOT NULL, \"ContentNo\" INTEGER NOT NULL, \"Content\" TEXT);",

                        "CREATE TABLE \"Company\" (\"ID\" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, \"Name\" TEXT NOT NULL, \"Address\" TEXT, \"ZipCode\" INTEGER,\"City\" TEXT); ",

                        "INSERT INTO \"Company\" (\"ID\", \"Name\", \"Address\", \"ZipCode\", \"City\") VALUES (0, '_UNBEKANNT_', 'Musterstraße 123', 12345, 'Modellstadt' );",

                        "INSERT INTO \"Company\" (\"ID\", \"Name\", \"Address\", \"ZipCode\", \"City\") VALUES (1, 'Kreutzträger Kältetechnik GmbH & Co. KG', 'Theodor-Barth-Str. 21', 28307, 'Bremen' );",

                        "CREATE TABLE \"Contact\"(\"ID\" INTEGER PRIMARY KEY AUTOINCREMENT UNIQUE, \"Time\" INTEGER NOT NULL, \"Name\" TEXT NOT NULL, " +
                        "\"CompanyID\" INTEGER, \"Email\" TEXT, \"Phone\" INTEGER, \"KeyWord\" TEXT, \"MaxInactiveHours\" INTEGER, \"SendWay\" INTEGER );",

                        "INSERT INTO \"Contact\" (\"ID\", \"Time\", \"Name\", \"CompanyID\", \"Email\", \"Phone\", \"SendWay\" ) VALUES (1, " + DateTimeOffset.UtcNow.ToUnixTimeSeconds() + ", 'SMSZentrale', 1, 'smszentrale@kreutztraeger.de', 4915142265412," + (ushort)MessageType.NoCategory + ");",

                        "INSERT INTO \"Contact\" (\"ID\", \"Time\", \"Name\", \"CompanyID\", \"Email\", \"Phone\", \"SendWay\" ) VALUES (2, " + DateTimeOffset.UtcNow.ToUnixTimeSeconds()+ ", 'MelBox2Admin', 1, 'harm.schnakenberg@kreutztraeger.de', 0," + (ushort)MessageType.SentToEmail + ");",

                        "INSERT INTO \"Contact\" (\"ID\", \"Time\", \"Name\", \"CompanyID\", \"Email\", \"Phone\", \"SendWay\" ) VALUES (3, " + DateTimeOffset.UtcNow.ToUnixTimeSeconds() + ", 'Bereitschaftshandy', 1, 'bereitschaftshandy@kreutztraeger.de', 491728362586," + (ushort)MessageType.SentToSms + ");",

                        "INSERT INTO \"Contact\" (\"ID\", \"Time\", \"Name\", \"CompanyID\", \"Email\", \"Phone\", \"SendWay\" ) VALUES (4, " + DateTimeOffset.UtcNow.ToUnixTimeSeconds() + ", 'Kreutzträger Service', 1, 'service@kreutztraeger.de', 0," +  (ushort)MessageType.SentToEmail + ");",

                        "INSERT INTO \"Contact\" (\"ID\", \"Time\", \"Name\", \"CompanyID\", \"Email\", \"Phone\", \"SendWay\" ) VALUES (5, " + DateTimeOffset.UtcNow.ToUnixTimeSeconds()+ ", 'Henry Kreutzträger', 1, 'henry.kreutztraeger@kreutztraeger.de', 491727889419," + (ushort)(MessageType.SentToEmail & MessageType.SentToSms) + ");",

                        "INSERT INTO \"Contact\" (\"ID\", \"Time\", \"Name\", \"CompanyID\", \"Email\", \"Phone\", \"SendWay\" ) VALUES (6, " + DateTimeOffset.UtcNow.ToUnixTimeSeconds() + ", 'Bernd Kreutzträger', 1, 'bernd.kreutztraeger@kreutztraeger.de', 491727875067," + (ushort)(MessageType.SentToEmail & MessageType.SentToSms) + ");",
                        
                        //Tabelle MessageTypes wird z.Zt. nicht verwendet! Dient als Dokumentation für die BitCodierung von MessageType.
                        "CREATE TABLE \"MessageType\" (\"ID\" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, \"Description\" TEXT NOT NULL);",

                        "INSERT INTO \"MessageType\" (\"ID\", \"Description\") VALUES (" + (ushort)MessageType.NoCategory + ", 'keine Zuordnung');",
                        "INSERT INTO \"MessageType\" (\"ID\", \"Description\") VALUES (" + (ushort)MessageType.System + ", 'von System');",
                        "INSERT INTO \"MessageType\" (\"ID\", \"Description\") VALUES (" + (ushort)MessageType.RecievedFromSms + ", 'von SMS');",
                        "INSERT INTO \"MessageType\" (\"ID\", \"Description\") VALUES (" + (ushort)MessageType.SentToSms + ", 'an SMS');",
                        "INSERT INTO \"MessageType\" (\"ID\", \"Description\") VALUES (" + (ushort)MessageType.RecievedFromEmail + ", 'von Email');",
                        "INSERT INTO \"MessageType\" (\"ID\", \"Description\") VALUES (" + (ushort)MessageType.SentToEmail + ", 'an Email');",

                        "CREATE TABLE \"MessageContent\" (\"ID\" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, \"Content\" TEXT NOT NULL UNIQUE );",

                        "INSERT INTO \"MessageContent\" (\"Content\") VALUES ('Datenbank neu erstellt.');",

                        "CREATE TABLE \"MessageLog\"( \"ID\" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, \"RecieveTime\" INTEGER NOT NULL, \"FromPersonID\" INTEGER NOT NULL, " +
                        " \"SendTime\" INTEGER, \"ToPersonIDs\" TEXT, \"Type\" INTEGER NOT NULL, \"ContentID\" INTEGER NOT NULL);",

                        "INSERT INTO \"MessageLog\" (\"RecieveTime\", \"FromPersonID\", \"Type\", \"ContentID\") VALUES " +
                        "(" + DateTimeOffset.UtcNow.ToUnixTimeSeconds() + ",0,1,1);",

                        "CREATE TABLE \"Shifts\"( \"ID\" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, \"EntryTime\" INTEGER NOT NULL, " +
                        "\"ContactID\" INTEGER NOT NULL, \"StartTime\" INTEGER NOT NULL, \"EndTime\" INTEGER NOT NULL, \"SendType\" INTEGER NOT NULL );",

                        "CREATE TABLE \"BlockedMessages\"( \"ID\" INTEGER NOT NULL UNIQUE, \"StartHour\" INTEGER NOT NULL, " +
                        "\"EndHour\" INTEGER NOT NULL, \"WorkdaysOnly\" INTEGER NOT NULL CHECK (\"WorkdaysOnly\" < 2));" +

                        "INSERT INTO \"BlockedMessages\" (\"ID\", \"StartHour\", \"EndHour\", \"WorkdaysOnly\" ) VALUES " +
                        "(1,8,8,0);"

                };

                foreach (string query in TableCreateQueries)
                {
                    SQLiteCommand sQLiteCommand = new SQLiteCommand(query, con);
                    using (SQLiteCommand cmd = sQLiteCommand)
                    {
                        cmd.ExecuteNonQuery();
                    }
                }

                //Es muss mindestens ein Eintrag in der Tabelle "Shifts" vorhanden sein.
                //Contact.ID = 3 => Berietschaftshandy
                CreateShiftDefault(GetContactFromDb(3));
            }
        }

        /// <summary>
        /// Liest aus der SQL-Datenbank und gibt ein DataTable-Object zurück.
        /// </summary>
        /// <param name="query">SQL-Abfrage mit Parametern</param>
        /// <param name="args">Parameter - Wert - Paare</param>
        /// <returns>Abfrageergebnis als DataTable.</returns>
        private DataTable ExecuteRead(string query, Dictionary<string, object> args)
        {
            if (string.IsNullOrEmpty(query.Trim()))
                return null;

            using (var con = new SQLiteConnection(Datasource))
            {
                con.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(query, con))
                {
                    if (args != null)
                    {
                        //set the arguments given in the query
                        foreach (var pair in args)
                        {
                            cmd.Parameters.AddWithValue(pair.Key, pair.Value);
                        }
                    }

                    var da = new SQLiteDataAdapter(cmd);
                    var dt = new DataTable();

                    try
                    {
                        da.Fill(dt);
                    }
                    catch (Exception ex)
                    {
                        MainWindow.Log(MainWindow.Topic.SQL, MainWindow.Prio.Warnung, 2003221804, "SQL-Fehler in " + query + " | " + ex.Message);
                    }
                    finally
                    {
                        da.Dispose();
                    }

                    return dt;
                }
            }
        }

        /// <summary>
        /// Führt Schreibaufgaben in DB aus. 
        /// </summary>
        /// <param name="query">SQL-Abfrage mit Parametern</param>
        /// <param name="args">Parameter - Wert - Paare</param>
        /// <returns>Anzahl betroffener Zeilen.</returns>
        private int ExecuteWrite(string query, Dictionary<string, object> args)
        {
            int numberOfRowsAffected = 0;

            try
            {
                //setup the connection to the database
                using (var con = new SQLiteConnection(Datasource))
                {
                    con.Open();

                    //open a new command
                    using (var cmd = new SQLiteCommand(query, con))
                    {
                        //set the arguments given in the query
                        foreach (var pair in args)
                        {
                            cmd.Parameters.AddWithValue(pair.Key, pair.Value);
                        }

                        //execute the query and get the number of row affected
                        numberOfRowsAffected = cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                //nichts unternehmen
                MainWindow.Log(MainWindow.Topic.Internal, MainWindow.Prio.Fehler, 2003181644, "Schreiben in SQL-Datenbank fehlgeschlagen. " + query + " | " + ex.Message);
            }

            return numberOfRowsAffected;
        }

        #endregion


        #region Logging

        /// <summary>
        /// Schreibt einen neuen Eintrag in die Tabelle 'Log'.
        /// </summary>
        /// <param name="message"></param>
        internal void CreateLogEntry(MainWindow.Topic topic, MainWindow.Prio prio, ulong contentNo, string content)
        {
            const string query = "INSERT INTO Log(Time, Topic, Prio, ContentNo, Content) VALUES (@timeStamp, @topic, @prio, @contentNo, @content)";

            var args = new Dictionary<string, object>
            {
                {"@timeStamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                {"@topic", topic.ToString() },
                {"@prio", (ushort)prio},
                {"@contentNo", contentNo},
                {"@content", content}
            };

            ExecuteWrite(query, args);
        }

        #endregion

        #region Contact

        internal Contact GetContactFromDb(uint contactId)
        {
            const string query = "SELECT Name, CompanyId, Email, Phone, KeyWord, SendWay FROM Contact WHERE ID = @Id";

            var args = new Dictionary<string, object>
            {
                {"@Id", contactId}
            };

            DataTable result = ExecuteRead(query, args);

            if (result.Rows.Count == 0) return null;

            Contact contact = new Contact
            {
                Id = contactId,
                Name = result.Rows[0][0].ToString(),
                CompanyId = uint.Parse(result.Rows[0][1].ToString()),
                Email = new System.Net.Mail.MailAddress(result.Rows[0][2].ToString(), result.Rows[0][0].ToString()),
                PhoneString= result.Rows[0][3].ToString(),
                KeyWord = result.Rows[0][4].ToString(),
                ContactType = (MessageType)ushort.Parse(result.Rows[0][5].ToString())
            };

            return contact;
        }

        #endregion


        #region SQL Schichten

        /// <summary>
        /// Erstellt eine neue Schicht im Bereitschaftsplan und gibt die ID der neuen Schicht wieder.
        /// </summary>
        /// <param name="contactId">ID des Bereitschftnehmers</param>
        /// <param name="startTime">Startzeitpunkt in UTC</param>
        /// <param name="endTime">Endzeitpunkt in UTC</param>
        /// <param name="sendingType">Benachrichtigungsweg MessageType.SentToEmail oder MessageType.SentToSms</param>
        /// <returns>Id der erzeugten Schicht</returns>
        internal void CreateShift(uint contactId, DateTime startTime, DateTime endTime, MessageType sendingType)
        {
            long entryTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            string query = "INSERT INTO Shifts ( EntryTime, ContactID, StartTime, EndTime, SendType ) " +
                           "VALUES ( @EntryTime, @ContactID, @StartTime, @EndTime, @SendType ); ";

            Dictionary<string, object> args = new Dictionary<string, object>()
            {
                    {"@EntryTime", entryTime},
                    {"@ContactID", contactId},
                    {"@StartTime", ((DateTimeOffset)startTime).ToUnixTimeSeconds()},
                    {"@EndTime", ((DateTimeOffset)endTime).ToUnixTimeSeconds()},
                    {"@SendType", (int)sendingType},
            };

            int affectedRows = ExecuteWrite(query, args);

            if (affectedRows == 0)
                MainWindow.Log(MainWindow.Topic.SQL, MainWindow.Prio.Fehler, 2003221749,
                    "Für Contact-ID [" + contactId + "] konnt keine neue Bereitschaft in die Datenbank eingetragen werden.");
        }

        /// <summary>
        /// Standard-Schicht, die erstellt wird, wenn kein Eintrag für den aktuellen Tag gefunden wurde.
        /// </summary>
        /// <param name="contact">Kontakt des Bereitschaftnehmers, wie er in der Datenbank abgelegt ist.</param>
        /// <returns>ID der neu erstellten Schicht.</returns>
        internal void CreateShiftDefault(Contact contact)
        {
            if (contact.Id < 1) return ;
            //TODO: Kontakt mit Datenbankinhalt validieren 

            DateTime date = DateTime.Now.Date;
            List<DateTime> holidays = MainWindow.Feiertage(date);

            int startHour;
            if (holidays.Contains(date) || date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            {
                startHour = MainWindow.ShiftEndHour; //Start = Ende Vortag.
            }
            else
            {
                if (date.DayOfWeek == DayOfWeek.Friday)
                {
                    startHour = MainWindow.ShiftStartHourFriday;
                }
                else
                {
                    startHour = MainWindow.ShiftStartHour;
                }
            }

            DateTime StartTime = date.AddHours(startHour);
            DateTime EndTime = date.AddDays(1).AddHours(MainWindow.ShiftEndHour);
            MainWindow.Log(MainWindow.Topic.Calendar, MainWindow.Prio.Info, 987654367,
                "Erstelle automatische Bereitschaft von " + StartTime.ToString("dd.MM.yyyy HH:mm") + " bis " + EndTime.ToString("dd.MM.yyyy HH:mm"));

            CreateShift(contact.Id, StartTime, EndTime, contact.ContactType);
        }

        #endregion


        #region SQL Nachrichten

        internal uint CountMessagesInDb()
        {
            const string query = "SELECT COUNT(ID) FROM MessageLog";

            DataTable result = ExecuteRead(query, null);

            return uint.Parse(result.Rows[0][0].ToString());

        }


        /// <summary>
        /// Speichert eine neue Meldung in die Datenbank.
        /// </summary>
        /// <param name="message"></param>
        /// <returns>true: erfolgreich bearbeitet oder Duplikat verworfen</returns>
        public bool CreateMessageEntry(Message message)
        {
            if (message.Content.Length < 2) message.Content = "- KEIN TEXT -";

            #region Ermittelt die ID des Nachrichteninhalts
            uint contendId = message.ContentId;

            if (contendId == 0)
            {

                const string contentQuery = "SELECT ID FROM MessageContent WHERE Content = @Content";

                var args1 = new Dictionary<string, object>
                {
                    {"@Content", message.Content }
                };

                DataTable dt1 = ExecuteRead(contentQuery, args1);

                if (dt1.Rows.Count > 0)
                {
                    //Eintrag vorhanden
                    uint.TryParse(dt1.Rows[0][0].ToString(), out contendId);
                }
                else
                {
                    //Eintrag neu erstellen
                    const string doubleQuery = "INSERT INTO MessageContent (Content) VALUES (@Content); " +
                                                        "SELECT ID FROM MessageContent ORDER BY ID DESC LIMIT 1";

                    dt1 = ExecuteRead(doubleQuery, args1);

                    uint.TryParse(dt1.Rows[0][0].ToString(), out contendId);
                }

                message.ContentId = contendId;

            }

            #endregion

            #region ist genau dieser Eintrag schon vorhanden?
            const string checkQuery = "SELECT ID, @Type FROM MessageLog WHERE RecieveTime = @RecieveTime AND FromPersonID = @FromPersonID AND ContentID = @ContentID";

            var args2 = new Dictionary<string, object>
                {
                    {"@RecieveTime", message.RecieveTime },
                    {"@FromPersonID", message.From.Id},
                    {"@Type", (ushort)message.Status},
                    {"@ContentID", contendId }
                };

            DataTable dt2 = ExecuteRead(checkQuery, args2);
            //Ist der Eintrag schon einmal vorhanden?
            if (dt2.Rows.Count > 0)
            {
                MainWindow.Log(MainWindow.Topic.Internal, MainWindow.Prio.Info, 2003282143,
                    "Die Nachricht mit der ID [" + dt2.Rows[0][0].ToString() + "] ist bereits in der Datenbank vorhanden.");
                return true;
            }

            #endregion

            #region schreibe in die Datenbank
            const string writeQuery = "INSERT INTO MessageLog (RecieveTime, FromPersonID, Type, ContentID) VALUES (@RecieveTime, @FromPersonID, @Type, @ContentID)";

            //Wurde kein neuer Eintrag erzeugt?
            if (ExecuteWrite(writeQuery, args2) == 0)
            {
                return false;
            }
            else
            {
                return true;
            }

            #endregion
        }


        #endregion

    }
}
