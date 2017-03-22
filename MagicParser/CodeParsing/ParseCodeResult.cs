using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicParser
{
    public class ParseCodeResult
    {
        public int type = 0; //Error - 1, bool - 2, float - 3, string - 4
        public Error error;
        public bool boo;
        public float number;
        public string str;

        public ParseCodeResult(Error error)
        {
            type = 1;
            this.error = error;
        }
        public ParseCodeResult(bool boo)
        {
            type = 2;
            this.boo = boo;
        }
        public ParseCodeResult(float fl)
        {
            type = 4;
            this.number = fl;
        }
        public ParseCodeResult(string str)
        {
            type = 1;
            this.str = str;
        }
    }
}
