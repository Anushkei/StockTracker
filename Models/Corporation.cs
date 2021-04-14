using StockTracker.Networking;
using System.Data.SqlClient;
using static StockTracker.Models.Auxiliary;

namespace StockTracker.Models
{
    public class Corporation : IDataModel
    {
        //Declarations of the Corporation table attributes with get/set functions

        public string StockSymbol { get; set; }
        public string Name { get; set; }
        public decimal StockPrice { get; set; }
        public long SharesOwned { get; set; }
        public long TotalShares { get; set; }
        public decimal MarketCap { get; set; }
        public string MarketCapString => DecimalToDollarString(MarketCap);

        public string GetDeleteQuery()
        {
            throw new System.NotImplementedException();
        }

        public string GetInsertQuery()
        {
            throw new System.NotImplementedException();
        }

        public string GetUpdateQuery()
        {
            return string.Format("UPDATE dbo.[Corporation] SET [Name] {0}, [StockPrice] = {1}, [SharesOwned] = {2}, [TotalShares] = {3}  WHERE [StockSymbol] = {4};",
                GetSingleQuotes(this.Name),
                this.StockPrice,
                this.SharesOwned,
                this.TotalShares,
                GetSingleQuotes(this.StockSymbol));
        }

        public Corporation()
        {
            this.MarketCap = 0.00M;
            this.StockPrice = 0.00M;
        }

        public void ReadData(SqlDataReader reader)
        {
            this.StockSymbol = reader.GetString(reader.GetOrdinal("StockSymbol")).Trim();
            this.Name = reader.GetString(reader.GetOrdinal("Name"));
            this.StockPrice = reader.GetDecimal(reader.GetOrdinal("StockPrice"));
            this.SharesOwned = reader.GetInt64(reader.GetOrdinal("SharesOwned"));
            this.TotalShares = reader.GetInt64(reader.GetOrdinal("TotalShares"));
            this.MarketCap = StockPrice * TotalShares;
        }
    }
}
