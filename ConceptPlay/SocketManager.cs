using DLL_ConnectionToDatabase;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Media.Imaging;

namespace ConceptPlay
{
    public class ServerMessage
    {
        public required string MessageType { get; set; }
        public string? ActionType { get; set; }
        public string? Table { get; set; }
        public int? Id { get; set; }
        public string? ImageName { get; set; }
        public string? Data { get; set; }
    }

    public class SocketManager(MainWindow window)
    {
        public Socket? ClientSocket { get; set; }
        public readonly Dictionary<string, BitmapImage?> ImagesBuffer = [];

        private readonly MainWindow? _window = window;

        // -- VARIABLE PARAMETERS -- 

        public static readonly string Ip = "127.0.0.1";
        public static readonly int Port = 80;
        public static readonly string? DbLogin = null;
        public static readonly string? DbPassword = null;

        // -- METHODS --

        public async Task StartSocketConnectionAsync()
        {
            while (true)
            {
                while (IsSocketConnected())
                {
                    await Task.Delay(
                       TimeSpan.FromSeconds(5));
                }

                ClientSocket = new Socket(
                    AddressFamily.InterNetwork,
                    SocketType.Stream,
                    ProtocolType.Tcp);

                try
                {
                    await ClientSocket.ConnectAsync(new IPEndPoint(
                        IPAddress.Parse(Ip), Port));

                    await ReceiveMessagesAsync();
                }
                catch
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(5));
                }
                finally
                {
                    if (IsSocketConnected())
                        ClientSocket.Close();
                }
            }
        }

        public bool IsSocketConnected()
        {
            try
            {
                if (ClientSocket is null)
                    return false;

                return !(ClientSocket.Poll(1, SelectMode.SelectRead) &&
                        ClientSocket.Available == 0);
            }
            catch { return false; }
        }

        public async Task RequestImageFromServerAsync(string imageName)
        {
            if (!IsSocketConnected())
                return;

            var payload = new
            {
                MessageType = "RequestImage",
                ImageName = imageName
            };

            string json = JsonConvert.SerializeObject(payload) + '\n';
            byte[] buffer = Encoding.UTF8.GetBytes(json);

            await ClientSocket!.SendAsync(buffer);
        }

        public async Task<BitmapImage?> GetImageFromServerAsync(string? imageName)
        {
            if (imageName is null)
                return null;

            if (!ImagesBuffer.TryGetValue(imageName, out BitmapImage? bitmap))
            {
                if (!IsSocketConnected())
                    return null;

                await RequestImageFromServerAsync(imageName);

                while (!ImagesBuffer.TryGetValue(imageName, out bitmap))
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(2));
                }
            }

            return bitmap;
        }

        public async Task<string?> SendImageToServerAsync(string imagePath)
        {
            if (!IsSocketConnected())
                return null;

            byte[] imageBytes = await File.ReadAllBytesAsync(imagePath);
            string base64Image = Convert.ToBase64String(imageBytes);

            string extension = Path.GetExtension(imagePath);
            string shortGuid = Guid.NewGuid().ToString("N")[..5];

            string newFileName = $"{DateTime.Now:yyyyMMddHHmmss}({shortGuid}){extension}";

            ImagesBuffer.TryAdd
            (
                newFileName,
                Utilities.GetBitmapImage(base64Image)
            );

            var payload = new
            {
                MessageType = "SendImage",
                ImageName = newFileName,
                Data = base64Image
            };

            string json = JsonConvert.SerializeObject(payload) + '\n';
            byte[] buffer = Encoding.UTF8.GetBytes(json);

            await ClientSocket!.SendAsync(buffer);

            return newFileName;
        }

        public async Task ReceiveMessagesAsync()
        {
            byte[] buffer = new byte[1024 * 1024];
            int bytesCount;

            try
            {
                while ((bytesCount = await ClientSocket!.ReceiveAsync(buffer)) > 0)
                {
                    if (!IsSocketConnected())
                        return;

                    string jsonStr = Encoding.UTF8
                        .GetString(buffer, 0, bytesCount);

                    string[] jsonList = jsonStr.Split('\n',
                        StringSplitOptions.RemoveEmptyEntries);

                    foreach (string json in jsonList)
                    {
                        var msg = JsonConvert
                            .DeserializeObject<ServerMessage>(json);

                        if (msg is null)
                            continue;

                        ProcessServerMessage(msg);
                    }
                }
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }

        private void ProcessServerMessage(ServerMessage msg)
        {
            if (msg.MessageType == "DbUpdate")
            {
                var dbContext = DbMenu.CreateContext(
                    Ip, DbLogin, DbPassword);

                DbChangeHandler.ProcessDbUpdates
                (
                    _window!,
                    dbContext,
                    msg.Table,
                    msg.ActionType,
                    msg.Id ?? 0
                );
            }
            else if (msg.MessageType == "GetImage")
            {
                ImagesBuffer.TryAdd
                (
                    msg.ImageName ?? "",
                    Utilities.GetBitmapImage(msg.Data ?? "")
                );
            }
        }
    }
}
