/*
 Description: Vega - Fastest ORM with enterprise features
 Author: Ritesh Sutaria
 Date: 9-Dec-2017
 Home Page: https://github.com/aadreja/vega
            http://www.vegaorm.com
*/

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Vega
{
    internal class SQLiteDatabase : Database
    {
        public SQLiteDatabase()
        {
            
        }
        
        public override string DEFAULTSCHEMA { get { return "main"; } }
        public override string CURRENTDATETIMESQL { get { return "datetime('now')"; } }
        public override string BITTRUEVALUE { get { return "1"; } }
        public override string BITFALSEVALUE { get { return "0"; } }
        public override string LASTINSERTEDROWIDSQL { get { return "SELECT last_insert_rowid();"; } }

        //https://www.sqlite.org/datatype3.html
        //https://system.data.sqlite.org/index.html/artifact/f62b1ba1f4eb0f21d16637a619c6ba4a0ec4219c
        public override Dictionary<DbType, string> DbTypeString
        {
            get
            {
                if (dbTypeString != null)
                    return dbTypeString;

                dbTypeString = new Dictionary<DbType, String>
                {
                    [DbType.String] = "text",
                    [DbType.AnsiString] = "text",
                    [DbType.AnsiStringFixedLength] = "text",
                    [DbType.StringFixedLength] = "text",
                    [DbType.Guid] = "text",
                    [DbType.Byte] = "tinyint",
                    [DbType.Int16] = "smallint",
                    [DbType.Int32] = "int",
                    [DbType.Int64] = "bigint",
                    [DbType.Boolean] = "bit",
                    [DbType.Decimal] = "decimal",
                    [DbType.Single] = "real",
                    [DbType.Double] = "float",
                    [DbType.DateTime] = "datetime",
                    [DbType.Binary] = "blob",
                };

                return dbTypeString;
            }
        }

        public override string DBObjectExistsQuery(string name, DBObjectTypeEnum objectType, string schema = null)
        {
            if (schema == null)
                schema = DEFAULTSCHEMA;

            string query = string.Empty;

            if (objectType == DBObjectTypeEnum.Database)
            {
                throw new NotSupportedException();
            }
            else if (objectType == DBObjectTypeEnum.Schema)
            {
                throw new NotSupportedException();
            }
            else if (objectType == DBObjectTypeEnum.Table)
            {
                query = $"SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = '{name}'";
            }
            else if (objectType == DBObjectTypeEnum.View)
            {
                query = $"SELECT 1 FROM sqlite_master WHERE type = 'view' AND name = '{name}'";
            }
            else if (objectType == DBObjectTypeEnum.Function)
            {
                throw new NotSupportedException();
            }
            else if (objectType == DBObjectTypeEnum.Procedure)
            {
                throw new NotSupportedException();
            }

            return query;
        }

        public override string CreateTableQuery(Type entity)
        {
            TableAttribute tableInfo = EntityCache.Get(entity);

            StringBuilder createSQL = new StringBuilder($"CREATE TABLE {tableInfo.FullName} (");

            for (int i = 0; i < tableInfo.Columns.Count; i++)
            {
                ColumnAttribute col = tableInfo.Columns.ElementAt(i).Value;

                if (tableInfo.PrimaryKeyColumn.Name == col.Name)
                {
                    if (tableInfo.PrimaryKeyAttribute.IsIdentity)
                    {
                        createSQL.Append($"{col.Name} INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT");
                    }
                    else
                    {
                        createSQL.Append($"{col.Name} {DbTypeString[col.ColumnDbType]} NOT NULL PRIMARY KEY");
                    }
                    createSQL.Append(",");
                }
                else if(col.IgnoreInfo.Insert || col.IgnoreInfo.Update)
                {
                    continue;
                }
                else
                {
                    createSQL.Append($"{col.Name} {GetDBTypeWithSize(col.ColumnDbType, col.NumericPrecision, col.NumericScale)}");

                    if (col.Name == Config.CREATEDON_COLUMN.Name || col.Name == Config.UPDATEDON_COLUMN.Name)
                    {
                        createSQL.Append(" DEFAULT (" + CURRENTDATETIMESQL + ")");
                    }
                    createSQL.Append(",");
                }
            }
            createSQL.RemoveLastComma(); //Remove last comma if exists

            createSQL.Append(");");

            return createSQL.ToString();

        }

        public override string CreateIndexQuery(string tableName, string indexName, string columns, bool isUnique)
        {
            return $@"CREATE {(isUnique ? "UNIQUE" : "")} INDEX {indexName} ON {tableName} ({columns})";
        }

        public override string IndexExistsQuery(string tableName, string indexName)
        {
            return $@"SELECT 1 FROM sqlite_master WHERE type='index' AND name='{indexName}' AND tbl_name='{tableName}'";
        }

        public override DBVersionInfo FetchDBServerInfo(IDbConnection connection)
        {
            //select sqlite_version();
            if (connection == null) throw new Exception("Required valid connection object to initialise database details");

            string query = @"SELECT sqlite_version();";
            bool isConOpen = connection.State == ConnectionState.Open;

            try
            {
                if (!isConOpen) connection.Open();

                IDbCommand command = connection.CreateCommand();
                command.CommandType = CommandType.Text;
                command.CommandText = query;

                DBVersionInfo dbVersion = new DBVersionInfo();

                using (IDataReader rdr = command.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        dbVersion.ProductName = "SQLite";
                        dbVersion.Version = new Version(rdr.GetString(0));

                        //dbVersion.Is64Bit = true;
                    }
                    rdr.Close();
                }

                return dbVersion;
            }
            catch
            {
                //ignore error
                return null;
            }
            finally
            {
                if (!isConOpen && connection.State == ConnectionState.Open) connection.Close();
            }
        }
    }
}
