using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HookedCall = Meka.Parser.KDB.HookedCall;

namespace Meka.Parser
{
    /// <summary>
    /// Parses KDB files
    /// </summary>
    public class KDBParser
    {
        #region Static Object Factories
        /// <summary>
        /// Create a new KDBParser object from a file
        /// </summary>
        /// <param name="path">The path to the file</param>
        /// <returns>a new KDBParser object</returns>
        public static KDBParser FromFile(string path)
        {
            return new KDBParser(File.ReadAllText(path));
        }
        #endregion

        #region Data Structures
        /// <summary>
        /// The types available in the KnowledgeDB system
        /// </summary>
        public enum Types
        {
            Undefined, String, Int, Property, Float, Double, Base, Child
        }

        /// <summary>
        /// Stores info extracted from the KnowledgeDB
        /// </summary>
        public struct Details
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public Types Type { get; set; }
        }

        Dictionary<Tuple<Types, string>, List<Details>> KnowledgeByType { get; set; }
        Dictionary<string, List<Details>> Knowledge { get; set; }
        List<string> Objects { get; set; }
        List<string> Components { get; set; }
        Dictionary<string, HookedCall> CSharpFunctionCalls { get; set; }
        #endregion

        public string Code { get; set; }

        /// <summary>
        /// Create a new KDBParser object
        /// </summary>
        /// <param name="code">The KDB code</param>
        public KDBParser(string code)
        {
            this.Code = code;
            KnowledgeByType = new Dictionary<Tuple<Types, string>, List<Details>>();
            Knowledge = new Dictionary<string, List<Details>>();
            Objects = new List<string>();
            Components = new List<string>();

            CSharpFunctionCalls = new Dictionary<string, HookedCall>();
        }

        private void Add(string name, Details details)
        {
            if (!Knowledge.ContainsKey(name)) Knowledge[name] = new List<Details>();
            Knowledge[name].Add(details);

            if (!KnowledgeByType.ContainsKey(new Tuple<Types, string>(details.Type, name))) KnowledgeByType[new Tuple<Types, string>(details.Type, name)] = new List<Details>();
            KnowledgeByType[new Tuple<Types, string>(details.Type, name)].Add(details);
        }

        /// <summary>
        /// Register a native function call with the KDB engine
        /// </summary>
        /// <param name="callName">The name of the function in KDB</param>
        /// <param name="call">The function to call</param>
        public void RegisterFunctionCall(string callName, HookedCall call)
        {
            CSharpFunctionCalls[callName] = call;
        }

        /// <summary>
        /// Returns a KDB object based on this KDBParser instance
        /// </summary>
        /// <returns>A KDB object based on this KDBParser instance</returns>
        public KDB GetKDB()
        {
            return new KDB()
            {
                Components = this.Components,
                CSharpFunctionCalls = this.CSharpFunctionCalls,
                Knowledge = this.Knowledge,
                KnowledgeByType = this.KnowledgeByType,
                Objects = this.Objects
            };
        }

        /// <summary>
        /// Parse the KDB data
        /// </summary>
        /// <returns>A KDB object based on the parsed data</returns>
        public KDB Parse()
        {
            Stack<string> Recursion = new Stack<string>();
            string expectedRecursion = "", parentName = "";

            string[] lines = Code.Split(new string[] { GlobalData.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            for (int lineNum = 0; lineNum < lines.Length; lineNum++)
            {
                //Skip comments
                while (lines[lineNum].StartsWith("//") || string.IsNullOrWhiteSpace(lines[lineNum])) lineNum++;
                string line = lines[lineNum];

                //If it's an import statement
                if (line.StartsWith("import"))
                {
                    string codeBackup = Code;
                    Code = File.ReadAllText(line.Remove("import").Remove("\"").Trim()); //Get the file path specified
                    this.Parse();   //Parse the other file, doing it like this immediately adds all the other's information into our db
                    Code = codeBackup;  //Restore our backup
                }
                else if (line.StartsWith("component"))
                {
                    string info = line.Remove("component").Remove(":").Trim();
                    expectedRecursion = "\t";
                    string[] parts = info.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    Components.Add(parts[0].ToLower());   //Add the component name to the database
                    Add(parts[0].ToLower(), new Details() { Name = parts[0].ToLower(), Type = Types.Undefined });
                    parentName = parts[0].ToLower();
                    //Start extracting info
                    //if(parts[1] == "
                }
                else if (line.StartsWith("object"))
                {
                    string info = line.Remove("object").Remove(":").Trim();
                    expectedRecursion = "\t";
                    string[] parts = info.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    Objects.Add(parts[0].ToLower());   //Add the component name to the database
                    Add(parts[0].ToLower(), new Details() { Name = parts[0].ToLower(), Type = Types.Undefined });
                    parentName = parts[0].ToLower();
                    //Start extracting info
                    if (parts.Length >= 3 && parts[1] == "of")
                    {
                        string[] sections = parts[2].Remove("[").Remove("]").Trim().Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        foreach (string s in sections)
                        {
                            Add(parts[0].ToLower(), new Details() { Name = s.ToLower(), Type = Types.Child });
                        }
                    }
                    else if (parts.Length >= 3 && (parts[1] == "is" || parts[3] == "is"))
                    {
                        string[] sections = parts[(parts[1] == "is")?2:4].Remove("[").Remove("]").Trim().Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        foreach (string s in sections)
                        {
                            Add(parts[0].ToLower(), new Details() { Name = s.ToLower(), Type = Types.Base });
                        }
                    }
                }
                else if (line.StartsWith(expectedRecursion + "var"))
                {
                    string[] parts = line.Remove(expectedRecursion + "var").Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    Details d = new Details();
                    
                    Types t = Types.Undefined;

                         if (parts[0].Trim().ToLower() == "(property)") t = Types.Property;
                    else if (parts[0].Trim().ToLower() == "(int)") t = Types.Int;
                    else if (parts[0].Trim().ToLower() == "(string)") t = Types.String;
                    else if (parts[0].Trim().ToLower() == "(float)") t = Types.Float;
                    else if (parts[0].Trim().ToLower() == "(double)") t = Types.Double;
                         
                    d.Type = t;
                    d.Name = parts[1].Trim().ToLower();

                    if (parts.Length >= 4)
                    {
                        string val = "";
                        for (int c = 3; c < parts.Length; c++)
                        {
                            val += " " + parts[c];
                        }
                        d.Value = val.Remove("\"").Trim();
                    }

                    Add(parentName, d);
                }

            }


            return GetKDB();
        }
    }
}
