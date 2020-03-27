using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelBox2_5
{
    class DataClass
    {
    }

    /// <summary>
    /// Die Nachrichten werden hiermit in Kategorien eingeordnet.
    /// Kategorien können bitweise vergeben werden z.B. MessageType = 36 (= 4 + 32) heisst: Nachricht empfangen als SMS, weitergeleitet als Email.
    /// Die hier vergebenen Texte tauchen in der Visualisierung auf.
    /// </summary>
    [Flags]
    public enum MessageType : short
    {
        NoCategory = 0,             //Nicht zugeordnet
        System = 1,                 //Vom System erzeugt
        RecievedFromSms = 2,        //Empfang von SMS
        SentToSms = 4,              //Senden an SMS
        RecievedFromEmail = 8,      //Empgang von Email
        SentToEmail = 16            //Senden an Email     
    }


    /// <summary>
    /// Abbildung von Einträgen aus der Datenbanktabelle "Contacts"
    /// </summary>
    public class Contact
    {
        private ulong _Phone;

        public uint Id { get; set; }

        public string Name { get; set; }

        public string KeyWord { get; set; }

        public uint CompanyId { get; set; }

        //public Company Company
        //{
        //    get
        //    {
        //        Sql sql = new Sql();
        //        return sql.GetCompanyFromDb(CompanyId);
        //    }
        //    set => Company = value;
        //}

        public System.Net.Mail.MailAddress Email { get; set; }

        public string EmailAddress
        {
            get => Email?.Address;
        }

        public string PhoneString { set { _Phone = HelperClass.ConvertStringToPhonenumber(value); } }
        public ulong Phone { get => _Phone;  }

        public MessageType ContactType { get; set; }

        public bool DeliverEmail
        {
            get { return (ContactType & MessageType.SentToEmail) == MessageType.SentToEmail; }
            set
            {

                ContactType |= MessageType.SentToEmail;

            }
        }

        public bool DeliverSms
        {
            get { return (ContactType & MessageType.SentToSms) == MessageType.SentToEmail; }
            set { ContactType |= MessageType.SentToSms; }
        }

        public ushort MaxInactiveHours { get; set; }
    }


    public class Company
    {
        public uint Id { get; set; }

        public string Name { get; set; }

        public string Address { get; set; }

        public uint ZipCode { get; set; }

        public string City { get; set; }
    }

}
