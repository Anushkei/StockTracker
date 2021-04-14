using System.Data.SqlClient;

namespace StockTracker.Networking
{
    // Defines the interface for all data types that can be constructed from, and inserted into, the database.
    public interface IDataModel
    {
        void ReadData(SqlDataReader reader);
        string GetInsertQuery();
        string GetUpdateQuery();
        string GetDeleteQuery();
    }
}
