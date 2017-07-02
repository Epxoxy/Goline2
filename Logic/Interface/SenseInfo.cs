using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicUnit.Interface
{
    public interface SenseInfo
    {
        void ChangedActive(string token);
        void OnStart();
        void OnEnded();
        void OnModeChange();
    }
}
