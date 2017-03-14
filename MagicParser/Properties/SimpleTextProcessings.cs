using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicParser.Properties
{
    public static class SimpleTextProcessings
    {
        public static int FindFirstSymbol(ref string str, char c, bool removeBefore = false) //ищет первое вхождение символа в строке, а если не находит, возвращает длину строки; если removeBefore = true, удаляет часть строки до этого символа.
        {
            int output;
            if (str.IndexOf(c) != -1)
            {
                output = str.IndexOf(c);
            }
            else
            {
                output = str.Count();
            }
            if (removeBefore)
            {
                str = str.Remove(0, output);
            }
            return output;
        }

        public static string CutBefore(ref string str, char c, bool removeBefore = true)
        {
            return str.Substring(0, FindFirstSymbol(ref str, c, true));
        }

        public static string CutBeforeWith(ref string str, string subStr) //ищет первое вхождение подстроки в строке, а если не находит, возвращает всю строку; удаляет последовательность от начала до подстроки включительно;
        {
            int index;
            string output;
            if (str.IndexOf(subStr) != -1)
            {
                index = str.IndexOf(subStr);
                output = str.Substring(0, index);
                str = str.Remove(0, index + subStr.Length);
            }
            else
            {
                output = str;
                str = "";
            }
            return output;
        }
    }
}
