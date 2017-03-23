using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicParser
{
    public class ParseCodeResult
    {
        public int type { get; private set; } //Error - 1, bool - 2, float - 3, string - 4
        public Error error { get; private set; }
        public bool boo { get; private set; }
        public float number { get; private set; }
        public string str { get; private set; }

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
            type = 3;
            this.number = fl;
        }
        public ParseCodeResult(string str)
        {
            type = 4;
            this.str = str;
        }
    }
}
