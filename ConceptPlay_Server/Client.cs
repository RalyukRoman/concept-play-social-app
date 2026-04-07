using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Text.Json;

namespace ConceptPlay_Server
{
    public class Client : IDisposable
    {
        private bool _disposed;

        public readonly string UploadsFolder;
        public readonly HashSet<Client> OwnerList;
        public readonly Socket ClientSocket;

        // -- CONSTRUCTOR --

        public Client(Socket socket, HashSet<Client> owner, string uploadsFolder)
        {
            ClientSocket = socket;
            OwnerList = owner;
            UploadsFolder = uploadsFolder;

            _ = ManageClient();
        }

        public void Dispose()
        {
            if (_disposed) 
                return;

            try 
            { 
                ClientSocket?.Shutdown(SocketShutdown.Both); 
            } 
            catch {}

            ClientSocket?.Close();
            _disposed = true;
        }

        // -- METHODS --

        public async Task ManageClient()
        {
            byte[] buffer = new byte[1024 * 1024 * 2];
            int bytesCount;

            try
            {
                while ((bytesCount = await ClientSocket.ReceiveAsync(buffer)) > 0)
                {
                    string jsonStr = Encoding.UTF8
                        .GetString(buffer, 0, bytesCount);

                    string[] jsonList = jsonStr.Split('\n', 
                        StringSplitOptions.RemoveEmptyEntries);

                    await ProcessJsList(jsonList);
                }
            }
            catch (Exception ex)
            {
                Debug.Print("ERROR: " + ex.Message);
            }
            finally
            {
                Dispose();
                OwnerList.Remove(this);
            }
        }

        public async Task ProcessJsList(string[] jsonList)
        {
            foreach (string json in jsonList)
            {
                var msg = JsonSerializer.Deserialize<ImageMessage>(json);

                if (msg is null)
                    continue;

                if (msg.MessageType == "RequestImage")
                {
                    await UploadImageToClient(msg.ImageName);
                }
                else if (msg.MessageType == "SendImage")
                {
                    await ConvertImageFromMessage(msg);
                }
            }
        }

        public async Task UploadImageToClient(string? imageName)
        {
            if (imageName is null)
                return;

            string filePath = Path.Combine(UploadsFolder, imageName);

            if (!File.Exists(filePath))
                return;

            byte[] imageBytes = await File.ReadAllBytesAsync(filePath);
            string base64Image = Convert.ToBase64String(imageBytes);

            var msg = new ImageMessage
            {
                MessageType = "GetImage",
                ImageName = imageName,
                Data = base64Image
            };

            string json = JsonSerializer.Serialize(msg) + '\n';
            byte[] buffer = Encoding.UTF8.GetBytes(json);

            await ClientSocket.SendAsync(buffer);
        }

        public async Task ConvertImageFromMessage(ImageMessage msg)
        {
            if (string.IsNullOrEmpty(msg.Data) ||
                string.IsNullOrEmpty(msg.ImageName))
            {
                return;
            }

            byte[] imageBytes = Convert.FromBase64String(msg.Data!);
            string filePath = Path.Combine(UploadsFolder, msg.ImageName!);

            await File.WriteAllBytesAsync(filePath, imageBytes);
        }
    }
}
