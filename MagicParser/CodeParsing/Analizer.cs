using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static MagicParser.Database;

namespace MagicParser.CodeParsing
{
    public class Analizer
    {
        #region Fields
        public string input { get; private set; } //текст для разбора
        private string token { get; set; } //Текущий токен - для удобства доступа в своём поле
        
        private Dictionary<string, Database> dbs { get; set; } //Список баз данных по типу 'внутреннее имя БД' - 'ссылка на БД'
        public string errorDescription { get; private set; } //Сюда записывается описание ошибки; после вызова каждого метода обязательно нужно делать проверку, нет ли здесь информации, если есть - прекращать работу анализатора
        public Dictionary<string, string> keyWords { get; set; } //Текстовые представления всех токенов-ключевых слов
        public static int tokenizerLastErrorPos = 0; //Последняя позиция токенайзера в случае, когда парсер вернул ошибку
        #endregion

        //Конструктор
        public Analizer(string input)
        {
            this.input = input;
            token = null;
            dbs = new Dictionary<string, Database>();
            errorDescription = null;
            
            keyWords = new Dictionary<string, string>();
            keyWords.Add("declarationBeginToken", "dbs");
            keyWords.Add("declarationEndToken", "enddbs");
            keyWords.Add("listBeginToken", "list");
            keyWords.Add("listEndToken", "endlist");
            keyWords.Add("databasesToken", "dbs");
            keyWords.Add("parseCommentsToken", "parseComments");
            keyWords.Add("filterToken", "filter");
            keyWords.Add("groupingToken", "group");
            keyWords.Add("sortingToken", "sort");
            keyWords.Add("formattingToken", "format");
            
        }


        #region General methods
        
        //получаем токен
        private void GetToken(Tokenizer t)
        {
            token = t.GetToken();
        }

        //Следующий токен - получается без изменения позиции токенайзера
        private string GetNextToken(Tokenizer t)
        {
            return t.ForseeToken();
        }

        //Шорткаты для ошибок
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
            return output;
        }

        private string ErrorQuotes(string whatToDo)
        {
            return "Unexpected token: " + token + ". You must " + whatToDo + " in single quotes.";
        }


        #endregion


        #region Parser

        //основной метод. Возвращает готовую строку с данными. Все другие методы сделаны в соответствии с BNF.
        #region input
        //input = ([databasesDeclaration] [list] [freeText])*
        public string Parse()
        {
            Tokenizer t = new Tokenizer(input);
            #region ([databasesDeclaration] [list] [freeText])*
            string output = "";
            int lastPos = 0;
            //пока токенайзер не просмотрит весь инпут, выполняем
            while (!t.endIsReached)
            {
                #region [databasesDeclaration]
                //databasesDeclaration = declarationBeginToken declaration* declarationEndToken
                if (GetNextToken(t).ToLower() == keyWords["declarationBeginToken"].ToLower())
                {
                    output += t.GetWhiteSpaces();
                    GetToken(t);
                    DBDeclarations(t);
                    if (errorDescription != null) { tokenizerLastErrorPos = t.pos; return output; }
                    GetToken(t);
                    if (token.ToLower() != keyWords["declarationEndToken"].ToLower()) { errorDescription = ErrorExpected(keyWords["declarationEndToken"]); tokenizerLastErrorPos = t.pos; return output; }
                    lastPos = t.pos;
                }
                #endregion
                #region [list]
                //list = listBeginToken listParams listEndToken
                else if (GetNextToken(t).ToLower() == keyWords["listBeginToken"].ToLower())
                {
                    output += t.GetWhiteSpaces();
                    GetToken(t);
                    string result = ListParams(t);
                    if (errorDescription != null) { tokenizerLastErrorPos = t.pos; return output; }
                    output += result;
                    GetToken(t);
                    if (token.ToLower() != keyWords["listEndToken"].ToLower()) { errorDescription = ErrorExpected(keyWords["listEndToken"]); tokenizerLastErrorPos = t.pos; return output; }
                    lastPos = t.pos;
                }
                #endregion
                #region [freeText]
                //Иначе необходимо добавить весь текст (с пробелами) в аутпут
                //freeText = ?string that doesn't include codeBeginToken?
                else
                {
                    GetToken(t);
                    output += input.Substring(lastPos, t.pos - lastPos);
                    lastPos = t.pos;
                }
                #endregion
            }

            tokenizerLastErrorPos = 0;
            return output;
            #endregion
        }
        #endregion

        //declaration*
        //declaration = name '=' "'" path "'";
        private void DBDeclarations(Tokenizer t)
        {
            while (!t.endIsReached)
            {
                //name = ?regexp?
                if (Regex.IsMatch(GetNextToken(t), @"^[a-zA-Z_]\w+$"))
                {
                    GetToken(t);
                    string DBname = token.ToLower();
                    GetToken(t);
                    if (token == "=")
                    {
                        GetToken(t);
                        if (token == "'")
                        {
                            token = t.GetUntil('\'');
                            //path = ?regexp?;
                            if (Regex.IsMatch(token, @"^(?i)([a-z]+:)?[\\/]?([\\/].*)*[\\/]?(?-i)$"))
                            {
                                string DBpath = token;
                                dbs.Add(DBname, new Database(DBpath));
                                GetToken(t);
                                if (token != "'") errorDescription = ErrorQuotes("declare the path");
                                return;
                            }
                            else { errorDescription = "Wrong file path: " + token; return; }
                        }
                        else { errorDescription = ErrorQuotes("declare the path"); return; }
                    }
                    else { errorDescription = ErrorExpected("="); return; }
                }
                else if (GetNextToken(t).ToLower() == keyWords["declarationEndToken"]) return;
                else { GetToken(t); errorDescription = "Wrong Database name: " + token; return; }
            }
        }

