using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MagicParser.Database;

namespace MagicParser
{
    //Являет собой класс оператора. Наследники - классы конкретных операторов.
    public abstract class Operator
    {
        //Символ экранирования
        public static string escapeCharacter = @"\";
        //Текстовое представление оператора в коде.
        public string Word { get; set; }
        //Приоритет выполнения оператора. 0 - не задан, 1 - для самых "внешних".
        public int priority;

        //Функция, которая выполняет оператор.
        public abstract ParseCodeResult Execute(Entry entry, string input, int operatorIndex);

        //Список операторов.
        public static List<Operator> list = new List<Operator>();

        //Последний номер приоритета
        public static int LastPriority
        {
            get
            {
                int max = 0;
                foreach (Operator o in list)
                {
                    max = Math.Max(max, o.priority);
                }
                return max;
            }
        }

        //Создаёт все синглтоны всех операторов
        public static void Init()
        {
            OperatorOpenGroup.GetInstance();
            OperatorCloseGroup.GetInstance();
            OperatorAnd.GetInstance();
            OperatorOr.GetInstance();
            OperatorEqual.GetInstance();
            OperatorNotEqual.GetInstance();
            OperatorGreater.GetInstance();
            OperatorGreaterOrEqual.GetInstance();
            OperatorLess.GetInstance();
            OperatorLessOrEqual.GetInstance();
            OperatorContains.GetInstance();
            OperatorDoesNotContain.GetInstance();
            OperatorAdd.GetInstance();
            OperatorSubstract.GetInstance();
            OperatorMultiply.GetInstance();
            OperatorDivide.GetInstance();
            OperatorIf.GetInstance();
            OperatorVar.GetInstance();
            OperatorQuote.GetInstance();
            OperatorNumber.GetInstance();
        }

        //Делит инпут на две части - до оператора и после, считает их значения и возвращает их, либо ошибку.
        protected ParseCodeResult[] Splice(Entry entry, string input, int operatorIndex)
        {
            //Отделяем левую и правую части от оператора и проверяем их на ошибки
            string leftString = input.Substring(0, operatorIndex).TrimEnd();
            ParseCodeResult leftResult = CodeParser.Operate(entry, leftString);
            if (leftResult.type == 1) { return new ParseCodeResult[] { leftResult }; }

            string rightString = input.Substring(operatorIndex + Word.Length).TrimStart();
            ParseCodeResult rightResult = CodeParser.Operate(entry, rightString);
            if (rightResult.type == 1) { return new ParseCodeResult[] { rightResult }; }

            return new ParseCodeResult[] { leftResult, rightResult };
        }
    }


    public class OperatorOpenGroup : Operator
    {
        #region Singleton
        private static OperatorOpenGroup instance;
        private OperatorOpenGroup(string Word) { this.Word = Word; }
        public static OperatorOpenGroup GetInstance(string Word = "(")
        {
            if (instance == null)
            {
                instance = new OperatorOpenGroup(Word.ToLower());
                list.Add(instance);
            }
            return instance;
        }
        #endregion

        public override ParseCodeResult Execute(Entry entry, string input, int operatorIndex)
        {
            return new ParseCodeResult(false);
        }
    }

    public class OperatorCloseGroup : Operator
    {
        #region Singleton
        private static OperatorCloseGroup instance;
        private OperatorCloseGroup(string Word) { this.Word = Word; }
        public static OperatorCloseGroup GetInstance(string Word = ")")
        {
            if (instance == null)
            {
                instance = new OperatorCloseGroup(Word.ToLower());
                list.Add(instance);
            }
            return instance;
        }
        #endregion

        public override ParseCodeResult Execute(Entry entry, string input, int operatorIndex)
        {
            return new ParseCodeResult(false);
        }
    }

    public class OperatorAnd : Operator
    {
        #region Singleton
        private static OperatorAnd instance;
        private OperatorAnd(string Word) { this.Word = Word; priority = 1; }
        public static OperatorAnd GetInstance(string Word = "&&")
        {
            if (instance == null)
            {
                instance = new OperatorAnd(Word.ToLower());
                list.Add(instance);
            }
            return instance;
        }
        #endregion

