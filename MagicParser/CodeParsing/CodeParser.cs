using MagicParser.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static MagicParser.Database;

namespace MagicParser
{
    static class CodeParser //
    {
        public static string text;
        public static string parsedText;
        private static string codeBegin_word = "code:";
        private static string codeEnd_word = "endcode;";
        private static string filterBegin_word = "filter";
        private static string filterEnd_word = "endfilter";
        private static string fileNameWord = "fileName";
        public static bool validated = false;

        //Основной метод, который парсит целиком текст, введённый пользователем. Вызывается при назначении текста переменной
        public static ParseCodeResult ParseText()
        {
            string data = text;

            //Текст будет распилен на чередующиеся текстовые блоки - обычный текст и код
            List<string> textBlocks = new List<string>();
            //если текста нет, информация не валидна
            if (data == "" || data == null) 
            {
                return new ParseCodeResult(new Error("Кажется, вы забыли ввести некоторые данные.\nПарсинг не удался."));
            }

            //во всех остальных случаях разбиваем на текстовые блоки, заодно проверяя валидность данных
            ParseCodeResult result = SeparateToTextBlocks(data, textBlocks);
            if (result != null) { return result; }

            //Блоки в textBlocks записываются чередуясь, причём первый всегда обычный блок без кода, даже если это пустая строка
            //Поэтому каждый второй блок проверяем на валидность и сразу парсим

            //простая проверка на валидность - если в каком-то из блоков кода ничего не содержится, дальше не парсим
            for (int i = 1; i < textBlocks.Count(); i += 2)
            {
                if (textBlocks[i] == null || textBlocks[i] == "")
                {
                    return new ParseCodeResult(new Error("Кажется, ты забыл написать сам код..."));
                }
            }

            //Теперь собственно распарсим код отдельно в каждой итерации и вернём вместо него готовый результат
            return ParseCode(textBlocks);
        }

        //Делит весь текст на текстовые блоки
        private static ParseCodeResult SeparateToTextBlocks(string data, List<string> textBlocks)
        {
            while (data != "" && data != null)
            {
                //если в оставшейся data есть начало кода
                if (data.Contains(codeBegin_word))
                {
                    //то берём всё до начала кода
                    string dataFirstPart = SimpleTextProcessings.CutBeforeWith(ref data, codeBegin_word);
                    //затем смотрим, есть ли вообще фрагмент конца кода в оставшейся части текста
                    if (data.Contains(codeEnd_word))
                    {
                        //и если да, то берём всё из оставшейся части текста до фрагмента конца кода
                        string dataSecondPart = SimpleTextProcessings.CutBeforeWith(ref data, codeEnd_word);

                        //если в оставшемся есть ещё начало кода - информация не валидна
                        if (dataSecondPart.Contains(codeBegin_word))
                        {
                            return new ParseCodeResult(new Error("Начало кода внутри кода..."));
                        }

                        //добавляем куски кода в список
                        textBlocks.Add(dataFirstPart);
                        textBlocks.Add(dataSecondPart);
                    }
                    //если фрагмента конца кода в оставшейся части текста нет, информация не валидна
                    else
                    {
                        return new ParseCodeResult(new Error("Ты код забыл закрыть."));
                    }
                }
                //если в оставшейся части data начала кода нет, информация валидна
                textBlocks.Add(data);
                data = "";
            }
            //Возвращаем null, потому что при null основной метод продолжит свою работу, а при ошибке - выведет ошибку.
            return null;
        }

