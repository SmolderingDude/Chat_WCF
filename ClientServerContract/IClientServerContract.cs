using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace ClientServerContract
{
    // ПРИМЕЧАНИЕ. Можно использовать команду "Переименовать" в меню "Рефакторинг", чтобы изменить имя интерфейса "IClientServerContract" в коде и файле конфигурации.
    [ServiceContract(CallbackContract = typeof(IClientServerContractCallback))]
    public interface IClientServerContract
    {
        [OperationContract]
        (string,int) Connect(string userName,int userId);

        [OperationContract(IsOneWay = true)]
        void Disconnect(int id);

        [OperationContract(IsOneWay = true)]
        void SendMsg(string msg, int id);

        [OperationContract(IsOneWay = true)]
        void DeleteMsg(int msgId, int userId);

        [OperationContract(IsOneWay = true)]
        void EditMsg(int msgId, int userId,string msgText);

        [OperationContract]
        string LoadMsgHistory(int id);

        [OperationContract]
        string UsersList(int id);       
    }

    public interface IClientServerContractCallback
    {
        [OperationContract(IsOneWay = true)]
        void MsgCallback(string userMessage);

        [OperationContract(IsOneWay = true)]
        void DeleteMsgCallback(int id);

        [OperationContract(IsOneWay = true)]
        void EditMsgCallback(int msgId,string msgText);
    }
}