        public override ParseCodeResult Execute(Entry entry, string input, int operatorIndex)
        {
            ParseCodeResult[] results = Splice(entry, input, operatorIndex);

            //Если ошибка - сразу возаращаем ошибку
            if (results[0].type == 1) { return results[0]; }

            ParseCodeResult leftResult = results[0];
            ParseCodeResult rightResult = results[1];

            //Если какой-то из операторов не является булевым оператором - возвращаем ошибку
            if (leftResult.type != 2 || rightResult.type != 2)
            {
                return new ParseCodeResult(new Error("Ошибка в логическом выражении:\n" + input));
            }
            
            //Сама функция
            if (leftResult.boo && rightResult.boo)
            {
                return new ParseCodeResult(true);
            }

            return new ParseCodeResult(false);
        }
    }

    public class OperatorOr : Operator
    {
        #region Singleton
        private static OperatorOr instance;
        private OperatorOr(string Word) { this.Word = Word; priority = 2; }
        public static OperatorOr GetInstance(string Word = "||")
        {
            if (instance == null)
            {
                instance = new OperatorOr(Word.ToLower());
                list.Add(instance);
            }
            return instance;
        }
        #endregion

        public override ParseCodeResult Execute(Entry entry, string input, int operatorIndex)
        {
            ParseCodeResult[] results = Splice(entry, input, operatorIndex);

            //Если ошибка - сразу возаращаем ошибку
            if (results[0].type == 1) { return results[0]; }

            ParseCodeResult leftResult = results[0];
            ParseCodeResult rightResult = results[1];

            //Если какой-то из операторов не является булевым оператором - возвращаем ошибку
            if (leftResult.type != 2 || rightResult.type != 2)
            {
                return new ParseCodeResult(new Error("Ошибка в логическом выражении:\n" + input));
            }

            //Сама функция
            if (leftResult.boo || rightResult.boo)
            {
                return new ParseCodeResult(true);
            }

            return new ParseCodeResult(false);
        }
    }

    public class OperatorEqual : Operator
    {
        #region Singleton
        private static OperatorEqual instance;
        private OperatorEqual(string Word) { this.Word = Word; priority = 3; }
        public static OperatorEqual GetInstance(string Word = "==")
        {
            if (instance == null)
            {
                instance = new OperatorEqual(Word.ToLower());
                list.Add(instance);
            }
            return instance;
        }
        #endregion

        public override ParseCodeResult Execute(Entry entry, string input, int operatorIndex)
        {
            ParseCodeResult[] results = Splice(entry, input, operatorIndex);

            //Если ошибка - сразу возаращаем ошибку
            if (results[0].type == 1) { return results[0]; }

            ParseCodeResult leftResult = results[0];
            ParseCodeResult rightResult = results[1];

            //Определяем, что именно мы сравниваем, и сравниваем
            if (leftResult.type == 2 && rightResult.type == 2)
            {
                if ((leftResult.boo && rightResult.boo) || (!leftResult.boo && !leftResult.boo)) return new ParseCodeResult(true);
            }
            else if (leftResult.type == 3 && rightResult.type == 3)
            {
                if (leftResult.number == rightResult.number) return new ParseCodeResult(true);
            }
            else if(leftResult.type == 4 && rightResult.type == 4)
            {
                if (leftResult.str.ToLower() == rightResult.str.ToLower()) return new ParseCodeResult(true);
            }
            else if (leftResult.type != rightResult.type)
            {
                return new ParseCodeResult(new Error("Ты сравниваешь несравнимое."));
            }
            return new ParseCodeResult(false);
        }
    }

    public class OperatorNotEqual : Operator
    {
        #region Singleton
        private static OperatorNotEqual instance;
        private OperatorNotEqual(string Word) { this.Word = Word; priority = 3; }
        public static OperatorNotEqual GetInstance(string Word = "!=")
        {
            if (instance == null)
            {
                instance = new OperatorNotEqual(Word.ToLower());
                list.Add(instance);
            }
            return instance;
        }
        #endregion

