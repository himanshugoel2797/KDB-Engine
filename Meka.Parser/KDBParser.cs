using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Meka.Parser
{
    public class KDBParser
    {
        #region Static Object Factories
        public static KDBParser FromFile(string path)
        {
            return new KDBParser(File.ReadAllText(path));
        }
        #endregion

        #region Data Structures
        public enum Types
        {
            Undefined, String, Int, Property, Float, Double, Base, Child
        }

        public struct Details
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public Types Type { get; set; }
        }

        public Dictionary<Tuple<Types, string>, List<Details>> KnowledgeByType { get; set; }
        public Dictionary<string, List<Details>> Knowledge { get; set; }
        public List<string> Objects { get; set; }
        public List<string> Components { get; set; }
        
        public delegate Details HookedCall(Details[] Args);
        
        Dictionary<string, HookedCall> CSharpFunctionCalls { get; set; }
        #endregion

        public string Code { get; set; }

        public KDBParser(string code)
        {
            this.Code = code;
            KnowledgeByType = new Dictionary<Tuple<Types, string>, List<Details>>();
            Knowledge = new Dictionary<string, List<Details>>();
            Objects = new List<string>();
            Components = new List<string>();

            CSharpFunctionCalls = new Dictionary<string, HookedCall>();
        }

        public void Add(string name, Details details)
        {
            if (!Knowledge.ContainsKey(name)) Knowledge[name] = new List<Details>();
            Knowledge[name].Add(details);

            if (!KnowledgeByType.ContainsKey(new Tuple<Types, string>(details.Type, name))) KnowledgeByType[new Tuple<Types, string>(details.Type, name)] = new List<Details>();
            KnowledgeByType[new Tuple<Types, string>(details.Type, name)].Add(details);
        }

        public void RegisterFunctionCall(string callName, HookedCall call)
        {
            CSharpFunctionCalls[callName] = call;
        }

        public bool Exists(string className, string variableName)
        {
            if (!className.Contains('_'))
            {
                className = new string(new PorterStemmer().stemTerm(className).ToLower().Where(c => !char.IsPunctuation(c)).ToArray());
            }

            className = className.ToLower();
            variableName = new string(new PorterStemmer().stemTerm(variableName).ToLower().Where(c => !char.IsPunctuation(c)).ToArray());
            variableName = variableName.ToLower();

            bool exists = Knowledge[className].Exists((Details d) => { return d.Name.ToLower() == variableName.ToLower(); });

            if (!exists)
            {
                if (KnowledgeByType.ContainsKey(new Tuple<Types, string>(Types.Base, className)))
                {
                    Details[] baseObjects = KnowledgeByType[new Tuple<Types, string>(Types.Base, className)].ToArray();
                    foreach (Details d in baseObjects)
                    {
                        exists = Exists(d.Name, variableName);
                        if (exists) break;
                    }
                }

                if (KnowledgeByType.ContainsKey(new Tuple<Types, string>(Types.Child, className)))
                {
                    Details[] baseComponents = KnowledgeByType[new Tuple<Types, string>(Types.Child, className)].ToArray();
                    foreach (Details d in baseComponents)
                    {
                        exists = Exists(d.Name, variableName);
                        if (exists) break;
                    }
                }
            }

            return exists;
        }

        public Details GetValue(string className, string variableName)
        {
            if (!className.Contains('_'))
            {
                className = new string(new PorterStemmer().stemTerm(className).ToLower().Where(c => !char.IsPunctuation(c)).ToArray());
            }

            className = className.ToLower();
            variableName = new string(new PorterStemmer().stemTerm(variableName).ToLower().Where(c => !char.IsPunctuation(c)).ToArray());
            variableName = variableName.ToLower();

            try
            {
                Details tmp = Knowledge[className].Find((Details d) => { return d.Name.ToLower() == variableName.ToLower(); });

                //If it's a registered function call, calll the registered function
                if (tmp.Value.StartsWith("[") && tmp.Value.EndsWith("]") &&
                    CSharpFunctionCalls.ContainsKey(tmp.Value.Remove("[").Remove("]").RemoveRange('(', ')')))
                {
                    string[] info = tmp.Value.SubstringRange('(', ')').Remove("(").Remove(")").Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    Details[] args = new Details[info.Length];
                    for (int c = 0; c < info.Length; c++)
                    {
                        string[] details = info[c].Split('.');
                        args[c] = Knowledge[(details.Length == 1) ? className : details[0]].Find((Details d) => { return d.Name.ToLower() == ((details.Length == 1) ? info[c] : details[1]).ToLower(); });
                    }

                    tmp = CSharpFunctionCalls[tmp.Value.Remove("[").Remove("]").RemoveRange('(', ')')](args);
                    tmp.Name = variableName;
                }
                return tmp;
            }
            catch (KeyNotFoundException)
            {
                return default(Details);
            }
        }

        public string[] GetClassWithValues(string[] vals)
        {
            List<string> className = new List<string>();

            Details[] tmp = new Details[vals.Length];
            for(int c = 0; c < tmp.Length; c++)
            {
                tmp[c] = new Details(){
                    Name = vals[c]
                };
            }

            foreach (string s in Knowledge.Keys)
            {
                if (Knowledge[s].Intersect(tmp, new LambdaComparer<Details>((d, e) => d.Name.ToLower() == e.Name.ToLower())).Any())
                {
                    className.Add(s);
                }
            }

            return className.ToArray();
        }

        public void Parse()
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
        }
    }
}
