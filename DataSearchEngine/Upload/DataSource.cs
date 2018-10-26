using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using DataSearchEngine.Utils;

namespace DataSearchEngine.Upload
{
    abstract public class DataSource
    {
        [UseAttribute("name")]
        public string Name { get; set; }

        public abstract IDbConnection CreateConnection();
        public abstract DataSet ExecuteQuery(string sql);
    }

    class SqliteDatabase : DataSource
    {
        [UseAttribute("fileName")]
        public string FileName { get; set; }

        public override IDbConnection CreateConnection()
        {
            var cnx = (new System.Data.SQLite.SQLiteConnectionStringBuilder
                           {
                               FailIfMissing = true,
                               ReadOnly = true,
                               DataSource = FileName
                           }).ConnectionString;
            return new System.Data.SQLite.SQLiteConnection(cnx);
        }

        public override DataSet ExecuteQuery(string sql)
        {
            var dataSet = new DataSet();
            using(var cnx = CreateConnection() as System.Data.SQLite.SQLiteConnection)
            {
                using (var cmd = cnx.CreateCommand())
                {
                    cmd.CommandText = sql;
                    (new System.Data.SQLite.SQLiteDataAdapter(cmd)).Fill(dataSet);
                }
            }
            return dataSet;
        }
    }

    class MsSql : DataSource
    {
        [UseAttribute("server")]
        public string Server { get; set; }
        [UseAttribute("catalog")]
        public string Catalog { get; set; }

        public override IDbConnection CreateConnection()
        {
            var cnx = (new System.Data.SqlClient.SqlConnectionStringBuilder
            {
                IntegratedSecurity = true,
                DataSource = Server,
                InitialCatalog = Catalog
            }).ConnectionString;
            return new System.Data.SqlClient.SqlConnection(cnx);
        }

        public override DataSet ExecuteQuery(string sql)
        {
            var dataSet = new DataSet();
            using (var cnx = CreateConnection() as System.Data.SqlClient.SqlConnection)
            {
                using (var cmd = cnx.CreateCommand())
                {
                    cmd.CommandText = sql;
                    (new System.Data.SqlClient.SqlDataAdapter(cmd)).Fill(dataSet);
                }
            }
            return dataSet;
        }


    }

}
