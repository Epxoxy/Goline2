using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goline2
{
    class ConsoleNotifier : GameLogic.IMessageNotifier
    {
        public void Notify(string title, string content)
        {
            var builder = new StringBuilder();
            builder.AppendLine("---------------------| - o x |");
            builder.AppendLine(title);
            builder.AppendLine(content);
            builder.AppendLine("--------------------------------");
            System.Diagnostics.Debug.WriteLine(builder.ToString());
        }
    }
}
