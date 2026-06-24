using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace Quiz_App
{
    public class SqlConnection : IDisposable
    {
        public string ConnectionString { get; set; }
        public ConnectionState State { get; private set; } = ConnectionState.Closed;

        public SqlConnection()
        {
            ConnectionString = string.Empty;
        }

        public SqlConnection(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public void Open()
        {
            State = ConnectionState.Open;
        }

        public void Close()
        {
            State = ConnectionState.Closed;
        }

        public SqlTransaction BeginTransaction()
        {
            return new SqlTransaction();
        }

        public void Dispose()
        {
            Close();
        }
    }

    public class SqlTransaction : IDisposable
    {
        public void Commit() { }
        public void Rollback() { }
        public void Dispose() { }
    }

    public class SqlCommand : IDisposable
    {
        public string CommandText { get; set; }
        public CommandType CommandType { get; set; } = CommandType.Text;
        public SqlConnection Connection { get; set; }
        public SqlTransaction Transaction { get; set; }
        public int CommandTimeout { get; set; }
        public SqlParameterCollection Parameters { get; } = new SqlParameterCollection();

        public SqlCommand()
        {
            CommandText = string.Empty;
        }

        public SqlCommand(string cmdText)
        {
            CommandText = cmdText;
        }

        public SqlCommand(string cmdText, SqlConnection connection)
        {
            CommandText = cmdText;
            Connection = connection;
        }

        public SqlCommand(string cmdText, SqlConnection connection, SqlTransaction transaction)
        {
            CommandText = cmdText;
            Connection = connection;
            Transaction = transaction;
        }

        public int ExecuteNonQuery()
        {
            if (CommandType == CommandType.StoredProcedure)
            {
                ApiClient.ExecuteProcedure(CommandText, Parameters.ToDictionary());
                return 1;
            }
            return ApiClient.ExecuteNonQuery(CommandText, Parameters.ToDictionary());
        }

        public object ExecuteScalar()
        {
            if (CommandType == CommandType.StoredProcedure)
            {
                // In our codebase, upserting theory scores is the only stored procedure that might run in nonquery context
                ApiClient.ExecuteProcedure(CommandText, Parameters.ToDictionary());
                return DBNull.Value;
            }
            return ApiClient.ExecuteScalar(CommandText, Parameters.ToDictionary());
        }

        public SqlDataReader ExecuteReader()
        {
            return new SqlDataReader(ExecuteTableInternal());
        }

        public DataTable ExecuteTableInternal()
        {
            if (CommandType == CommandType.StoredProcedure)
            {
                throw new NotSupportedException("Stored procedures are not supported in ExecuteReader via proxy.");
            }
            return ApiClient.ExecuteTable(CommandText, Parameters.ToDictionary());
        }

        public void Dispose() { }
    }

    public class SqlParameterCollection : IEnumerable
    {
        private readonly List<SqlParameter> _parameters = new List<SqlParameter>();

        public int Count => _parameters.Count;

        public void Add(SqlParameter parameter)
        {
            _parameters.Add(parameter);
        }

        public SqlParameter AddWithValue(string parameterName, object value)
        {
            var param = new SqlParameter(parameterName, value);
            _parameters.Add(param);
            return param;
        }

        public SqlParameter Add(string parameterName, SqlDbType dbType)
        {
            var param = new SqlParameter(parameterName, null) { DbType = dbType };
            _parameters.Add(param);
            return param;
        }

        public SqlParameter Add(string parameterName, SqlDbType dbType, int size)
        {
            var param = new SqlParameter(parameterName, null) { DbType = dbType, Size = size };
            _parameters.Add(param);
            return param;
        }

        public void Clear()
        {
            _parameters.Clear();
        }

        public Dictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>();
            foreach (var p in _parameters)
            {
                dict[p.ParameterName] = p.Value;
            }
            return dict;
        }

        public IEnumerator GetEnumerator()
        {
            return _parameters.GetEnumerator();
        }
    }

    public class SqlParameter
    {
        public string ParameterName { get; set; }
        public object Value { get; set; }
        public SqlDbType DbType { get; set; }
        public int Size { get; set; }

        public SqlParameter()
        {
            ParameterName = string.Empty;
        }

        public SqlParameter(string name, object value)
        {
            ParameterName = name;
            Value = value;
        }
    }

    public class SqlDataReader : IDataReader
    {
        private readonly DataTable _dt;
        private int _currentIndex = -1;

        public bool HasRows => _dt.Rows.Count > 0;

        public SqlDataReader(DataTable dt)
        {
            _dt = dt ?? new DataTable();
        }

        public bool Read()
        {
            _currentIndex++;
            return _currentIndex < _dt.Rows.Count;
        }

        public object this[int index] => _dt.Rows[_currentIndex][index];
        public object this[string name] => _dt.Rows[_currentIndex][name];

        public string GetString(int i) => _dt.Rows[_currentIndex][i]?.ToString() ?? string.Empty;
        public int GetInt32(int i) => Convert.ToInt32(_dt.Rows[_currentIndex][i]);
        public decimal GetDecimal(int i) => Convert.ToDecimal(_dt.Rows[_currentIndex][i]);
        public double GetDouble(int i) => Convert.ToDouble(_dt.Rows[_currentIndex][i]);

        public bool IsDBNull(int i)
        {
            return _dt.Rows[_currentIndex][i] == DBNull.Value || _dt.Rows[_currentIndex][i] == null;
        }

        public void Close() { IsClosed = true; }
        public void Dispose() { Close(); }

        // IDataReader members
        public int Depth => 0;
        public bool IsClosed { get; private set; } = false;
        public int RecordsAffected => -1;

        public DataTable GetSchemaTable() => _dt;
        public bool NextResult() => false;

        // IDataRecord members
        public int FieldCount => _dt.Columns.Count;
        public string GetName(int i) => _dt.Columns[i].ColumnName;
        public string GetDataTypeName(int i) => _dt.Columns[i].DataType.Name;
        public Type GetFieldType(int i) => _dt.Columns[i].DataType;
        public object GetValue(int i) => _dt.Rows[_currentIndex][i];
        public int GetValues(object[] values)
        {
            int num = Math.Min(values.Length, FieldCount);
            for (int i = 0; i < num; i++)
            {
                values[i] = GetValue(i);
            }
            return num;
        }
        public int GetOrdinal(string name) => _dt.Columns.IndexOf(name);

        public bool GetBoolean(int i) => Convert.ToBoolean(GetValue(i));
        public byte GetByte(int i) => Convert.ToByte(GetValue(i));
        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            byte[] bytes = (byte[])GetValue(i);
            int num = Math.Min(bytes.Length - (int)fieldOffset, length);
            Buffer.BlockCopy(bytes, (int)fieldOffset, buffer, bufferoffset, num);
            return num;
        }
        public char GetChar(int i) => Convert.ToChar(GetValue(i));
        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            char[] chars = GetValue(i).ToString().ToCharArray();
            int num = Math.Min(chars.Length - (int)fieldoffset, length);
            Array.Copy(chars, (int)fieldoffset, buffer, bufferoffset, num);
            return num;
        }
        public Guid GetGuid(int i) => (Guid)GetValue(i);
        public short GetInt16(int i) => Convert.ToInt16(GetValue(i));
        public long GetInt64(int i) => Convert.ToInt64(GetValue(i));
        public float GetFloat(int i) => Convert.ToSingle(GetValue(i));
        public DateTime GetDateTime(int i) => Convert.ToDateTime(GetValue(i));
        public IDataReader GetData(int i) => null;
    }

    public class SqlDataAdapter : IDisposable
    {
        public SqlCommand SelectCommand { get; set; }

        public SqlDataAdapter() { }

        public SqlDataAdapter(SqlCommand selectCommand)
        {
            SelectCommand = selectCommand;
        }

        public SqlDataAdapter(string selectCommandText, SqlConnection selectConnection)
        {
            SelectCommand = new SqlCommand(selectCommandText, selectConnection);
        }

        public int Fill(DataTable dt)
        {
            if (SelectCommand == null) return 0;
            DataTable result = SelectCommand.ExecuteTableInternal();

            // Setup schema in target DataTable
            foreach (DataColumn col in result.Columns)
            {
                if (!dt.Columns.Contains(col.ColumnName))
                {
                    dt.Columns.Add(col.ColumnName, col.DataType);
                }
            }

            // Copy rows
            foreach (DataRow row in result.Rows)
            {
                dt.ImportRow(row);
            }
            return result.Rows.Count;
        }

        public int Fill(DataSet ds)
        {
            DataTable dt = new DataTable();
            int count = Fill(dt);
            ds.Tables.Add(dt);
            return count;
        }

        public void Dispose() { }
    }

    public static class ApiClient
    {
        private static readonly HttpClient _client;
        private static readonly string _baseUrl;

        static ApiClient()
        {
            string url = System.Configuration.ConfigurationManager.AppSettings["BackendApiUrl"];
            if (string.IsNullOrWhiteSpace(url))
            {
                url = "http://localhost:5096/";
            }
            if (!url.EndsWith("/"))
            {
                url += "/";
            }
            _baseUrl = url;

            _client = new HttpClient();
            _client.BaseAddress = new Uri(_baseUrl);
            _client.Timeout = TimeSpan.FromSeconds(30);
        }

        public static string BaseUrl => _baseUrl;

        public static DataTable ExecuteTable(string sql, Dictionary<string, object> parameters)
        {
            var request = new QueryRequest { Sql = sql, Parameters = parameters };
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = _client.PostAsync("api/database/execute-table", content).Result;
            response.EnsureSuccessStatusCode();

            var responseJson = response.Content.ReadAsStringAsync().Result;
            var list = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(responseJson);
            return ConvertToDataTable(list);
        }

        public static object ExecuteScalar(string sql, Dictionary<string, object> parameters)
        {
            var request = new QueryRequest { Sql = sql, Parameters = parameters };
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = _client.PostAsync("api/database/execute-scalar", content).Result;
            response.EnsureSuccessStatusCode();

            var responseJson = response.Content.ReadAsStringAsync().Result;
            var resultObj = JsonConvert.DeserializeObject<ScalarResponse>(responseJson);

            if (resultObj == null || resultObj.Value == null) return DBNull.Value;

            if (resultObj.IsBinary)
            {
                return Convert.FromBase64String(resultObj.Value.ToString());
            }

            return resultObj.Value;
        }

        public static int ExecuteNonQuery(string sql, Dictionary<string, object> parameters)
        {
            var request = new QueryRequest { Sql = sql, Parameters = parameters };
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = _client.PostAsync("api/database/execute-nonquery", content).Result;
            response.EnsureSuccessStatusCode();

            var responseJson = response.Content.ReadAsStringAsync().Result;
            var resultObj = JsonConvert.DeserializeObject<NonQueryResponse>(responseJson);
            return resultObj?.RowsAffected ?? 0;
        }

        public static void ExecuteProcedure(string procName, Dictionary<string, object> parameters)
        {
            var request = new ProcedureRequest { ProcedureName = procName, Parameters = parameters };
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = _client.PostAsync("api/database/execute-procedure", content).Result;
            response.EnsureSuccessStatusCode();
        }

        public static bool CheckHealth(out string msg)
        {
            try
            {
                var response = _client.GetAsync("api/database/health").Result;
                if (response.IsSuccessStatusCode)
                {
                    msg = "Connected successfully to backend Web API.";
                    return true;
                }
                msg = "Backend Web API health check failed: " + response.ReasonPhrase;
                return false;
            }
            catch (Exception ex)
            {
                msg = "Could not connect to backend Web API: " + ex.Message;
                return false;
            }
        }

        private static DataTable ConvertToDataTable(List<Dictionary<string, object>> list)
        {
            DataTable dt = new DataTable();
            if (list == null || list.Count == 0) return dt;

            foreach (var key in list[0].Keys)
            {
                dt.Columns.Add(key);
            }

            foreach (var item in list)
            {
                DataRow row = dt.NewRow();
                foreach (var kvp in item)
                {
                    if (kvp.Value is string s && (kvp.Key.EndsWith("_image", StringComparison.OrdinalIgnoreCase) || kvp.Key.Equals("q_image", StringComparison.OrdinalIgnoreCase) || kvp.Key.Equals("question_image", StringComparison.OrdinalIgnoreCase) || kvp.Key.Equals("ques_image", StringComparison.OrdinalIgnoreCase)) && IsBase64String(s, out byte[] bytes))
                    {
                        row[kvp.Key] = bytes;
                    }
                    else
                    {
                        row[kvp.Key] = kvp.Value ?? DBNull.Value;
                    }
                }
                dt.Rows.Add(row);
            }
            return dt;
        }

        private static bool IsBase64String(string s, out byte[] bytes)
        {
            bytes = null;
            if (string.IsNullOrEmpty(s) || s.Length % 4 != 0 || s.Contains(" ") || s.Contains("\t") || s.Contains("\r") || s.Contains("\n"))
                return false;
            try
            {
                bytes = Convert.FromBase64String(s);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private class QueryRequest
        {
            public string Sql { get; set; }
            public Dictionary<string, object> Parameters { get; set; }
        }

        private class ProcedureRequest
        {
            public string ProcedureName { get; set; }
            public Dictionary<string, object> Parameters { get; set; }
        }

        private class ScalarResponse
        {
            public object Value { get; set; }
            public bool IsBinary { get; set; }
        }

        private class NonQueryResponse
        {
            public int RowsAffected { get; set; }
        }
    }
}
