using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicParser
{
    public class Error
    {
        public string Message { get; set; }
        public Error (string errorMessage)
        {
            this.Message = errorMessage;
        }
    }
}
