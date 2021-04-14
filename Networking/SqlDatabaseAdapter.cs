using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;

namespace StockTracker.Networking
{
    public class SqlDatabaseAdapter : IDatabaseAdapter
    {
        private SqlConnection m_Connection;
        private SqlConnection Connection
        {
            get
            {
                if (m_Connection == null || (m_Connection.State != System.Data.ConnectionState.Open))
                {
                    m_Connection = ConnectToDatabase();
                }

                return m_Connection;
            }
        }

        private SqlConnection ConnectToDatabase()
        {
            string connectionString = "Data Source=(localdb)\\ProjectsV13;Initial Catalog=StockTrackerDB;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False;";
            var connection = new SqlConnection(connectionString);
            connection.Open();
            return connection;
        }

        public IEnumerable<T> Select<T>(string query) where T : IDataModel
        {
            SqlDataReader reader = null;
            try
            {
                var connection = this.Connection;
                SqlCommand cmd = new SqlCommand(query, connection);

                List<T> newItems = new List<T>();

                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var newRecord = (T)Activator.CreateInstance(typeof(T));
                    newRecord.ReadData(reader);
                    newItems.Add(newRecord);
                }

                return newItems;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return new List<T>();
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
            }
        }

        public bool Insert(List<IDataModel> itemsToInsert) 
        {
            try
            {
                var connection = this.Connection;
                List<string> queries = itemsToInsert.ConvertAll(item => item.GetInsertQuery());
                this.IterateThroughQueries(queries);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool Update(List<IDataModel> itemsToUpdate) 
        {
            try
            {
                var connection = this.Connection;
                List<string> queries = itemsToUpdate.ConvertAll(item => item.GetUpdateQuery());
                this.IterateThroughQueries(queries);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool Delete(List<IDataModel> itemsToDelete) 
        {
            try
            {
                var connection = this.Connection;
                List<string> queries = itemsToDelete.ConvertAll(item => item.GetDeleteQuery());
                this.IterateThroughQueries(queries);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private void IterateThroughQueries(IEnumerable<string> queries)
        {
            foreach (string query in queries)
            {
                try
                {
                    var connection = this.Connection;
                    SqlCommand command = new SqlCommand(query, connection);
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                }
            }
        }
    }
}