        public override ParseCodeResult Execute(Entry entry, string input, int operatorIndex)
        {
            ParseCodeResult[] results = Splice(entry, input, operatorIndex);

            //Если ошибка - сразу возаращаем ошибку
            if (results[0].type == 1) { return results[0]; }

            ParseCodeResult leftResult = results[0];
            ParseCodeResult rightResult = results[1];

            //Определяем, что именно мы сравниваем, и сравниваем
            if (leftResult.type == 2 && rightResult.type == 2)
            {
                if ((leftResult.boo && !rightResult.boo) || (!leftResult.boo && leftResult.boo)) return new ParseCodeResult(true);
            }
            else if (leftResult.type == 3 && rightResult.type == 3)
            {
                if (leftResult.number != rightResult.number) return new ParseCodeResult(true);
            }
            else if (leftResult.type == 4 && rightResult.type == 4)
            {
                if (leftResult.str.ToLower() != rightResult.str.ToLower()) return new ParseCodeResult(true);
            }
            else if (leftResult.type != rightResult.type)
            {
                return new ParseCodeResult(new Error("Ты сравниваешь несравнимое."));
            }
            return new ParseCodeResult(false);
        }
    }
    
    public class OperatorGreater : Operator
    {
        #region Singleton
        private static OperatorGreater instance;
        private OperatorGreater(string Word) { this.Word = Word; priority = 4; }
        public static OperatorGreater GetInstance(string Word = ">")
        {
            if (instance == null)
            {
                instance = new OperatorGreater(Word.ToLower());
                list.Add(instance);
            }
            return instance;
        }
        #endregion

        public override ParseCodeResult Execute(Entry entry, string input, int operatorIndex)
        {
            ParseCodeResult[] results = Splice(entry, input, operatorIndex);

            //Если ошибка - сразу возаращаем ошибку
            if (results[0].type == 1) { return results[0]; }

            ParseCodeResult leftResult = results[0];
            ParseCodeResult rightResult = results[1];

            //Если что-то из сравниваемого - строка, выдаём ошибку - строки нельзя сравнивать.
            if (leftResult.type == 4 || rightResult.type == 4)
            {
                return new ParseCodeResult(new Error("Что больше - \"Вобла\" или \"Экзистенциализм\"? Строки нельзя сравнивать!\n" + input));
            }

            //Если что-то из сравниваемого - булево, выдаём ошибку - нельзя сравнивать.
            if (leftResult.type == 2 || rightResult.type == 2)
            {
                return new ParseCodeResult(new Error("Истины больше, чем лжи, или лжи больше, чем истины... В общем, нельзя сравнивать переменные булева типа.\n" + input));
            }

            //Т.о., сравниваем только числа:
            if (leftResult.type == 3 && rightResult.type == 3)
            {
                if (leftResult.number > rightResult.number) return new ParseCodeResult(true);
            }
            else if (leftResult.type != rightResult.type)
            {
                return new ParseCodeResult(new Error("Ты сравниваешь несравнимое."));
            }
            return new ParseCodeResult(false);
        }
    }

    public class OperatorGreaterOrEqual : Operator
    {
        #region Singleton
        private static OperatorGreaterOrEqual instance;
        private OperatorGreaterOrEqual(string Word) { this.Word = Word; priority = 4; }
        public static OperatorGreaterOrEqual GetInstance(string Word = ">=")
        {
            if (instance == null)
            {
                instance = new OperatorGreaterOrEqual(Word.ToLower());
                list.Add(instance);
            }
            return instance;
        }
        #endregion

        public override ParseCodeResult Execute(Entry entry, string input, int operatorIndex)
        {
            ParseCodeResult[] results = Splice(entry, input, operatorIndex);

            //Если ошибка - сразу возаращаем ошибку
            if (results[0].type == 1) { return results[0]; }

            ParseCodeResult leftResult = results[0];
            ParseCodeResult rightResult = results[1];

            //Если что-то из сравниваемого - строка, выдаём ошибку - строки нельзя сравнивать.
            if (leftResult.type == 4 || rightResult.type == 4)
            {
                return new ParseCodeResult(new Error("Что больше - \"Вобла\" или \"Экзистенциализм\"? Строки нельзя сравнивать!\n" + input));
            }

            //Если что-то из сравниваемого - булево, выдаём ошибку - нельзя сравнивать.
            if (leftResult.type == 2 || rightResult.type == 2)
            {
                return new ParseCodeResult(new Error("Истины больше, чем лжи, или лжи больше, чем истины... В общем, нельзя сравнивать переменные булева типа.\n" + input));
            }

            //Т.о., сравниваем только числа:
            if (leftResult.type == 3 && rightResult.type == 3)
            {
                if (leftResult.number >= rightResult.number) return new ParseCodeResult(true);
            }
            else if (leftResult.type != rightResult.type)
            {
                return new ParseCodeResult(new Error("Ты сравниваешь несравнимое."));
            }
            return new ParseCodeResult(false);
        }
    }

