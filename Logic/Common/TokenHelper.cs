using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicUnit
{
    internal class TokenHelper
    {
        internal static string NewToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray()).TrimEnd('=');
        }

    }
}
