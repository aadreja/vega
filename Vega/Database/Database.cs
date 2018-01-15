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
    internal static class DBCache
    {
        private static Dictionary<string, Database> dbs = new Dictionary<string, Database>();

        public static Database Get(IDbConnection con)
        {
            string key = con.GetType().Name;

            Database db;
            lock (dbs)
            {
                if (dbs.TryGetValue(key, out db)) return db;
            }
            if (key.Equals("npgsqlconnection", StringComparison.OrdinalIgnoreCase))
                db = new Data.PgSqlDatabase();
            else if(key.Equals("sqliteconnection", StringComparison.OrdinalIgnoreCase))
                db = new Data.SQLiteDatabase();
            else
                db = new Data.MsSqlDatabase();

            lock (dbs)
            {
                return dbs[key] = db;
            }
        }
    }

    internal abstract class Database
    {
        protected Dictionary<DbType, String> dbTypeString;

        #region abstract methods

        public abstract string DBObjectExistsQuery(string name, DBObjectTypeEnum objectType, string schema = null);
        public abstract string IndexExistsQuery(string tableName, string indexName);
        public abstract string CreateTableQuery(Type entity);
        public abstract string CreateIndexQuery(string tableName, string indexName, string columns, bool isUnique);

        #endregion

        #region virtual methods

        public virtual string DropTableQuery(Type entity)
        {
            TableAttribute tableInfo = EntityCache.Get(entity);

            return $"DROP TABLE {tableInfo.FullName}";
        }

        public virtual string VirtualForeignKeyCheckQuery(ForeignKey vfk)
        {
            StringBuilder query = new StringBuilder();

            query.Append($"SELECT 1 FROM {vfk.FullTableName} WHERE {vfk.ColumnName}=@Id");

            if (vfk.ContainsIsActive)
                query.Append($" AND {Config.ISACTIVE_COLUMNNAME}={BITTRUEVALUE}");

            query.Append(" LIMIT 1 ");

            return query.ToString();
        }

        #endregion

        #region abstract properties

        public abstract string DEFAULTSCHEMA { get; }
        public abstract string CURRENTDATETIMESQL { get; }
        public abstract string BITTRUEVALUE { get; }
        public abstract string BITFALSEVALUE { get; }
        public abstract string LASTINSERTEDROWIDSQL { get; }

        public abstract Dictionary<DbType, String> DbTypeString { get; }

        #endregion

        #region properties

        #endregion

        #region Create CRUD commands

        internal virtual void CreateAddCommand(IDbCommand command, EntityBase entity, AuditTrial audit = null, string columnNames = null, bool doNotAppendCommonFields = false)
        {
            TableAttribute tableInfo = EntityCache.Get(entity.GetType());

            List<string> columns = new List<string>();

            if (!string.IsNullOrEmpty(columnNames)) columns.AddRange(columnNames.Split(','));
            else columns.AddRange(tableInfo.DefaultUpdateColumns);//Get columns from Entity attributes loaded in TableInfo

            entity.CreatedBy = Session.CurrentUserId;
            entity.UpdatedBy = Session.CurrentUserId;

            bool isPrimaryKeyEmpty = false;
            if (tableInfo.PrimaryKeyAttribute.IsIdentity && entity.IsKeyIdEmpty())
            {
                isPrimaryKeyEmpty = true;
                //if identity remove keyfield if added in field list
                columns.Remove(tableInfo.PrimaryKeyColumn.Name);
            }
            else if (entity.KeyId is Guid && entity.IsKeyIdEmpty())
            {
                isPrimaryKeyEmpty = true;
                //if not identity and key not generated, generate before save
                entity.KeyId = Guid.NewGuid();
            }

            #region append common columns

            if (!doNotAppendCommonFields)
            {
                if (!tableInfo.NoIsActive)
                {
                    if (!columns.Contains(Config.ISACTIVE_COLUMN.Name))
                        columns.Add(Config.ISACTIVE_COLUMN.Name);

                    command.AddInParameter("@" + Config.ISACTIVE_COLUMN.Name, Config.ISACTIVE_COLUMN.ColumnDbType, true);

                    if (tableInfo.NeedsHistory) audit.AppendDetail(Config.ISACTIVE_COLUMN.Name, true, DbType.Boolean);
                }

                if (!tableInfo.NoVersionNo)
                {
                    if (!columns.Contains(Config.VERSIONNO_COLUMN.Name))
                        columns.Add(Config.VERSIONNO_COLUMN.Name);

                    command.AddInParameter("@" + Config.VERSIONNO_COLUMN.Name, Config.VERSIONNO_COLUMN.ColumnDbType, 1);
                }

                if (!tableInfo.NoCreatedBy)
                {
                    if (!columns.Contains(Config.CREATEDBY_COLUMN.Name))
                        columns.Add(Config.CREATEDBY_COLUMN.Name);

                    command.AddInParameter("@" + Config.CREATEDBY_COLUMN.Name, Config.CREATEDBY_COLUMN.ColumnDbType, Session.CurrentUserId);
                }

                if (!tableInfo.NoCreatedOn & !columns.Contains(Config.CREATEDON_COLUMN.Name))
                {
                    columns.Add(Config.CREATEDON_COLUMN.Name);
                }

                if (!tableInfo.NoUpdatedBy)
                {
                    if (!columns.Contains(Config.UPDATEDBY_COLUMN.Name))
                        columns.Add(Config.UPDATEDBY_COLUMN.Name);

                    command.AddInParameter("@" + Config.UPDATEDBY_COLUMN.Name, Config.UPDATEDBY_COLUMN.ColumnDbType, Session.CurrentUserId);
                }

                if (!tableInfo.NoUpdatedOn & !columns.Contains(Config.UPDATEDON_COLUMN.Name))
                {
                    columns.Add(Config.UPDATEDON_COLUMN.Name);
                }
            }

            #endregion

            //append @ before each fields to add as parameter
            List<string> parameters = columns.Select(c => "@" + c).ToList();

            int pIndex = parameters.FindIndex(c => c == "@" + Config.CREATEDON_COLUMN.Name);
            if (pIndex >= 0)
                parameters[pIndex] = CURRENTDATETIMESQL;

            pIndex = parameters.FindIndex(c => c == "@" + Config.UPDATEDON_COLUMN.Name);
            if (pIndex >= 0)
                parameters[pIndex] = CURRENTDATETIMESQL;

            StringBuilder commandText = new StringBuilder();
            commandText.Append($"INSERT INTO {tableInfo.FullName} ({string.Join(",", columns)}) VALUES({string.Join(",", parameters)});");

            if (tableInfo.PrimaryKeyAttribute.IsIdentity && isPrimaryKeyEmpty)
            {
                //add query to get inserted id
                commandText.Append(LASTINSERTEDROWIDSQL);
            }

            //remove common columns and parameters already added above
            columns.RemoveAll(c => c == Config.CREATEDON_COLUMN.Name || c == Config.CREATEDBY_COLUMN.Name
                                    || c == Config.UPDATEDON_COLUMN.Name || c == Config.UPDATEDBY_COLUMN.Name
                                    || c == Config.VERSIONNO_COLUMN.Name || c == Config.ISACTIVE_COLUMN.Name);

            command.CommandType = CommandType.Text;
            command.CommandText = commandText.ToString();

            for (int i = 0; i < columns.Count(); i++)
            {
                tableInfo.Columns.TryGetValue(columns[i], out ColumnAttribute columnInfo); //find column attribute

                DbType dbType = DbType.Object;
                object columnValue = null;

                if (columnInfo != null && columnInfo.GetMethod != null)
                {
                    dbType = columnInfo.ColumnDbType;
                    columnValue = columnInfo.GetAction(entity);

                    if (tableInfo.NeedsHistory) audit.AppendDetail(columns[i], columnValue, dbType);
                }
                command.AddInParameter("@" + columns[i], dbType, columnValue);
            }
        }

        internal virtual bool CreateUpdateCommand(IDbCommand command, EntityBase entity, EntityBase oldEntity, AuditTrial audit = null, string columnNames = null, bool doNotAppendCommonFields = false)
        {
            bool isUpdateNeeded = false;

            TableAttribute tableInfo = EntityCache.Get(entity.GetType());

            List<string> columns = new List<string>();

            if (!string.IsNullOrEmpty(columnNames)) columns.AddRange(columnNames.Split(','));
            else columns.AddRange(tableInfo.DefaultUpdateColumns);//Get columns from Entity attributes loaded in TableInfo

            entity.UpdatedBy = Session.CurrentUserId;

            StringBuilder commandText = new StringBuilder();

            commandText.Append($"UPDATE {tableInfo.FullName} SET ");

            //add default columns if doesn't exists
            if (!doNotAppendCommonFields)
            {
                if (!columns.Contains(Config.VERSIONNO_COLUMN.Name))
                    columns.Add(Config.VERSIONNO_COLUMN.Name);

                if (!columns.Contains(Config.UPDATEDBY_COLUMN.Name))
                    columns.Add(Config.UPDATEDBY_COLUMN.Name);

                if (!columns.Contains(Config.UPDATEDON_COLUMN.Name))
                    columns.Add(Config.UPDATEDON_COLUMN.Name);
            }

            //remove primarykey, createdon and createdby columns if exists
            columns.RemoveAll(c => c == tableInfo.PrimaryKeyColumn.Name || c == Config.CREATEDON_COLUMN.Name || c == Config.CREATEDBY_COLUMN.Name);

            for (int i = 0; i < columns.Count(); i++)
            {
                if (columns[i].Equals(Config.VERSIONNO_COLUMN.Name, StringComparison.OrdinalIgnoreCase))
                {
                    commandText.Append($"{columns[i]} = {columns[i]}+1");
                    commandText.Append(",");
                }
                else if (columns[i].Equals(Config.UPDATEDBY_COLUMN.Name, StringComparison.OrdinalIgnoreCase))
                {
                    commandText.Append($"{columns[i]} = @{columns[i]}");
                    commandText.Append(",");
                    command.AddInParameter("@" + columns[i], Config.UPDATEDBY_COLUMN.ColumnDbType, Session.CurrentUserId);
                }
                else if (columns[i].Equals(Config.UPDATEDON_COLUMN.Name, StringComparison.OrdinalIgnoreCase))
                {
                    commandText.Append($"{columns[i]} = {CURRENTDATETIMESQL}");
                    commandText.Append(",");
                }
                else
                {
                    bool includeInUpdate = true;
                    tableInfo.Columns.TryGetValue(columns[i], out ColumnAttribute columnInfo); //find column attribute

                    DbType dbType = DbType.Object;
                    object columnValue = null;

                    if (columnInfo != null && columnInfo.GetMethod != null)
                    {
                        dbType = columnInfo.ColumnDbType;
                        columnValue = columnInfo.GetAction(entity);

                        includeInUpdate = oldEntity == null; //include in update when oldEntity not available

                        //compare with old object to check whether update is needed or not
                        if (oldEntity != null)
                        {
                            object oldObjectValue = columnInfo.GetAction(oldEntity);

                            if (oldObjectValue != null && columnValue != null)
                            {
                                if (!oldObjectValue.Equals(columnValue)) //add to xml only if property is modified
                                {
                                    includeInUpdate = true;
                                }
                            }
                            else if (oldObjectValue == null && columnValue != null)
                            {
                                includeInUpdate = true;
                            }
                            else if (oldObjectValue != null)
                            {
                                includeInUpdate = true;
                            }
                        }

                        if (tableInfo.NeedsHistory && includeInUpdate) audit.AppendDetail(columns[i], columnValue, dbType);
                    }

                    if (includeInUpdate)
                    {
                        isUpdateNeeded = true;

                        commandText.Append($"{columns[i]} = @{columns[i]}");
                        commandText.Append(",");
                        command.AddInParameter("@" + columns[i], dbType, columnValue);
                    }
                }
            }
            commandText.RemoveLastComma(); //Remove last comma if exists

            commandText.Append($" WHERE {tableInfo.PrimaryKeyColumn.Name}=@{tableInfo.PrimaryKeyColumn.Name}");
            command.AddInParameter("@" + tableInfo.PrimaryKeyColumn.Name, tableInfo.PrimaryKeyColumn.ColumnDbType, entity.KeyId);

            if (Config.DB_CONCURRENCY_CHECK)
            {
                commandText.Append($" AND {Config.VERSIONNO_COLUMN.Name}=@{Config.VERSIONNO_COLUMN.Name}");
                command.AddInParameter("@" + Config.VERSIONNO_COLUMN.Name, Config.VERSIONNO_COLUMN.ColumnDbType, entity.VersionNo);
            }

            command.CommandType = CommandType.Text;
            command.CommandText = commandText.ToString();

            return isUpdateNeeded;
        }

        internal virtual StringBuilder CreateSelectCommand(IDbCommand command, string query, object parameters = null)
        {
            return CreateSelectCommand(command, query, null, parameters);
        }

        internal virtual StringBuilder CreateSelectCommand(IDbCommand command, string query, string criteria = null, object parameters = null)
        {
            bool hasWhere = query.ToLowerInvariant().Contains("where");

            StringBuilder commandText = new StringBuilder(query);

            if (!string.IsNullOrEmpty(criteria))
            {
                //add WHERE statement if not exists in query or criteria
                if (!hasWhere && !criteria.ToLowerInvariant().Contains("where"))
                    commandText.Append(" WHERE ");

                commandText.Append(criteria);
            }

            if(parameters != null)
                ParameterCache.GetFromCache(parameters, command).Invoke(parameters, command);

            return commandText;
        }

        internal void AppendStatusCriteria(StringBuilder commandText, RecordStatusEnum status = RecordStatusEnum.All)
        {
            if (status == RecordStatusEnum.All) return; //nothing to do

            //add missing where clause
            if (!commandText.ToString().ToLowerInvariant().Contains("where"))
                commandText.Append(" WHERE ");

            if (status == RecordStatusEnum.Active)
                commandText.Append($" {Config.ISACTIVE_COLUMN.Name}={BITTRUEVALUE}");
            else if (status == RecordStatusEnum.InActive)
                commandText.Append($" {Config.ISACTIVE_COLUMN.Name}={BITFALSEVALUE}");
        }


        internal virtual string GetDBTypeWithSize(DbType type, int size, int scale=0)
        {
            if(type == DbType.String || type == DbType.StringFixedLength)
            {
                if (size > 0)
                    return DbTypeString[DbType.StringFixedLength] + "(" + size + ")";
                else
                    return DbTypeString[DbType.String];
            }
            else if (type == DbType.AnsiString || type == DbType.AnsiStringFixedLength)
            {
                if (size > 0)
                    return DbTypeString[DbType.AnsiStringFixedLength] + "(" + size + ")";
                else
                    return DbTypeString[DbType.AnsiString];
            }
            else if (type == DbType.Decimal)
            {
                if (size > 0 && scale > 0)
                    return DbTypeString[DbType.Decimal] + $"({size},{scale})";
                else if (size > 0)
                    return DbTypeString[DbType.Decimal] + $"({size})";
                else
                    return DbTypeString[DbType.Decimal];
            }
            else
                return DbTypeString[type];
        }

        #endregion

    }
}
