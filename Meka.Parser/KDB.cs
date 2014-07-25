using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Details = Meka.Parser.KDBParser.Details;
using Types = Meka.Parser.KDBParser.Types;

namespace Meka.Parser
{
    /// <summary>
    /// KnowledgeDB data container
    /// </summary>
    public struct KDB
    {
        /// <summary>
        /// Stores all knowledge based on its type
        /// </summary>
        public Dictionary<
            Tuple<Meka.Parser.KDBParser.Types, string>,
            List<Meka.Parser.KDBParser.Details>
            > KnowledgeByType { get; set; }

        /// <summary>
        /// Stores all knowledge by name
        /// </summary>
        public Dictionary<string, 
            List<Meka.Parser.KDBParser.Details>
            > Knowledge { get; set; }

        /// <summary>
        /// Stores a list of defined objects
        /// </summary>
        public List<string> Objects { get; set; }

        /// <summary>
        /// Stores a list of defined components
        /// </summary>
        public List<string> Components { get; set; }

        /// <summary>
        /// Engine hooked function call delegate
        /// </summary>
        /// <param name="funcName">The KDB name of the function called</param>
        /// <param name="Args">The arguments passed to the function</param>
        /// <returns></returns>
        public delegate Meka.Parser.KDBParser.Details HookedCall(string funcName, Meka.Parser.KDBParser.Details[] Args);

        internal Dictionary<string, HookedCall> CSharpFunctionCalls { get; set; }

        /// <summary>
        /// Add new knowledge to the system
        /// </summary>
        /// <param name="name">The name/identifier of the knowledge</param>
        /// <param name="details">The information about the knowledge</param>
        public void Add(string name, Details details)
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
        /// Checks if the specific data exists
        /// </summary>
        /// <param name="className">The 'class' to check</param>
        /// <param name="variableName">The specific variable to check</param>
        /// <returns>true if the data exists, otherwise false</returns>
        public bool Exists(string className, string variableName)
        {
            if (!className.Contains('_'))
            {
                className = new string(new PorterStemmer().stemTerm(className).ToLower().Where(c => !char.IsPunctuation(c)).ToArray());
            }

            className = className.ToLower();
            variableName = new string(new PorterStemmer().stemTerm(variableName).ToLower().Where(c => !char.IsPunctuation(c)).ToArray());
            variableName = variableName.ToLower();

            if (!Knowledge.ContainsKey(className)) return false;

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

        /// <summary>
        /// Get the value of some knowledge data
        /// </summary>
        /// <param name="className">The 'class' to check</param>
        /// <param name="variableName">The specific variable to check</param>
        /// <returns>The value of the data</returns>
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

                    string funcName = tmp.Value.Remove("[").Remove("]").RemoveRange('(', ')');
                    tmp = CSharpFunctionCalls[funcName](funcName, args);
                    tmp.Name = variableName;
                }
                return tmp;
            }
            catch (KeyNotFoundException)
            {
                return default(Details);
            }
        }

        /// <summary>
        /// Get a list of 'classes' which have a certain set of variables defined
        /// </summary>
        /// <param name="vals">The variables to check for</param>
        /// <returns>An array of all classes which have the requested variables</returns>
        public string[] GetClassWithValues(params string[] vals)
        {
            List<string> className = new List<string>();

            Details[] tmp = new Details[vals.Length];
            for (int c = 0; c < tmp.Length; c++)
            {
                tmp[c] = new Details()
                {
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
    }
}
