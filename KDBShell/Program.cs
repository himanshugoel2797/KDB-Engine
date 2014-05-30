using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meka.Parser;

namespace KDBShell
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "KDBShell";
            KDBParser parser = new KDBParser("");

            while (true)
            {
                Console.Write(">");
                Console.ForegroundColor = ConsoleColor.Green;
                string input = Console.ReadLine();
                Console.ForegroundColor = ConsoleColor.Gray;
                if (input == "clear") Console.Clear();
                else if (input.StartsWith("load"))
                {
                    parser.Code = "import \"" + input.Remove(0, 5).Trim() + "\"";
                    parser.Parse();
                    Console.WriteLine("Loaded " + input.Remove(0, 5).Trim());
                }
                else if ((input.StartsWith("is", true, null) || input.StartsWith("can", true, null)  ))
                {
                    input = input.Replace(" of ", " ").Replace(" made ", " ").Replace(" a ", " ");

                    Console.WriteLine(parser.Exists(input.Split(' ')[1].Trim(), input.Split(' ')[2].Trim()));
                }
                else if (input.StartsWith("what is", true, null))
                {
                    input = input.Replace("what is", "");

                    Console.WriteLine(parser.GetValue(input.Split(' ')[1].Trim(), input.Split(' ')[2].Trim()).Value);
                }
                else if (!string.IsNullOrEmpty(input))
                {
                    Console.WriteLine("Unknown Command " + input);
                }
            }
        }
    }
}
