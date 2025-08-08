using Microsoft.Data.Sqlite;
namespace risk.control.system.Services
{
    public interface ISqliteSchemaService
    {
        List<string> GetAllTables();
        List<Dictionary<string, object>> GetTableData(string tableName);
        List<(string ColumnName, string DataType, bool NotNull)> GetTableSchema(string tableName);
    }
    public class SqliteSchemaService : ISqliteSchemaService
    {
        private readonly string _connectionString;
        private readonly IConfiguration config;

        public SqliteSchemaService(IConfiguration config)
        {
            this.config = config;
            _connectionString = config.GetConnectionString("Database");
        }

        public List<string> GetAllTables()
        {
            var tables = new List<string>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                tables.Add(reader.GetString(0));
            }

            return tables;
        }
        public List<Dictionary<string, object>> GetTableData(string tableName)
        {
            var rows = new List<Dictionary<string, object>>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT * FROM [{tableName}]";

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = reader.GetName(i);
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[columnName] = value;
                }
                rows.Add(row);
            }

            return rows;
        }

        public List<(string ColumnName, string DataType, bool NotNull)> GetTableSchema(string tableName)
        {
            var schema = new List<(string ColumnName, string DataType, bool NotNull)>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info({tableName});";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string name = reader.GetString(1); // Column name
                string type = reader.GetString(2); // Data type
                bool notnull = reader.GetInt32(3) == 1;

                schema.Add((name, type, notnull));
            }

            return schema;
        }
    }
}
