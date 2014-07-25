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
        static KDBParser.Details CalculateAge(string funcName, KDBParser.Details[] details)
        {
            int year = int.Parse(details[0].Value);
            return new KDBParser.Details()
            {
                Value = (DateTime.Today.Year - year).ToString()
            };
        }

        static void Main(string[] args)
        {
            Console.Title = "KDBShell";
            KDBParser parser = new KDBParser("");
            KDB data;
            parser.RegisterFunctionCall("calculateAge", CalculateAge);

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
                    data = parser.Parse();
                    Console.WriteLine("Loaded " + input.Remove(0, 5).Trim());
                }
                else if ((input.StartsWith("is", true, null) || input.StartsWith("can", true, null)  ))
                {
                    input = input.Replace(" of ", " ").Replace(" made ", " ").Replace(" a ", " ");

                    Console.WriteLine(data.Exists(input.Split(' ')[1].Trim(), input.Split(' ')[2].Trim()));
                }
                else if (input.StartsWith("what is", true, null))
                {
                    input = input.Replace("what is", "").Trim();

                    Console.WriteLine(data.GetValue(input.Split(' ')[0].Trim(), input.Split(' ')[1].Trim()).Value);
                }
                else if (input.StartsWith("what contains", true, null))
                {
                    input = input.Replace("what contains", "").Replace("?", "").Trim();

                    string[] list = data.GetClassWithValues(input.Split(','));

                    foreach (string item in list)
                    {
                        Console.WriteLine(item);
                    }
                }
                else if (!string.IsNullOrEmpty(input))
                {
                    Console.WriteLine("Unknown Command " + input);
                }
            }
        }
    }
}
