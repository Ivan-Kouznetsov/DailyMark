using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Data.Sqlite;
using System.IO;
using DailyMark.Models;

namespace DailyMark.DAO
{
    public static class LocalDAO
    {
        private const string dbname = "localtm.db";
        private const string statuscodesFilename = "StatusCodes.json";
        private const string settingsFileName = "Settings.json";

        static readonly Settings defaultSettings = new Settings(AppDomain.CurrentDomain.BaseDirectory + "Reports",  DateTime.Now.AddDays(-1), DateTime.Now.AddDays(-10), DateTime.MinValue, ReportFormat.Html, 5);

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

        public static bool CreateDatabaseIfNeeded()
        {

            if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + dbname))
            {
                if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + statuscodesFilename))
                {

                    string statusCodesJson = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + statuscodesFilename);

                    try
                    {
                        List<StatusCode> statusCodes = JsonConvert.DeserializeObject<List<StatusCode>>(statusCodesJson);

                        SqliteConnection sqliteConnection = new SqliteConnection("Data Source=" + dbname + ";");

                        string createCommand = "CREATE TABLE TrademarkApplications(SerialNumber INTEGER PRIMARY KEY, MarkLiteralElements TEXT, StatusCode INTEGER, FilingDate Date, DateAdded DATE);" +
                                               "CREATE TABLE StatusCodes (Id INTEGER PRIMARY KEY, Indicator varchar(225), Description varchar(225)); ";

                        
                        SqliteCommand sqlitCreateCommand = new SqliteCommand(createCommand, sqliteConnection);

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
                        return false;
                    }
                }
                else {
                    return false;
                }
            }

            return true;
        }

        public static List<int> GetNewApplicationStatusCodes()
        {
            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Error
            };

            List<StatusCode> statusCodes = JsonConvert.DeserializeObject<List<StatusCode>>(File.ReadAllText(statuscodesFilename), jsonSerializerSettings);
            List<StatusCode> newAppStatusCodes = statusCodes.FindAll(c => c.IsNewApplication);
            List<int> result = new List<int>();

            foreach (StatusCode s in newAppStatusCodes)
            {
                result.Add(s.Id);
            }

            return result;
        }

        public static DateTime GetEarliestDate()
        {
            SqliteConnection sqliteConnection = new SqliteConnection("Data Source=" + dbname + ";");
            SqliteCommand sqliteCommand = new SqliteCommand("SELECT FilingDate from TrademarkApplications ORDER BY FilingDate ASC LIMIT 1;", sqliteConnection);
            DateTime result = new DateTime();

            sqliteConnection.Open();
            SqliteDataReader reader = sqliteCommand.ExecuteReader();
            while (reader.Read())
            {
                result = DateTime.Parse((string)reader["FilingDate"]);
            }
            sqliteConnection.Close();

            return result;
        }


        public static void SaveList(List<TrademarkApplication> list)
        {
            SqliteConnection sqliteConnection = new SqliteConnection("Data Source=" + dbname + ";");
            sqliteConnection.Open();
            using (SqliteCommand sqliteCommand = new SqliteCommand("INSERT OR REPLACE INTO TrademarkApplications (SerialNumber, MarkLiteralElements, StatusCode, FilingDate, DateAdded) VALUES (@SerialNumber, @MarkLiteralElements, @StatusCode, @FilingDate, @DateAdded)", sqliteConnection))
            using (var t = sqliteConnection.BeginTransaction())
            {
                sqliteCommand.Transaction = t;
                foreach (TrademarkApplication tm in list)
                {
                    if (tm.MarkLiteralElements != null)
                    {
                        sqliteCommand.Parameters.Clear();
                        sqliteCommand.Parameters.AddWithValue("@SerialNumber", tm.SerialNumber);
                        sqliteCommand.Parameters.AddWithValue("@MarkLiteralElements", tm.MarkLiteralElements);
                        sqliteCommand.Parameters.AddWithValue("@StatusCode", tm.StatusCode.Id);
                        sqliteCommand.Parameters.AddWithValue("@FilingDate", tm.FilingDate.Date);
                        sqliteCommand.Parameters.AddWithValue("@DateAdded", tm.DateAdded.Date);
                        sqliteCommand.ExecuteNonQuery();
                    }
                }

                t.Commit();
            }

            sqliteConnection.Close();
        }


        public static SearchResult Search(string queryName, string searchPattern, DateTime from)
        {
            SqliteConnection sqliteConnection = new SqliteConnection("Data Source=" + dbname + ";");
            SqliteCommand sqliteCommand = new SqliteCommand("SELECT SerialNumber, MarkLiteralElements, FilingDate, DateAdded, StatusCode, Indicator, Description from TrademarkApplications JOIN StatusCodes on TrademarkApplications.StatusCode = StatusCodes.Id WHERE MarkLiteralElements LIKE @searchPattern AND FilingDate >= @from;", sqliteConnection);

            sqliteCommand.Parameters.AddWithValue("@searchPattern", searchPattern);
            sqliteCommand.Parameters.AddWithValue("@from", from);
         
            List<TrademarkApplication> results = new List<TrademarkApplication>();

            sqliteConnection.Open();
            SqliteDataReader reader = sqliteCommand.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new TrademarkApplication(DateTime.Parse((string)reader["FilingDate"]), DateTime.Parse((string)reader["DateAdded"]), Convert.ToInt32(reader["SerialNumber"]), (string)reader["MarkLiteralElements"], new StatusCode(Convert.ToInt32(reader["StatusCode"]),(string)reader["Indicator"],(string)reader["Description"])));

            }
            sqliteConnection.Close();


            return new SearchResult(queryName, searchPattern,from, DateTime.Now, results);
        }

    }
}
