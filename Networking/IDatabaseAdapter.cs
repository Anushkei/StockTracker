using System.Collections.Generic;

namespace StockTracker.Networking
{
    /// <summary>
    /// Defines the interface for an object that can be used to connect with a database.
    /// </summary>
    public interface IDatabaseAdapter
    {
        IEnumerable<T> Select<T>(string query) where T : IDataModel;

        bool Insert(List<IDataModel> itemsToInsert);
        bool Update(List<IDataModel> itemsToUpdate);
        bool Delete(List<IDataModel> itemsToDelete);
    }
}
