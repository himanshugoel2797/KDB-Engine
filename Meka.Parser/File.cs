using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Meka.Parser
{
    internal class File
    {
        public static string ReadAllText(string path)
        {
            if (System.IO.File.Exists(path)) return System.IO.File.ReadAllText(path);
            else return System.IO.File.ReadAllText(System.IO.Path.Combine(GlobalData.StdLibPath, path));
        }
    }
}
