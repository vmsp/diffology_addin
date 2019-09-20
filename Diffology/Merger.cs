using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Diffology
{
    public sealed class Merger
    {
        private static readonly HttpClient http = new HttpClient();

        readonly User _user = new User("TestUser", "@TestUser", "TestUser");

        // Target of the merge i.e. data stored on the user's local Access file.
        readonly DataSet _dest = new DataSet("Destination");
        string _dbPath;

        // Source of the merge i.e. data stored on the local git repository.
        readonly DataSet _orig = new DataSet("Origin");
        string _repoPath;

        public async Task Sync(string dbPath)
        {
            _dbPath = dbPath;
            _dest.Reset();
            _orig.Reset();

            var id = await FetchRepositoryId();
            if (id == null)
            {
                id = (await CreateRemoteRepository()).id;
                await CreateDiffologyTable(id);
            }

            _repoPath = Path.Combine(Consts.REPO_DIR, id);
            if (!Directory.Exists(_repoPath)) await GitClient.Clone(_user, id);
            var git = new GitClient(_repoPath, _user);

            await Task.Run(() => Export());
            if (await git.IsDirty())
            {
                await git.AddAll();
                await git.Commit();
                await git.Pull();
                await git.Push();
            }
            else
            {
                await git.Pull();
            }
            await Task.Run(() => Import());
        }

        async Task<Repository> CreateRemoteRepository()
        {
            var content = new FormUrlEncodedContent(Enumerable.Empty<KeyValuePair<string, string>>());
            var resp = await http.PostAsync($"http://172.105.152.179/repositories.json", content);
            return JsonConvert.DeserializeObject<Repository>(await resp.Content.ReadAsStringAsync());
        }

        async Task<string> FetchRepositoryId()
        {
            using (var cn = NewConnection())
            {
                cn.Open();
                var tables = cn.GetSchema("Tables");
                for (int i = 0; i < tables.Rows.Count; ++i)
                {
                    var tableName = tables.Rows[i][2].ToString();
                    if (tableName == Consts.DIFFOLOGY_TABLE_NAME)
                    {
                        var cmd = new OleDbCommand(
                            $"SELECT [Value] FROM [{Consts.DIFFOLOGY_TABLE_NAME}]", cn);
                        return (string)await cmd.ExecuteScalarAsync();
                    }
                }
            }
            return null;
        }

        async Task CreateDiffologyTable(string remoteId)
        {
            using (var cn = NewConnection())
            {
                cn.Open();

                var createTable = new OleDbCommand(
                    "CREATE TABLE [" + Consts.DIFFOLOGY_TABLE_NAME + "](" +
                    "  [ID] AUTOINCREMENT PRIMARY KEY," +
                    "  [Key] TEXT NOT NULL," +
                    "  [Value] TEXT NOT NULL" +
                    ");", cn);
                await createTable.ExecuteNonQueryAsync();

                var insert = new OleDbCommand(
                    "INSERT INTO [" + Consts.DIFFOLOGY_TABLE_NAME + "]([Key], [Value]) " +
                    "VALUES (@Key, @Value);", cn);
                insert.Parameters.Add("@Key", OleDbType.VarChar).Value = "ID";
                insert.Parameters.Add("@Value", OleDbType.VarChar).Value = remoteId;
                var changed = await insert.ExecuteNonQueryAsync();

                Debug.Assert(changed == 1);
            }
        }

        void Export()
        {
            using (var cn = NewConnection())
            {
                cn.Open();

                var tables = cn.GetSchema("Tables");
                for (int i = 0; i < tables.Rows.Count; ++i)
                {
                    var tableName = tables.Rows[i][2].ToString();
                    if (tableName[0] == '~' ||
                        tableName.StartsWith("MSys") ||
                        tableName == Consts.DIFFOLOGY_TABLE_NAME)
                    {
                        // Ignore temporary tables that Access creates (that start with '~'),
                        // system tables, and our own table.
                        continue;
                    }
                    // Access will let you name a table with a reserved keyword but, when using
                    // OLEDB, it won't work. We escape the table's name below. The export file
                    // won't be as readable but it will display as it expected in Access.
                    tableName = $"[{tableName}]";
                    using (var adapter = NewAdapter(tableName, cn))
                    {
                        adapter.FillSchema(_dest, SchemaType.Source, tableName);
                        _dest.Tables[tableName].BeginLoadData();
                        adapter.Fill(_dest, tableName);
                    }
                }
                foreach (DataTable table in _dest.Tables) table.EndLoadData();

                _dest.WriteXmlSchema(Path.Combine(_repoPath, $"Schema.xsd"));
                _dest.WriteXml(Path.Combine(_repoPath, $"Data.xml"));
            }
        }

        void Import()
        {
            using (var cn = NewConnection())
            {
                cn.Open();

                _orig.ReadXmlSchema(Path.Combine(_repoPath, $"Schema.xsd"));

                foreach (DataTable table in _orig.Tables) table.BeginLoadData();
                _orig.ReadXml(Path.Combine(_repoPath, $"Data.xml"), XmlReadMode.IgnoreSchema);
                foreach (DataTable table in _orig.Tables) table.EndLoadData();

                _dest.Merge(_orig, false, MissingSchemaAction.AddWithKey);

                foreach (DataTable table in _orig.Tables)
                {
                    using (var adapter = NewAdapter(table.TableName, cn))
                    {
                        // CommandBuilder figures out the UpdateCommand from the adapter's
                        // SelectCommand.
                        adapter.UpdateCommand =
                            new OleDbCommandBuilder(adapter).GetUpdateCommand();
                        adapter.Update(_dest, table.TableName);
                    }
                }
            }
        }

        OleDbConnection NewConnection()
        {
            return new OleDbConnection(
                "Provider = Microsoft.ACE.OLEDB.12.0;" +
                "Data Source = " + _dbPath + ";" +
                "OLE DB Services = -2;");
        }

        OleDbDataAdapter NewAdapter(string table, OleDbConnection cn)
        {
            return new OleDbDataAdapter($"SELECT * FROM {table}", cn);
        }
    }
}