    public class OperatorLess : Operator
    {
        #region Singleton
        private static OperatorLess instance;
        private OperatorLess(string Word) { this.Word = Word; priority = 4; }
        public static OperatorLess GetInstance(string Word = "<")
        {
            if (instance == null)
            {
                instance = new OperatorLess(Word.ToLower());
                list.Add(instance);
            }
            return instance;
        }
        #endregion

        public override ParseCodeResult Execute(Entry entry, string input, int operatorIndex)
        {
            ParseCodeResult[] results = Splice(entry, input, operatorIndex);

            //Если ошибка - сразу возаращаем ошибку
            if (results[0].type == 1) { return results[0]; }

            ParseCodeResult leftResult = results[0];
            ParseCodeResult rightResult = results[1];

            //Если что-то из сравниваемого - строка, выдаём ошибку - строки нельзя сравнивать.
            if (leftResult.type == 4 || rightResult.type == 4)
            {
                return new ParseCodeResult(new Error("Что больше - \"Вобла\" или \"Экзистенциализм\"? Строки нельзя сравнивать!\n" + input));
            }

            //Если что-то из сравниваемого - булево, выдаём ошибку - нельзя сравнивать.
            if (leftResult.type == 2 || rightResult.type == 2)
            {
                return new ParseCodeResult(new Error("Истины больше, чем лжи, или лжи больше, чем истины... В общем, нельзя сравнивать переменные булева типа.\n" + input));
            }

            //Т.о., сравниваем только числа:
            if (leftResult.type == 3 && rightResult.type == 3)
            {
                if (leftResult.number < rightResult.number) return new ParseCodeResult(true);
            }
            else if (leftResult.type != rightResult.type)
            {
                return new ParseCodeResult(new Error("Ты сравниваешь несравнимое."));
            }
            return new ParseCodeResult(false);
        }
    }

    public class OperatorLessOrEqual : Operator
    {
        #region Singleton
        private static OperatorLessOrEqual instance;
        private OperatorLessOrEqual(string Word) { this.Word = Word; priority = 4; }
        public static OperatorLessOrEqual GetInstance(string Word = "<=")
        {
            if (instance == null)
            {
                instance = new OperatorLessOrEqual(Word.ToLower());
                list.Add(instance);
            }
            return instance;
        }
        #endregion

        public override ParseCodeResult Execute(Entry entry, string input, int operatorIndex)
        {
            ParseCodeResult[] results = Splice(entry, input, operatorIndex);

            //Если ошибка - сразу возаращаем ошибку
            if (results[0].type == 1) { return results[0]; }

            ParseCodeResult leftResult = results[0];
            ParseCodeResult rightResult = results[1];

            //Если что-то из сравниваемого - строка, выдаём ошибку - строки нельзя сравнивать.
            if (leftResult.type == 4 || rightResult.type == 4)
            {
                return new ParseCodeResult(new Error("Что больше - \"Вобла\" или \"Экзистенциализм\"? Строки нельзя сравнивать!\n" + input));
            }

            //Если что-то из сравниваемого - булево, выдаём ошибку - нельзя сравнивать.
            if (leftResult.type == 2 || rightResult.type == 2)
            {
                return new ParseCodeResult(new Error("Истины больше, чем лжи, или лжи больше, чем истины... В общем, нельзя сравнивать переменные булева типа.\n" + input));
            }

            //Т.о., сравниваем только числа:
            if (leftResult.type == 3 && rightResult.type == 3)
            {
                if (leftResult.number <= rightResult.number) return new ParseCodeResult(true);
            }
            else if (leftResult.type != rightResult.type)
            {
                return new ParseCodeResult(new Error("Ты сравниваешь несравнимое."));
            }
            return new ParseCodeResult(false);
        }
    }

