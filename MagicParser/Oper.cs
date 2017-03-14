using MagicParser.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static MagicParser.Database;

namespace MagicParser
{
    public static class Oper
    {
        static string groupOpen_word = "(";
        static string groupClose_word = ")";

        static string or_word = "||";

        static string equalTo_word = "=";
        static string notEqualTo_word = "!=";

        static string greaterThan_word = ">";
        static string greaterThanOrEqualTo_word = ">=";
        static string lessThan_word = "<";
        static string lessThanOrEqualTo_word = "<=";

        static string contains_word = "has";
        static string doesNotContain_word = "has not";

        static string if_word = "$if";
        static string add_word = "$add";
        static string substract_word = "$substract";
        static string divide_word = "$divide";
        static string multiply_word = "$multiply";

        static string quote_word = "\"";

        public static string error = "___ERROR___";
        public static string filterRemove = "___remove___";
        public static string filterAccept = "___accept___";
        public static string noResult = "___no_result___";
        
        //Главный класс, который находит в строке приоритетный оператор и выполняет его, рекурсивно считая все остальные операторы.
        //Возвращает error в случае ошибки, filterRemove, если нужно удалить карту, filterAccept, если карту удалять не нужно (она соответствует заданному фильтру).
        public static string Operate (Entry entry, string input, List<int[]> ranges = null) //
        {
            input = input.TrimStart().TrimEnd();
            //Если в строке есть скобки - надо разобраться с ними:
            if (ranges == null && input.ToLower().Contains(groupOpen_word)) 
            {
                return DealWithBrackets(entry, input);
            }
            //если указаны диапазоны для поиска операторов, то для каждого диапазона ищем оператор
            else if (ranges != null)
            {
                foreach (int[] range in ranges)
                {
                    //???вообще мне походу нужно возвращать сразу после первого ранжа, что бы там ни было... надо проверить
                    return RouteInput(entry, input, range); //проверяем на наличие операторов ВНУТРИ РАНЖЕЙ в порядке приоритета
                }
                return error + "Странная ошибка. Откуда она?";
            }
            //проверяем на наличие операторов в порядке приоритета
            else
            {
                return RouteInput(entry, input);
            }

            return filterRemove;
        }

        private static string DealWithBrackets(Entry entry, string input)
        {
            //считываем скобки и делаем диапазоны, соответствующие самым внешним скобкам
            int openBracketQty = 0;
            List<int[]> ranges = new List<int[]>();
            int start = 0;
            for (int i = 0; i < input.Length - Math.Max(groupOpen_word.Length, groupClose_word.Length) + 1; i++)
            {
                if (input.Substring(i, groupOpen_word.Length) == groupOpen_word)
                {
                    openBracketQty++;
                    if (openBracketQty == 1)
                    {
                        start = i;
                    }
                    i += groupOpen_word.Length - 1;
                }
                else if (input.Substring(i, groupClose_word.Length) == groupClose_word)
                {
                    openBracketQty--;
                    if (openBracketQty == 0)
                    {
                        ranges.Add(new int[] { start, i });
                        start = 0;
                    }
                    //с количеством скобок что-то не так
                    else if (openBracketQty < 0)
                    {
                        return error + "Количество закрывающих скобок чутка зашкаливает.\n\"" + input + "\"";
                    }
                    i += groupOpen_word.Length - 1;
                }
            }
            //с количеством скобок что-то не так
            if (openBracketQty != 0) 
            {
                return error + "Нужно МЕНЬШЕ открывающих скобок.\n\"" + input + "\\";
            }
            //если есть лишь один диапазон, который покрывает всю строку => она целиком в одних скобках, надо их раскрыть
            if (ranges.Count() == 1 && ranges[0][0] == 0 && ranges[0][1] == input.Length - 1) 
            {
                input = input.Substring(1, input.Length - 2);
                return Operate(entry, input);
            }
            //в других случаях список диапазонов надо "перевернуть", чтобы найти те диапазоны, в которых можно и нужно искать операторы
            List<int[]> ranReversed = ReverseRange(ranges, input.Length);

            //Продолжаем искать операторы, теперь указаны диапазоны, где их следует искать
            return Operate(entry, input, ranReversed);
        }

        //На основе диапазонов, в которых нельзя искать оператор, делает диапазоны, в которых можно искать оператор.
        private static List<int[]> ReverseRange(List<int[]> ranges, int maxLength)
        {
            List<int[]> rangesReversed = new List<int[]>();

            //если диапазон начинается НЕ с начала строки, то добавляем один диапазон
            if (ranges[0][0] != 0) 
            {
                rangesReversed.Add(new int[] { 0, ranges[0][0] - 1 });
            }
            //добавляем все основные диапазоны
            for (int i = 0; i < ranges.Count() - 1; i++)
            {
                rangesReversed.Add(new int[] { ranges[i][1] + 1, ranges[i + 1][0] - 1 });
            }
            //добавляем в конец ранж
            rangesReversed.Add(new int[] { ranges[ranges.Count() - 1][1] + 1, maxLength });

            return rangesReversed;
        }

