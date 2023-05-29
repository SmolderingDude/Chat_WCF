using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ServiceModel;
using System.Security.Policy;
using ClientServerContract;
using System.ServiceModel.Description;

namespace ChatServer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>

    public partial class MainWindow
    {
        ServiceHost host = new ServiceHost(typeof(Service));
        bool isActiveServer;
        string serverIp, serverPort;


        public MainWindow()
        {
            InitializeComponent();

            serverIp = "localhost";
            serverPort = "1234";

            isActiveServer = false;

        }

        private void Port_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!Char.IsDigit(e.Text, 0)) e.Handled = true;
        }

        private bool IsIpAdresse(string ip)
        {
            bool isIpAdresse = false;

            try
            {
                IPAddress ip2;
                isIpAdresse = IPAddress.TryParse(ip, out ip2);
            }
            catch (Exception e) { };

            return isIpAdresse;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            host.Abort();
        }

        private void ServerButton_Click(object sender, RoutedEventArgs e)
        {


            if (!isActiveServer)
            {

                if (Ip.Text != string.Empty)
                {
                    serverIp = Ip.Text;

                    if (!IsIpAdresse(serverIp) && serverIp != "localhost")
                    {
                        MessageBox.Show("Не удалось создать сервер.\n" + "Неккоректно указан IP ", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    serverIp = "localhost";
                }

                if (Port.Text == "0" || Port.Text == string.Empty)
                {
                    Random rnd = new Random();
                    int port = rnd.Next();
                    serverPort = (port % 50000 + 1).ToString();
                }
                else
                {
                    serverPort = Port.Text;
                }

                string URI = "net.tcp://" + serverIp + ":" + serverPort + "/IClientServerContract";

                try 
                {                
                    
                    host = new ServiceHost(typeof(Service));
                    
                    Uri address = new Uri(URI);
                    NetTcpBinding binding = new NetTcpBinding();
                    binding.Security.Mode = SecurityMode.None;
                    Type contract = typeof(IClientServerContract);

                    host.AddServiceEndpoint(contract, binding, address);

                    host.Open();
                }
                catch (Exception ex)
                {
                    host.Abort();
                    MessageBox.Show("Не удалось создать сервер.\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                MessageBox.Show("Сервер успешно запущен!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);

                Ip.Text = serverIp;
                Port.Text = serverPort;
                ServerURI.Text = "URI сервера: " + URI;

                Ip.IsReadOnly = true;
                Port.IsReadOnly = true;

                isActiveServer = true;
                ServerButton.Content = "Отключить сервер";
            }

            else
            {
                host.Close();

                MessageBox.Show("Сервер отключен!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);

                Ip.IsReadOnly = false;
                Port.IsReadOnly = false;

                ServerURI.Text = string.Empty;

                isActiveServer = false;
                ServerButton.Content = "Запустить сервер";

            }
        }
    }
}