        //listParams = databases [options] [filter] [[grouping] sorting] [formatting]
        private string ListParams(Tokenizer t)
        {
            List<Database> currentDBs = new List<Database>();
            GetToken(t);
            //databases = databasesToken '=' "'" dbsValue "'"
            if (token.ToLower() == keyWords["databasesToken"].ToLower())
            {
                GetToken(t);
                if (token == "=")
                {
                    GetToken(t);
                    if (token == "'")
                    {
                        //dbsValue = name+
                        do
                        {
                            GetToken(t);
                            if (dbs.ContainsKey(token.ToLower()))
                            {
                                currentDBs.Add(dbs[token.ToLower()]);
                            }
                            else { errorDescription = "Database with the name '" + token + "' doesn't exist. Declare it first."; return ""; }
                        }
                        while (!t.endIsReached && GetNextToken(t) != "'");
                        GetToken(t);
                        if (token != "'") { errorDescription = ErrorQuotes("define the names"); return ""; }
                    }
                    else { errorDescription = ErrorQuotes("define the names"); return ""; }
                }
                else { errorDescription = ErrorExpected("="); return ""; }
            }
            else { errorDescription = "You must choose databases you want to use in the listing."; return ""; }

            //[options] [filter] [[grouping] sorting] [formatting]
            //options = option+;
            //option = parseComments
            //parseComments = parseCommentsToken '=' bool
            if (GetNextToken(t).ToLower() == keyWords["parseCommentsToken"].ToLower())
            {
                GetToken(t);
                GetToken(t);
                if (token == "=")
                {
                    GetToken(t);
                    if (token.ToLower() == "true" || token == "1") foreach (Database db in currentDBs) db.notesAsIs = true;
                    else if (token.ToLower() == "false" || token == "0") foreach (Database db in currentDBs) db.notesAsIs = false;
                    else { errorDescription = ErrorExpected("Bool value", false); return ""; }
                }
                else { errorDescription = ErrorExpected("="); return ""; }
            }

            //После определения всех опций про парсинг парсим базы
            foreach (Database db in currentDBs)
            {
                bool ok = db.ReadFile();
                if (!ok) { errorDescription = "Can't read file: '" + db.fileName + "'. Make sure the file exists and it contains only text export from Magic Album"; return ""; }
            }
            //Затем сливаем их
            Database mergedDB = Merge(currentDBs);

            //[filter]
            if (GetNextToken(t).ToLower() == keyWords["filterToken"].ToLower())
            {
                Filter(t, mergedDB);
                if (errorDescription != null) return "";
            }

            //[grouping]
            List<List<string>> groupFields = new List<List<string>>();
            if (GetNextToken(t).ToLower() == keyWords["groupingToken"].ToLower())
            {
                groupFields = Group(t);
                if (errorDescription != null) return "";
            }

            //[sorting]
            if (GetNextToken(t).ToLower() == keyWords["sortingToken"].ToLower())
            {
                if (groupFields.Count != 0) Sort(t, mergedDB, groupFields);
                else Sort(t, mergedDB);
                
                if (errorDescription != null) return "";
            }

            //[formatting]
            string output = "";
            if (GetNextToken(t).ToLower() == keyWords["formattingToken"].ToLower())
            {
                output = Format(t, mergedDB);
                if (errorDescription != null) return output;
            }

            return output;
        }

        //filter = filterToken '=' "'" boolValue "'" ';'
        private void Filter(Tokenizer t, Database mergedDB)
        {
            GetToken(t);
            if (token == keyWords["filterToken"])
            {
                GetToken(t);
                if (token == "=")
                {
                    GetToken(t);
                    if (token == "'")
                    {
                        List<Entry> cardList = new List<Entry>(mergedDB.cardList);
                        foreach (Entry card in cardList)
                        {
                            Tokenizer T = new Tokenizer(t.input.Substring(t.pos));
                            bool delete = !BoolValue(T, card);
                            if (errorDescription != null) return;
                            if (delete) mergedDB.cardList.Remove(card);

                            GetToken(T);
                            if (token != "'") { errorDescription = ErrorQuotes("define the filter"); return; }

                            t.SetPos(T.pos + t.input.Length - T.input.Length);
                        }
                    }
                    else { errorDescription = ErrorQuotes("define the filter"); return; }
                }
                else { errorDescription = ErrorExpected("="); return; }
            }
            else { errorDescription = ErrorExpected(keyWords["filterToken"]); return; }


        }

