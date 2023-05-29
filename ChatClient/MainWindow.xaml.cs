using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using ClientServerContract;
using System.Diagnostics;
using System.Runtime.Serialization.Json;
using System.ComponentModel;
using System.Text.Json;
using System.ServiceModel.Channels;

namespace ChatClient
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IClientServerContractCallback
    {
        bool isConnected; // флаг подключения клиента
        bool isChatScroll; // флаг скролла чата
        DuplexChannelFactory<IClientServerContract> factory; // фабрика для создания канала связи
        IClientServerContract client; // канал связи
        int ID; // ID клиента на сервере
        string userName,URI; // Ник клиента на сервере и URI ссылка
        BindingList<UserMessage> messages; // сообщения, отображаемые у клиента
        int selectedMsgInd; // индекс выделенного пользователем сообщения

        string log; // лог
        LogWindow logWindow;
        
        public MainWindow()
        {
            InitializeComponent();
            isConnected = false;
            isChatScroll = true;
            selectedMsgInd = -1;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            URI = string.Empty;
            log = string.Empty;
            LogUpdate("Клиент запущен.");
            WritingURI();
        }

        private void WritingURI()
        {
            WriteURI writeURI = new WriteURI();
            writeURI.ShowDialog();

            if (writeURI.ServerURI.Text != URI)
                ID = 0;

            URI = writeURI.ServerURI.Text;
            LogUpdate($"Введена ссылка URI:{URI}.");
        }

        private void LogUpdate(string add)
        {
            log += $"[{DateTime.Now}] {add}\n";
            if (logWindow != null)
                logWindow.LoadLog(log);
        }


        private void ScrollingState(bool flag) // отслеживание состояния скроллинга чата
        {
            isChatScroll = flag;


            if (!isChatScroll)
            {
                bBottomScroll.Content = "▼";
            }
            else
            {
                lbChat.SelectedIndex = selectedMsgInd = -1;
                if (lbChat.Items.Count > 0)
                    lbChat.ScrollIntoView(lbChat.Items[lbChat.Items.Count - 1]);
                bBottomScroll.Content = "▲";
            }
        }

        private void ConnectUser() 
        {
            if (!isConnected)
            {
                try
                {
                    Uri addresse = new Uri(URI);
                    NetTcpBinding binding = new NetTcpBinding();
                    binding.Security.Mode = SecurityMode.None;
                    EndpointAddress endpoint = new EndpointAddress(addresse);

                    InstanceContext instanceContext = new InstanceContext(this);

                    factory = new DuplexChannelFactory<IClientServerContract>(instanceContext, binding,endpoint);
                    client = factory.CreateChannel();
                }
                catch (Exception ex) 
                {
                    client = null;
                    LogUpdate($"Не удалось подключиться к серверу!");
                    MessageBox.Show("Не удалось подключиться к серверу!\n" + ex.Message, "Ошибка подключение", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                (string,int) clientData = (string.Empty, 0);
                try
                {
                    clientData = client.Connect(tbUserName.Text,ID);
                }
                catch (Exception ex)
                {
                    client = null;
                    LogUpdate($"Не удалось подключиться к серверу!");
                    MessageBox.Show("Ошибка на сервере!\n" + ex.Message, "Ошибка подключение", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                ID = clientData.Item2;
                userName = clientData.Item1;

                messages = new BindingList<UserMessage>();
                lbChat.ItemsSource = messages;
                
                
                bSend.IsEnabled = bLoadH.IsEnabled = bServerUsers.IsEnabled = bBottomScroll.IsEnabled = true;
                tbUserName.Text = userName;
                tbUserName.IsEnabled = bURI.IsEnabled = false;
                bConnDiscon.Content = "Отключиться";
                isConnected = true;

                LogUpdate($"Соединение с сервером установлено.");
            }
        }

        private void DisconnectUser() 
        {
            if (isConnected)
            {
                try
                {
                    client.Disconnect(ID);
                }
                catch(Exception ex) { }

                client = null;
                tbUserName.IsEnabled = bURI.IsEnabled = true;
                bConnDiscon.Content = "Подключиться";
                isConnected = false;

                lbChat.ItemsSource = null;
                bSend.IsEnabled = bLoadH.IsEnabled = bServerUsers.IsEnabled = bBottomScroll.IsEnabled = false;
                messages = null;

                LogUpdate($"Произведено отключение от сервера.");
            }

        }

        private void ConnDisconButton_Click(object sender, RoutedEventArgs e) // кнопка подключения-отключения
        {
            if (isConnected)
                DisconnectUser();

            else
                ConnectUser();
        }

        private void Window_Closing(object sender, CancelEventArgs e) // отключение клиента при закрытии окна
        {
            DisconnectUser();
            if (logWindow != null)
                logWindow.Close();
        }

        private void tbMessage_KeyDown(object sender, KeyEventArgs e) // отправка сообщения при нажатии на enter
        {
            if (e.Key == Key.Enter)
            {
                SendMessage();  
            }
        }

        private void SendMessage()
        {
            if (client != null && tbMessage.Text != string.Empty)
            {
                try
                {
                    client.SendMsg(tbMessage.Text, ID);
                    LogUpdate($"На сервер отправлено сообщение с текстом:<{tbMessage.Text}>.");
                }
                catch (Exception ex)
                {
                    LogUpdate($"Ошибка отправки сообщения на сервер.");
                    MessageBox.Show("Не удалось удалить сообщение!\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                ScrollingState(true);
            }
            tbMessage.Text = string.Empty;
        }
        
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedMsgInd < 0) return;

            try
            {              
                client.DeleteMsg(messages[selectedMsgInd].MsgID, ID);
                LogUpdate($"На сервер отправлена команда удаления сообщения с ID:{messages[selectedMsgInd].MsgID}.");
            }
            catch(Exception ex) 
            {
                LogUpdate($"Ошибка отправки команды удаления сообщения на сервер.");
                MessageBox.Show("Не удалось удалить сообщение!\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void lbChat_SelectionChanged(object sender, SelectionChangedEventArgs e) // событие при изменении индекса выделенного пользователем сообщения
        {
            selectedMsgInd = lbChat.SelectedIndex;
            bDel.IsEnabled = bEdit.IsEnabled = false;
            ScrollingState(false);
            tbMessage.Text = string.Empty;


            if (selectedMsgInd < 0) return;
           
            bDel.IsEnabled = bEdit.IsEnabled = messages[selectedMsgInd].AddresseeID == ID;
            if (bEdit.IsEnabled)
                tbMessage.Text = messages[selectedMsgInd].MsgText;
        }

        private void bLoadH_Click(object sender, RoutedEventArgs e)
        {
            BindingList<UserMessage>  messagesOld = new BindingList<UserMessage>(messages);
            messages.Clear();
            
            try
            {
                LogUpdate($"Отправлен запрос серверу на получение истории сообщений.");
                string list = client.LoadMsgHistory(ID);
                messages = JsonSerializer.Deserialize<BindingList<UserMessage>>(list);
                LogUpdate($"Получена история сообщений от сервера.");

                foreach (var item in messages)
                {
                    item.ConvertTime();
                }
            }
            catch(Exception ex) 
            {
                LogUpdate($"Ошибка получения истории сообщений.");
                messages = new BindingList<UserMessage>(messagesOld);             
                MessageBox.Show("Не удалось загрузить историю сообщений!\n" + ex.Message); 
            }
            
            lbChat.ItemsSource = messages;
            ScrollingState(true);
            bLoadH.IsEnabled = false;
        }
        
        private void bBottomScroll_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected) return;

            ScrollingState(!isChatScroll);
        }

        private void bURI_Click(object sender, RoutedEventArgs e) // перезапись URI ссылка
        {
            WritingURI();
        }

        private void bSend_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void bEdit_Click(object sender, RoutedEventArgs e)
        {
            if (selectedMsgInd < 0) return;

            try
            {
                client.EditMsg(messages[selectedMsgInd].MsgID, ID, tbMessage.Text);
                LogUpdate($"На сервер отправлена команда редактирования сообщения с ID:{messages[selectedMsgInd].MsgID}.Новый текст сообщения:<{tbMessage.Text}>");
            }
            catch(Exception ex)
            {
                LogUpdate($"Ошибка отправки команды редактирования сообщения на сервер.");
                MessageBox.Show("Не удалось отредактировать сообщение!\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            tbMessage.Text = string.Empty;
        }

        private void bServerUsers_Click(object sender, RoutedEventArgs e)
        {
            string list = string.Empty;
            
            try
            {
                LogUpdate($"Отправлен запрос серверу на получение списка пользователей сервера.");
                list = client.UsersList(ID);
                UserListWindow window = new UserListWindow(list);
                LogUpdate($"Получена список пользователей сервера от сервера.");
                window.Show();
            }
            catch (Exception ex)
            {
                LogUpdate($"Ошибка получения списка пользователей сервера.");
                MessageBox.Show("Не удалось загрузить список пользователей!\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        // функции для принятия сообщений от сервера
        public void MsgCallback(string userMessage) 
        {
            LogUpdate($"От сервера получено сообщение.");

            var message = JsonSerializer.Deserialize<UserMessage>(userMessage);
            message.ConvertTime();

            if (message != null)
                messages.Add(message);

            LogUpdate($"Сообщение с ID:{message.MsgID} успешно добавлено.");

            if (isChatScroll)
                lbChat.ScrollIntoView(lbChat.Items[lbChat.Items.Count - 1]);
        }

        public void EditMsgCallback(int msgId,string msgText)
        {
            LogUpdate($"От сервера получена команда редактирования сообщения.");

            try
            {
                var msg = messages.FirstOrDefault(i => i.MsgID == msgId);
                int ind = messages.IndexOf(msg);

                msg.EditingMsg(msgText);
                messages[ind] = msg;
            }
            catch 
            {
                LogUpdate($"Сообщение с ID:{msgId} не отредактировано, так как такого сообщения нет.");
                return;
            }
            LogUpdate($"Сообщение с ID:{msgId} успешно отредактировано.");
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F1)
            {
                logWindow = new LogWindow();
                logWindow.LoadLog(log);
                logWindow.Show();
            }
        }

        public void DeleteMsgCallback(int msgId)
        {
            UserMessage delMsg = messages.FirstOrDefault(i => i.MsgID == msgId);
            LogUpdate($"От сервера получена команда удаления сообщения.");

            if (delMsg != null)
            {
                messages.Remove(delMsg);
                LogUpdate($"Сообщение с ID:{msgId} успешно удалено.");
            }
            else
            {
                LogUpdate($"Сообщение с ID:{msgId} не удалено, так как такого сообщения нет.");
            }
        }
    }
}