        //Ищет оператор в input и направляет в соответствующий метод для обработки оператора
        private static string RouteInput(Entry entry, string input, int[] range = null)
        {
            //Если не заданы диапазоны, то диапазон будет по всей длине строки
            if (range == null) { range = new int[] { 0, input.Length }; }

            //Во всех условиях дальше проверяется наличие в указанных диапазонах соответствующих операторов в порядке приоритета.
            //Затем мы получаем индекс (номер в строке) первого из них и отправляем на исполнение.

            // ||
            if (input.ToLower().IndexOf(or_word.ToLower(), range[0], range[1] - range[0]) != -1) 
            {
                int operatorIndex = input.ToLower().IndexOf(or_word.ToLower(), range[0], range[1] - range[0]);
                return Execute(entry, input, operatorIndex, or_word);
            }

            // = != 
            else if (
                input.ToLower().IndexOf(equalTo_word.ToLower(), range[0], range[1] - range[0]) != -1 ||
                input.ToLower().IndexOf(notEqualTo_word.ToLower(), range[0], range[1] - range[0]) != -1
                )
            {
                int operatorEqualIndex = input.ToLower().IndexOf(equalTo_word.ToLower(), range[0], range[1] - range[0]);
                int operatorNotEqualIndex = input.ToLower().IndexOf(notEqualTo_word.ToLower(), range[0], range[1] - range[0]);
                //Если нашёлся equal...
                if (operatorEqualIndex != -1)
                {
                    //если нашёлся equal и notEqual - исполняем тот, что первее в строке;
                    if (operatorNotEqualIndex != -1)
                    {
                        if (operatorEqualIndex < operatorNotEqualIndex)
                        {
                            return Execute(entry, input, operatorEqualIndex, equalTo_word);
                        }
                        else
                        {
                            return Execute(entry, input, operatorNotEqualIndex, notEqualTo_word);
                        }
                    }
                    //если нашёлся только equal - исполняем его
                    else
                    {
                        return Execute(entry, input, operatorEqualIndex, equalTo_word);
                    }
                }
                //если equal не нашёлся, то по-любому нашёлся notEqual. Иполняем его
                return Execute(entry, input, operatorNotEqualIndex, notEqualTo_word);
            }

            //< > <= >=
            else if (
                input.ToLower().IndexOf(greaterThan_word.ToLower(), range[0], range[1] - range[0]) != -1 ||
                input.ToLower().IndexOf(greaterThanOrEqualTo_word.ToLower(), range[0], range[1] - range[0]) != -1 ||
                input.ToLower().IndexOf(lessThan_word.ToLower(), range[0], range[1] - range[0]) != -1 ||
                input.ToLower().IndexOf(lessThanOrEqualTo_word.ToLower(), range[0], range[1] - range[0]) != -1
                )
            {
                int operatorGreaterThanIndex = input.ToLower().IndexOf(greaterThan_word.ToLower(), range[0], range[1] - range[0]);
                int operatorGreaterOrEqualToThanIndex = input.ToLower().IndexOf(greaterThanOrEqualTo_word.ToLower(), range[0], range[1] - range[0]);
                int operatorLessThanIndex = input.ToLower().IndexOf(lessThan_word.ToLower(), range[0], range[1] - range[0]);
                int operatorLessThanOrEqualToIndex = input.ToLower().IndexOf(lessThanOrEqualTo_word.ToLower(), range[0], range[1] - range[0]);

                //Запишем в список значения всех индексов - щас будем выбирать из них наименьший
                List<int> ints = new List<int>(); 
                //Для каждого оператора проверяется, есть ли он в строке, если есть - добавляем в список
                if (operatorGreaterThanIndex != -1) { ints.Add(operatorGreaterThanIndex); }
                if (operatorGreaterOrEqualToThanIndex != -1) { ints.Add(operatorGreaterOrEqualToThanIndex); }
                if (operatorLessThanIndex != -1) { ints.Add(operatorLessThanIndex); }
                if (operatorLessThanOrEqualToIndex != -1) { ints.Add(operatorLessThanOrEqualToIndex); }

                //Затем среди тех, что есть в строке, находим наименьший
                int min = input.Length;
                for (int i = 0; i < ints.Count() - 1; i++) {
                    min = Math.Min(ints[i], ints[i + 1]);
                }

                //наконец, выполняем тот, который первый в строке
                if (operatorGreaterThanIndex == min) { return Execute(entry, input, operatorGreaterThanIndex, greaterThan_word); }
                if (operatorGreaterOrEqualToThanIndex == min) { return Execute(entry, input, operatorGreaterOrEqualToThanIndex, greaterThan_word); }
                if (operatorLessThanIndex == min) { return Execute(entry, input, operatorLessThanIndex, greaterThan_word); }
                return Execute(entry, input, operatorLessThanOrEqualToIndex, greaterThan_word);
            }
            //has has not
            else if ( 
                input.ToLower().IndexOf(contains_word.ToLower(), range[0], range[1] - range[0]) != -1 ||
                input.ToLower().IndexOf(doesNotContain_word.ToLower(), range[0], range[1] - range[0]) != -1
                )
            {
                int operatorContainsIndex = input.ToLower().IndexOf(contains_word.ToLower(), range[0], range[1] - range[0]);
                int operatorDoesNotContainsIndex = input.ToLower().IndexOf(doesNotContain_word.ToLower(), range[0], range[1] - range[0]);
                //если нашёлся has...
                if (operatorContainsIndex != -1)
                {
                    //если нашёлся has и has not - исполняем тот, что первее в строке;
                    if (operatorDoesNotContainsIndex != -1)
                    {
                        if (operatorContainsIndex < operatorDoesNotContainsIndex)
                        {
                            return Execute(entry, input, operatorContainsIndex, contains_word);
                        }
                        else
                        {
                            return Execute(entry, input, operatorDoesNotContainsIndex, doesNotContain_word);
                        }
                    }
                    //Если нашёлся только has - исполняем его
                    else
                    {
                        return Execute(entry, input, operatorContainsIndex, contains_word);
                    }
                }
                //если has не нашёлся, то по-любому нашёлся has not - исполняем его
                return Execute(entry, input, operatorDoesNotContainsIndex, doesNotContain_word);
            }
            //если ничего не соответствует ни одному шаблону выше, то это не оператор, а число или строка - возвращаем как есть
            else
            {
                return (input);
            }
        }

