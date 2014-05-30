using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meka.Parser;

namespace InterMeka
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Write(
@"
    Usage: InterMeka [options] *.imk ...

    Options:
    -wdir=WORKING_DIR   Specify the working directory
"
                    );
                return;
            }


        }
    }
}
