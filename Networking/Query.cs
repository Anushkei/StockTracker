using System.Collections.Generic;

namespace StockTracker.Networking
{
    public class Query
    {
        public string Text { get; set; }
        public Dictionary<string, object> Parameters { get; set; }

        public Query(string text)
        {
            this.Text = text;
            this.Parameters = new Dictionary<string, object>();
        }
    }
}
