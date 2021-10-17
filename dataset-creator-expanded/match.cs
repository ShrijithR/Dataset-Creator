using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Newtonsoft.Json;
using Deedle;
using System.IO;
using System.Linq;
using System.Text;

namespace DaxtraAPIJoParser
{
    public class DataMatch
    {

        public string GetDownloadList(List<String> storage_list, List<string> fn_ln)
        {
            
            Regex rx = CreateRegex(fn_ln);
            var matches_result = FindMatches(rx, storage_list);
            return matches_result;
        }
        public List<List<string>> CreateRequiredCandidateList()
        {
            List<List<string>> name_list = new List<List<string>>();
            
            var path = "Q:/Thinkbridge/OneDrive - Thinkbridge/Projects/JO Parser/ml/test-dataset-creator/JoParser-Prasanna/DaxtraAPIJoParser/test.csv";
            var df = Frame.ReadCsv(location:path);
            var candidate_name = df.Columns["CandidateName"].GetAllValues().ToList();

            foreach (var name in candidate_name)
            {   
                List<string> temp_list = new List<string>();        
                var b = name.ToString();
                var a = b.Split(" ");        
                foreach (var n in a)
                {
                    temp_list.Add(n);
                }
                name_list.Add(temp_list);
            }
            return name_list;
        }    
        static Regex CreateRegex(List<string> cand)
        {
            Regex rx = new Regex(@"
            ([^0-9a-zA-Z])*?"+
            "("+cand[0]+"|"+cand[1]+")"+
            @"([^0-9a-zA-Z])*?"+
            "("+cand[1]+"|"+cand[0]+")"+
            @"([^0-9a-zA-Z])*?
            .pdf
            ",
            RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            return rx;
        }

        static string FindMatches(Regex rx, List<String> storage_list)
        {
            // Find matches.
            foreach (String n in storage_list)
            {
                MatchCollection matches = rx.Matches(n);
                if (matches.Count == 1)
                {
                    return n;
                }
            }
            return "false";
            //     GroupCollection groups = match.Groups;
            //     Console.WriteLine(groups["word"].Value);

        }
    }    
}