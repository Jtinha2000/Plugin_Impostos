using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bootleg___Taxes.Utils
{
    public static class TimeSpanExtension
    {
        public static string ConvertToUnturnedSpan(this TimeSpan Span)
        {
            if(Span.Days > 0)
            {
                return Span.ToString(@"dd\:hh\:mm\:ss");
            }
            if (Span.Hours > 0)
            {
                return Span.ToString(@"hh\:mm\:ss");
            }
            return Span.ToString(@"mm\:ss");
        }
    }
}
