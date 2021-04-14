using StockTracker.Networking;
using System;
using System.Data.SqlClient;
using static StockTracker.Models.Auxiliary;

namespace StockTracker.Models
{
    public class TransactionLog : IDataModel
    {
        //Declarations of the Transaction_Log table attributes with get/set functions

        public long TransactionId { get; set; }
        public DateTime Timestamp { get; set; }
        public long SellerAccountId { get; set; }
        public long BuyerAccountId { get; set; }
        public string StockSymbol { get; set; }
        public decimal StockPrice { get; set; }
        public long Quantity { get; set; }
        public decimal FinalPrice { get; set; }
        public string FinalPriceString => DecimalToDollarString(FinalPrice);
        public string TransactionString
        {
            get
            {
                return string.Format("{0}: #{1} bought {2} share(s) of {3} at {4} from {5}",
                    Timestamp.ToString("yyyy-MM-dd hh:mm:ss"),
                    this.BuyerAccountId,
                    this.Quantity,
                    this.StockSymbol,
                    DecimalToDollarString(this.StockPrice),
                    this.GetSellerString());
            }
        }

        private string GetSellerString()
        {
            if ((this.SellerAccountId <= 0) && !string.IsNullOrEmpty(this.StockSymbol))
            {
                return "corporation " + this.StockSymbol;
            }
            else
            {
                return "#" + this.SellerAccountId.ToString();
            }
        }

        string IDataModel.GetInsertQuery()
        {
            return string.Format("INSERT INTO dbo.[Transaction_Log] ([SellerAccountId], [BuyerAccountId], [StockSymbol], [StockPrice], [Quantity]) VALUES ({0}, {1}, {2}, {3}, {4});",
                this.SellerAccountId,
                this.BuyerAccountId,
                GetSingleQuotes(this.StockSymbol),
                this.StockPrice,
                this.Quantity);
        }

        // Not implemented because transaction logs shouldn't be updated! They're permanent.
        string IDataModel.GetUpdateQuery()
        {
            throw new NotImplementedException();
        }

        // Not implemented because transaction logs shouldn't be deleted! They're permanent.
        string IDataModel.GetDeleteQuery()
        {
            throw new NotImplementedException();
        }

        public void ReadData(SqlDataReader reader)
        {
            this.TransactionId = reader.GetInt64(reader.GetOrdinal("TransactionId"));
            this.Timestamp = reader.GetDateTime(reader.GetOrdinal("Timestamp"));
            this.SellerAccountId = reader.GetInt64(reader.GetOrdinal("SellerAccountId"));
            this.BuyerAccountId = reader.GetInt64(reader.GetOrdinal("BuyerAccountId"));
            this.StockSymbol = reader.GetString(reader.GetOrdinal("StockSymbol"));
            this.StockPrice = reader.GetDecimal(reader.GetOrdinal("StockPrice"));
            this.Quantity = reader.GetInt64(reader.GetOrdinal("Quantity"));
        }
    }
}