    public class OperatorContains : Operator
    {
        #region Singleton
        private static OperatorContains instance;
        private OperatorContains(string Word) { this.Word = Word; priority = 5; }
        public static OperatorContains GetInstance(string Word = "$has")
        {
            if (instance == null)
            {
                instance = new OperatorContains(Word.ToLower());
                list.Add(instance);
            }
            return instance;
        }
        #endregion

        public override ParseCodeResult Execute(Entry entry, string input, int operatorIndex)
        {
            ParseCodeResult[] results = Splice(entry, input, operatorIndex);

            //Если ошибка - сразу возаращаем ошибку
            if (results[0].type == 1) { return results[0]; }

            ParseCodeResult leftResult = results[0];
            ParseCodeResult rightResult = results[1];

            //Если что-то из сравниваемого - не строка, выдаём ошибку
            if (leftResult.type != 4 || rightResult.type != 4)
            {
                return new ParseCodeResult(new Error("Где-то тут неправильно использован оператор содержания:\n" + input));
            }

            //Собственно, само условие оператора
            if (leftResult.str.ToLower().Contains(rightResult.str.ToLower()))
            {
                return new ParseCodeResult(true);
            }
            return new ParseCodeResult(false);
        }
    }
    
    public class OperatorDoesNotContain : Operator
    {
        #region Singleton
        private static OperatorDoesNotContain instance;
        private OperatorDoesNotContain(string Word) { this.Word = Word; priority = 5; }
        public static OperatorDoesNotContain GetInstance(string Word = "$hasnot")
        {
            if (instance == null)
            {
                instance = new OperatorDoesNotContain(Word.ToLower());
                list.Add(instance);
            }
            return instance;
        }
        #endregion

        public override ParseCodeResult Execute(Entry entry, string input, int operatorIndex)
        {
            ParseCodeResult[] results = Splice(entry, input, operatorIndex);

            //Если ошибка - сразу возаращаем ошибку
            if (results[0].type == 1) { return results[0]; }

            ParseCodeResult leftResult = results[0];
            ParseCodeResult rightResult = results[1];

            //Если что-то из сравниваемого - не строка, выдаём ошибку
            if (leftResult.type != 4 || rightResult.type != 4)
            {
                return new ParseCodeResult(new Error("Где-то тут неправильно использован оператор содержания:\n" + input));
            }

            //Собственно, само условие оператора
            if (!leftResult.str.ToLower().Contains(rightResult.str.ToLower()))
            {
                return new ParseCodeResult(true);
            }
            return new ParseCodeResult(false);
        }
    }

    public class OperatorAdd : Operator
    {
        #region Singleton
        private static OperatorAdd instance;
        private OperatorAdd(string Word) { this.Word = Word; priority = 6; }
        public static OperatorAdd GetInstance(string Word = "+")
        {
            if (instance == null)
            {
                instance = new OperatorAdd(Word.ToLower());
                list.Add(instance);
            }
            return instance;
        }
        #endregion

        public override ParseCodeResult Execute(Entry entry, string input, int operatorIndex)
        {
            ParseCodeResult[] results = Splice(entry, input, operatorIndex);

            //Если ошибка - сразу возаращаем ошибку
            if (results[0].type == 1) { return results[0]; }

            ParseCodeResult leftResult = results[0];
            ParseCodeResult rightResult = results[1];

            //Если оба слагаемых - числа, возвращаем сумму
            if (leftResult.type == 3 && rightResult.type == 3)
            {
                return new ParseCodeResult(leftResult.number + rightResult.number);
            }

            //Если оба слагаемых - строки, возвращаем сумму строк
            if (leftResult.type == 4 && rightResult.type == 4)
            {
                return new ParseCodeResult(leftResult.str + rightResult.str);
            }

            return new ParseCodeResult(new Error("Нет, складывать пчёл с тумбочками не получился.\n" + input));
        }
    }

    public class OperatorSubstract : Operator
    {
        #region Singleton
        private static OperatorSubstract instance;
        private OperatorSubstract(string Word) { this.Word = Word; priority = 6; }
        public static OperatorSubstract GetInstance(string Word = "-")
        {
            if (instance == null)
            {
                instance = new OperatorSubstract(Word.ToLower());
                list.Add(instance);
            }
            return instance;
        }
        #endregion

