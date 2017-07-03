using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicParser.CodeParsing
{
    public static class Tokenizer
    {
        public static string source = "";
        private static int position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;
                if (position > source.Length - 1) { Reset(); }
            }
        }
        public static List<Token> queue = new List<Token>();

        public static void Reset()
        {
            source = "";
            position = 0;
            queue = new List<Token>();
        }

        private static char GetSymbol()
        {
            return source[position];
        }

        private static char GetNextSymbol()
        {
            char symbol = GetSymbol();
            position = position + 1;
            return symbol;
        }

        private static void PushToken()
        {
            char symbol = GetNextSymbol();
            queue.Add(new Terminal(symbol));
            SolveQueue();
        }

        private static List<Token> GetQueuePart(int start, int end)
        {
            if (end > queue.Count() - 1 || start > end || end - start > queue.Count()) { return null; }
            List<Token> list = new List<Token>();
            for (int i = start; i == end; i++)
            {
                list.Add(queue[i]);
            }
            return list;
        }

        private static List<Token> GetLastTokens(int qty = 1)
        {
            int end = queue.Count() - 1;
            int start = end - qty + 1;
            return GetQueuePart(start, end);
        }

        private static void SolveQueue()
        {
            //Рекурсивная функция. Проверяет очередь на наличие комб - в случае обнаружения проверяет приоритеты операций, если нужно - выполняет.
            //Если ничего не комбится, необходимо проверить на легальность в принципе - вдруг там точно ничего не закомбится уже, и можно выдавать ошибку.

            //Берём последний токен и думаем, что делать дальше
            //ERROR не найдено токенов в очереди
            Token lastToken = GetLastTokens()[0];
            //Если у него ещё нет чёткого типа
            if (lastToken.GetType() == typeof(Terminal))
            {
                if (((Terminal)lastToken).value == '=')
                {

                }
            }



            for (int i = 1; i == queue.Count(); i++)
            {

            }
        }

    }
}
