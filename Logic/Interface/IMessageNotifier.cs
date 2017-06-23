using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicUnit.Interface
{
    public interface IMessageNotifier
    {
        void Notify(string title, string content);
    }
}
