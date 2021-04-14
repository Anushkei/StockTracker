using StockTracker.Networking;
using System.Data.SqlClient;
using static StockTracker.Models.Auxiliary;

namespace StockTracker.Models
{
    /// <summary>
    /// Defines a user-account model.
    /// </summary>
    public class Account : IDataModel
    {
        public long AccountId { get; set; }
        public string Type { get; set; }
        public decimal CashBalance { get; set; }
        public string CashBalanceString => DecimalToDollarString(CashBalance);

        public string GetDeleteQuery()
        {
            return "DELETE FROM dbo.[Account] WHERE [AccountId] = " + this.AccountId.ToString() + ";";
        }

        public string GetInsertQuery()
        {
            return string.Format("INSERT INTO dbo.[Account] ([Type], [CashBalance]) VALUES ({0}, {1};",
                GetSingleQuotes(this.Type),
                this.CashBalance);
        }

        public string GetUpdateQuery()
        {
            return string.Format("UPDATE dbo.[Account] SET [Type] = {0}, [CashBalance] = {1} WHERE [AccountId] = {2};",
                GetSingleQuotes(this.Type),
                this.CashBalance,
                this.AccountId);
        }

        public void ReadData(SqlDataReader reader)
        {
            this.AccountId = reader.GetInt64(reader.GetOrdinal("AccountId"));
            this.CashBalance = reader.GetDecimal(reader.GetOrdinal("CashBalance"));
            this.Type = reader.GetString(reader.GetOrdinal("Type"));
        }
    }
}
