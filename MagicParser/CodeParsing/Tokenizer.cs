using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicParser.CodeParsing
{
    public class Tokenizer
    {

        public string input { get; private set; } //текст для разбора
        public int pos { get; private set; } //позиция в инпуте
        public bool endIsReached
        {
            get
            {
                return pos >= input.Length;
            }
        } //возвращает true, когда позиция в инпуте поравнялась с концом строки

        //Конструктор с вводом начальных данных
        public Tokenizer(string input)
        {
            this.input = input;
            pos = 0;
        }

        //получить следующий символ, сдвинуть позицию токенайзера на следующий символ (непрочитанный)
        public char GetSymbol()
        {
            if (!endIsReached)
            {
                char c = input[pos];
                pos++;
                return c;
            }
            return '~';

        }

        //Возвращает следующий символ, но не меняет позицию Токенайзера.
        public char ForseeSymbol()
        {
            int p = pos;
            char symbol = GetSymbol();
            pos = p;
            return symbol;
        }

        //Основной метод - возвращает токен или последовательность символов до ближайшего токена или пробела (пробелы в начале подстроки пропускает).
        //Сдвигает позицию токенайзера на следующий за токеном символ (непрочитанный).
        public string GetToken()
        {
            string token = "";
            //Выполняем функцию до тех пор, пока не пройдём весь файл или не получим токен
            while (!endIsReached)
            {
                char symbol = GetSymbol();

                //если пробел...
                if (Char.IsWhiteSpace(symbol))
                {
                    //...и непробельных символов ещё не было - читаем следующий символ
                    if (token == "") continue;
                    //...и непробельные символы уже записаны в токен - возвращаем токен.
                    //Приходится сбивать позицию токенайзера на пробельный символ, потому что в других методах из-за этого пришлось делать ещё больше костылей.
                    else { pos--; return token; }
                }

                //если не пробел - смотрим на символ
                switch (symbol)
                {
                    case '|':
                    case '&':
                    case '=':

                    case '+':
                    case '-':
                    case '*':
                    case '/':

                    case '(':
                    case ')':

                    case ',':
                    case '$':

                    case '\'':
                    case '"':
                        //Если токен ещё пуст - этот символ и есть токен. Возвращаем его;
                        if (token == "") return symbol.ToString();
                        //Если токен уже содержит символы, то рассматриваемый символ - следующий токен. Откатываем позицию, чтобы можно было в дальнейшем получить этот символ как токен.
                        else { pos--; return token; }

                    case '!':
                    case '<':
                    case '>':
                        //Эти символы могут быть отдельными токенами, а могут составлять один токен с символом '='. Поэтому,
                        //если токен ещё пуст - проверяем следующий символ.
                        if (token == "")
                        {
                            char nextSymbol = ForseeSymbol();
                            //Если следующий символ - '=', возвращаем токен из двух символов и двигаем позицию вперёд.
                            if (nextSymbol == '=') { pos++; return symbol.ToString() + nextSymbol; }
                            //Иначе возвращаем токен.
                            else return symbol.ToString();
                        }
                        //иначе токен уже содержит символы, и рассматриваемый символ - следующий токен. Откатываем позицию, чтобы можно было в дальнейшем получить этот символ как токен.
                        else { pos--; return token; }
                }
                //во всех остальных случаях прибавляем к слову символ и начинаем цикл по новой
                token += symbol;
            }
            //Если закончились символы во входных данных, возвращаем как токен то что есть.
            return token;
        }

        //Возвращает все символы с текущего положения до конца следующего токена.
        //Сдвигает позицию токенайзера на следующий за токеном символ (непрочитанный).
        public string GetTokenWithSpaces()
        {
            string token = "";
            int startPos = pos;
            GetToken();
            if (!endIsReached) token = input.Substring(startPos, pos - startPos);
            return token;
        }

        //Возвращает последовательность чаров с текущей позиции до ближайшего не вайтспейса не включительно.
        //Позиция токенайзера будет на первом не вайтспейсе.
        public string GetWhiteSpaces()
        {
            string output = "";
            while(!endIsReached)
            {
                char c = GetSymbol();
                if (Char.IsWhiteSpace(c)) output += c;
                else { pos--; break; }
            }
            return output;
        }

        //Возвращает следующий токен, но не меняет позицию Токенайзера.
        public string ForseeToken()
        {
            int p = pos;
            string token = GetToken();
            pos = p;
            return token;
        }

        //возвращает все символы input, пока не встретит c. Позиция токенайзера будет на символе c. Если include = true, в строке также будет сам символ c.
        public string GetUntil(char c, bool include = false)
        {
            string output = "";
            while (!endIsReached)
            {
                char symbol = GetSymbol();
                //когда находим соответствие
                if (symbol == c)
                {
                    //если символ нужно включать в выход - включаем
                    if (include)
                    {
                        output += c;
                        return output;
                    }
                    else //иначе не включаем и откатываем позицию
                    { pos--; return output; }
                }
                //иначе добавляем текущий символ в выход
                output += symbol;
            }
            return output;
        }

        public void SetPos(int position)
        {
            pos = position;
        }
    }
}