        //Исполняет оператор
        private static string Execute(Entry entry, string input, int operatorIndex, string operatorWord)
        {
            //Отделяем левую и правую части от оператора и проверяем их на ошибки
            string leftString = input.Substring(0, operatorIndex).TrimEnd();
            string leftResult = Operate(entry, leftString);
            if (ReturnError(leftResult) != null) { return ReturnError(leftResult); }

            string rightString = input.Substring(operatorIndex + operatorWord.Length).TrimStart();
            string rightResult = Operate(entry, rightString);
            if (ReturnError(rightResult) != null) { return ReturnError(rightResult); }
            
            //В зависимости от поданного оператора, исполняем для каждого своё

            // ||
            if (operatorWord == or_word)
            {
                //если ни одного true, возвращаем false (0)
                if (leftResult != filterAccept && rightResult != filterAccept) 
                {
                    return filterRemove;
                }
                return filterAccept;
            }

            // =
            else if (operatorWord == equalTo_word)
            {
                if (leftResult.ToLower() == rightResult.ToLower())
                {
                    return filterAccept;
                }
                return filterRemove;
            }

            // !=
            else if (operatorWord.ToLower() == notEqualTo_word.ToLower())
            {
                if (leftResult != rightResult)
                {
                    return filterAccept;
                }
                return filterRemove;
            }

            //Операторы сравнения могут сравнивать только числа, поэтому пытаемся превратить данные в числа, в случае провала - возвращаем ошибку;
            // >
            else if (operatorWord == greaterThan_word)
            {
                try
                {
                    float leftResultConverted = float.Parse(leftResult);
                    float rightResultConverted = float.Parse(rightResult);
                    if (leftResultConverted > rightResultConverted)
                    {
                        return filterAccept;
                    }
                    return filterRemove;
                }
                catch
                {
                    return error + "Ты пытаешься сравнивать строки! Где-то тут:\n\"" + leftResult + " " + operatorWord + " " + rightResult + "\"";
                }
                
            }
            // >=
            else if (operatorWord == greaterThanOrEqualTo_word)
            {
                try
                {
                    float leftResultConverted = float.Parse(leftResult);
                    float rightResultConverted = float.Parse(rightResult);
                    if (leftResultConverted >= rightResultConverted) 
                    {
                        return filterAccept;
                    }
                    return filterRemove;
                }
                catch
                {
                    return error + "Ты пытаешься сравнивать строки! Где-то тут:\n\"" + leftResult + " " + operatorWord + " " + rightResult + "\"";
                }
            }
            // <
            else if (operatorWord == lessThan_word)
            {
                try
                {
                    float leftResultConverted = float.Parse(leftResult);
                    float rightResultConverted = float.Parse(rightResult);
                    if (leftResultConverted < rightResultConverted)
                    {
                        return filterAccept;
                    }
                    return filterRemove;
                }
                catch
                {
                    return error + "Ты пытаешься сравнивать строки! Где-то тут:\n\"" + leftResult + " " + operatorWord + " " + rightResult + "\"";
                }
            }
            // <=
            else if (operatorWord == lessThanOrEqualTo_word)
            {
                try
                {
                    float leftResultConverted = float.Parse(leftResult);
                    float rightResultConverted = float.Parse(rightResult);
                    if (leftResultConverted <= rightResultConverted)
                    {
                        return filterAccept;
                    }
                    return filterRemove;
                }
                catch
                {
                    return error + "Ты пытаешься сравнивать строки! Где-то тут:\n\"" + leftResult + " " + operatorWord + " " + rightResult + "\"";
                }
            }
            // has
            else if (operatorWord == contains_word)
            {
                if (leftResult.Contains(rightResult))
                {
                    return filterAccept;
                }
                return filterRemove;
            }
            // has not
            else if (operatorWord == doesNotContain_word)
            {
                if (!leftResult.Contains(rightResult))
                {
                    return filterAccept;
                }
                return filterRemove;
            }
            return error + "Странная ошибка, надо посмотреть в коде.";
        }

