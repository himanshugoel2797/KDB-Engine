using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Meka.Parser
{
    static class Helpers
    {
        public static string Remove(this string s, string toRemove)
        {
            return s.Replace(toRemove, string.Empty);
        }

        public static string RemoveRange(this string s, char start, char end)
        {
            if (s.Contains(start) && s.Contains(end))
            {
                return s.Remove(s.IndexOf(start), s.IndexOf(end) - s.IndexOf(start) + 1);
            }
            else return s;
        }

        public static string SubstringRange(this string s, char start, char end)
        {
            if (s.Contains(start) && s.Contains(end))
            {
                return s.Substring(s.IndexOf(start), s.IndexOf(end) - s.IndexOf(start) + 1);
            }
            else return "";
        }

        public static bool IsAlphanumeric(this string s)
        {
            return s.All(Char.IsLetterOrDigit);
        }
    }
}
