using System;

namespace StockTracker.Models
{
    public static class Auxiliary
    {
        public static string DecimalToDollarString(decimal dec)
        {
            return "$" + (Math.Round(dec, 2));
        }

        public static string GetSingleQuotes(object obj) => "'" + obj.ToString() + "'";
    }
}
