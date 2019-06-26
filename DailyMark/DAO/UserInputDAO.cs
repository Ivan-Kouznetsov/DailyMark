using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace DailyMark.DAO
{
    public static class UserInputDAO
    {      
        private static bool TrySimpleIniParse(string[] lines, out Dictionary<string, string> queries, out string errorMessage)
        {
            queries = new Dictionary<string, string>();


            for (int i = 0; i < lines.Length; i++) { 
                if (!lines[i].StartsWith("#"))
                {
                    string[] split = lines[i].Split('=');

                    if (split.Length != 2) {                        
                        errorMessage = "Wrong number of equal signs on line " + i + ": " + lines[i];
                        return false;
                    }

                    if (queries.ContainsKey(split[0])) {                      
                        errorMessage = "Name is not unique on line " + i + ": " + lines[i];
                        return false;
                    }

                    queries.Add(split[0], split[1]);
                }
            }

            if (queries.Count == 0) {
                errorMessage = "File is empty.";
                return false;
            }

            errorMessage = null;

            return true;
        }
              

        public static bool TryGetQueries(string queriesFilename, out Dictionary<string, string> queries, out string errorMessage)
        {
            bool fileExists = File.Exists(queriesFilename);
            bool parsed = false;

            queries = null;            
            errorMessage = "File not found";

            if (fileExists)
            {
                parsed = TrySimpleIniParse(File.ReadAllLines(queriesFilename), out queries, out errorMessage);
            }
            
            
            return fileExists && parsed;
        }

    }
}
