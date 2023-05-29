using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;



namespace ClientServerContract
{
    [Serializable]
    public class UserMessage
    {
        private DateTime msgSendTime;
        
        public int MsgID { get; }

        public int AddresseeID { get; set; }

        public string AddresseeName { get; }

        public string MsgText { get; set; }

        public string ServerText { get; set; }

        bool isEdit = false;
        public bool IsEdit 
        {
            get
            {
                return isEdit;    
            }
            set
            {
                if (!isEdit)
                    isEdit = value;   // устанавливаем новое значение свойства
            }
        }

        public UserMessage(int msgId, int addresseeId, string addresseeName, string msgText)
        {
            msgSendTime = DateTime.UtcNow;
            MsgID = msgId;
            AddresseeID = addresseeId;
            AddresseeName = addresseeName;
            MsgText = msgText;
            isEdit = false;
            BuildServerText();
        }

        private void BuildServerText()
        {
            ServerText = $"[{msgSendTime.ToShortDateString()} {msgSendTime.ToShortTimeString()}]";
            
            if (AddresseeName == string.Empty)
                ServerText += $" {MsgText}";
            else
            {
                ServerText += $"<{AddresseeName}> {MsgText}";
                if (IsEdit) ServerText += " (ред.)";
            }
        }

        public void EditingMsg(string msg)
        {
            if (msg == MsgText) return;

            MsgText = msg;
            isEdit = true;
            BuildServerText();
        }

        public void ConvertTime()
        {
            msgSendTime = msgSendTime.ToLocalTime();
            BuildServerText();
        }
        
        public override string ToString()
        {
            return ServerText;
        }


    }
}
