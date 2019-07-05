using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Data.Sqlite;
using System.IO;
using DailyMark.Models;
using System.Threading.Tasks;

namespace DailyMark.DAO
{
    public static class LocalDAO
    {
        private const string dbname = "localtm.db";
        private const string statuscodesFilename = "StatusCodes.json";
        private const string settingsFileName = "Settings.json";

        static readonly Settings defaultSettings = new Settings(AppDomain.CurrentDomain.BaseDirectory + "Reports",  DateTime.Now.AddDays(-1), DateTime.Now.AddDays(-10), DateTime.MinValue, ReportFormat.Html, 5);
        public static List<StatusCode> StatusCodes { get; private set; }

        static LocalDAO()
        {
            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Error
            };

            StatusCodes = JsonConvert.DeserializeObject<List<StatusCode>>(File.ReadAllText(statuscodesFilename), jsonSerializerSettings);
        }

        private static Settings CreateSettingsFile()
        {
            File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + settingsFileName, JsonConvert.SerializeObject(defaultSettings));
            return defaultSettings;
        }
        public static Settings GetSettings()
        {
            string filePath = AppDomain.CurrentDomain.BaseDirectory + settingsFileName;

            if (File.Exists(filePath))
            {
                try
                {
                    JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
                    {
                        MissingMemberHandling = MissingMemberHandling.Error
                    };

                    return JsonConvert.DeserializeObject<Settings>(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + settingsFileName), jsonSerializerSettings);
                }
                catch { }             
            }        

            return CreateSettingsFile();
        }

        public static void SaveSettings(Settings settings)
        {
            string filePath = AppDomain.CurrentDomain.BaseDirectory + settingsFileName;
            File.WriteAllText(filePath, JsonConvert.SerializeObject(settings));
        }

        public static void DeleteDatabase()
        {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + dbname))
            {
                try
                {
                    File.Delete(AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + dbname);
                }
                catch (IOException e){
                    Console.WriteLine(e.Message);
                    Task.Delay(5000).Wait();
                    DeleteDatabase();
                }
            }
        }
        public static bool CreateDatabaseIfNeeded()
        {

            if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + dbname))
            {
                if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + statuscodesFilename))
                {

                    string statusCodesJson = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + statuscodesFilename);

                   
                        List<StatusCode> statusCodes = JsonConvert.DeserializeObject<List<StatusCode>>(statusCodesJson);

                        string createCommand = "CREATE TABLE TrademarkApplications(SerialNumber INTEGER PRIMARY KEY, MarkLiteralElements TEXT, StatusCode INTEGER, FilingDate Date, DateAdded DATE);" +
                                            "CREATE TABLE StatusCodes (Id INTEGER PRIMARY KEY, Indicator varchar(225), Description varchar(225)); ";


                        SqliteConnection sqliteConnection = new SqliteConnection("Data Source=" + dbname + ";"); 
                        SqliteCommand sqlitCreateCommand = new SqliteCommand(createCommand, sqliteConnection);
                        try{

                        sqliteConnection.Open();
                        sqlitCreateCommand.ExecuteNonQuery();

                        //populate StatusCodes

                        using (SqliteCommand sqliteInsertCommand = new SqliteCommand("INSERT INTO StatusCodes (Id, Indicator, Description) VALUES (@Id, @Indicator, @Description)", sqliteConnection))
                        using (var t = sqliteConnection.BeginTransaction())
                        {
                            sqliteInsertCommand.Transaction = t;
                            foreach (StatusCode s in statusCodes)
                            {
                                sqliteInsertCommand.Parameters.Clear();
                                sqliteInsertCommand.Parameters.AddWithValue("@Id", s.Id);
                                sqliteInsertCommand.Parameters.AddWithValue("@Indicator", s.Indicator);
                                sqliteInsertCommand.Parameters.AddWithValue("@Description", s.Description);
                                sqliteInsertCommand.ExecuteNonQuery();
                            }

                            t.Commit();
                        }

                        sqliteConnection.Close();

                    }
                    catch
                    {
                        sqliteConnection.Close();
                        return false;
                    }
                }
                else {
                    return false;
                }
            }

            return true;
        }

        public static bool ValidateDatabase()
        {
            string result = String.Empty;

            using (SqliteConnection sqliteConnection = new SqliteConnection("Data Source=" + dbname + ";"))
            using (SqliteCommand sqliteCommand = new SqliteCommand("pragma integrity_check;", sqliteConnection))
            {
                
                try
                {
                    sqliteConnection.Open();
                    SqliteDataReader reader = sqliteCommand.ExecuteReader();
                    while (reader.Read())
                    {
                        result = (string)reader["integrity_check"];
                    }
                    sqliteConnection.Close();

                    
                }
                catch
                {

                    sqliteConnection.Close();
                    sqliteConnection.Dispose();
                    return false;
                }
            }

            return result == "ok";
        }

        public static List<int> GetNewApplicationStatusCodes()
        {
            
            List<StatusCode> newAppStatusCodes = StatusCodes.FindAll(c => c.IsNewApplication);
            List<int> result = new List<int>();

            foreach (StatusCode s in newAppStatusCodes)
            {
                result.Add(s.Id);
            }

            return result;
        }

        public static List<int> GetDeadApplicationStatusCodes()
        {
            List<StatusCode> deadAppStatusCodes = StatusCodes.FindAll(c => c.IsDead);
            List<int> result = new List<int>();

            foreach (StatusCode s in deadAppStatusCodes)
            {
                result.Add(s.Id);
            }

            return result;
        }

        public static DateTime GetEarliestDate()
        {
            DateTime result = new DateTime();

            using (SqliteConnection sqliteConnection = new SqliteConnection("Data Source=" + dbname + ";"))
            {
                sqliteConnection.Open();
                using (SqliteCommand sqliteCommand = new SqliteCommand("SELECT FilingDate from TrademarkApplications ORDER BY FilingDate ASC LIMIT 1;", sqliteConnection))
                using (SqliteDataReader reader = sqliteCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result = DateTime.Parse((string)reader["FilingDate"]);
                    }

                }
            }
            return result;
        }

        private static List<int> GetSerialNumbers(List<TrademarkApplication> applications)
        {
            List<int> serialNumbers = new List<int>();
            foreach (TrademarkApplication tm in applications)
            {
                serialNumbers.Add(tm.SerialNumber);
            }

            return serialNumbers;
        }

        public static void SaveApplications(List<TrademarkApplication> newApplications, List<TrademarkApplication> deadApplications)
        {
            using (SqliteConnection sqliteConnection = new SqliteConnection("Data Source=" + dbname + ";"))
            {
                sqliteConnection.Open();
                using (SqliteCommand sqliteCommand = new SqliteCommand("INSERT OR REPLACE INTO TrademarkApplications (SerialNumber, MarkLiteralElements, StatusCode, FilingDate, DateAdded, CaseFileDate) VALUES (@SerialNumber, @MarkLiteralElements, @StatusCode, @FilingDate, @DateAdded, @CaseFileDate)", sqliteConnection))
                using (var t = sqliteConnection.BeginTransaction())
                {
                    sqliteCommand.Transaction = t;
                    foreach (TrademarkApplication tm in newApplications)
                    {
                        if (tm.MarkLiteralElements != null)
                        {
                            sqliteCommand.Parameters.Clear();
                            sqliteCommand.Parameters.AddWithValue("@SerialNumber", tm.SerialNumber);
                            sqliteCommand.Parameters.AddWithValue("@MarkLiteralElements", tm.MarkLiteralElements);
                            sqliteCommand.Parameters.AddWithValue("@StatusCode", tm.StatusCode.Id);
                            sqliteCommand.Parameters.AddWithValue("@FilingDate", tm.FilingDate.Date);
                            sqliteCommand.Parameters.AddWithValue("@DateAdded", tm.DateAdded.Date);
                            sqliteCommand.Parameters.AddWithValue("@CaseFileDate", tm.CaseFileDate.Date);
                            sqliteCommand.ExecuteNonQuery();
                        }
                    }

                    t.Commit();
                }

                if (deadApplications.Count > 0)
                {
                    using (SqliteCommand sqliteCommand = new SqliteCommand("DELETE FROM TrademarkApplications WHERE SerialNumber in (@deletelist)", sqliteConnection))
                    {
                        List<int> deadSerialNumbers = GetSerialNumbers(deadApplications);
                        List<string> deadSerialNumberStrings = new List<string>();

                        foreach (int s in deadSerialNumbers)
                        {
                            deadSerialNumberStrings.Add(s.ToString());
                        }

                        sqliteCommand.Parameters.AddWithValue("@deletelist", String.Join(',', deadSerialNumberStrings));
                        sqliteCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        public static SearchResult Search(string queryName, string searchPattern, DateTime from)
        {
            List<TrademarkApplication> results = new List<TrademarkApplication>();

            using (SqliteConnection sqliteConnection = new SqliteConnection("Data Source=" + dbname + ";"))
            using (SqliteCommand sqliteCommand = new SqliteCommand("SELECT SerialNumber, MarkLiteralElements, FilingDate, DateAdded, StatusCode, Indicator, Description from TrademarkApplications JOIN StatusCodes on TrademarkApplications.StatusCode = StatusCodes.Id WHERE MarkLiteralElements LIKE @searchPattern AND FilingDate >= @from;", sqliteConnection))
            {
                sqliteCommand.Parameters.AddWithValue("@searchPattern", searchPattern);
                sqliteCommand.Parameters.AddWithValue("@from", from);
                sqliteConnection.Open();               

                SqliteDataReader reader = sqliteCommand.ExecuteReader();
                while (reader.Read())
                {
                    results.Add(new TrademarkApplication(DateTime.Parse((string)reader["FilingDate"]),
                        DateTime.Parse((string)reader["DateAdded"]),
                        Convert.ToInt32(reader["SerialNumber"]),
                        (string)reader["MarkLiteralElements"],
                        new StatusCode(Convert.ToInt32(reader["StatusCode"]),
                        (string)reader["Indicator"], 
                        (string)reader["Description"])));
                }
            }            

            return new SearchResult(queryName, searchPattern,from, DateTime.Now, results);
        }
    }
}