        //Если строка содержит код, указывающий на ошибку, возвращает текст ошибки, иначе возвращает null
        private static string ReturnError(string returnedString)
        {
            if (returnedString.Contains(error))
            {
                return returnedString.Remove(0, error.Length);
            }
            return null;
        }

        private static string EqualOperator(Entry entry, string input, int operatorIndex)
        {
            string leftString = input.Substring(0, operatorIndex).TrimEnd();
            string rightString = input.Substring(operatorIndex + equalTo_word.Length).TrimStart();

            string leftResult = Operate(entry, leftString);
            if (leftResult == "error")
            {
                return "error"; //проверяем на ошибки
            }
            string rightResult = Operate(entry, leftString);
            if (rightResult == "error")
            {
                return "error";
            }

            if (leftResult == rightResult) //если строки равны, возвращаем ок
            {
                return filterAccept;
            }
            return filterRemove;
        }

        /*
        public static bool Operate (Database db, string variable, string value, string oper)
        {
            List<Entry> filtered = new List<Entry>();
            List<string> values = new List<string>();
            while (value != null)
            {
                if (value.Contains("||"))
                {
                    values.Add(CodeParser.CutBeforeWith(ref value, "||").TrimStart().TrimEnd());
                } else
                {
                    values.Add(value);
                }
            }
            
            foreach (Entry entry in db.cardList)
            {
                foreach (string val in values)
                {
                    try
                    {
                        string executedVariable = entry.GetType().GetField(variable).GetValue(entry).ToString();
                        if ( //если = != has has not
                            (oper == "=" && executedVariable.ToLower() == value.ToLower()) ||
                            (oper == "!=" && executedVariable.ToLower() != value.ToLower()) ||
                            (oper == "has" && executedVariable.ToLower().Contains(value.ToLower())) ||
                            (oper == "has not" && !executedVariable.ToLower().Contains(value.ToLower()))
                            )
                        {
                            filtered.Add(entry);
                        }
                        else if (oper == ">" || oper == "<" || oper == ">=" || oper == "<=")
                        {
                            float convertedVariable;
                            float.TryParse(executedVariable, out convertedVariable);
                            float convertedValue;
                            float.TryParse(value, out convertedValue);
                            if ( // > < >= <=
                                (oper == ">" && convertedVariable > convertedValue) ||
                                (oper == ">=" && convertedVariable >= convertedValue) ||
                                (oper == "<" && convertedVariable < convertedValue) ||
                                (oper == "<=" && convertedVariable <= convertedValue)
                                )
                            {
                                filtered.Add(entry);
                            }
                        }

                    }
                    catch
                    {
                        return false;
                    }
                }

            }
            db.cardList = filtered;
            return true;
        }
        */

        /*
        public static bool IfCondition (Database db, string condition, string ifTrue, string ifFalse)
        {
            //надо написать метод, которым парсится строка до запятой и отправляется на вход Operate()!
            //разбираем condition тем же методом, что любые операторы, но не вызываем Operate();
            //разбираем ifTrue и ifFalse тем же методом, что и любые операторы, вызываем Operate();
            //в Operate необходимо добавить на вход опционально условие, которым будет являться condition, и оно будет && ко всем другим условиям в случае наличия;

            return true;
        }
        */
    }
}