        //grouping = groupingToken '=' "'" groupingValue "'"
        private List<List<string>> Group(Tokenizer t)
        {
            GetToken(t);
            if (token == keyWords["groupingToken"])
            {
                List<List<string>> groupFields = new List<List<string>>();
                GetToken(t);
                if (token == "=")
                {
                    //groupingValue = "'" field+ (',' field+)* "'"
                    GetToken(t);
                    if (token == "'")
                    {
                        //field+
                        //field = ?fieldName?
                        List<string> list = new List<string>();
                        do
                        {
                            GetToken(t);
                            //Проверяем существование поля с таким названием
                            FieldInfo field = typeof(Entry).GetField(token, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                            if (field != null) list.Add(token.ToLower());
                            else { errorDescription = "Wrong field name: " + token; return null; }
                        }
                        while (!t.endIsReached && GetNextToken(t) != "," && GetNextToken(t) != "'");
                        groupFields.Add(list);
                        while (!t.endIsReached && GetNextToken(t) != "'")
                        {
                            if (GetNextToken(t) == ",")
                            {
                                GetToken(t); //','
                                List<string> l = new List<string>();
                                do
                                {
                                    GetToken(t);
                                    if (token == "'") { errorDescription = ErrorExpected("Database name", false); return null; }
                                    //Проверяем существование поля с таким названием
                                    FieldInfo field = typeof(Entry).GetField(token, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                                    if (field != null) l.Add(token.ToLower());
                                    else { errorDescription = "Wrong field name: " + token; return null; }
                                }
                                while (!t.endIsReached && GetNextToken(t) != "," && GetNextToken(t) != "'");
                            }
                            else if (GetNextToken(t) != "'") { GetToken(t); errorDescription = ErrorExpected("'"); return null; }
                            groupFields.Add(list);
                        }
                        GetToken(t);
                        if (token != "'") { errorDescription = ErrorExpected("'"); return null; }
                    }
                    else { errorDescription = ErrorQuotes("define the grouping"); return null; }
                }
                else { errorDescription = ErrorExpected("="); return null; }
                return groupFields;
            }
            else { errorDescription = ErrorExpected(keyWords["groupingToken"]); return null; }
        }

        //sorting = sortingToken '=' "'" sortingValue "'"
        private void Sort(Tokenizer t, Database db, List<List<string>> groupFields = null)
        {
            GetToken(t);
            if (token == keyWords["sortingToken"])
            {
                GetToken(t);
                if (token == "=")
                {
                    GetToken(t);
                    if (token == "'")
                    {
                        //sortingValue = (['!'] field)+;
                        List<Tuple<string, bool>> fields = new List<Tuple<string, bool>>();
                        do
                        {
                            bool directOrder = true;
                            if (GetNextToken(t) == "!")
                            {
                                GetToken(t);
                                directOrder = false;
                            }

                            GetToken(t);
                            //Проверяем существование поля с таким названием
                            FieldInfo field = typeof(Entry).GetField(token, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                            if (field != null) fields.Add(new Tuple<string, bool>(token.ToLower(), directOrder));
                            else { errorDescription = "Wrong field name: " + token; return; }
                        }
                        while (!t.endIsReached && GetNextToken(t) != "'");

                        GetToken(t);
                        if (token != "'") { errorDescription = ErrorQuotes("define sorting"); return; }
                        
                        SortDB(db, fields);
                        if (errorDescription != null) return;
                        if (groupFields != null)
                        {
                            GiveGroupIDs(db, groupFields);
                            if (errorDescription != null) return;
                            fields.Insert(0, new Tuple<string, bool>("groupID", true));
                            SortDB(db, fields);
                            if (errorDescription != null) return;
                        }
                    }
                    else { errorDescription = ErrorQuotes("define sorting"); return; }
                }
                else { errorDescription = ErrorExpected("="); return; }
            }
            else { errorDescription = ErrorExpected(keyWords["sortingToken"]); return; }
        }

        //Выдаём айдишники для группировки отсортированному (!) списку карт. На вход подаётся список параметров группировки, каждый параметр является списком полей, по которым необходимо сгруппировать.
        private void GiveGroupIDs(Database sortedDB, List<List<string>> fieldNames)
        {
            List<Entry> cards = sortedDB.cardList;
            //Последний использованный айди
            int lastID = 0;
            //пробегаемся по каждому списку полей для группировки
            foreach (List<string> element in fieldNames)
            {
                //берём данные о реальных полях из имён и составляем список
                List<FieldInfo> fields = new List<FieldInfo>();
                foreach (string fieldName in element)
                {
                    FieldInfo field = typeof(Entry).GetField(fieldName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    if (field == null) { errorDescription = "Wrong value name: " + token; return; }
                    fields.Add(field);
                }

                //Проходимся по картам
                for (int i = 0; i < cards.Count; i++)
                {
                    //если карте айди не назначен - назначаем (иначе просто пропускаем)
                    if (cards[i].groupID == 0)
                    {
                        cards[i].groupID = lastID + 1;
                        lastID++;
                        //назначив айди, проходим по оставшемуся списку и в случае соответствия каждого поля приравняем айди
                        if (i + 1 < cards.Count)
                        {
                            for (int j = i + 1; j < cards.Count; j++)
                            {
                                bool addToGroup = true;
                                foreach (FieldInfo field in fields)
                                {
                                    object left = field.GetValue(cards[i]);
                                    object right = field.GetValue(cards[j]);
                                    if (!field.GetValue(cards[i]).Equals(field.GetValue(cards[j])))
                                    {
                                        addToGroup = false;
                                        break;
                                    }
                                }
                                if (addToGroup) cards[j].groupID = cards[i].groupID;
                            }
                        }
                    }
                }
            }
        }

        //Сортировка базы данных по указанным полям. В тупле содержится поле и порядок сортировки (true - прямой порядок, false - обратный)
        private void SortDB (Database db, List<Tuple<string, bool>> fieldNames)
        {
            db.cardList.Sort(delegate (Entry x, Entry y)
            {
                //Обходим филднеймы из списка по очереди, если по приоритетному параметру различаются - возвращаем, иначе переходим к следующему филднейму
                foreach (Tuple<string,bool> fieldName in fieldNames)
                {
                    FieldInfo field = typeof(Entry).GetField(fieldName.Item1, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    if (field != null)
                    {
                        if (field.GetValue(x).GetType() == typeof(bool))
                        {
                            bool xx = (bool)field.GetValue(x);
                            bool yy = (bool)field.GetValue(y);
                            if (xx && !yy && fieldName.Item2 || !xx && yy && !fieldName.Item2) return 1;
                            else if (!xx && yy && fieldName.Item2 || xx && !yy && !fieldName.Item2) return -1;
                        }
                        else if (field.GetValue(x).GetType() == typeof(int))
                        {
                            int xx = (int)field.GetValue(x);
                            int yy = (int)field.GetValue(y);
                            if (xx > yy && fieldName.Item2 || yy > xx && !fieldName.Item2) return 1;
                            else if (yy > xx && fieldName.Item2 || xx>yy && !fieldName.Item2) return -1;
                        }
                        else if (field.GetValue(x).GetType() == typeof(float))
                        {
                            float xx = (float)field.GetValue(x);
                            float yy = (float)field.GetValue(y);
                            if (xx > yy && fieldName.Item2 || yy > xx && !fieldName.Item2) return 1;
                            else if (yy > xx && fieldName.Item2 || xx > yy && !fieldName.Item2) return -1;
                        }
                        else if (field.GetValue(x).GetType() == typeof(string))
                        {
                            string xx = (string)field.GetValue(x);
                            string yy = (string)field.GetValue(y);
                            int comp = String.Compare(xx, yy);
                            if (comp > 0 && fieldName.Item2 || comp < 0 && !fieldName.Item2) return 1;
                            else if (comp < 0 && fieldName.Item2 || comp > 0 && !fieldName.Item2) return -1;
                        }
                        else { errorDescription = "Very strange error: wrong typeof() in sorting. You should debug it."; return 0; }
                    }
                    else { errorDescription = "Wrong value name: " + field; return 0; }
                }
                return 0;
            });
        }

        //formatting = formattingToken '=' "'" formattingValue "'"
        private string Format(Tokenizer t, Database db)
        {
            GetToken(t);
            if (token == keyWords["formattingToken"])
            {
                GetToken(t);
                if (token == "=")
                {
                    GetToken(t);
                    if (token == "'")
                    {
                        //Создаём выходную переменную
                        string output = "";
                        Tokenizer T = new Tokenizer(t.input.Substring(t.pos));
                        //Для каждой карты будем дописывать одну строку в аутпут
                        foreach (Entry card in db.cardList)
                        {
                            //Для каждой карты создаём свой токенайзер
                            T = new Tokenizer(t.input.Substring(t.pos));
                            //formattingValue = ([function] [field] [freeText])*;
                            while (!T.endIsReached && GetNextToken(T) != "'")
                            {
                                if (GetNextToken(T) == "$")
                                {
                                    output += T.GetWhiteSpaces();
                                    GetToken(T);
                                    //Дальше это либо функция, либо поле.
                                    if (GetNextToken(T) == "(")
                                    {
                                        Tuple<string, bool, string, float> result = EmptyFunction(T, card);
                                        if (errorDescription != null) return output;
                                        switch (result.Item1)
                                        {
                                            case "bool":
                                                output += result.Item2.ToString();
                                                break;
                                            case "string":
                                                output += result.Item3;
                                                break;
                                            case "number":
                                                output += result.Item4.ToString();
                                                break;
                                        }
                                    }
                                    else //либо поле, либо непустая функция
                                    {
                                        Tokenizer tempT = new Tokenizer(T.input.Substring(T.pos));
                                        string stringField = StringField(tempT, card);
                                        if (errorDescription != null)
                                        {
                                            tempT.SetPos(0);
                                            errorDescription = null;
                                            float numberField = NumberField(tempT, card);
                                            if (errorDescription != null)
                                            {
                                                tempT.SetPos(0);
                                                errorDescription = null;
                                                bool boolField = BoolField(tempT, card);
                                                if (errorDescription != null)
                                                {
                                                    tempT.SetPos(0);
                                                    errorDescription = null;
                                                    string stringResult = StringFunction(tempT, card);
                                                    if (errorDescription != null)
                                                    {
                                                        tempT.SetPos(0);
                                                        string stringErrorDescription = errorDescription;
                                                        errorDescription = null;
                                                        float numberResult = NumberFunction(tempT, card);
                                                        if (errorDescription != null)
                                                        {
                                                            tempT.SetPos(0);
                                                            string numberErrorDescription = errorDescription;
                                                            errorDescription = null;
                                                            bool boolResult = BoolFunction(tempT, card);
                                                            if (errorDescription != null) { errorDescription = "Parsing failed. Wrong field name or another error.\r\nParsing as string ended with an error:\r\n" + stringErrorDescription + "\r\nParsing as number ended with an error:\r\n" + numberErrorDescription + "\r\nParsing as bool ended with an error:\r\n" + errorDescription; return output; }
                                                            else output += boolResult;
                                                        }
                                                        else output += numberResult;
                                                    }
                                                    else output += stringResult;
                                                }
                                                else output += boolField;
                                            }
                                            else output += numberField;
                                        }
                                        else output += stringField;

                                        T.SetPos(tempT.pos + T.input.Length - tempT.input.Length);
                                    }
                                }
                                else output += T.GetSymbol();
                            }
                            //после того, как закончили парсить формулу - переводим строку, переходим к следующей карте, для которой заново будем парсить формулу
                            output += "\r\n";
                        }
                        t.SetPos(T.pos + t.input.Length - T.input.Length);
                        GetToken(t);
                        if (token != "'") { errorDescription = ErrorQuotes("define formatting"); return output; }
                        return output;
                    }
                    else { errorDescription = ErrorQuotes("define formatting"); return ""; }
                }
                else { errorDescription = ErrorExpected("="); return ""; }
            }
            else { errorDescription = ErrorExpected(keyWords["formattingToken"]); return ""; }
        }

        #endregion

        #region Operators

        //value = boolValue | numberValue | stringValue
        private Tuple<string, bool, string, float> Value(Tokenizer t, Entry card)
        {
            Tokenizer T = new Tokenizer(t.input.Substring(t.pos));
            Tuple<string, bool, string, float> output;
            bool boolResult = BoolValue(T, card);
            if (errorDescription != null)
            {
                T.SetPos(0);
                string boolErrorDescription = errorDescription;
                errorDescription = null;
                float numberResult = NumberValue(T, card);
                if (errorDescription != null)
                {
                    T.SetPos(0);
                    string numberErrorDescription = errorDescription;
                    errorDescription = null;
                    string stringResult = StringValue(T, card);
                    if (errorDescription != null) { errorDescription = "Parsing failed. Wrong field name or another error.\r\nParsing as bool ended with an error:\r\n" + boolErrorDescription + "\r\nParsing as number ended with an error:\r\n" + numberErrorDescription + "\r\nParsing as string ended with an error:\r\n" + errorDescription; return new Tuple<string, bool, string, float>("error", false, null, 0); }
                    else output = new Tuple<string, bool, string, float>("string", false, stringResult, 0);
                }
                else output = new Tuple<string, bool, string, float>("number", false, null, numberResult);
            }
            else output = new Tuple<string, bool, string, float>("bool", boolResult, null, 0);
            t.SetPos(T.pos + t.input.Length - T.input.Length);
            return output;
        }

        //emptyFunction = brackets
        //brackets = boolBrackets | numberBrackets | stringBrackets; (* '(' value ')' *)
        //brackets = '(' value ')'
        private Tuple<string, bool, string, float> EmptyFunction(Tokenizer t, Entry card)
        {
            GetToken(t);
            if (token == "(")
            {
                Tuple<string, bool, string, float> output = Value(t, card);
                if (errorDescription != null) return output;
                GetToken(t);
                if (token == ")") return output;
                else { errorDescription = ErrorExpected(")"); return output; }
            }
            else { errorDescription = ErrorExpected("("); return new Tuple<string, bool, string, float>("error", false, null, 0); }
        }

        #region Bool operators

        //boolValue = or
        //or = and (('|') and)*
        private bool BoolValue(Tokenizer t, Entry card)
        {
            bool left = And(t, card);
            if (errorDescription != null) return left;
            while (GetNextToken(t) == "|")
            {
                GetToken(t); //'|'
                bool right = And(t, card);
                left = left || right;
            }
            return left;
        }

        //and = equality (('&') equality)*
        private bool And(Tokenizer t, Entry card)
        {
            bool left = Equality(t, card);
            if (errorDescription != null) return left;
            while (GetNextToken(t) == "&")
            {
                GetToken(t);
                bool right = Equality(t, card);
                if (errorDescription != null) return left && right;
                left = left && right;
            }
            return left;
        }

        //equality = (comparsion (('=' | '!=') comparsion)*) | (numberValue (('=' | '!=') numberValue)+) | (stringValue (('=' | '!=') stringValue)+)
        private bool Equality(Tokenizer t, Entry card)
        {
            //создаём отдельный токенайзер
            Tokenizer T = new Tokenizer(t.input.Substring(t.pos));
            bool output = Comparsion(T, card);
            //если парсинг как сущность boolValue НЕ выдаёт ошибку - ищем дальше связки ('=' | '!=') comparsion.
            if (errorDescription == null)
            {
                while (GetNextToken(T) == "=" || GetNextToken(T) == "!=")
                {
                    GetToken(T);
                    if (GetNextToken(T) == "=")
                    {
                        bool boolRight = Comparsion(T, card);
                        output = output == boolRight;
                    }
                    else
                    {
                        bool boolRight = Comparsion(T, card);
                        output = output != boolRight;
                    }
                    if (errorDescription != null) return output;
                }
            }
            //если парсинг как сущность comparsion выдаёт ошибку - пытаемся парсить как сущность numberValue
            else
            {
                T.SetPos(0);
                string boolErrorDescription = errorDescription;
                errorDescription = null;
                float numberLeft = NumberValue(T, card);
                //если парсинг как сущность numberValue НЕ выдаёт ошибку - ищем дальше связки ('=' | '!=') numberValue. Должна быть хотя бы одна такая связка.
                if (errorDescription == null)
                {
                    do
                    {
                        GetToken(T);
                        if (token == "=")
                        {
                            float numberRight = NumberValue(T, card);
                            output = numberLeft == numberRight;
                            numberLeft = numberRight;
                        }
                        else if (token == "!=")
                        {
                            float numberRight = NumberValue(T, card);
                            output = numberLeft != numberRight;
                            numberLeft = numberRight;
                        }
                        else { errorDescription = ErrorExpected("'=' or '!='", false); return output; }
                        if (errorDescription != null) return output;
                    }
                    while (GetNextToken(T) == "=" || GetNextToken(T) == "!=");
                }
                //если парсинг как сущность numberValue выдаёт ошибку - пытаемся парсить как сущность stringValue
                else
                {
                    T.SetPos(0);
                    string numberErrorDescription = errorDescription;
                    errorDescription = null;
                    string stringLeft = StringValue(T, card);
                    //если парсинг как сущность stringValue НЕ выдаёт ошибку - ищем дальше связки ('=' | '!=') stringValue. Должна быть хотя бы одна такая связка.
                    if (errorDescription == null)
                    {
                        do
                        {
                            GetToken(T);
                            if (token == "=")
                            {
                                string stringRight = StringValue(T, card);
                                output = stringLeft.ToLower() == stringRight.ToLower();
                                stringLeft = stringRight;
                            }
                            else if (token == "!=")
                            {
                                string stringRight = StringValue(T, card);
                                output = stringLeft.ToLower() != stringRight.ToLower();
                                stringLeft = stringRight;
                            }
                            else { errorDescription = ErrorExpected("'=' or '!='", false); return output; }
                            if (errorDescription != null) return output;
                        }
                        while (GetNextToken(T) == "=" || GetNextToken(T) == "!=");
                    }
                    else { errorDescription = "Parsing failed.\r\nParsing as bool ended with an error:\r\n" + boolErrorDescription + "\r\nParsing as number ended with an error:\r\n" + numberErrorDescription + "\r\nParsing as string ended with an error:\r\n" + errorDescription; return output; }
                }
            }
            t.SetPos(T.pos + t.input.Length - T.input.Length);
            return output;
        }

        //comparsion = boolArg | numberValue (('>' | '<' | '>=' | '<=') numberValue)+
        private bool Comparsion(Tokenizer t, Entry card)
        {
            //создаём отдельный токенайзер
            Tokenizer T = new Tokenizer(t.input.Substring(t.pos));
            bool output = BoolArg(T, card);
            //если парсинг как сущность boolArg выдаёт ошибку - пытаемся парсить как сущность numberValue
            if (errorDescription != null)
            {
                T.SetPos(0);
                string boolValueErrorDescription = errorDescription;
                errorDescription = null;
                float numberLeft = NumberValue(T, card);
                //если парсинг как сущность boolValue НЕ выдаёт ошибку - ищем дальше связки ('>' | '>=' | '<' | '<=') boolValue . Должна быть хотя бы одна такая связка.
                if (errorDescription == null)
                {
                    do
                    {
                        GetToken(T);
                        
                        if (token == ">")
                        {
                            float numberRight = NumberValue(T, card);
                            output = numberLeft > numberRight;
                            numberLeft = numberRight;
                        }
                        else if (token == ">=")
                        {
                            float numberRight = NumberValue(T, card);
                            output = numberLeft >= numberRight;
                            numberLeft = numberRight;
                        }
                        else if (token == "<")
                        {
                            float numberRight = NumberValue(T, card);
                            output = numberLeft < numberRight;
                            numberLeft = numberRight;
                        }
                        else if (token == "<=")
                        {
                            float numberRight = NumberValue(T, card);
                            output = numberLeft <= numberRight;
                            numberLeft = numberRight;
                        }
                        else { errorDescription = ErrorExpected("'>', '>=', '<', or '<='", false); return output; }
                        if (errorDescription != null) return output;
                    }
                    while (GetNextToken(T) == ">" || GetNextToken(T) == ">=" || GetNextToken(T) == "<" || GetNextToken(T) == "<=");
                }
                else { errorDescription = "Parsing failed.\r\nParsing as bool ended with an error:\r\n" + boolValueErrorDescription + "\r\nParsing as number ended with an error:\r\n" + errorDescription; return output; }
            }
            //если парсинг как сущность boolArg НЕ выдаёт ошибку - возвращаем полученное значение
            t.SetPos(T.pos + t.input.Length - T.input.Length);
            return output;
        }

        //boolArg = ['!'] ('$' (boolFunction | boolField)) | boolBrackets |  bool
        private bool BoolArg(Tokenizer t, Entry card)
        {
            bool reverseOutput = false;
            bool output;
            if (GetNextToken(t) == "!") reverseOutput = true;

            if (GetNextToken(t) == "$")
            {
                GetToken(t);

                //Дальше это либо поле, либо непустая функция.
                Tokenizer T = new Tokenizer(t.input.Substring(t.pos));
                output = BoolField(T, card);
                if (errorDescription != null)
                {
                    T.SetPos(0);
                    errorDescription = null;
                    bool boolResult = BoolFunction(T, card);
                    output = boolResult;
                    if (errorDescription != null) { errorDescription = "Parsing failed. Wrong field or function name.\r\nParsing as bool ended with an error:\r\n" + errorDescription; return reverseOutput != output; }
                }
                t.SetPos(T.pos + t.input.Length - T.input.Length);
            }
            else if (GetNextToken(t) == "(")
            {
                output = BoolBrackets(t, card);
                if (errorDescription != null) return reverseOutput != output;
            }
            //bool = 'true' | 'false'
            else if (GetNextToken(t).ToLower() == "true") { GetToken(t); output = true; }
            else if (GetNextToken(t).ToLower() == "false") { GetToken(t); output = false; }
            else { GetToken(t); errorDescription = ErrorExpected(); return false; }

            return reverseOutput != output;
        }

        //boolFunction = contains
        private bool BoolFunction(Tokenizer t, Entry card)
        {
            bool output;
            if (GetNextToken(t).ToLower() == "contains") output = Contains(t, card);
            else { GetToken(t); errorDescription = "Unknown function: " + token; output = false; }
            return output;
        }
        
        //contains = 'contains' '(' stringArg ',' stringArg ')'
        private bool Contains(Tokenizer t, Entry card)
        {
            GetToken(t);
            if (token.ToLower() == "contains")
            {
                GetToken(t);
                if (token == "(")
                {
                    string firstArg = StringArg(t, card);
                    if (errorDescription != null) return false;
                    GetToken(t);
                    if (token == ",")
                    {
                        bool output;
                        string secondArg = StringArg(t, card);
                        output = firstArg.ToLower().Contains(secondArg.ToLower());
                        if (errorDescription != null) return output;

                        GetToken(t);
                        if (token == ")") return output;
                        else { errorDescription = ErrorExpected(")"); return output; }
                    }
                    else { errorDescription = "'contains' function has two inputs: 'first' contains 'second'."; return false; }
                }
                else { errorDescription = ErrorExpected("("); return false; }
            }
            else { errorDescription = ErrorExpected("contains"); return false; }
        }

        //boolBrackets = '(' boolValue ')'
        private bool BoolBrackets(Tokenizer t, Entry card)
        {
            GetToken(t);
            if (token == "(")
            {
                bool output = BoolValue(t, card);
                if (errorDescription != null) return output;
                GetToken(t);
                if (token == ")") errorDescription = ErrorExpected(")");
                return output;
            }
            else { errorDescription = ErrorExpected("("); return false; }
        }

        //boolField = ?boolField?
        private bool BoolField(Tokenizer t, Entry card)
        {
            bool output;
            GetToken(t);
            switch (token.ToLower())
            {
                case "foil":
                    output = card.foil; break;
                case "bothfoilandnonfoil":
                    output = card.bothFoilAndNonFoil; break;
                default:
                    errorDescription = "Wrong value name: " + token; output = false; break;
            }
            return output;
        }

        #endregion

        #region Number operators

        //numberValue = sum;
        //sum = composition (('+' | '-') composition)*;
        private float NumberValue(Tokenizer t, Entry card)
        {
            float left = Composition(t, card);
            if (errorDescription != null) return left;
            while (GetNextToken(t) == "+" || GetNextToken(t) == "-")
            {
                GetToken(t);
                if (token == "+")
                {
                    float right = Composition(t, card);
                    left = left + right;
                }
                else
                {
                    float right = Composition(t, card);
                    left = left - right;
                }
            }
            return left;
        }

        //composition = numberArg (('*' | '/') numberArg)*
        private float Composition(Tokenizer t, Entry card)
        {
            float left = NumberArg(t, card);
            if (errorDescription != null) return left;
            while (GetNextToken(t) == "*" || GetNextToken(t) == "/")
            {
                GetToken(t);
                if (token == "*")
                {
                    float right = NumberArg(t, card);
                    left = left * right;
                }
                else
                {
                    float right = NumberArg(t, card);
                    left = left / right;
                }
            }
            return left;
        }

        //numberArg = ['-'] ('$' (numberFunction | numberField)) | numberBrackets |  positiveNumber
        private float NumberArg(Tokenizer t, Entry card)
        {
            bool reverseOutput = false;
            float output;
            if (GetNextToken(t) == "-") reverseOutput = true;

            if (GetNextToken(t) == "$")
            {
                GetToken(t);

                //Дальше это либо поле, либо непустая функция.
                Tokenizer T = new Tokenizer(t.input.Substring(t.pos));
                output = NumberField(T, card);
                if (errorDescription != null)
                {
                    T.SetPos(0);
                    errorDescription = null;
                    float numberResult = NumberFunction(T, card);
                    output = numberResult;
                    if (errorDescription != null) { errorDescription = "Parsing failed. Wrong field or function name.\r\nParsing as number ended with an error:\r\n" + errorDescription; return reverseOutput ? -output : output; }
                }
                t.SetPos(T.pos + t.input.Length - T.input.Length);
            }
            else if (GetNextToken(t) == "(")
            {
                output = NumberBrackets(t, card);
                if (errorDescription != null) return reverseOutput ? -output : output;
            }
            //positiveNumber = positiveInteger ['.' positiveInteger];
            //positiveInteger = digit+;
            //digit = ?digit?;
            else if (float.TryParse(GetNextToken(t).Replace('.', ','), out output)) GetToken(t);
            else { errorDescription = ErrorExpected(); return reverseOutput ? -output : output; }
            
            return reverseOutput ? -output : output;
        }

        //numberFunction = numberIf
        private float NumberFunction(Tokenizer t, Entry card)
        {
            float output;
            if (GetNextToken(t).ToLower() == "if") output = NumberIf(t, card);
            else { GetToken(t); errorDescription = "Unknown function: " + token; output = 0; }
            return output;
        }

        //numberIf = 'if' '(' boolValue ',' numberValue [',' numberValue] ')'
        private float NumberIf(Tokenizer t, Entry card)
        {
            GetToken(t);
            if (token.ToLower() == "if")
            {
                GetToken(t);
                if (token == "(")
                {
                    bool condition = BoolValue(t, card);
                    if (errorDescription != null) return 0;
                    GetToken(t);
                    if (token == ",")
                    {
                        float output;
                        float firstArg = NumberValue(t, card);
                        if (errorDescription != null) return 0;
                        float secondArg;
                        if (GetNextToken(t) == ",")
                        {
                            GetToken(t);
                            secondArg = NumberValue(t, card);
                        }
                        else secondArg = 0;
                        output = condition ? firstArg : secondArg;
                        if (errorDescription != null) return output;
                        GetToken(t);
                        if (token != ")") errorDescription = ErrorExpected(")");
                        return output;
                    }
                    else { errorDescription = "'if' function has two or three inputs: if 'first' is true, return 'second', else - if 'third' is defined, return 'third', else return 0."; return 0; }
                }
                else { errorDescription = ErrorExpected("("); return 0; }
            }
            else { errorDescription = ErrorExpected("if"); return 0; }
        }

        //numberBrackets = '(' numberValue ')'
        private float NumberBrackets(Tokenizer t, Entry card)
        {
            GetToken(t);
            if (token == "(")
            {
                float output = NumberValue(t, card);
                if (errorDescription != null) return output;
                GetToken(t);
                if (token != ")") errorDescription = ErrorExpected(")");
                return output;
            }
            else { errorDescription = ErrorExpected("("); return 0; }
        }

        //numberField = ?numberField?
        private float NumberField(Tokenizer t, Entry card)
        {
            float output;
            GetToken(t);
            switch (token.ToLower())
            {
                case "buyprice":
                    output = card.buyPrice; break;
                case "buyqty":
                    output = card.buyQty; break;
                case "pricef":
                    output = card.priceF; break;
                case "pricer":
                    output = card.priceR; break;
                case "proxies":
                    output = card.proxies; break;
                case "qtyf":
                    output = card.qtyF; break;
                case "qtyr":
                    output = card.qtyR; break;
                case "rating":
                    output = card.rating; break;
                case "sellprice":
                    output = card.sellPrice; break;
                case "sellqty":
                    output = card.sellQty; break;
                case "used":
                    output = card.used; break;
                case "qty":
                    output = card.qty; break;
                case "dollarrate":
                    output = card.dollarRate; break;
                case "discount":
                    output = card.discount; break;
                case "price":
                    output = card.price; break;
                case "priority":
                    output = card.priority; break;
                case "groupid":
                    output = card.groupID; break;
                default:
                    errorDescription = "Wrong value name: " + token; output = 0; break;
            }
            return output;
        }

        #endregion

        #region String operators

        //stringValue = stringSum
        //stringSum = stringArg ('+' stringArg)*
        private string StringValue(Tokenizer t, Entry card)
        {
            string left = StringArg(t, card);
            if (errorDescription != null) return left;
            while (GetNextToken(t) == "+")
            {
                GetToken(t);
                string right = StringArg(t, card);
                left = left + right;
            }
            return left;
        }

        //stringArg = ('$' (stringFunction | stringField)) | stringBrackets | string
        private string StringArg(Tokenizer t, Entry card)
        {
            string output;
            
            if (GetNextToken(t) == "$")
            {
                GetToken(t);

                //Дальше это либо поле, либо непустая функция.
                Tokenizer T = new Tokenizer(t.input.Substring(t.pos));
                output = StringField(T, card);
                if (errorDescription != null)
                {
                    T.SetPos(0);
                    errorDescription = null;
                    string stringResult = StringFunction(T, card);
                    output = stringResult;
                    if (errorDescription != null) { errorDescription = "Parsing failed. Wrong field or function name.\r\nParsing as string ended with an error:\r\n" + errorDescription; return output; }
                }
                t.SetPos(T.pos + t.input.Length - T.input.Length);
            }
            else if (GetNextToken(t) == "(")
            {
                output = StringBrackets(t, card);
                if (errorDescription != null) return output;
            }
            else if (GetNextToken(t) == "\"")
            {
                GetToken(t);
                output = t.GetUntil('"');
                GetToken(t);
                if (token != "\"") { errorDescription = ErrorExpected("\""); }
            }
            else { errorDescription = ErrorExpected(); return ""; } 

            return output;
        }

        //stringFunction = stringIf | toString
        private string StringFunction(Tokenizer t, Entry card)
        {
            string output;
            if (GetNextToken(t).ToLower() == "if") output = StringIf(t, card);
            else if (GetNextToken(t).ToLower() == "tostring") output = ToString(t, card);
            else { GetToken(t); errorDescription = "Unknown function: " + token; output = ""; }
            return output;
        }

        //stringIf = 'if' '(' boolValue ',' stringValue [',' stringValue] ')'
        private string StringIf(Tokenizer t, Entry card)
        {
            GetToken(t);
            if (token.ToLower() == "if")
            {
                GetToken(t);
                if (token == "(")
                {
                    bool condition = BoolValue(t, card);
                    if (errorDescription != null) return "";
                    GetToken(t);
                    if (token == ",")
                    {
                        string output;
                        string firstArg = StringValue(t, card);
                        if (errorDescription != null) return "";
                        string secondArg;
                        if (GetNextToken(t) == ",")
                        {
                            GetToken(t);
                            secondArg = StringValue(t, card);
                        }
                        else secondArg = "";
                        output = condition ? firstArg : secondArg;
                        if (errorDescription != null) return output;
                        GetToken(t);
                        if (token != ")") errorDescription = ErrorExpected(")");
                        return output;
                    }
                    else { errorDescription = "'if' function has two or three inputs: if 'first' is true, return 'second', else - if 'third' is defined, return 'third', else return 0."; return ""; }
                }
                else { errorDescription = ErrorExpected("("); return ""; }
            }
            else { errorDescription = ErrorExpected("if"); return ""; }
        }

        //toString = 'toString' '(' value ')'
        private string ToString(Tokenizer t, Entry card)
        {
            GetToken(t);
            if (token.ToLower() == "tostring")
            {
                GetToken(t);
                if (token == "(")
                {
                    string output = "";
                    Tuple<string, bool, string, float> result = Value(t, card);
                    switch (result.Item1)
                    {
                        case "bool":
                            output = result.Item2.ToString();
                            break;
                        case "string":
                            output = result.Item3;
                            break;
                        case "number":
                            output = result.Item4.ToString();
                            break;
                    }

                    GetToken(t);
                    if (token != ")") errorDescription = ErrorExpected(")");
                    return output;
                }
                else { errorDescription = ErrorExpected("("); return ""; }
            }
            else { errorDescription = ErrorExpected("toString"); return ""; }
            
        }

        //stringBrackets = '(' stringValue ')'
        private string StringBrackets(Tokenizer t, Entry card)
        {
            GetToken(t);
            if (token == "(")
            {
                string output = StringValue(t, card);
                if (errorDescription != null) return output;
                GetToken(t);
                if (token != ")") errorDescription = ErrorExpected(")");
                return output;
            }
            else { errorDescription = ErrorExpected("("); return ""; }
        }

        //stringField = ?stringField?
        private string StringField(Tokenizer t, Entry card)
        {
            string output;
            GetToken(t);
            switch (token.ToLower())
            {
                case "artist":
                    output = card.artist; break;
                case "border":
                    output =  card.border; break;
                case "color":
                    output =  card.color; break;
                case "copyright":
                    output =  card.copyright; break;
                case "cost":
                    output =  card.cost; break;
                case "gradef":
                    output =  card.gradeF; break;
                case "grader":
                    output =  card.gradeR; break;
                case "language":
                    output =  card.language; break;
                case "legality":
                    output =  card.legality; break;
                case "name":
                    output =  card.name; break;
                case "nameoracle":
                    output =  card.nameOracle; break;
                case "notes":
                    output =  card.notes; break;
                case "number":
                    output =  card.number; break;
                case "pt":
                    output =  card.pt; break;
                case "rarity":
                    output =  card.rarity; break;
                case "set":
                    output =  card.set; break;
                case "text":
                    output =  card.text; break;
                case "textoracle":
                    output =  card.textOracle; break;
                case "type":
                    output =  card.type; break;
                case "typeoracle":
                    output =  card.typeOracle; break;
                case "version":
                    output =  card.version; break;
                case "comment":
                    output =  card.comment; break;
                case "grade":
                    output =  card.grade; break;
                default: errorDescription = "Wrong value name: " + token; output = ""; break;
            }
            return output;
        }

        #endregion

        #endregion

        //Разукрашиваем токены в кейворде
        public void Paint(System.Windows.Forms.RichTextBox box)
        {
            int initPos = box.SelectionStart;
            int initLength = box.SelectionLength;
            box.SelectAll();
            box.SelectionColor = Color.Black;
            box.ForeColor = Color.Black;
            Tokenizer t = new Tokenizer(input);

            while (!t.endIsReached)
            {
                bool got = false;
                GetToken(t);
                foreach (KeyValuePair<string, string> pair in keyWords)
                {
                    if (token.ToLower() == pair.Value.ToLower())
                    {
                        got = true;
                        if (Char.IsWhiteSpace(t.input[t.pos - 1])) { t.SetPos(t.pos - 1); }
                        box.Select(t.pos - token.Length, token.Length);
                        box.SelectionColor = Color.Blue;
                    }
                }
                if (got) continue;
                if (token == "\"")
                {
                    got = true;
                    token = t.GetUntil('"');
                    box.Select(t.pos - token.Length - 1, token.Length + 2);
                    box.SelectionColor = Color.FromArgb(163, 21, 21);
                    GetToken(t);
                }
                if (got) continue;
                if (token == "$")
                {
                    got = true;
                    GetToken(t);
                    if (token == "(")
                    {
                        box.Select(t.pos - 2, 1);
                        box.SelectionColor = Color.FromArgb(43, 145, 175);
                    }
                    else if (token.ToLower() == "if")
                    {
                        GetToken(t);
                        if (token == "(")
                        {
                            box.Select(t.pos - 4, 3);
                            box.SelectionColor = Color.FromArgb(43, 145, 175);
                        }
                    }
                    else if (token.ToLower() == "contains")
                    {
                        box.Select(t.pos - 9, 9);
                        box.SelectionColor = Color.FromArgb(43, 145, 175);
                    }
                    else if (token.ToLower() == "tostring")
                    {
                        box.Select(t.pos - 9, 9);
                        box.SelectionColor = Color.FromArgb(43, 145, 175);
                    }
                    else if (token != "|" && token != "&" && token != "=" && token != "+" && token != "-" && token != "*" && token != "/" && token != ")" && token != "," && token != "$" && token != "!=" && token != ">" && token != "<" && token != ">=" && token != "<=" && token != "'" && token != "\"" && token != "")
                    {
                        if (Char.IsWhiteSpace(t.input[t.pos - 1])) { t.SetPos(t.pos - 1); }
                        box.Select(t.pos - token.Length - 1, token.Length + 1);
                        box.SelectionColor = Color.OrangeRed;
                    }
                }
            }
            box.SelectionStart = initPos;
            box.SelectionLength = initLength;
            box.SelectionColor = Color.Black;
        }

    }
}
