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
    internal class MsSqlDatabase : Database
    {

        public MsSqlDatabase()
        {

        }

        public override string DEFAULTSCHEMA { get { return "dbo"; } }
        public override string CURRENTDATETIMESQL { get { return "getdate()"; } }
        public override string BITTRUEVALUE { get { return "1"; } }
        public override string BITFALSEVALUE { get { return "0"; } }
        public override string LASTINSERTEDROWIDSQL { get { return "SELECT SCOPE_IDENTITY();"; }}

        //https://msdn.microsoft.com/en-us/library/cc716729(v=vs.100).aspx
        public override Dictionary<DbType, string> DbTypeString
        {
            get
            {
                if (dbTypeString != null)
                    return dbTypeString;

                dbTypeString = new Dictionary<DbType, String>
                {
                    [DbType.String] = "nvarchar(MAX)",
                    [DbType.StringFixedLength] = "nvarchar",
                    [DbType.AnsiString] = "varchar(MAX)",
                    [DbType.AnsiStringFixedLength] = "varchar",
                    [DbType.Guid] = "uniqueidentifier",
                    [DbType.Byte] = "tinyint",
                    [DbType.Int16] = "smallint",
                    [DbType.Int32] = "int",
                    [DbType.Int64] = "bigint",
                    [DbType.Boolean] = "bit",
                    [DbType.Decimal] = "decimal",
                    [DbType.Single] = "single",
                    [DbType.Double] = "double",
                    [DbType.DateTime] = "datetime",
                    [DbType.Binary] = "binary"
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
                query = $"SELECT 1 FROM sys.databases WHERE name='{name}'";
            }
            else if (objectType == DBObjectTypeEnum.Schema)
            {
                query = $"SELECT 1 FROM sys.schemas WHERE name='{name}'";
            }
            else if (objectType == DBObjectTypeEnum.Table)
            {
                query = $"SELECT 1 FROM sys.tables WHERE name='{name}' AND schema_id=SCHEMA_ID('{schema}')";
            }
            else if (objectType == DBObjectTypeEnum.View)
            {
                query = $"SELECT 1 FROM sys.views WHERE name='{name}' AND schema_id=SCHEMA_ID('{schema}')"; 
            }
            else if (objectType == DBObjectTypeEnum.Function)
            {
                query = $"SELECT 1 FROM Information_schema.Routines WHERE specific_name='{name}' AND SPECIFIC_SCHEMA='{schema}' AND routine_type='FUNCTION'";
            }
            else if (objectType == DBObjectTypeEnum.Procedure)
            {
                query = $"SELECT 1 FROM Information_schema.Routines WHERE specific_name='{name}' AND SPECIFIC_SCHEMA='{schema}' AND routine_type='PROCEDURE'";
            }

            return query;
        }

        public override string CreateTableQuery(Type entity)
        {
            TableAttribute tableInfo = EntityCache.Get(entity);

            StringBuilder createSQL = new StringBuilder($"CREATE TABLE {tableInfo.FullName} (");

            string primaryKeyCols = string.Empty;

            for (int i = 0; i < tableInfo.Columns.Count; i++)
            {
                ColumnAttribute col = tableInfo.Columns.ElementAt(i).Value;

                if (col.IsPrimaryKey)
                {
                    primaryKeyCols += col.Name + ",";

                    //13-May-15 Ritesh - fixed bug when primary key type is varchar size was ignored
                    createSQL.Append($"{col.Name} {GetDBTypeWithSize(col.ColumnDbType, col.Size)} NOT NULL ");

                    if (col.PrimaryKeyInfo.IsIdentity)
                    {
                        createSQL.Append(" IDENTITY ");
                    }
                    createSQL.Append(",");
                }
                else if (col.IgnoreInfo != null && col.IgnoreInfo.Insert && col.IgnoreInfo.Update)
                {
                    continue;
                }
                else
                {
                    createSQL.Append($"{col.Name} {GetDBTypeWithSize(col.ColumnDbType, col.Size, col.NumericScale)}");

                    if (IsNullableType(col.Property.PropertyType))
                    {
                        createSQL.Append(" NULL ");
                    }

                    if (col.Name == Config.CREATEDON_COLUMN.Name || col.Name == Config.UPDATEDON_COLUMN.Name)
                    {
                        createSQL.Append(" DEFAULT " + CURRENTDATETIMESQL);
                    }
                    createSQL.Append(",");
                }
            }
            
            if (!string.IsNullOrEmpty(primaryKeyCols))
            {
                primaryKeyCols = primaryKeyCols.RemoveLastComma();

                createSQL.Append($"PRIMARY KEY CLUSTERED({primaryKeyCols})");
            }

            createSQL.RemoveLastComma(); //Remove last comma if exists

            createSQL.Append(");");

            if (tableInfo.IsKeyIdentity())
            {
                createSQL.Append($"SET IDENTITY_INSERT {tableInfo.FullName} OFF");
            }
            return createSQL.ToString();
        }

        public override string CreateIndexQuery(string tableName, string indexName, string columns, bool isUnique)
        {
            return $@"CREATE {(isUnique ? "UNIQUE" : "")} INDEX {indexName} ON {tableName} ({columns})";
        }

        public override string IndexExistsQuery(string tableName, string indexName)
        {
            /*si.object_id AS ObjectId, si.index_id AS IndexId, 
            si.name AS IndexName, si.type AS IndexType, si.type_desc AS IndexTypeDesc, 
            si.is_unique AS IndexIsUnique, si.is_primary_key AS IndexIsPrimarykey, 
            si.fill_factor AS IndexFillFactor, sic.column_id, sc.name*/

            return $@"SELECT 1 FROM sys.indexes AS si
                        WHERE type<> 0 AND name='{indexName}' AND si.object_id IN(SELECT object_id from sys.tables WHERE name='{tableName}')";
        }

        public override string VirtualForeignKeyCheckQuery(ForeignKey vfk)
        {
            StringBuilder query = new StringBuilder();

            query.Append($"SELECT TOP 1 {vfk.ColumnName} FROM {vfk.FullTableName} WHERE {vfk.ColumnName}=@Id");

            if (vfk.ContainsIsActive)
                query.Append($" AND {Config.IsActiveColumnName}={BITTRUEVALUE}");

            return query.ToString();
        }

        public override DBVersionInfo FetchDBServerInfo(IDbConnection connection)
        {
            if (connection == null) throw new Exception("Required valid connection object to initialise database details");

            string query = @"SELECT SERVERPROPERTY('Edition') AS Edition,SERVERPROPERTY('ProductVersion') AS ProductVersion;";
            bool isConOpen = connection.State == ConnectionState.Open;

            try
            {
                if (!isConOpen) connection.Open();

                IDbCommand cmd = connection.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = query;

                using (IDataReader rdr = cmd.ExecuteReader())
                {
                    DBVersionInfo dbVersion = new DBVersionInfo();
                    if (rdr.Read())
                    {

                        dbVersion.ProductName = "Microsoft SQL Server";
                        dbVersion.Edition = rdr.GetString(0);
                        dbVersion.Version = new Version(rdr.GetString(1));

                        //https://sqlserverbuilds.blogspot.in/
                        //https://support.microsoft.com/en-in/help/321185/how-to-determine-the-version--edition-and-update-level-of-sql-server-a
                        if (dbVersion.Version.Major == 14)
                            dbVersion.ProductName += "2017 " + rdr.GetString(0);
                        else if (dbVersion.Version.Major == 13)
                            dbVersion.ProductName += "2016 " + rdr.GetString(0);
                        else if (dbVersion.Version.Major == 12)
                            dbVersion.ProductName += "2014 " + rdr.GetString(0);
                        else if (dbVersion.Version.Major == 11)
                            dbVersion.ProductName += "2012 " + rdr.GetString(0);
                        else if (dbVersion.Version.Major == 10)
                        {
                            if (dbVersion.Version.Minor >= 50)
                                dbVersion.ProductName += "2008 R2 " + rdr.GetString(0);
                            else
                                dbVersion.ProductName += "2008 " + rdr.GetString(0);
                        }
                        else if (dbVersion.Version.Major == 9)
                            dbVersion.ProductName += "2005 " + rdr.GetString(0);
                        else if (dbVersion.Version.Major == 8)
                            dbVersion.ProductName += "2000 " + rdr.GetString(0);
                        else if (dbVersion.Version.Major == 7)
                            dbVersion.ProductName += "7.0 " + rdr.GetString(0);

                        dbVersion.Is64Bit = dbVersion.Edition.ToLowerInvariant().Contains("64-bit");
                    }
                    rdr.Close();

                    return dbVersion;
                }
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
