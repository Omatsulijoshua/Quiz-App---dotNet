using Quiz_App.Properties;
using System;
using System.Configuration;
using System.IO;

namespace Quiz_App
{
    public enum DatabaseMode
    {
        Local,
        Azure,
        Offline
    }

    public static class connection_class
    {
        public static DatabaseMode CurrentMode
        {
            get
            {
                // Force to Local, since we proxy everything to the SQLite Web API backend
                return DatabaseMode.Local;
            }
        }

        public static string CurrentConnectionString
        {
            get
            {
                return ApiClient.BaseUrl;
            }
        }

        public static string LocalConnectionString => ApiClient.BaseUrl;

        public static string AzureConnectionString => ApiClient.BaseUrl;

        public static bool HasAzureConfiguration => true;

        public static string LocalDatabaseFilePath
        {
            get
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "quizApp.db");
            }
        }

        public static System.Data.SqlClient.SqlConnectionStringBuilder GetConnectionDetails(DatabaseMode mode)
        {
            try
            {
                string connectionString = Settings.Default.quizAppConnectionString;
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    return new System.Data.SqlClient.SqlConnectionStringBuilder();
                }
                return new System.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
            }
            catch
            {
                return new System.Data.SqlClient.SqlConnectionStringBuilder();
            }
        }

        public static void Initialize()
        {
            // No initialization needed for local SQL Server
        }

        public static void SetMode(DatabaseMode mode)
        {
            // Dummy implementation
            Settings.Default.ActiveDatabaseMode = mode.ToString();
            Settings.Default.Save();
        }

        public static void ConfigureLocalConnection(string server, string database, string userId, string password, bool trustServerCertificate = true, bool encrypt = true, bool multipleActiveResultSets = true, int timeoutSeconds = 30)
        {
            Settings.Default.Save();
        }

        public static void ConfigureAzureConnection(string server, string database, string userId, string password, bool trustServerCertificate = false, bool encrypt = true, bool multipleActiveResultSets = false, int timeoutSeconds = 30)
        {
            Settings.Default.Save();
        }

        public static bool TryOpenConnection(out string message)
        {
            return ApiClient.CheckHealth(out message);
        }

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(CurrentConnectionString);
        }

        public static string GetModeLabel(DatabaseMode mode)
        {
            return "Web API Backend (SQLite)";
        }

        public static bool EnsureLocalDatabaseCopy(out string message)
        {
            message = "Local database is managed by the backend Web API.";
            return true;
        }
    }
}