        //Превращает текстовые блоки с кодом в готовые списки и сшивает текстовые блоки
        private static ParseCodeResult ParseCode(List<string> textBlocks)
        {
            //Блоки в textBlocks записываются чередуясь, причём первый всегда обычный блок без кода, даже если это пустая строка
            //Поэтому парсим каждый второй блок
            for (int i = 1; i < textBlocks.Count(); i += 2)
            {
                Database db = new Database();
                string currentTextBlock = textBlocks[i].TrimStart().TrimEnd();
                //Пока не съедим всю строку, пытаемся парсить
                while (currentTextBlock != null && currentTextBlock != "")
                {
                    string currentCodeSnippet = SimpleTextProcessings.CutBeforeWith(ref currentTextBlock, ";");
                    currentTextBlock = currentTextBlock.TrimStart();
                    
                    //Сверяем текущий фрагмент кода с одним из доступных шаблонов:

                    //notesAsIs
                    if (currentCodeSnippet.ToLower() == "notesasis") { db.notesAsIs = true; }

                    //filname = "some/file/path"
                    else if (Regex.IsMatch(currentCodeSnippet, "^(?i)" + fileNameWord + "([\\s\\n\\r])*=([\\s\\n\\r])*\"(.)+([\\\\/]+(.)+)*[\\\\/]?\"(?-i)"))
                    {
                        string fileName = currentCodeSnippet.Substring(currentCodeSnippet.IndexOf("\"") + 1);
                        fileName = fileName.Substring(0, fileName.Length - 1);
                        if (File.Exists(fileName)) { db.FileName = fileName; } //здесь создаётся и сразу парсится база данных
                        else { return new ParseCodeResult(new Error("Похоже, что файла по адресу \"" + fileName + "\" не существует :(\nВ другой раз попарсим.")); }
                    }

                    // filter ... endfilter
                    else if (Regex.IsMatch(currentCodeSnippet, @"[.\s]*(?i)" + filterEnd_word + @"(?-i)$")) 
                    {
                        ParseCodeResult result = FilterHandling(db, currentCodeSnippet);
                        if (result != null) { return result; }
                    }

                    //sort = ...
                    else if (Regex.IsMatch(currentCodeSnippet, @"^(?i)sort([\s\n\r])*=([\s\n\r])*(.)+(,([\s\n\r])*(.)+)*(?-i)")) // sort
                    {
                        ParseCodeResult result = Sort();
                        if (result != null) { return result; }
                    }

                    //format = ...
                    else if (Regex.IsMatch(currentCodeSnippet, "^(?i)format([\\s\\n\\r])*=([\\s\\n\\r])*\"(.\\n\\r)*\"(?-i)")) // format
                    {
                        ParseCodeResult result = Format();
                        if (result != null) { return result; }
                    }

                    //если что-то не подходит под эти шаблоны, то информация не валидна
                    else
                    {
                        return new ParseCodeResult(new Error("К сожалению, в код вкралась ошибка. Она где-то здесь:\n\"" + currentCodeSnippet + "\""));
                    }
                }

                //ЗДЕСЬ ДОЛЖНА БЫТЬ ФУНКЦИЯ СБОРКИ БОКСОВ или чё-то такое.
            }
            //ЗДЕСЬ ВСЁ ОКОНЧАТЕЛЬНО ДОСОБИРАЕТСЯ И ПИШЕТСЯ В parsedText, ну или должно быть уже запилено к этому моменту.
            return null;
        }

        //Парсинг кода фильтрации
        private static ParseCodeResult FilterHandling(Database db, string currentCodeSnippet)
        {
            //проверяем, есть ли данные в базе, ибо если нет - парсить нельзя
            if (db.cardList == null)
            {
                return new ParseCodeResult(new Error("База данных пуста, как твоя душа. Возможно, ты забыл написать её адрес?"));
            }
            if (db.cardList.Count() == 0)
            {
                return new ParseCodeResult(new Error("База данных пуста, как твоя душа. Возможно, ты забыл написать её адрес?"));
            }

            //Удаляем теги filter и endfilter
            currentCodeSnippet = currentCodeSnippet.Substring(filterBegin_word.Length, currentCodeSnippet.Length - filterBegin_word.Length - filterEnd_word.Length).TrimStart().TrimEnd();
            string[] expressions = currentCodeSnippet.Split(','); // разбиваем код на отдельные выражения

            
            //для каждого выражения для каждой карты проверяем, подходит ли она. Если нет - удаляем её из базы
            foreach (string expression in expressions)
            {
                List<Entry> temp = new List<Entry>(db.cardList);

                foreach (Entry entry in temp)
                {
                    ParseCodeResult result = CodeParser.Operate(entry, expression);
                    if (result.type == 2)
                    {
                        if (!result.boo)
                        {
                            db.cardList.Remove(entry);
                        }
                    }
                    else if (result.type == 1) { return new ParseCodeResult(new Error(expression + "\nВыглядит так, будто тут ошибка синтаксиса.")); }
                    else if (result.type != 2) { return new ParseCodeResult(new Error("Похоже, что это выражение ничего не означает.")); }
                }
            }
            
            return new ParseCodeResult(true);
        }

        /* 
         * Если есть кавычки - делаем "слепые" диапазоны на кавычки,
         * после чего отправляем функцию на поиск скобок, сохраняя диапазон.
         * Однако если кавычки вокруг всего текста, это отдельный оператор.
         * Кавычка, перед которой стоит символ экранирования, не является оператором.
         * 
         * Если кавычек нет, просто делаем "слепые" диапазоны на скобки по всему диапазону.
         * 
         * Функция возвращает ошибку или результат выполнения оператора.
         * 
        */
        public static ParseCodeResult Operate(Entry entry, string input, List<int[]> ranges = null) //
        {
            input = input.TrimStart().TrimEnd();
            //ищем кавычки:
            if (ranges == null && input.ToLower().Contains(OperatorQuote.GetInstance().Word))
            {
                return DealWithQuotes(entry, input);
            }
            //Если в строке есть скобки - надо разобраться с ними:
            if (ranges == null && input.ToLower().Contains(OperatorOpenGroup.GetInstance().Word))
            {
                return DealWithBrackets(entry, input);
            }
            //иначе ищем самый приоритетный оператор в диапазонах
            return FindOperator(entry, input, ranges); //проверяем на наличие операторов ВНУТРИ РАНЖЕЙ в порядке приоритета
        }

