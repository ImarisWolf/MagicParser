using MagicParser.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MagicParser
{
    public class Database //является выгрузкой одного конкретного файла txt
    {
        private string fileName;
        public string FileName
        {
            get
            {
                return fileName;
            }
                
            set
            {
                fileName = value;
                cardList = new List<Entry>();
                ReadFile();
            }

        }
        public bool notesAsIs;
        public class Entry
        {
            #region Magic Album original fields
            public string artist;
            public string border;
            public float buyPrice;
            public int buyQty;
            public string color;
            public string copyright;
            public string cost;
            public string gradeF;
            public string gradeR;
            public string language;
            public string legality;
            public string name;
            public string nameOracle;
            public string notes;
            public string number;
            public string pt;
            public float priceF;
            public float priceR;
            public int proxies;
            public int qtyF;
            public int qtyR;
            public string rarity;
            public float rating;
            public float sellPrice;
            public int sellQty;
            public string set;
            public string text;
            public string textOracle;
            public string type;
            public string typeOracle;
            public int used;
            public string version;
            #endregion

            #region Additional fields
            public int qty;
            public bool foil;
            public float dollarRate;
            public float discount;
            public string comment;
            public string grade;
            public float price;
            public int priority; //manual priority
            //public float cmc;
            //public float power;
            //public float toughness;
            //public float loyalty;

            #endregion

            #region Service fields
            public bool bothFoilAndNonFoil;
            #endregion
        }
        public List<Entry> cardList;
        public struct Group
        {
            public int qty;
            public bool foil;
            public float dollarRate;
            public float discount;
            public string comment;
            public string grade;
            public bool bothFoilAndNonFoil;

            public Group(bool foo)
            {
                qty = 0;
                foil = false;
                dollarRate = 0;
                discount = 0;
                comment = null;
                grade = "";
                bothFoilAndNonFoil = false;
            }
        }
        
        //Конструкторы
        public Database(string fileName)
        {
            FileName = fileName;
        }
        public Database(){}

        #region Private methods

        //Основной метод, считывающий и парсящий выгрузку
        private void ReadFile()
        {
            StreamReader sr = new StreamReader(fileName);
            string currentLine = sr.ReadLine();
            //заменяем в шапке все названия с пробелами, чтобы дальше пропарсить нормально
            currentLine = currentLine.Replace("Buy Price", "BuyPrice");
            currentLine = currentLine.Replace("Buy Qty", "BuyQty");
            currentLine = currentLine.Replace("Grade (F)", "GradeF");
            currentLine = currentLine.Replace("Grade (R)", "GradeR");
            currentLine = currentLine.Replace("Name (Oracle)", "NameOracle");
            currentLine = currentLine.Replace("Price (F)", "PriceF");
            currentLine = currentLine.Replace("Price (R)", "PriceR");
            currentLine = currentLine.Replace("Qty (F)", "QtyF");
            currentLine = currentLine.Replace("Qty (R)", "QtyR");
            currentLine = currentLine.Replace("Sell Price", "SellPrice");
            currentLine = currentLine.Replace("Sell Qty", "SellQty");
            currentLine = currentLine.Replace("Text (Oracle)", "TextOracle");
            currentLine = currentLine.Replace("Type (Oracle)", "TypeOracle");

            //делаем шапку
            List<string> header = MakeHeader(currentLine);
            ParseFile(sr, header);
            sr.Close();
            
            //Делаем всё остальное, что необходимо сделать с позициями
            ResidualParsing();
        }

        //Создание заголовка по строчке
        private List<string> MakeHeader (string line)
        {
            //магический метод, который парсит шапку
            List<string> header = new List<string>();
            while (line != null && line != "")
            {
                string columnHead = line[0].ToString();
                line = line.Remove(0, 1);
                while (true)
                {
                    char firstSymbol = line[0];
                    if (firstSymbol == ' ')
                    {
                        columnHead += firstSymbol;
                        line = line.Remove(0, 1);
                    }
                    else
                    {
                        if (line.IndexOf(" ") != -1)
                        {
                            columnHead += line.Substring(0, line.IndexOf(" "));
                            line = line.Remove(0, line.IndexOf(" "));
                        }
                        else
                        {
                            columnHead += line;
                            line = "";
                        }
                        header.Add(columnHead);
                        columnHead = "";
                        break;
                    }
                }
            }
            return header;
        }

        //Главный парсинг
        private void ParseFile (StreamReader sr, List<string> header)
        {
            //считывает строку за строкой и парсит самые базовые поля
            string currentLine = sr.ReadLine();
            while (currentLine != null && currentLine != "")
            {
                //создаём запись
                Entry entry = new Entry();

                //для каждого столбца в хедере отхреначиваем кусок строки и записываем содержимое в соответствующее поле
                foreach (string columnHead in header)
                {
                    int symbolsQty = currentLine.Count();
                    int columnWidth = columnHead.Count();
                    if (columnWidth > symbolsQty)
                    {
                        columnWidth = symbolsQty;
                    }
                    switch (columnHead.Trim())
                    {
                        case "Artist":
                            entry.artist = currentLine.Substring(0, columnWidth).TrimStart();
                            currentLine = currentLine.Remove(0, columnWidth);
                            break;

                        case "Border":
                            entry.border = currentLine.Substring(0, columnWidth).TrimStart();
                            currentLine = currentLine.Remove(0, columnWidth);
                            break;
                        case "BuyPrice":
                            Single.TryParse(currentLine.Substring(0, columnWidth + 1).TrimStart().Replace('.', ','), out entry.buyPrice);
                            currentLine = currentLine.Remove(0, columnWidth + 1);
                            break;
                        case "BuyQty":
                            Int32.TryParse(currentLine.Substring(0, columnWidth + 1).TrimStart(), out entry.buyQty);
                            currentLine = currentLine.Remove(0, columnWidth + 1);
                            break;
                        case "Color":
                            entry.color = currentLine.Substring(0, columnWidth).TrimStart();
                            currentLine = currentLine.Remove(0, columnWidth);
                            break;
                        case "Copyright":
                            entry.copyright = currentLine.Substring(0, columnWidth).TrimStart();
                            currentLine = currentLine.Remove(0, columnWidth);
                            break;
                        case "Cost":
                            entry.cost = currentLine.Substring(0, columnWidth).TrimStart();
                            currentLine = currentLine.Remove(0, columnWidth);
                            break;
                        case "GradeF":
                            entry.gradeF = currentLine.Substring(0, columnWidth + 3).TrimStart();
                            currentLine = currentLine.Remove(0, columnWidth + 3);
                            break;
                        case "GradeR":
                            entry.gradeR = currentLine.Substring(0, columnWidth + 3).TrimStart();
                            currentLine = currentLine.Remove(0, columnWidth + 3);
                            break;
                        case "Language":
                            entry.language = currentLine.Substring(0, columnWidth).TrimStart();
                            currentLine = currentLine.Remove(0, columnWidth);
                            break;
                        case "Legality":
                            entry.legality = currentLine.Substring(0, columnWidth).TrimStart();
                            currentLine = currentLine.Remove(0, columnWidth);
                            break;
                        case "Name":
                            entry.name = currentLine.Substring(0, columnWidth).TrimStart();
                            currentLine = currentLine.Remove(0, columnWidth);
                            break;
                        case "NameOracle":
                            entry.nameOracle = currentLine.Substring(0, columnWidth + 3).TrimStart();
                            currentLine = currentLine.Remove(0, columnWidth + 3);
                            break;
                        case "Notes":
                            entry.notes = currentLine.Substring(0, columnWidth).TrimStart();
                            currentLine = currentLine.Remove(0, columnWidth);
                            break;
                        case "Number":
                            entry.number = currentLine.Substring(0, columnWidth).TrimStart();
                            currentLine = currentLine.Remove(0, columnWidth);
                            break;
                        case "P/T":
                            entry.pt = currentLine.Substring(0, columnWidth).TrimStart();
                            currentLine = currentLine.Remove(0, columnWidth);
                            break;
                        case "PriceF":
                            Single.TryParse(currentLine.Substring(0, columnWidth + 3).TrimStart().Replace('.', ','), out entry.priceF);
                            currentLine = currentLine.Remove(0, columnWidth + 3);
                            break;
                        case "PriceR":
                            Single.TryParse(currentLine.Substring(0, columnWidth + 3).TrimStart().Replace('.', ','), out entry.priceR);
                            currentLine = currentLine.Remove(0, columnWidth + 3);
                            break;
                        case "Proxies":
                            Int32.TryParse(currentLine.Substring(0, columnWidth).TrimStart().Replace('.', ','), out entry.proxies);
                            currentLine = currentLine.Remove(0, columnWidth);
                            break;
                        case "QtyF":
                            Int32.TryParse(currentLine.Substring(0, columnWidth + 3).TrimStart(), out entry.qtyF);
                            currentLine = currentLine.Remove(0, columnWidth + 3);
                            break;
                        case "QtyR":
                            Int32.TryParse(currentLine.Substring(0, columnWidth + 3).TrimStart().Replace('.', ','), out entry.qtyR);
                            currentLine = currentLine.Remove(0, columnWidth + 3);
                            break;
                        case "Rarity":
                            entry.rarity = currentLine.Substring(0, columnWidth).TrimStart();
                            currentLine = currentLine.Remove(0, columnWidth);
                            break;
                        case "Rating":
                            Single.TryParse(currentLine.Substring(0, columnWidth).TrimStart().Replace('.', ','), out entry.rating);
                            currentLine = currentLine.Remove(0, columnWidth);
                            break;
                        case "SellPrice":
                            Single.TryParse(currentLine.Substring(0, columnWidth + 1).TrimStart().Replace('.', ','), out entry.sellPrice);
                            currentLine = currentLine.Remove(0, columnWidth + 1);
                            break;
                        case "SellQty":
                            Int32.TryParse(currentLine.Substring(0, columnWidth + 1).TrimStart().Replace('.', ','), out entry.sellQty);
                            currentLine = currentLine.Remove(0, columnWidth + 1);
                            break;
                        case "Set":
                            entry.set = currentLine.Substring(0, columnWidth).TrimStart();
                            currentLine = currentLine.Remove(0, columnWidth);
                            break;
                        case "Text":
                            entry.text = currentLine.Substring(0, columnWidth).TrimStart();
                            currentLine = currentLine.Remove(0, columnWidth);
                            break;
                        case "TextOracle":
                            entry.textOracle = currentLine.Substring(0, columnWidth + 3).TrimStart();
                            currentLine = currentLine.Remove(0, columnWidth + 3);
                            break;
                        case "Type":
                            entry.type = currentLine.Substring(0, columnWidth).TrimStart();
                            currentLine = currentLine.Remove(0, columnWidth);
                            break;
                        case "TypeOracle":
                            entry.typeOracle = currentLine.Substring(0, columnWidth + 3).TrimStart();
                            currentLine = currentLine.Remove(0, columnWidth + 3);
                            break;
                        case "Used":
                            Int32.TryParse(currentLine.Substring(0, columnWidth).TrimStart().Replace('.', ','), out entry.used);
                            currentLine = currentLine.Remove(0, columnWidth);
                            break;
                        case "Version":
                            entry.version = currentLine.Substring(0, columnWidth).TrimStart();
                            currentLine = currentLine.Remove(0, columnWidth);
                            break;
                    }
                    //выходим из цикла, когда кончается строка
                    if (currentLine == null || currentLine == "") { break; }
                }
                //парсим полученную запись дальше на несколько записей с разными свойствами - в этом же методе они добавятся в cardList
                ParseEntry(entry);
                //читаем следующую строку
                currentLine = sr.ReadLine();
            }
        }
        
        private void ParseEntry(Entry entry)
        {
            // если есть notes и не стоит флаг не парсить по заметке, то парсим по заметке
            if (entry.notes != null && entry.notes != "" && !notesAsIs)
            {
                ParseNote(entry);
            }
            //если notes отсутствует, то просто разделяем на фойло и не фойло и добавляем
            else if (entry.qtyR >0 && entry.qtyF > 0) 
            {
                ParseFoil(entry);

            }
            //если делить нечего, то просто дописываем нужные поля и добавляем
            else
            {
                ParseWithoutComment(entry);
            }
        }

        //Парсит по комментарию
        private void ParseNote (Entry entry)
        {
            string note = entry.notes;
            //удаляем одну точку в конце, если она есть
            if (note[note.Count() - 1] == '.') 
            {
                note = note.Remove(note.Count() - 1);
            }

            string[] groupsAsStrings = note.Split(',', ';'); // разбиваем заметку на несколько групп
            List<Group> allGeneralGroups = new List<Group>(); // список групп, параметры которых применяются ко всем картам
            List<Group> restGeneralGroups = new List<Group>(); // список групп, параметры которых применяются ко всем только фойловым/нефойловым картам
            List<Group> separatedGroups = new List<Group>(); // список отдельных друг от друга групп

            //для каждой группы, на которые мы разбили заметку, выполняем парсинг
            foreach (string g in groupsAsStrings)
            {
                string stringInGroup = g.TrimStart(); //будем отрезать куски от группы, разбивая её на подстроки
                Group currentGroup = new Group(true);
                while (stringInGroup != "" && stringInGroup != null)
                {
                    stringInGroup = stringInGroup.TrimStart();
                    string wordInGroup = SimpleTextProcessings.CutBefore(ref stringInGroup, ' ');
                    
                    //Если в слове есть кавычка, то нужно в слово дописать все остальные слова до следующей кавычки
                    if (wordInGroup.IndexOf('\"') != -1)
                    {
                        if (stringInGroup != "")
                        {
                            wordInGroup += SimpleTextProcessings.CutBefore(ref stringInGroup, '\"') + "\"";
                            stringInGroup = stringInGroup.Remove(0, 1);
                        };
                    }

                    //парсим слово
                    ParseWord(wordInGroup, currentGroup);
                }

                //если количество карт в группе - 0, это общая группа (её свойства воздействую на все карты, не перезаписывая другие указанные свойства)
                if (currentGroup.qty == 0)
                {
                    if (currentGroup.bothFoilAndNonFoil) { allGeneralGroups.Add(currentGroup); }
                    else { restGeneralGroups.Add(currentGroup); }
                }
                //если в количестве группы больше нуля карт, то это отдельная группа, которая воздействует только на карты внутри себя
                else
                {
                    separatedGroups.Add(currentGroup);
                    if (currentGroup.foil) { entry.qtyF -= currentGroup.qty; }
                    else { entry.qtyR -= currentGroup.qty; }
                }
            }

            if (entry.qtyR < 0 || entry.qtyF < 0)
            {
                //возвращаем ошибку - в комментарии указано больше карт, чем в реальном количестве
                //прекращаем выполнение парсинга, просим исправить
                //также надо возвращать ошибку, когда bothFoilAndNonFoil == true, но при этом в этой группе указывалось количество карт, либо указывалось foil/non-foil
            }

            //добавляем группы с дефолтными параметрами для всех карт, которые не были описаны особо
            if (entry.qtyR > 0)
            {
                Group emptyGroup = new Group(true) { qty = entry.qtyR };
                separatedGroups.Add(emptyGroup);
            }
            if (entry.qtyF > 0)
            {
                Group emptyGroup = new Group(true) { qty = entry.qtyF, foil = true };
                separatedGroups.Add(emptyGroup);
            }

            //создаём записи, перенося данные из групп в порядке приоритета
            MakeParsedEntries(entry, allGeneralGroups, restGeneralGroups, separatedGroups);
            
        }

        //определяет, что делать с одним словом (или данными в кавычках) в комменте
        private void ParseWord(string wordInGroup, Group currentGroup)
        {
            if (Regex.IsMatch(wordInGroup, @"^[1-9]+")) // число
            {
                Int32.TryParse(wordInGroup, out currentGroup.qty);
            }
            else if (wordInGroup.ToLower() == "foil") // фойло
            {
                currentGroup.foil = true;
            }
            else if (wordInGroup.ToLower() == "non-foil") // не фойло
            {
                currentGroup.foil = false;
            }
            else if (wordInGroup.ToLower() == "all") // относится и к фойлу, и не к фойлу
            {
                currentGroup.bothFoilAndNonFoil = true;
            }
            else if (Regex.IsMatch(wordInGroup.ToLower(), @"^(c|r)([1-9])+(\.([1-9])+)?")) // курс доллара в виде c50 или r35
            {
                Single.TryParse(wordInGroup.Substring(1), out currentGroup.dollarRate);
            }
            else if (Regex.IsMatch(wordInGroup.ToLower(), @"^d(-)?([1-9])+(\.([1-9])+)?")) // скидка в виде d20 или d-10
            {
                Single.TryParse(wordInGroup.Substring(1), out currentGroup.discount);
            }
            else if (Regex.IsMatch(wordInGroup, "^\\\"(.)+\\\"")) // запись в кавычках
            {
                currentGroup.comment = wordInGroup.Substring(1, wordInGroup.Count() - 2);
            }
            else // все остальные случаи (всё, что осталось)
            {
                currentGroup.grade = (currentGroup.grade + " " + wordInGroup).TrimStart();
            }
        }

        //Создаёт записи, записывает в них информацию из групп в порядке приоритета, добавляет записи
        private void MakeParsedEntries(Entry entry, List<Group> allGeneralGroups, List<Group> restGeneralGroups, List<Group> separatedGroups)
        {
            foreach (Group currentSeparetedGroup in separatedGroups)
            {
                Entry parsedEntry = new Entry();
                parsedEntry = entry;
                parsedEntry.foil = currentSeparetedGroup.foil;
                if (parsedEntry.foil)
                {
                    parsedEntry.grade = parsedEntry.gradeF;
                    parsedEntry.price = parsedEntry.sellPrice;
                }
                else
                {
                    parsedEntry.grade = parsedEntry.gradeR;
                    parsedEntry.price = parsedEntry.buyPrice;
                }
                foreach (Group currentGeneralGroups in allGeneralGroups)
                {
                    parsedEntry.dollarRate = currentGeneralGroups.dollarRate;
                    parsedEntry.discount = currentGeneralGroups.discount;
                    parsedEntry.comment = currentGeneralGroups.comment;
                    if (currentGeneralGroups.grade != "") { parsedEntry.grade = currentGeneralGroups.grade; }
                }
                foreach (Group currentGeneralGroups in restGeneralGroups)
                {
                    if (parsedEntry.foil == currentGeneralGroups.foil)
                    {
                        if (currentGeneralGroups.dollarRate != 0) { parsedEntry.dollarRate = currentGeneralGroups.dollarRate; }
                        if (currentGeneralGroups.discount != 0) { parsedEntry.discount = currentGeneralGroups.discount; }
                        if (currentGeneralGroups.comment != "") { parsedEntry.comment = currentGeneralGroups.comment; }
                        if (currentGeneralGroups.grade != "") { parsedEntry.grade = currentGeneralGroups.grade; }
                    }
                }
                parsedEntry.qty = currentSeparetedGroup.qty;
                if (currentSeparetedGroup.dollarRate != 0) { parsedEntry.dollarRate = currentSeparetedGroup.dollarRate; }
                if (currentSeparetedGroup.discount != 0) { parsedEntry.discount = currentSeparetedGroup.discount; }
                if (currentSeparetedGroup.comment != "" && currentSeparetedGroup.comment != null) { parsedEntry.comment = currentSeparetedGroup.comment; }
                if (currentSeparetedGroup.grade != "" && currentSeparetedGroup.grade != null) { parsedEntry.grade = currentSeparetedGroup.grade; }

                cardList.Add(parsedEntry);
            }
        }

        //разделяет карту на фойловые и не фойловые и добавляет их
        private void ParseFoil(Entry entry)
        {
            Entry nonFoilEntry = new Entry();
            nonFoilEntry = entry;
            nonFoilEntry.qty = entry.qtyR;
            nonFoilEntry.grade = entry.gradeR;
            nonFoilEntry.price = entry.sellPrice;
            if (notesAsIs)
            {
                nonFoilEntry.comment = nonFoilEntry.notes;
                nonFoilEntry.notes = null;
            }
            cardList.Add(nonFoilEntry);

            Entry foilEntry = new Entry();
            foilEntry = entry;
            foilEntry.foil = true;
            foilEntry.qty = entry.qtyF;
            foilEntry.grade = entry.gradeF;
            foilEntry.price = entry.buyPrice;
            if (notesAsIs)
            {
                foilEntry.comment = nonFoilEntry.notes;
                foilEntry.notes = null;
            }
            cardList.Add(foilEntry);
        }

        private void ParseWithoutComment(Entry entry)
        {
            if (entry.qtyF > 0)
            {
                entry.foil = true;
                entry.qty = entry.qtyF;
                entry.grade = entry.gradeF;
                entry.price = entry.buyPrice;
            }
            else
            {
                entry.qty = entry.qtyR;
                entry.grade = entry.gradeR;
                entry.price = entry.sellPrice;
            }
            if (notesAsIs)
            {
                entry.comment = entry.notes;
                entry.notes = null;
            }
            cardList.Add(entry);
        }

        //парсит всё оставшееся - переносит поля, чистит лишние поля
        private void ResidualParsing() 
        {
            foreach (Entry entry in cardList)
            {
                entry.qtyR = 0;
                entry.qtyF = 0;
                entry.gradeR = "";
                entry.gradeF = "";
                entry.sellPrice = 0;
                entry.buyPrice = 0;
            }
        }
        
        #endregion
    }
}