        public override ParseCodeResult Execute(Entry entry, string input, int operatorIndex)
        {
            ParseCodeResult[] results = Splice(entry, input, operatorIndex);

            //Если ошибка - сразу возаращаем ошибку
            if (results[0].type == 1) { return results[0]; }

            ParseCodeResult leftResult = results[0];
            ParseCodeResult rightResult = results[1];

            //Если оба слагаемых - числа, возвращаем разность
            if (leftResult.type == 3 && rightResult.type == 3)
            {
                return new ParseCodeResult(leftResult.number - rightResult.number);
            }

            return new ParseCodeResult(new Error("Нет, вычитать из пчёл тумбочки не выйдет.\n" + input));
        }
    }

    public class OperatorMultiply : Operator
    {
        #region Singleton
        private static OperatorMultiply instance;
        private OperatorMultiply(string Word) { this.Word = Word; priority = 7; }
        public static OperatorMultiply GetInstance(string Word = "*")
        {
            if (instance == null)
            {
                instance = new OperatorMultiply(Word.ToLower());
                list.Add(instance);
            }
            return instance;
        }
        #endregion

        public override ParseCodeResult Execute(Entry entry, string input, int operatorIndex)
        {
            ParseCodeResult[] results = Splice(entry, input, operatorIndex);

            //Если ошибка - сразу возаращаем ошибку
            if (results[0].type == 1) { return results[0]; }

            ParseCodeResult leftResult = results[0];
            ParseCodeResult rightResult = results[1];

            //Если оба слагаемых - числа, возвращаем произведение
            if (leftResult.type == 3 && rightResult.type == 3)
            {
                return new ParseCodeResult(leftResult.number * rightResult.number);
            }

            return new ParseCodeResult(new Error("Нет, умножать пчёл на тумбочки нельзя.\n" + input));
        }
    }

    public class OperatorDivide : Operator
    {
        #region Singleton
        private static OperatorDivide instance;
        private OperatorDivide(string Word) { this.Word = Word; priority = 7; }
        public static OperatorDivide GetInstance(string Word = "/")
        {
            if (instance == null)
            {
                instance = new OperatorDivide(Word.ToLower());
                list.Add(instance);
            }
            return instance;
        }
        #endregion

        public override ParseCodeResult Execute(Entry entry, string input, int operatorIndex)
        {
            ParseCodeResult[] results = Splice(entry, input, operatorIndex);

            //Если ошибка - сразу возаращаем ошибку
            if (results[0].type == 1) { return results[0]; }

            ParseCodeResult leftResult = results[0];
            ParseCodeResult rightResult = results[1];

            //Если оба слагаемых - числа, возвращаем частное
            if (leftResult.type == 3 && rightResult.type == 3)
            {
                return new ParseCodeResult(leftResult.number / rightResult.number);
            }

            return new ParseCodeResult(new Error("Нет, делить пчёл на тумбочки запрещено.\n" + input));
        }
    }

    public class OperatorIf : Operator
    {
        #region Singleton
        private static OperatorIf instance;
        private OperatorIf(string Word) { this.Word = Word; priority = 8; }
        public static OperatorIf GetInstance(string Word = "$if")
        {
            if (instance == null)
            {
                instance = new OperatorIf(Word.ToLower());
                list.Add(instance);
            }
            return instance;
        }
        #endregion

        public override ParseCodeResult Execute(Entry entry, string input, int operatorIndex)
        {
            //ДОПИШИ ОШИБКУ
            return new ParseCodeResult(new Error(""));
        }
    }

    public class OperatorVar : Operator
    {
        #region Singleton
        private static OperatorVar instance;
        private OperatorVar(string Word) { this.Word = Word; priority = 9; }
        public static OperatorVar GetInstance(string Word = "$")
        {
            if (instance == null)
            {
                instance = new OperatorVar(Word.ToLower());
                list.Add(instance);
            }
            return instance;
        }
        #endregion

