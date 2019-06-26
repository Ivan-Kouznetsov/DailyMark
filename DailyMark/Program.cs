using System;
using System.Collections.Generic;
using DailyMark.DAO;
using DailyMark.Models;
using Newtonsoft.Json;
using System.IO;
using DailyMark.Reports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace DailyMark
{
    public enum FileProblems {
        NotFound,
        NotValid
    }

    class Program
    {
        const string dateFormat = "MMM dd, yyyy";
        const string queriesFilename = "SearchQueries.txt";


        readonly static string licenseStatement = "DailyMark trademark application monitoring software ©2019 Ivan Kouznetsov" + Environment.NewLine + Environment.NewLine +
                                                  "Distributed under the Affero General Public License 3.0" + Environment.NewLine + "WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the GNU Affero General Public License for more details. https://www.gnu.org/licenses/agpl-3.0.en.html";

        public static IEnumerable<DateTime> EachDay(DateTime from, DateTime thru)
        {
            for (var day = from.Date; day.Date <= thru.Date; day = day.AddDays(1))
                yield return day;
        }

        static void WriteTimeStampedLine(string s) {
            Console.WriteLine(DateTime.Now.ToShortTimeString() + " " + s);
        }

        static string RunReport(Dictionary<string, string> queries, Settings settings, DateTime startDate) {
            string result = string.Empty;
            
            List<SearchResult> searchResults = new List<SearchResult>();

            foreach (string k in queries.Keys)
            {               
                searchResults.Add(LocalDAO.Search(k, queries[k], startDate));
            }

            
            try{
                    result = ReportBuilder.SaveReport(settings, searchResults);
            }catch (Exception ex){
                    Console.WriteLine("There was an error saving the report. Error details: " + ex.Message);
                    Console.ReadLine();
            }
           
            settings.LastSuccessfulReportDate = DateTime.Now.Date;
            LocalDAO.SaveSettings(settings);
            return result;
        }

        

        static bool PromptYesNo(string prompt) {
            char c = '\0';
            char[] yesno = new char[] {'y','n'}; 
            do
            {
                Console.Write(prompt + "[y/n]");
                c = Console.ReadKey().KeyChar;
                Console.WriteLine();
            } while (!yesno.Contains(c));

            return c == 'y';
        }

        static int PromptInt(string prompt, int min, int max)
        {
            bool validInput = false;
            int input = 0;
            string rawInput = string.Empty;
            do
            {
                Console.Write(prompt + " ({0}-{1}): ",min.ToString(),max.ToString());
                rawInput = Console.ReadLine();

                if (int.TryParse(rawInput, out input))
                {
                    if (input >= min && input <= max)
                    {
                        validInput = true;
                    }                    
                }
                
                if (!validInput) Console.WriteLine("Please enter a number from {0} to {1}", min.ToString(), max.ToString());

                Console.WriteLine();

            } while (!validInput);

            return input;
        }

        static void PressAnyKey() {
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static DateTime PromptPastDate(string prompt)
        {
            bool validInput = false;
            DateTime input = new DateTime();
            string rawInput = string.Empty;
            do
            {
                Console.Write(prompt);
                rawInput = Console.ReadLine();

                if (DateTime.TryParse(rawInput,out input))
                {
                   if (DateTime.Now.Date > input) validInput = true;
                }

                if (!validInput) Console.WriteLine("Please enter a date in the past. Example: Jan 1 2019");

                Console.WriteLine();

            } while (!validInput);

            return input;
        }

        static DateTime PromptPastDate(string prompt, DateTime earliestDate) {
            DateTime dt = PromptPastDate(prompt);
            if (dt < earliestDate)
            {
                Console.WriteLine("Date can't be ealier than " + earliestDate.ToString(dateFormat));
                return PromptPastDate(prompt, earliestDate);
            }

            //else
            return dt;
        }

        //as per https://github.com/dotnet/corefx/issues/10361
        public static void OpenBrowser(string url)
        {      
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }                       
        }

        static void Main(string[] args)
        {

            DateTime? searchDate = null;

            bool downloadOnly = false;
            if (args.Length > 0) {
                if (args[0] == "-downloadonly") downloadOnly = true;
                if ((args[0]) == "-version") {
                    System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                    FileVersionInfo fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location);
                    Console.WriteLine(fileVersion.FileVersion);

                    return;
                }
            } 

            Console.Write(licenseStatement);  
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();

            List<int> newAppStatusCodeIds = LocalDAO.GetNewApplicationStatusCodes();

            if (UserInputDAO.TryGetQueries(queriesFilename, out Dictionary<string, string> queries, out string errorMessage))
            {

                Console.WriteLine("There are " + queries.Count.ToString() + " queries in " + queriesFilename);

                Settings settings = LocalDAO.GetSettings();

                if (LocalDAO.CreateDatabaseIfNeeded())
                {

                    if (!LocalDAO.ValidateDatabase()) {
                        Console.WriteLine("Local trademark application database is corrupted");
                        Console.WriteLine("Deleting and re-creating database");
                        LocalDAO.DeleteDatabase();
                        LocalDAO.CreateDatabaseIfNeeded();
                        settings.EarliestFilingDate = DateTime.MinValue;
                    } 

                    DateTime latestDate = settings.LastDownloadDate;

                    if (settings.EarliestFilingDate == DateTime.MinValue || settings.LastSuccessfulReportDate == DateTime.MinValue)
                    {
                        //first run
                        searchDate = PromptPastDate("Search Start Date (ex: Jan 1 2019):");
                        settings.EarliestFilingDate = (DateTime)searchDate;
                        settings.LastSuccessfulReportDate = settings.EarliestFilingDate;
                        LocalDAO.SaveSettings(settings);
                        latestDate = settings.EarliestFilingDate;
                    }
                   

                        
                    DateTime yesterday = DateTime.Now.AddDays(-1).Date;

                    if ((latestDate).Date >= yesterday)
                    {
                        Console.WriteLine("Local trademark application database is up-to-date");
                    }
                    else
                    {
                        foreach (DateTime day in EachDay(latestDate, yesterday))
                        {
                            WriteTimeStampedLine("Getting USPTO applications from " + day.ToString("MMM dd, yyyy"));
                            List<TrademarkApplication> trademarkApplications = UsptoDAO.GetDailyTrademarkApplications(day, newAppStatusCodeIds, settings.EarliestFilingDate, settings.DownloadAttempts);
                            LocalDAO.SaveList(trademarkApplications);
                            settings.LastDownloadDate = day;
                            LocalDAO.SaveSettings(settings);
                        }
                    }

                    if (!downloadOnly)
                    {
                        Console.WriteLine();
                        //run report
                        
                       
                        Console.WriteLine("Earliest Filings Downloaded: " + settings.EarliestFilingDate.ToString(dateFormat));
                        Console.WriteLine("Latest Filings Downloaded: " + settings.LastDownloadDate.ToString(dateFormat));
                        Console.WriteLine("Last Report Date: " + settings.LastSuccessfulReportDate.ToString(dateFormat));
                        Console.WriteLine();

                        if (searchDate == null) searchDate = PromptPastDate("Search Start Date:", settings.EarliestFilingDate);
                        string reportName = RunReport(queries, settings, (DateTime)searchDate);                                            
                        OpenBrowser(settings.ReportsDirectory + Path.DirectorySeparatorChar + reportName);
                    }
                }
                else
                {
                    Console.WriteLine("One or more necessary files are missing or corrupted. Please re-install.");
                    PressAnyKey();
                }
            }
            else {
                Console.WriteLine("ERROR: " + queriesFilename + " " + errorMessage);
                PressAnyKey();
            }

         
        }
    }
}
