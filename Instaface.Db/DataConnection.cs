using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace Instaface.Db
{
    using System.IO;
    using Newtonsoft.Json.Linq;

    public interface IDataConnection
    {
        Task<IDbConnection> Connect();

        Task Setup();
    }

    public class DataConnection : IDataConnection
    {
        private readonly string _connectionString;

        public DataConnection(IConfiguration configuration)
        {
            var searchPath = Path.GetDirectoryName(GetType().Assembly.Location);
            while (!string.IsNullOrWhiteSpace(searchPath))
            {
                var configFile = Path.Combine(searchPath, "Instaface.json");
                if (File.Exists(configFile))
                {
                    var config = JObject.Parse(File.ReadAllText(configFile));
                    _connectionString = config["DefaultConnection"]?.Value<string>();
                    break;
                }

                searchPath = Path.GetDirectoryName(searchPath);
            }
            
            if (_connectionString == null)
            {
                _connectionString = configuration?.GetConnectionString("DefaultConnection");
            }
        }

        private static async Task<IDbConnection> Connect(string connectionString)
        {
            var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            return connection;
        }

        public Task<IDbConnection> Connect()
        {
            return Connect(_connectionString);
        }

        public async Task Setup()
        {
            var details = new MySqlConnectionStringBuilder(_connectionString);
            var schema = details.Database;
            details.Database = null;

            using (var db = await Connect(details.ToString()))
            {
                var count = await db.ExecuteScalarAsync<int>(
                    $@"select count(*) from information_schema.schemata
                       where schema_name = '{schema}'");
                if (count == 0)
                {
                    await db.ExecuteAsync($"create schema `{schema}`");
                }
            }

            using (var db = await Connect())
            {
                await CreateTable(db, schema, "entity", "id",
                    ("id", "int(11) NOT NULL AUTO_INCREMENT"),
                    ("type", "char(10) NOT NULL"),
                    ("attributes", "JSON NULL"),
                    ("created", "datetime NOT NULL"));

                await CreateTable(db, schema, "association", "id",
                    ("id", "int(11) NOT NULL AUTO_INCREMENT"),
                    ("type", "char(10) NOT NULL"),
                    ("attributes", "JSON NULL"),
                    ("created", "datetime NOT NULL"),
                    ("from", "int(11) NOT NULL"),
                    ("to", "int(11) NOT NULL"));
            }
        }

        private static async Task CreateTable(IDbConnection db,
                                              string schema,
                                              string name,
                                              string primaryKey, 
                                              params (string Name, string Type)[] columns)
        {
            var count = await db.ExecuteScalarAsync<int>(
                $@"select count(*) from information_schema.tables 
                   where table_name = '{name}' and table_schema = '{schema}'");
            if (count != 0) return;

            var lines = columns.Select(c => $"`{c.Name}` {c.Type}").ToList();
            if (primaryKey != null) lines.Add($"primary key (`{primaryKey}`)");
            var definition = string.Join(",\r\n", lines);
            
            await db.ExecuteAsync($"CREATE TABLE `{name}` (\r\n{definition}\r\n)");
        }
    }
}