        public override ParseCodeResult Execute(Entry entry, string input, int operatorIndex)
        {
            //Берём одно слово начиная с $, дальше в зависимости от этого слова что-нибудь возвращаем.
            int length = input.IndexOf(' ');
            if (length == -1) { length = input.Length - 1; }
            if (Word.Length == input.Length) { return new ParseCodeResult(new Error("Синтаксическая ошибка где-то здесь:\n" + input)); }
            string oper = input.Substring(operatorIndex+Word.Length, length).ToLower();

            switch (oper)
            {
                case "true":
                    return new ParseCodeResult(true);
                case "false":
                    return new ParseCodeResult(false);

                case "artist":
                    return new ParseCodeResult(entry.artist);
                case "border":
                    return new ParseCodeResult(entry.border);
                case "buyqty":
                    return new ParseCodeResult(entry.buyQty);
                case "color":
                    return new ParseCodeResult(entry.color);
                case "copyright":
                    return new ParseCodeResult(entry.copyright);
                case "cost":
                    return new ParseCodeResult(entry.cost);
                case "language":
                    return new ParseCodeResult(entry.language);
                case "legality":
                    return new ParseCodeResult(entry.legality);
                case "name":
                    return new ParseCodeResult(entry.name);
                case "nameoracle":
                    return new ParseCodeResult(entry.nameOracle);
                case "notes":
                    return new ParseCodeResult(entry.notes);
                case "number":
                    return new ParseCodeResult(entry.number);
                case "pt":
                    return new ParseCodeResult(entry.pt);
                case "proxies":
                    return new ParseCodeResult(entry.proxies);
                case "rarity":
                    return new ParseCodeResult(entry.rarity);
                case "rating":
                    return new ParseCodeResult(entry.rating);
                case "sellqty":
                    return new ParseCodeResult(entry.sellQty);
                case "set":
                    return new ParseCodeResult(entry.set);
                case "text":
                    return new ParseCodeResult(entry.text);
                case "textoracle":
                    return new ParseCodeResult(entry.textOracle);
                case "type":
                    return new ParseCodeResult(entry.type);
                case "typeoracle":
                    return new ParseCodeResult(entry.typeOracle);
                case "used":
                    return new ParseCodeResult(entry.used);
                case "version":
                    return new ParseCodeResult(entry.version);

                case "qty":
                    return new ParseCodeResult(entry.qty);
                case "foil":
                    return new ParseCodeResult(entry.foil);
                case "dollarate":
                    return new ParseCodeResult(entry.dollarRate);
                case "discount":
                    return new ParseCodeResult(entry.discount);
                case "comment":
                    return new ParseCodeResult(entry.comment);
                case "grade":
                    return new ParseCodeResult(entry.grade);
                case "price":
                    return new ParseCodeResult(entry.price);
                case "priority":
                    return new ParseCodeResult(entry.priority);
            }

            return new ParseCodeResult(new Error("Внимание! Неопознанный оператор!\n" + input));
        }
    }

    public class OperatorQuote : Operator
    {
        #region Singleton
        private static OperatorQuote instance;
        private OperatorQuote(string Word) { this.Word = Word; priority = 10; }
        public static OperatorQuote GetInstance(string Word = "\"")
        {
            if (instance == null)
            {
                instance = new OperatorQuote(Word.ToLower());
                list.Add(instance);
            }
            return instance;
        }
        #endregion

        public override ParseCodeResult Execute(Entry entry, string input, int operatorIndex)
        {
            return CodeParser.CheckQuotes(entry, input);
        }
    }

    public class OperatorNumber : Operator
    {
        #region Singleton
        private static OperatorNumber instance;
        private OperatorNumber(string Word) { this.Word = Word; priority = 11; }
        public static OperatorNumber GetInstance(string Word = "")
        {
            if (instance == null)
            {
                instance = new OperatorNumber(Word.ToLower());
                list.Add(instance);
            }
            return instance;
        }
        #endregion

        public override ParseCodeResult Execute(Entry entry, string input, int operatorIndex)
        {
            try
            {
                return new ParseCodeResult(float.Parse(input));
            } catch
            {
                return new ParseCodeResult(new Error("Программа ничего не поняла. Текст пиши в кавычках, число - числом, операторы - своими символами.\n" + input));
            }
        }
    }

}
