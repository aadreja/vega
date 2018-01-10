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

namespace Vega.Data
{
    public class PgSqlDatabase : Database
    {
        public PgSqlDatabase() { }

        public override string DEFAULTSCHEMA { get { return "public"; } }
        public override string CURRENTDATETIMESQL { get { return "now()"; } }
        public override string BITTRUEVALUE { get { return "TRUE"; } }
        public override string BITFALSEVALUE { get { return "FALSE";} }
        public override string LASTINSERTEDROWIDSQL { get { return "SELECT lastval();";} }

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
                    [DbType.Guid] = "uuid",
                    [DbType.Byte] = "bit[1]",
                    [DbType.Int16] = "smallint",
                    [DbType.Int32] = "int",
                    [DbType.Int64] = "bigint",
                    [DbType.Boolean] = "boolean",
                    [DbType.StringFixedLength] = "char",
                    [DbType.Decimal] = "money",
                    [DbType.Single] = "float4",
                    [DbType.Double] = "float8",
                    [DbType.DateTime] = "timestamp",
                    [DbType.Binary] = "bytea",
                    [DbType.Date] = "date",
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
                query = $"SELECT 1 FROM pg_database WHERE datname ILIKE '{name}'";
            }
            else if (objectType == DBObjectTypeEnum.Schema)
            {
                query = $"SELECT 1 FROM information_schema.schemata WHERE schema_name = '{name}'";
            }
            else if (objectType == DBObjectTypeEnum.Table)
            {
                query = $"SELECT 1 FROM information_schema.tables WHERE table_schema = '{schema}' AND table_name ILIKE '{name}'";
            }
            else if (objectType == DBObjectTypeEnum.View)
            {
                query = $"SELECT 1 FROM information_schema.views WHERE table_schema = '{schema}' AND table_name ILIKE '{name}'";
            }
            else if (objectType == DBObjectTypeEnum.Function)
            {
                query = $"SELECT 1 FROM pg_proc a JOIN pg_namespace b ON a.pronamespace=b.oid WHERE a.proname ILIKE '{name}' AND b.nspname='{schema}'";
            }
            else if (objectType == DBObjectTypeEnum.Procedure)
            {
                query = $"SELECT 1 FROM pg_proc a JOIN pg_namespace b ON a.pronamespace=b.oid WHERE a.proname ILIKE '{name}' AND b.nspname='{schema}'";
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

                if (col.Name == tableInfo.PrimaryKeyColumn.Name)
                {
                    if (tableInfo.PrimaryKeyAttribute.IsIdentity)
                    {
                        createSQL.Append($"{col.Name} {(col.ColumnDbType == DbType.Int64 ? "BIGSERIAL" : "SERIAL")} NOT NULL PRIMARY KEY");
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
                    createSQL.Append($"{col.Name} {DbTypeString[col.ColumnDbType]}");

                    if (col.Name == Config.CREATEDON_COLUMN.Name || col.Name == Config.UPDATEDON_COLUMN.Name)
                    {
                        createSQL.Append(" DEFAULT " + CURRENTDATETIMESQL);
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
            /*t.relname as table_name, i.relname as index_name, a.attname as column_name*/

            return $@"SELECT 1 FROM 
                        pg_class t, pg_class i, pg_index ix, pg_attribute a
                        WHERE t.oid = ix.indrelid
                            AND i.oid = ix.indexrelid
                            AND a.attrelid = t.oid
                            AND a.attnum = ANY(ix.indkey)
                            AND t.relkind = 'r'
                            AND t.relname ILIKE '{tableName}' AND i.relname ILIKE '{indexName}';";
        }
    }
}
