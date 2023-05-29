using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using ClientServerContract;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Interop;
using System.ComponentModel;
using System.Text.Json;
using System.Runtime.ExceptionServices;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Text.RegularExpressions;

namespace ChatServer
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    internal class Service : IClientServerContract
    {
        List<ServerUser> users = new List<ServerUser>();
        List<UserMessage> messages = new List<UserMessage>();
        int nextId = 1, nextMsgId = 1;

        LogWindow logWindow;
        string log = string.Empty;

        public (string, int) Connect(string userName,int userId)
        {
            if (logWindow == null)
            {
                logWindow = new LogWindow();
                LogUpdate("Сервер запущен и ожидает подключения...");
                logWindow.Show();
            }

            userName = userName.Trim();
            userName = Regex.Replace(userName, "[ ]+", " ");

            if (userName == string.Empty)
                userName = "Юзер";

            if (userId == 0 || users.FirstOrDefault(i => i.ID == userId) == null)
            {
                if (users.FirstOrDefault(i => i.Name == userName) != null)
                {
                    int ind = 1;
                    string new_name = userName + $"({ind++})";

                    while (users.FirstOrDefault(i => i.Name == new_name) != null)
                        new_name = userName + $"({ind++})";

                    userName = new_name;
                }

                ServerUser user = new ServerUser()
                {
                    ID = nextId++,
                    Name = userName,
                    operationContext = OperationContext.Current,
                    IsActive = true,
                };


                SendMsg(user.Name + " подключился к чату!", 0);
                users.Add(user);

                LogUpdate($"Клиент с ником:{user.Name} и ID:{user.ID} подключен к серверу.");

                return (user.Name, user.ID);
            }
            else
            {
                ServerUser oldUser = users.FirstOrDefault(i => i.ID == userId);
                int ind = -1;
                
                if (oldUser != null)
                {
                    ind = users.IndexOf(oldUser);
                    
                    if (users[ind].Name != userName)
                    {
                        SendMsg($"{users[ind].Name} сменил никнейм на {userName}!", 0);
                        LogUpdate($"Клиент с ником:{users[ind].Name} и ID:{users[ind].ID} сменил ник на {userName}.");
                        users[ind].Name = userName;
                    }

                    SendMsg(users[ind].Name + " переподключился к чату!", 0);

                    users[ind].IsActive = true;
                    users[ind].operationContext = OperationContext.Current;                  
                }

                return (users[ind].Name, users[ind].ID);
            }
        }

        
        public void Disconnect(int id)
        {
            var user = users.FirstOrDefault(i => i.ID == id);
            if (user != null)
            {
                user.IsActive = false;
                user.operationContext = null;
                SendMsg(user.Name + " покинул чат!", 0);
                LogUpdate($"Клиент с ником:{user.Name} и ID:{user.ID} отключен от сервера.");
            }
        }

        public void SendMsg(string msg, int id)
        {
            string addressee = string.Empty;

            msg = msg.Trim();
            msg = Regex.Replace(msg, "[ ]+", " "); // удаление пробелов
            
            if (msg == string.Empty)
                return;

            var user = users.FirstOrDefault(i => i.ID == id);
            if (user != null)
            {
                addressee = user.Name;
                LogUpdate($"Получено новое сообщение от пользователя с ником:{addressee} и ID:{id}.  Добавлено сообщение с ID:{nextMsgId} и текстом: <{msg}>.");
            }
            else
            {
                LogUpdate($"Сервер добавил сообщение с ID:{nextMsgId} и текстом: <{msg}>.");
            }
            
            messages.Add(new UserMessage(nextMsgId++, id, addressee, msg));
            
            string jsonConvertedMessage = JsonSerializer.Serialize(messages.Last());

            List<int> errorUserId = new List<int>();
            foreach (var item in users)
                if (item.IsActive)
                {
                    try
                    {                      
                        item.operationContext.GetCallbackChannel<IClientServerContractCallback>().MsgCallback(jsonConvertedMessage);
                        LogUpdate($"По обратному каналу связи пользователю с ником: {item.Name} и ID:{item.ID} отправлено сообщение с ID:{messages.Last().MsgID}.");
                    }
                    catch 
                    {
                        LogUpdate($"Ошибка отправки сообщения по обратному каналу связи пользователю с ником: {item.Name} и ID:{item.ID}.");
                        item.IsActive = false;
                        errorUserId.Add(item.ID);
                    }
                }

            foreach (var item in errorUserId)
                Disconnect(item);
        }

        public void DeleteMsg(int msgId, int userId)
        {
            var msg = messages.FirstOrDefault(i => i.MsgID == msgId);
            if (msg != null && msg.AddresseeID == userId)
            {
                LogUpdate($"От пользователя с ником: {users.FirstOrDefault(i=>i.ID == userId).Name} и ID:{userId} получена команда удалить сообщение с ID:{msgId}.");

                List<int> errorUserId = new List<int>();
                foreach (var item in users)
                    if (item.IsActive)
                    {
                        try
                        {
                            item.operationContext.GetCallbackChannel<IClientServerContractCallback>().DeleteMsgCallback(msgId);
                            LogUpdate($"По обратному каналу связи пользователю с ником: {item.Name} и ID:{item.ID} отправлена команда удалить сообщение с ID:{messages.Last().MsgID}.");
                        }
                        catch
                        {
                            LogUpdate($"Ошибка отправки команды удаления по обратному каналу связи пользователю с ником: {item.Name} и ID:{item.ID}.");
                            item.IsActive = false;
                            errorUserId.Add(item.ID);
                        }
                    }

                foreach (var item in errorUserId)
                    Disconnect(item);

                messages.Remove(msg);
                LogUpdate($"Сообщение с ID:{msgId} отредактировано.");
            }
            else
            {
                LogUpdate($"Сообщение с ID:{msgId} не будет удалено, так как оно не найдено или у пользователя нет прав на его удаление.");
            }
        }

        public string LoadMsgHistory(int userId)
        {                       
            string jsonConvertedList = JsonSerializer.Serialize(new BindingList<UserMessage>(messages));

            if (users.FirstOrDefault(i => i.ID == userId) != null)
                LogUpdate($"Пользователю с ником:{users.FirstOrDefault(i=>i.ID == userId).Name} и ID:{userId} отправлена история сообщений.");
            
            return jsonConvertedList;
        }

        public void EditMsg(int msgId, int userId,string msgText)
        {
            msgText = Regex.Replace(msgText, "[ ]+", " "); // удаление пробелов
            var msg = messages.FirstOrDefault(i => i.MsgID == msgId);

            if (msgText == string.Empty)
                return;

            if (msg != null && msg.AddresseeID == userId)
            {
                LogUpdate($"От пользователя с ником: {users.FirstOrDefault(i => i.ID == userId).Name} и ID:{userId} получена команда редактировать сообщение с ID:{msgId}. Новый текст сообщения: <{msgText}>.");

                List<int> errorUserId = new List<int>();
                foreach (var item in users)
                    if (item.IsActive)
                    {
                        try
                        {
                            item.operationContext.GetCallbackChannel<IClientServerContractCallback>().EditMsgCallback(msgId, msgText);
                            LogUpdate($"По обратному каналу связи пользователю с ником: {item.Name} и ID:{item.ID} отправлена команда редактировать сообщение с ID:{messages.Last().MsgID}.");
                        }
                        catch
                        {
                            LogUpdate($"Ошибка отправки команды редактирования по обратному каналу связи пользователю с ником: {item.Name} и ID:{item.ID}.");
                            item.IsActive = false;
                            errorUserId.Add(item.ID);
                        }
                    }

                foreach (var item in errorUserId)
                    Disconnect(item);


                int ind = messages.IndexOf(msg);
                msg.EditingMsg(msgText);
                messages[ind] = msg;

                LogUpdate($"Сообщение с ID:{msgId} отредактировано.");
            }
            else 
            {
                LogUpdate($"Сообщение с ID:{msgId} не будет отредактировано, так как оно не найдено или у пользователя нет прав на его редактирование.");
            }
        }

        public string UsersList(int userId)
        {
            string list = string.Empty;
            
            foreach (var item in users)
                if (item.IsActive)
                {
                    list += item.Name;
                    if (item.ID == userId)
                        list += " (вы)";
                    list += "\n";
                }

            if (users.FirstOrDefault(i => i.ID == userId) != null)
                LogUpdate($"Пользователю с ником:{users.FirstOrDefault(i => i.ID == userId).Name} и ID:{userId} отправлен cписок клиентов сервера.");
            
            return list;
        }

        private void LogUpdate(string add) 
        {
            log += $"[{DateTime.Now}] {add}\n";
            logWindow.LoadLog(log);
        }
    }
}
