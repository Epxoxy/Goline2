using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoLine
{
    public class SelectionItem<T>
    {
        public string Display { get; set; }
        public T Value { get; set; }
        public SelectionItem(string display, T value)
        {
            Display = display;
            Value = value;
        }
    }
}
