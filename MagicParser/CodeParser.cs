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
        public static string ParseText()
        {
            string data = text;

            //Текст будет распилен на чередующиеся текстовые блоки - обычный текст и код
            List<string> textBlocks = new List<string>();
            //если текста нет, информация не валидна
            if (data == "" || data == null) 
            {
                return "Кажется, вы забыли ввести некоторые данные.\nПарсинг не удался.";
            }

            //во всех остальных случаях разбиваем на текстовые блоки, заодно проверяя валидность данных
            string result = SeparateToTextBlocks(data, textBlocks);
            if (result != null) { return result; }

            //Блоки в textBlocks записываются чередуясь, причём первый всегда обычный блок без кода, даже если это пустая строка
            //Поэтому каждый второй блок проверяем на валидность и сразу парсим

            //простая проверка на валидность - если в каком-то из блоков кода ничего не содержится, дальше не парсим
            for (int i = 1; i < textBlocks.Count(); i += 2)
            {
                if (textBlocks[i] == null || textBlocks[i] == "")
                {
                    return "Кажется, ты забыл написать сам код...";
                }
            }

            //Теперь собственно распарсим код отдельно в каждой итерации и вернём вместо него готовый результат
            return ParseCode(textBlocks);
        }

        //Делит весь текст на текстовые блоки
        private static string SeparateToTextBlocks(string data, List<string> textBlocks)
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
                            return "Начало кода внутри кода...";
                        }

                        //добавляем куски кода в список
                        textBlocks.Add(dataFirstPart);
                        textBlocks.Add(dataSecondPart);
                    }
                    //если фрагмента конца кода в оставшейся части текста нет, информация не валидна
                    else
                    {
                        return "Ты код забыл закрыть.";
                    }
                }
                //если в оставшейся части data начала кода нет, информация валидна
                textBlocks.Add(data);
                data = "";
            }
            return null;
        }

        //Превращает текстовые блоки с кодом в готовые списки и сшивает текстовые блоки
        private static string ParseCode(List<string> textBlocks)
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
                        else { return "Похоже, что файла по адресу \"" + fileName + "\" не существует :(\nВ другой раз попарсим."; }
                    }

                    // filter ... endfilter
                    else if (Regex.IsMatch(currentCodeSnippet, @"[.\s]*(?i)" + filterEnd_word + @"(?-i)$")) 
                    {
                        string result = FilterHandling(db, currentCodeSnippet);
                        if (result != null) { return result; }
                    }

                    //sort = ...
                    else if (Regex.IsMatch(currentCodeSnippet, @"^(?i)sort([\s\n\r])*=([\s\n\r])*(.)+(,([\s\n\r])*(.)+)*(?-i)")) // sort
                    {
                        string result = Sort();
                        if (result != null) { return result; }
                    }

                    //format = ...
                    else if (Regex.IsMatch(currentCodeSnippet, "^(?i)format([\\s\\n\\r])*=([\\s\\n\\r])*\"(.\\n\\r)*\"(?-i)")) // format
                    {
                        string result = Format();
                        if (result != null) { return result; }
                    }

                    //если что-то не подходит под эти шаблоны, то информация не валидна
                    else
                    {
                        return "К сожалению, в код вкралась ошибка. Она где-то здесь:\n\"" + currentCodeSnippet + "\"";
                    }
                }

                //ЗДЕСЬ ДОЛЖНА БЫТЬ ФУНКЦИЯ СБОРКИ БОКСОВ или чё-то такое.
            }
            //ЗДЕСЬ ВСЁ ОКОНЧАТЕЛЬНО ДОСОБИРАЕТСЯ И ПИШЕТСЯ В parsedText, ну или должно быть уже запилено к этому моменту.
            return null;
        }

        //Парсинг кода фильтрации
        private static string FilterHandling(Database db, string currentCodeSnippet)
        {
            //проверяем, есть ли данные в базе, ибо если нет - парсить нельзя
            if (db.cardList == null)
            {
                return "База данных пуста, как твоя душа. Возможно, ты забыл написать её адрес?";
            }
            if (db.cardList.Count() == 0)
            {
                return "База данных пуста, как твоя душа. Возможно, ты забыл написать её адрес?";
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
                    string result = Oper.Operate(entry, expression);
                    if (result == Oper.filterRemove) { db.cardList.Remove(entry); }
                    else if (result == Oper.error) { return expression + "\nВыглядит так, будто тут ошибка синтаксиса."; }
                    else if (result != Oper.filterAccept) { return "Похоже, что это выражение ничего не означает."; }
                }

            }
            
            return null;
        }

        //Парсинк кода сортировки
        private static string Sort()
        {
            return null;
        }

        //Парсинг кода форматирования
        private static string Format()
        {
            return null;
        }
    }
}
