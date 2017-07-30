using MagicParser.CodeParsing;
using MagicParser.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MagicParser
{
    public class Database //является выгрузкой одного конкретного файла txt
    {
        public string fileName { get; set; } //Путь к файлу, из которого берётся выгрузка базы
        public bool notesAsIs { get; set; } //Если указан этот параметр, то при считывании базы комментарии не будут парситься особым образом
        public static string token; //Текущий токен при парсинге с токенайзером
        public string errorDescription;

        //Класс, содержащий в себе информацию об одной записи.
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
            public int groupID;
            #endregion

            public Entry() { }
            public Entry(Entry entry)
            {
                FieldInfo[] fields = typeof(Entry).GetFields();
                foreach (FieldInfo field in fields)
                {
                    field.SetValue(this, field.GetValue(entry));
                }
            }

        }
        public List<Entry> cardList { get; set; }
        public class Parameter
        {
            public int qty;
            public string type;

            public string comment;
            public float discount;
            public float dollarRate;
            public FieldInfo field;
            public string fieldValue;
            public string grade;
            public string language;
            public float price;
            public int priority;

            public float foilPrice;
            public float nonFoilPrice;
            public string foilGrade;
            public string nonFoilGrade;
            //public bool bothFoilAndNonFoil;

            public Parameter(Entry entry)
            {
                qty = 0;
                type = "";

                comment = "";
                discount = 0;
                dollarRate = 0;
                field = null;
                fieldValue = "";
                grade = entry.grade;
                language = entry.language;
                price = 0;
                priority = 0;
                
                foilPrice = entry.buyPrice;
                nonFoilPrice = entry.sellPrice;
                foilGrade = entry.gradeF;
                nonFoilGrade = entry.gradeR;
                //bothFoilAndNonFoil = false;
            }
        }
        
        //Конструктор
        public Database(string fileName = null)
        {
            if (fileName != null)
            {
                this.fileName = fileName;
            }
            notesAsIs = false;
            cardList = new List<Entry>();
        }

        #region Private methods

        private void GetToken(Tokenizer t)
        {
            token = t.GetToken();
        }

        private string GetNextToken(Tokenizer t)
        {
            return t.ForseeToken();
        }

        private string ErrorExpected(string expected = "", bool quotes = true)
        {
            string output = "";
            if (token != "'") output = "Unexpected token: '" + token + "' .";
            else output = "Unexpected token: \"" + token + "\" .";

            if (expected != "")
            {
                if (quotes) output += " '" + expected + "' expected.";
                else output += " " + expected + " expected.";
            }
            output += " Check your Magic Album file.";
            return output;
        }

        private string ErrorQuotes(string whatToDo)
        {
            return "Unexpected token: " + token + ". You must " + whatToDo + " in single quotes.";
        }

        //Основной метод, считывающий и парсящий выгрузку
        public bool ReadFile()
        {
            //try
            //{
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
                return true;
            //}
            //catch
            //{
                //return false;
            //}
            
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
                    if (columnWidth > symbolsQty) columnWidth = symbolsQty;
                    
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
            //если есть notes и не стоит флаг 'парсить как есть', то парсим по заметке
            if (!String.IsNullOrEmpty(entry.notes) && !notesAsIs) ParseNote(entry);
            //если notes отсутствует, то просто разделяем на фойло и не фойло и добавляем
            else if (entry.qtyR >0 && entry.qtyF > 0) ParseFoil(entry);
            //если делить нечего, то просто дописываем нужные поля и добавляем
            else RewriteQty(entry);

            //(Разделение на фойловые и не фойловые карты происходит внутри парсинга по заметке. Это сделано потому, что в комментарии к записи может быть параметр, общий для обоих типов карт)
        }

        //Парсит по комментарию
        private void ParseNote(Entry entry)
        {
            //note = [parameter] ((',' | ';') parameter)*['.'];
            string note = entry.notes;
            //удаляем одну точку в конце, если она есть (потому что я могу её поставить)
            if (note[note.Count() - 1] == '.') note = note.Remove(note.Count() - 1);

            string[] parametersRaw = note.Split(',', ';'); // разбиваем заметку на несколько групп - параметров
            //Создаём список параметров для текущей записи
            List<Parameter> parameters = new List<Parameter>();
            //parameter = [qty] [type] property+;
            foreach (string parameterRaw in parametersRaw)
            {
                Tokenizer t = new Tokenizer(parameterRaw);
                Parameter par = new Parameter(entry);
                GetToken(t);
                //[qty]
                if (Int32.TryParse(token.Replace('.', ','), out par.qty)) { GetToken(t); }
                //[type]
                if (token.ToLower() == "foil") { GetToken(t); par.type = "foil"; }
                else if (token.ToLower() == "nonfoil" || token.ToLower() == "non-foil") { GetToken(t); par.type = "non-foil"; }
                else if (token.ToLower() == "foil") { GetToken(t); par.type = "foil"; }

                //property = grade | price | language | dollarRate | discount | comment | priority | field;
                do
                {
                    //comment = '"' ?anyText? '"';
                    if (token == "\"")
                    {
                        token = t.GetUntil('"');
                        par.comment = token;
                        GetToken(t);
                    }
                    //discount = ('d' ?number?) | (?number? '%');
                    else if (token.ToLower() == "d")
                    {
                        GetToken(t);
                        if (token == "-")
                        {
                            GetToken(t);
                            if (Regex.IsMatch(token, @"^(?i)\d+(\.\d+)?(?-i)$"))
                            {
                                float.TryParse(token.Replace('.', ','), out par.dollarRate);
                                par.dollarRate = -par.dollarRate;
                                GetToken(t);
                            }
                            else { errorDescription = ErrorExpected(); return; }
                        }
                        else { errorDescription = ErrorExpected(); return; }
                    }
                    else if (Regex.IsMatch(token, @"^(?i)d\d+(\.\d+)?(?-i)$"))
                    {
                        float.TryParse(token.Substring(1).Replace('.', ','), out par.dollarRate);
                        GetToken(t);
                    }
                    else if (Regex.IsMatch(token, @"^(?i)\d+(\.\d+)?(?-i)%$"))
                    {
                        float.TryParse(token.Substring(0, token.Length - 2).Replace('.', ','), out par.dollarRate);
                    }
                    //dollarRate = ('c' | 'r')  ?number?;
                    else if (Regex.IsMatch(token, @"^(?i)(c|r)\d+(\.\d+)?(?-i)$"))
                    {
                        float.TryParse(token.Substring(1).Replace('.', ','), out par.dollarRate);
                        GetToken(t);
                    }
                    //field = '$' ?fieldName? '=' ?anyText?;
                    else if (token == "$")
                    {
                        GetToken(t);
                        //Проверяем существование поля с таким названием
                        FieldInfo field = typeof(Entry).GetField(token, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                        par.field = field;
                        if (field != null)
                        {
                            GetToken(t);
                            if (token == "=")
                            {
                                GetToken(t);
                                par.fieldValue = token;
                                GetToken(t);
                            }
                            else { errorDescription = ErrorExpected("="); return; }
                        }
                        else { errorDescription = "Wrong field name: " + token + ". Check your Magic Album file."; return; }
                    }
                    //grade = 'M' | 'Mint' | 'NM/M' | 'NM-' | 'SP+' | 'SP' | 'SP-' | 'MP' | 'MP/HP' | 'HP';
                    else if (Regex.IsMatch(token, @"^(?i)M|Mint|NM/M|NM-|SP\+|SP|SP-|MP|MP/HP|HP(?-i)$"))
                    {
                        par.grade = token;
                        GetToken(t);
                    }
                    //language = 'English' | 'ENG' | 'Italian' | 'ITA' | 'Korean' | 'KOR' | 'Russian' | 'RUS' | 'Spanish' | 'SPA' | 'French' | 'FRA' | 'Japan' | 'JPN' | 'German' | 'GER' | 'Portuguese' | 'POR' | 'ChineseSimplified' | 'SimplifiedChinese' | 'ZHC' | 'ChineseTraditional' | 'TraditionalChinese' | 'ZHT' | 'Hebrew' | HEB' | 'Arabic' | 'ARA' | 'Latin' | 'LAT' | 'Sanskrit' | 'SAN' | 'AncientGreek' | 'GRK' | 'Phyrexian' | 'PHY';
                    else if (Regex.IsMatch(token, @"^(?i)English|ENG|Italian|ITA|Korean|KOR|Russian|RUS|Spanish|SPA|French|FRA|Japan|JPN|German|GER|Portuguese|POR|ChineseSimplified|SimplifiedChinese|ZHC|ChineseTraditional|TraditionalChinese|ZHT|Hebrew|HEB|Arabic|ARA|Latin|LAT|Sanskrit|SAN|AncientGreek|GRK|Phyrexian|PHY(?-i)$"))
                    {
                        par.language = token;
                        GetToken(t);
                    }
                    //price = 'p' ?number?;
                    else if (Regex.IsMatch(token, @"^p\d+(\.\d+)?$"))
                    {
                        float.TryParse(token.Substring(1).Replace('.', ','), out par.price);
                        GetToken(t);
                    }
                    //priority = 'p' ?number?;
                    else if (Regex.IsMatch(token, @"^(?i)p\d+(?-i)$"))
                    {
                        Int32.TryParse(token.Substring(1), out par.priority);
                        GetToken(t);
                    }
                    else { errorDescription = "Wrong parameter: " + token + ". Check your Magic Album file."; return; }
                }
                while (!t.endIsReached);

                //Корректируем цены и состояния на фойло
                if (par.type == "foil")
                {
                    if (par.price != 0) par.foilPrice = par.price;
                    if (!String.IsNullOrEmpty(par.grade)) par.foilGrade = par.grade;
                }
                else if (par.type == "non-foil" || par.type == "nonfoil" || (par.qty > 0 && par.type == ""))
                {
                    if (par.price != 0) par.nonFoilPrice= par.price;
                    if (!String.IsNullOrEmpty(par.grade)) par.nonFoilGrade = par.grade;
                }
                else //qty = 0, type = ""
                {
                    if (par.price != 0) par.foilPrice = par.price;
                    if (par.price != 0) par.nonFoilPrice = par.price;
                    if (!String.IsNullOrEmpty(par.grade)) par.foilGrade = par.grade;
                    if (!String.IsNullOrEmpty(par.grade)) par.nonFoilGrade = par.grade;
                }

                parameters.Add(par);
            }

            //Проверим количество - если карт больше, возвращаем ошибку
            int qtyF = 0;
            int qtyR = 0;
            foreach (Parameter par in parameters)
            {
                if (par.type.ToLower() == "foil") qtyF += par.qty;
                else if (par.type.ToLower() == "non-foil" || par.type.ToLower() == "nonfoil" || par.type.ToLower() == "") qtyR += par.qty;
            }
            if (qtyF > entry.qtyF || qtyR > entry.qtyR) { errorDescription = "Wrong cards quantity. Check your Magic Album file."; return; }

            /*
             * Если количество и типа отсутствуют - параметр применяется ко всем картам.
             * Затем, если количество отсутствует, но указан тип - параметр применяется ко всем картам указанного типа.
             * Затем, если количество присутствует, но тип не указан - параметр применяется к qty не-фойловым картам.
             * Затем, если количество и тип указаны - параметр применяется к qty карт указанного типа.
            */

            //Если количество и тип отсутствуют...
            foreach (Parameter par in parameters)
            {
                if (par.qty == 0 && par.type.ToLower() == "") SetParameters(entry, par);
            }

            //Если количество отсутствует, но есть тип...
            //'Наследуем' два типа записей от базового типа
            Entry nonFoilEntry = new Entry(entry);
            Entry foilEntry = new Entry(entry);
            foreach (Parameter par in parameters)
            {
                if (par.qty == 0 && (par.type.ToLower() == "non-foil" || par.type.ToLower() == "nonfoil"))
                {
                    SetParameters(nonFoilEntry, par);
                    nonFoilEntry.foil = false;
                    nonFoilEntry.grade = nonFoilEntry.gradeR;
                    nonFoilEntry.price = nonFoilEntry.priceR;
                }
                else if (par.qty == 0 && par.type.ToLower() == "foil")
                {
                    SetParameters(foilEntry, par);
                    foilEntry.foil = true;
                    nonFoilEntry.grade = nonFoilEntry.gradeF;
                    nonFoilEntry.price = nonFoilEntry.priceF;
                }
            }

            //Если количество есть (нет типа = указанный 'non-foil' тип)
            foreach (Parameter par in parameters)
            {
                if (par.qty > 0 && (par.type.ToLower() == "" || par.type.ToLower() == "non-foil" || par.type.ToLower() == "nonfoil"))
                {
                    Entry newEntry = new Entry(nonFoilEntry);
                    newEntry.qty = par.qty;
                    entry.qtyR -= par.qty; //Уменьшаем счётчик
                    SetParameters(newEntry, par);
                    newEntry.foil = false;
                    newEntry.grade = newEntry.gradeR;
                    newEntry.price = newEntry.sellPrice;
                    cardList.Add(newEntry);
                }
                else if (par.qty > 0 && par.type.ToLower() == "foil")
                {
                    Entry newEntry = new Entry(foilEntry);
                    newEntry.qty = par.qty;
                    entry.qtyF -= par.qty; //Уменьшаем счётчик
                    SetParameters(newEntry, par);
                    newEntry.foil = true;
                    newEntry.grade = newEntry.gradeF;
                    newEntry.price = newEntry.buyPrice;
                    cardList.Add(newEntry);
                }
            }

            //Если ещё остались карты, не обработанные конкретными параметрами - добавляем их как более общие карты в указанных количествах
            if (entry.qtyR != 0)
            {
                nonFoilEntry.qty = entry.qtyR;
                nonFoilEntry.grade = nonFoilEntry.gradeR;
                nonFoilEntry.price = nonFoilEntry.sellPrice;
                cardList.Add(nonFoilEntry);
            }
            if (entry.qtyF != 0)
            {
                foilEntry.qty = entry.qtyF;
                foilEntry.grade = foilEntry.gradeF;
                foilEntry.price = foilEntry.buyPrice;
                cardList.Add(foilEntry);
            }
        }

        //Настраивает поля записи, в соответствии с входным параметром (без учёта типа - тип учитывается вне этой функции)
        private void SetParameters(Entry entry, Parameter par)
        {
            entry.gradeR = par.nonFoilGrade;
            entry.gradeF = par.foilGrade;
            entry.sellPrice = par.nonFoilPrice;
            entry.buyPrice = par.foilPrice;
            entry.language = par.language;
            entry.dollarRate = par.dollarRate;
            entry.discount = par.discount;
            entry.comment = par.comment;
            entry.priority = par.priority;
            if (par.field != null)
            {
                if (par.field.GetValue(entry).GetType() == typeof(bool))
                {
                    bool val;
                    if (par.fieldValue.ToLower() == "true" || par.fieldValue.ToLower() == "1") val = true;
                    else if (par.fieldValue.ToLower() == "false" || par.fieldValue.ToLower() == "0") val = false;
                    else { errorDescription = "Failed to parse the value to bool: " + token + ". Check your Magic Album file."; return; }
                    par.field.SetValue(entry, val);
                }
                else if (par.field.GetValue(entry).GetType() == typeof(int))
                {
                    int val;
                    if (Int32.TryParse(par.fieldValue, out val)) par.field.SetValue(entry, val);
                    else { errorDescription = "Failed to parse the value to int: " + token + ". Check your Magic Album file."; return; }
                }
                else if (par.field.GetValue(entry).GetType() == typeof(float))
                {
                    float val;
                    if (float.TryParse(par.fieldValue, out val)) par.field.SetValue(entry, val);
                    else { errorDescription = "Failed to parse the value to float: " + token + ". Check your Magic Album file."; return; }
                }
                else if (par.field.GetValue(entry).GetType() == typeof(string))
                {
                    par.field.SetValue(entry, par.fieldValue);
                }
                else { errorDescription = "Very strange error: wrong typeof() in sorting. You should debug it. Also, check your Magic Album file."; return; }
            }
        }
        
        //разделяет карту на фойловые и не фойловые и добавляет их
        private void ParseFoil(Entry entry)
        {
            Entry nonFoilEntry = new Entry(entry);
            nonFoilEntry.qty = entry.qtyR;
            nonFoilEntry.grade = entry.gradeR;
            nonFoilEntry.price = entry.sellPrice;
            nonFoilEntry.comment = nonFoilEntry.notes;
            nonFoilEntry.notes = "";
            cardList.Add(nonFoilEntry);

            Entry foilEntry = new Entry();
            foilEntry = entry;
            foilEntry.foil = true;
            foilEntry.qty = entry.qtyF;
            foilEntry.grade = entry.gradeF;
            foilEntry.price = entry.buyPrice;
            foilEntry.comment = nonFoilEntry.notes;
            foilEntry.notes = "";
            cardList.Add(foilEntry);
        }

        private void RewriteQty(Entry entry)
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
            entry.comment = entry.notes;
            entry.notes = "";
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

        public static Database Merge(List<Database> dbs)
        {
            Database mergedDB = new Database();
            foreach (Database db in dbs)
            {
                foreach (Entry card in db.cardList)
                {
                    mergedDB.cardList.Add(card);
                }
            }
            return mergedDB;
        }
    }
}
