using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MelBox2_5
{
    public partial class MainWindow : Window
    {
       
        public enum Topic
        {
            General,
            Contacts,
            Calendar,
            Email,
            SMS,
            Internal,
            SQL,
            COM
        }

        public enum Prio
        {
            Unbekannt,
            Fehler,
            Warnung,
            Info
        }

        public static void Log(Topic topic, Prio prio, ulong contentId, string content)
        {
            switch (prio)
            {
                case Prio.Fehler:
                case Prio.Warnung:
                    StatusClass.ErrorCount++;
                    break;
            }
            Sql Sql = new Sql();
            Sql.CreateLogEntry(topic, prio, contentId, content);
        }

    }

}
