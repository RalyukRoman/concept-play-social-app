using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ConceptPlay_Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public readonly string UploadsFolder = 
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "uploads");

        public readonly HashSet<Client> ClientsList = [];
        public ObservableCollection<DbMessage> DbRequestsCollection { get; } = [];

        public DbChangeObserver? DbChangeObserver;
        public Socket? ServerSocket;

        public CancellationTokenSource? Cts;
        public CancellationToken Token;

        // -- CONSTRUCTOR --

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            DbChangeObserver = new DbChangeObserver(ClientsList, UploadsFolder);

            DbChangeObserver.OnDbChange += async (sender, eventArgs) =>
            {
                await AddDbChangeToCollectionAsync(eventArgs);
                await BroadcastTableChange(eventArgs);
            };
        }

        // -- METHODS --

        public void Start()
        {
            Cts = new CancellationTokenSource();
            Token = Cts.Token;
            DbChangeObserver!.Token = Token;

            CreateServerSocket();

            TextBlockStatus.Text = $"Status: ON";
            TextBlockStatus.Foreground = Brushes.DarkGreen;

            _ = Task.Run(WaitForClients);
            _ = Task.Run(DbChangeObserver.SearchTableChangesAsync);
            _ = Task.Run(DbChangeObserver.TrackUselessImages);
        }

        public void Stop()
        {
            Cts?.Cancel();
            ServerSocket?.Close();

            DbRequestsCollection.Clear();
            ClientsList.Clear();

            TextBlockStatus.Text = $"Status: OFF";
            TextBlockStatus.Foreground = Brushes.DarkRed;
        }

        public void CreateServerSocket()
        {
            ServerSocket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);

            ServerSocket.Bind(new IPEndPoint(
                IPAddress.Parse("127.0.0.1"), 80));

            ServerSocket.Listen(100);
        }

        public async Task WaitForClients()
        {
            while (!Token.IsCancellationRequested)
            {
                Socket clientSocket = await ServerSocket!.AcceptAsync();

                var client = new Client(clientSocket, ClientsList, UploadsFolder);

                ClientsList.Add(client);
            }
        }

        public async Task AddDbChangeToCollectionAsync(
            DbMessage msg)
        {
            if (Token.IsCancellationRequested)
                return;

            await Dispatcher.InvokeAsync(() =>
            {
                if (DbRequestsCollection.Count > 1000)
                    DbRequestsCollection.RemoveAt(0);

                DbRequestsCollection.Add(msg);
            });
        }

        public async Task BroadcastTableChange(DbMessage msg)
        {
            string json = JsonSerializer.Serialize(msg) + '\n';
            byte[] buffer = Encoding.UTF8.GetBytes(json);

            foreach (var client in ClientsList)
            {
                try
                {
                    await client.ClientSocket.SendAsync(buffer);
                }
                catch
                {
                    client.Dispose();
                    ClientsList.Remove(client);
                }
            }
        }

        // -- EVENT --

        private void ButtonClickHandler(
            object sender, RoutedEventArgs e)
        {
            try
            {
                switch (((Button)sender).Name)
                {
                    case nameof(ButtonOn):
                        Start();
                        break;

                    case nameof(ButtonOff):
                        Stop();
                        break;
                }
            }
            catch (Exception ex) 
            { 
                Debug.Print("ERROR: " + ex.Message); 
            }
        }

        private void Window_Closed(
            object sender, EventArgs e)
        {
            try
            {
                Stop();
            }
            catch (Exception ex)
            {
                Debug.Print("ERROR: " + ex.Message);
            }
        }
    }
}