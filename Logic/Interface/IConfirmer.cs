﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameLogic
{
    public interface IConfirmer<T>
    {
        bool Confirm(T data);
    }
}