        //Считывает кавычки в инпуте, если ошибка с кавычками - возвращает ошибку,
        //иначе переходим к поиску скобок, а если их нет - ищем операторы.
        private static ParseCodeResult DealWithQuotes(Entry entry, string input)
        {
            string quote_word = OperatorQuote.GetInstance().Word;
            bool quotesAreOpened = false;
            int start = 0;
            List<int[]> ranges = new List<int[]>();
            //Проходимся по каждой подстроке длиной в quote_word и ищем там quote_word
            for (int i = 0; i < input.Length - quote_word.Length + 1; i++)
            {
                if (input.Substring(i, quote_word.Length).ToLower() == quote_word)
                {
                    if (quotesAreOpened)
                    {
                        ranges.Add(new int[] { start, i });
                        quotesAreOpened = false;
                    } else { start = i; }
                }
            }

            //Если кавычка не закрыта - выдаём ошибку:
            if (quotesAreOpened)
            {
                return new ParseCodeResult(new Error("Тут явно что-то не так с кавычками:\n" + input));
            }
        }

        //Считывает скобки в инпуте, еслии ошибка в скобках - возвращает ошибку, иначе перезапускает Operate с учётом посчитанных скобок
        private static ParseCodeResult DealWithBrackets(Entry entry, string input)
        {
            string groupOpen_word = OperatorOpenGroup.GetInstance().Word;
            string groupClose_word = OperatorCloseGroup.GetInstance().Word;
            //считываем скобки и делаем диапазоны, соответствующие самым внешним скобкам
            int openBracketQty = 0;
            List<int[]> ranges = new List<int[]>();
            int start = 0;
            for (int i = 0; i < input.Length - Math.Max(groupOpen_word.Length, groupClose_word.Length) + 1; i++)
            {
                if (input.Substring(i, groupOpen_word.Length).ToLower() == groupOpen_word)
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
                        return new ParseCodeResult(new Error("Количество закрывающих скобок чутка зашкаливает.\n\"" + input + "\""));
                    }
                    i += groupOpen_word.Length - 1;
                }
            }
            //с количеством скобок что-то не так
            if (openBracketQty != 0)
            {
                return new ParseCodeResult(new Error("Нужно МЕНЬШЕ открывающих скобок.\n\"" + input + "\\"));
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

        //Ищет приоритетный оператор в доступных диапазонах поиска input и вызывает его обработку
        private static ParseCodeResult FindOperator(Entry entry, string input, List<int[]> ranges = null)
        {
            //Если ranges не заданы, ищем по всей строке
            if (ranges == null) { ranges = new List<int[]>(); ranges.Add(new int[] { 0, input.Length }); }

            //Для каждой приоритезированной группы операторов будем проверять наличие оператора в доступных диапазонах
            for (int i = 1; i <= Operator.LastPriority; i++)
            {
                foreach (int[] range in ranges)
                {
                    //Будем записывать все операторы наивысшего приоритета в список, потом слева направо его ресолвить
                    List<Operator> currentOperators = new List<Operator>();
                    List<int> currentOperatorsIndexes = new List<int>();
                    foreach (Operator o in Operator.list)
                    {
                        if (o.priority == i)
                        {
                            //находим первое упоминание этого оператора, записываем
                            if (input.ToLower().IndexOf(o.Word, range[0], range[1] - range[0]) != -1)
                            {
                                currentOperators.Add(o);
                                currentOperatorsIndexes.Add(input.ToLower().IndexOf(o.Word, range[0], range[1] - range[0]));
                            }
                        }
                    }
                    //далее если на текущем приоритете нашли операторы, среди них находим самый левый и выполянем его
                    if (currentOperators.Count() != 0)
                    {
                        //Смотрим, у какого оператора из выбранных наименьший индекс (стоит левее всех). Его и выполняем
                        int min = input.Length;
                        for (int j = 0; j < currentOperators.Count(); j++)
                        {
                            min = Math.Min(min, currentOperatorsIndexes[j]);
                        }
                        //Выполняем тот самый оператор, индекс которого - min
                        for (int j = 0; j < currentOperators.Count(); j++)
                        {
                            if (currentOperatorsIndexes[j] == min)
                            {
                                return currentOperators[j].Execute(entry, input, currentOperatorsIndexes[j]);
                            }
                        }
                    }
                    return new ParseCodeResult(new Error("Кажется, это синтаксическая ошибка.\n" + input));
                }
            }
            return new ParseCodeResult(new Error("Неизвестная ошибка о_о\n" + input));
        }

        //Парсинг кода сортировки
        private static ParseCodeResult Sort()
        {
            return new ParseCodeResult(true);
        }

        //Парсинг кода форматирования
        private static ParseCodeResult Format()
        {
            return new ParseCodeResult(true);
        }
    }
}
