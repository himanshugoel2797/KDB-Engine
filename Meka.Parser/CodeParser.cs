using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Meka.Parser
{
    public class CodeParser
    {
        #region Static Object Factories
        public static CodeParser FromFile(string path)
        {
            return new CodeParser(File.ReadAllText(path));
        }
        #endregion
        public string Code { get; set; }

        public CodeParser(string code)
        {
            this.Code = code;

        }

        //Parse and run the script
        public void Run()
        {

        }
    }
}
