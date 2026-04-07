using Microsoft.Data.SqlClient;
using System.IO;

namespace ConceptPlay_Server
{
    public class DbChangeObserver
    {
        private static readonly string _conStr = @"Server=localhost\SQLEXPRESS;
                                                  Database=ConceptPlay;
                                                  Encrypt=False;
                                                  Trusted_Connection=True;
                                                  TrustServerCertificate=True";

        public readonly string UploadsFolder;
        public readonly HashSet<Client> ClientsList;
        public CancellationToken Token;

        public event EventHandler<DbMessage>? OnDbChange;

        // -- CONSTRUCTOR --

        public DbChangeObserver(
            HashSet<Client> clients, string uploadsFolder)
        {
            ClientsList = clients;
            UploadsFolder = uploadsFolder;
        }

        // -- METHODS --

        public async Task SearchTableChangesAsync()
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            long lastVersion = await GetVersionAsync(conn);
            var tables = await GetTablesAsync(conn);

            while (!Token.IsCancellationRequested)
            {
                long currentVersion = await GetVersionAsync(conn);

                await Task.Delay(TimeSpan.FromSeconds(5), Token);

                if (currentVersion == lastVersion)
                    continue;

                foreach (var table in tables)
                {
                    await FindTableChangeAsync(table, conn, lastVersion);
                }

                lastVersion = currentVersion;
            }
        }

        public async Task<List<string>> GetTablesAsync(SqlConnection conn)
        {
            var getTablesCmd = new SqlCommand(@"
                    SELECT t.name
                    FROM sys.change_tracking_tables ct
                    JOIN sys.tables t ON ct.object_id = t.object_id;", conn);

            var tables = new List<string>();

            using (var reader = await getTablesCmd.ExecuteReaderAsync(Token))
            {
                while (await reader.ReadAsync(Token))
                {
                    tables.Add(reader.GetString(0));
                }
            }

            return tables;
        }

        public async Task<long> GetVersionAsync(SqlConnection conn)
        {
            var getVersionCmd = new SqlCommand(
                "SELECT CHANGE_TRACKING_CURRENT_VERSION()", conn);

            long version = Convert.ToInt64(
                await getVersionCmd.ExecuteScalarAsync(Token));

            return version;
        }

        public async Task FindTableChangeAsync(
            string table, SqlConnection conn, long lastVersion)
        {
            var cmd = new SqlCommand($@"
                SELECT * 
                FROM CHANGETABLE(CHANGES {table}, @last_sync_version) 
                AS CT", conn);

            cmd.Parameters.AddWithValue("@last_sync_version", lastVersion);

            using var reader = await cmd.ExecuteReaderAsync(Token);

            while (await reader.ReadAsync(Token))
            {
                var msg = new DbMessage()
                {
                    MessageType = "DbUpdate",
                    Table = table,
                    Id = (int)reader["Id"],
                    ActionType = GetChangeOperator(reader)
                };

                OnDbChange?.Invoke(this, msg);
            }
        }

        public static string GetChangeOperator(SqlDataReader reader)
        {
            return reader["SYS_CHANGE_OPERATION"] switch
            {
                "U" => "Update",
                "I" => "Insert",
                "D" => "Delete",
                _ => "Unknown",
            };
        }

        public async Task TrackUselessImages()
        {
            if (!Directory.Exists(UploadsFolder))
                return;

            using var conn = new SqlConnection(_conStr);

            while (!Token.IsCancellationRequested)
            {
                string?[] filesArray = Directory.GetFiles(UploadsFolder);
                string?[] fileNamesArray = filesArray.Select(Path.GetFileName).ToArray();

                var usedFileNames = new HashSet<string>(
                    await GetUsedFiles(conn));

                foreach (var fileName in fileNamesArray)
                {
                    if (fileName is null)
                        continue;

                    if (!usedFileNames.Contains(fileName))
                    {
                        string filePath = Path.Combine(UploadsFolder, fileName);
                        File.Delete(filePath);
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(5), Token);
            }
        }

        private static async Task<List<string>> GetUsedFiles(SqlConnection conn)
        {
            string query = @"
                SELECT Avatar AS FileName FROM UserInfo WHERE AvatarImage IS NOT NULL
                UNION
                SELECT Image FROM ConceptImages WHERE Image IS NOT NULL";

            var result = new List<string>();

            using var cmd = new SqlCommand(query, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(reader.GetString(0));
            }

            return result;
        }
    }
}
