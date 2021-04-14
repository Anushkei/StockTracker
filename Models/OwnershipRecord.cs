using StockTracker.Networking;
using System;
using System.Data.SqlClient;
using static StockTracker.Models.Auxiliary;

namespace StockTracker.Models
{
    public class OwnershipRecord : IDataModel
    {
        //Declarations of the Ownership_Record table attributes with get/set functions

        public long AccountId { get; set; }
        public string StockSymbol { get; set; }        
        public long Quantity { get; set; }
        public decimal StockPrice { get; private set; }
        public string StockPriceString => DecimalToDollarString(StockPrice);
        public decimal CurrentValue { get; set; }
        public string CurrentValueString => DecimalToDollarString(CurrentValue);

        public string GetDeleteQuery()
        {
            return string.Format("DELETE FROM dbo.[Ownership_Record] WHERE [AccountId] = {0} AND [StockSymbol] = {1};",
                this.AccountId,
                GetSingleQuotes(this.StockSymbol));
        }

        public string GetInsertQuery()
        {
            return string.Format("INSERT INTO dbo.[Ownership_Record] ([AccountId], [StockSymbol], [Quantity]) VALUES ({0}, {1}, {2});",
                this.AccountId,
                GetSingleQuotes(this.StockSymbol),
                this.Quantity);
        }

        public string GetUpdateQuery()
        {
            return string.Format("UPDATE dbo.[Ownership_Record] SET [Quantity] = {0} WHERE [AccountId] = {1} AND [StockSymbol] = {2};",
                this.Quantity,
                this.AccountId,
                GetSingleQuotes(this.StockSymbol));
        }

        public void ReadData(SqlDataReader reader)
        {
            this.AccountId = reader.GetInt64(reader.GetOrdinal("AccountId"));
            this.StockSymbol = reader.GetString(reader.GetOrdinal("StockSymbol")).Trim();
            this.Quantity = reader.GetInt64(reader.GetOrdinal("Quantity"));
            this.CurrentValue = 0.00M;
            this.StockPrice = 0.00M;
        }

        internal void SetStockPrice(decimal stockPrice)
        {
            this.StockPrice = stockPrice;
            this.CurrentValue = (decimal)Quantity * stockPrice;
        }
    }
}
