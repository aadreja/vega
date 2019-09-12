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
using System.Threading;

namespace Vega
{
    internal static class DBCache
    {
        private static ReaderWriterLockSlim cacheLock = new ReaderWriterLockSlim();
        private static Dictionary<string, Database> dbs = new Dictionary<string, Database>();

        public static Database Get(IDbConnection con)
        {
            string key = con.GetType().Name + "," + con.ConnectionString;

            Database db;

            try
            {
                cacheLock.EnterReadLock();
                if (dbs.TryGetValue(key, out db)) return db;
            }
            finally
            {
                cacheLock.ExitReadLock();
            }
            
            if (key.ToLowerInvariant().Contains("npgsqlconnection"))
                db = new PgSqlDatabase();
            else if (key.ToLowerInvariant().Contains("sqliteconnection"))
                db = new SQLiteDatabase();
            else
                db = new MsSqlDatabase();

            try 
            {
                cacheLock.EnterWriteLock();
                return dbs[key] = db;
            }
            finally
            {
                cacheLock.ExitWriteLock();
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
        public abstract DBVersionInfo FetchDBServerInfo(IDbConnection connection);

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
                query.Append($" AND {Config.VegaConfig.IsActiveColumnName}={BITTRUEVALUE}");

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

        DBVersionInfo dbVersion;
        internal DBVersionInfo GetDBVersion(IDbConnection connection)
        {
            if (dbVersion == null)
                dbVersion = FetchDBServerInfo(connection);

            return dbVersion;
        }

        #endregion

        #region Create CRUD commands

        internal virtual void CreateAddCommand(IDbCommand command, object entity, AuditTrial audit = null, string columnNames = null, bool doNotAppendCommonFields = false, bool overrideCreatedUpdatedOn = false)
        {
            TableAttribute tableInfo = EntityCache.Get(entity.GetType());

            if (tableInfo.NeedsHistory && tableInfo.IsCreatedByEmpty(entity))
                throw new MissingFieldException("CreatedBy is required when Audit Trail is enabled");

            List<string> columns = new List<string>();

            if (!string.IsNullOrEmpty(columnNames)) columns.AddRange(columnNames.Split(','));
            else columns.AddRange(tableInfo.DefaultInsertColumns);//Get columns from Entity attributes loaded in TableInfo

            bool isPrimaryKeyEmpty = false;

            foreach(ColumnAttribute pkCol in tableInfo.Columns.Where(p=>p.Value.IsPrimaryKey).Select(p=>p.Value))
            {
                if (pkCol.PrimaryKeyInfo.IsIdentity && tableInfo.IsKeyIdEmpty(entity, pkCol))
                {
                    isPrimaryKeyEmpty = true;
                    //if identity remove keyfield if added in field list
                    columns.Remove(pkCol.Name);
                }
                else if (pkCol.Property.PropertyType == typeof(Guid) && tableInfo.IsKeyIdEmpty(entity, pkCol))
                {
                    isPrimaryKeyEmpty = true;
                    //if not identity and key not generated, generate before save
                    tableInfo.SetKeyId(entity, pkCol, Guid.NewGuid());
                }
            }

            #region append common columns

            if (!doNotAppendCommonFields)
            {
                if (!tableInfo.NoIsActive)
                {
                    if (!columns.Contains(Config.ISACTIVE_COLUMN.Name))
                        columns.Add(Config.ISACTIVE_COLUMN.Name);

                    bool isActive = tableInfo.GetIsActive(entity) ?? true;
                    //when IsActive is not set then true for insert
                    command.AddInParameter("@" + Config.ISACTIVE_COLUMN.Name, Config.ISACTIVE_COLUMN.ColumnDbType, isActive);

                    if (tableInfo.NeedsHistory)
                        audit.AppendDetail(Config.ISACTIVE_COLUMN.Name, isActive, DbType.Boolean);

                    tableInfo.SetIsActive(entity, isActive); //Set IsActive value
                }

                if (!tableInfo.NoVersionNo)
                {
                    int versionNo = tableInfo.GetVersionNo(entity) ?? 1;  //set defualt versionno 1 for Insert
                    if (versionNo == 0) versionNo = 1; //set defualt versionno 1 for Insert even if its zero or null

                    if (!columns.Contains(Config.VERSIONNO_COLUMN.Name))
                        columns.Add(Config.VERSIONNO_COLUMN.Name);

                    command.AddInParameter("@" + Config.VERSIONNO_COLUMN.Name, Config.VERSIONNO_COLUMN.ColumnDbType, versionNo);

                    tableInfo.SetVersionNo(entity, versionNo); //Set VersionNo value
                }

                if (!tableInfo.NoCreatedBy)
                {
                    if (!columns.Contains(Config.CREATEDBY_COLUMN.Name))
                        columns.Add(Config.CREATEDBY_COLUMN.Name);

                    command.AddInParameter("@" + Config.CREATEDBY_COLUMN.Name, Config.CREATEDBY_COLUMN.ColumnDbType, tableInfo.GetCreatedBy(entity));
                }

                if (!tableInfo.NoCreatedOn & !columns.Contains(Config.CREATEDON_COLUMN.Name))
                {
                    columns.Add(Config.CREATEDON_COLUMN.Name);
                }

                if (!tableInfo.NoUpdatedBy)
                {
                    if (!columns.Contains(Config.UPDATEDBY_COLUMN.Name))
                        columns.Add(Config.UPDATEDBY_COLUMN.Name);

                    command.AddInParameter("@" + Config.UPDATEDBY_COLUMN.Name, Config.UPDATEDBY_COLUMN.ColumnDbType, tableInfo.GetCreatedBy(entity));
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
            {
                var createdOn = Helper.GetDateTimeOrDatabaseDateTimeSQL(tableInfo.GetCreatedOn(entity), this, overrideCreatedUpdatedOn);
                if (createdOn is string)
                {
                    parameters[pIndex] = (string)createdOn;
                }
                else
                {
                    command.AddInParameter(parameters[pIndex], Config.CREATEDON_COLUMN.ColumnDbType, createdOn);
                }
                //parameters[pIndex] = (string)Helper.GetDateTimeOrDatabaseDateTimeSQL(tableInfo.GetCreatedOn(entity), this, overrideCreatedUpdatedOn);
            }

            pIndex = parameters.FindIndex(c => c == "@" + Config.UPDATEDON_COLUMN.Name);
            if (pIndex >= 0)
            {
                var updatedOn = Helper.GetDateTimeOrDatabaseDateTimeSQL(tableInfo.GetUpdatedOn(entity), this, overrideCreatedUpdatedOn);
                if (updatedOn is string)
                {
                    parameters[pIndex] = (string)updatedOn;
                }
                else
                {
                    command.AddInParameter(parameters[pIndex], Config.CREATEDON_COLUMN.ColumnDbType, updatedOn);
                }
                //parameters[pIndex] = (string)Helper.GetDateTimeOrDatabaseDateTimeSQL(tableInfo.GetUpdatedOn(entity), this, overrideCreatedUpdatedOn);
            }

            StringBuilder commandText = new StringBuilder();
            commandText.Append($"INSERT INTO {tableInfo.FullName} ({string.Join(",", columns)}) VALUES({string.Join(",", parameters)});");

            if (tableInfo.IsKeyIdentity() && isPrimaryKeyEmpty)
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

        internal virtual bool CreateUpdateCommand(IDbCommand command, object entity, object oldEntity, AuditTrial audit = null, string columnNames = null, bool doNotAppendCommonFields = false, bool overrideCreatedUpdatedOn = false)
        {
            bool isUpdateNeeded = false;

            TableAttribute tableInfo = EntityCache.Get(entity.GetType());

            if (!tableInfo.NoUpdatedBy && tableInfo.IsUpdatedByEmpty(entity))
                throw new MissingFieldException("Updated By is required");

            List<string> columns = new List<string>();

            if (!string.IsNullOrEmpty(columnNames)) columns.AddRange(columnNames.Split(','));
            else columns.AddRange(tableInfo.DefaultUpdateColumns);//Get columns from Entity attributes loaded in TableInfo

            StringBuilder commandText = new StringBuilder();
            commandText.Append($"UPDATE {tableInfo.FullName} SET ");

            //add default columns if doesn't exists
            if (!doNotAppendCommonFields)
            {
                if (!tableInfo.NoVersionNo && !columns.Contains(Config.VERSIONNO_COLUMN.Name))
                    columns.Add(Config.VERSIONNO_COLUMN.Name);

                if (!tableInfo.NoUpdatedBy && !columns.Contains(Config.UPDATEDBY_COLUMN.Name))
                    columns.Add(Config.UPDATEDBY_COLUMN.Name);

                if (!tableInfo.NoUpdatedOn && !columns.Contains(Config.UPDATEDON_COLUMN.Name))
                    columns.Add(Config.UPDATEDON_COLUMN.Name);
            }

            //remove primarykey, createdon and createdby columns if exists
            columns.RemoveAll(c => tableInfo.PkColumnList.Select(p=>p.Name).Contains(c));
            columns.RemoveAll(c => c == Config.CREATEDON_COLUMN.Name || 
                                    c == Config.CREATEDBY_COLUMN.Name);

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
                    command.AddInParameter("@" + columns[i], Config.UPDATEDBY_COLUMN.ColumnDbType, tableInfo.GetUpdatedBy(entity));
                }
                else if (columns[i].Equals(Config.UPDATEDON_COLUMN.Name, StringComparison.OrdinalIgnoreCase))
                {
                    var updatedOn = Helper.GetDateTimeOrDatabaseDateTimeSQL(tableInfo.GetUpdatedOn(entity), this, overrideCreatedUpdatedOn);
                    if (updatedOn is string)
                    {
                        commandText.Append($"{columns[i]} = {CURRENTDATETIMESQL}");
                    }
                    else
                    {
                        commandText.Append($"{columns[i]} = @{columns[i]}");
                        command.AddInParameter("@" + columns[i], Config.UPDATEDON_COLUMN.ColumnDbType, updatedOn);
                    }
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

            commandText.Append(" WHERE ");
            if (tableInfo.PkColumnList.Count > 1)
            {
                int index = 0;
                foreach (ColumnAttribute pkCol in tableInfo.PkColumnList)
                {
                    commandText.Append($" {(index > 0 ? " AND " : "")} {pkCol.Name}=@{pkCol.Name}");
                    command.AddInParameter("@" + pkCol.Name, pkCol.ColumnDbType, tableInfo.GetKeyId(entity, pkCol));
                    index++;
                }
            }
            else
            {
                commandText.Append($" {tableInfo.PkColumn.Name}=@{tableInfo.PkColumn.Name}");
                command.AddInParameter("@" + tableInfo.PkColumn.Name, tableInfo.PkColumn.ColumnDbType, tableInfo.GetKeyId(entity));
            }

            if (Config.VegaConfig.DbConcurrencyCheck && !tableInfo.NoVersionNo)
            {
                commandText.Append($" AND {Config.VERSIONNO_COLUMN.Name}=@{Config.VERSIONNO_COLUMN.Name}");
                command.AddInParameter("@" + Config.VERSIONNO_COLUMN.Name, Config.VERSIONNO_COLUMN.ColumnDbType, tableInfo.GetVersionNo(entity));
            }

            command.CommandType = CommandType.Text;
            command.CommandText = commandText.ToString();

            return isUpdateNeeded;
        }

        internal virtual void CreateSelectCommandForReadOne<T>(IDbCommand command, object id, string columns)
        {
            TableAttribute tableInfo = EntityCache.Get(typeof(T));
            command.CommandType = CommandType.Text;

            if (tableInfo.PkColumnList.Count > 1)
            {
                if (!(id is T))
                {
                    throw new InvalidOperationException("Entity has multiple primary keys. Pass entity setting Primary Key attributes.");
                }

                StringBuilder commandText = new StringBuilder($"SELECT {columns} FROM {tableInfo.FullName} WHERE ");

                int index = 0;
                foreach (ColumnAttribute pkCol in tableInfo.PkColumnList)
                {
                    commandText.Append($" {(index > 0 ? " AND " : "")} {pkCol.Name}=@{pkCol.Name}");
                    command.AddInParameter("@" + pkCol.Name, pkCol.ColumnDbType, tableInfo.GetKeyId(id, pkCol));
                    index++;
                }
                command.CommandText = commandText.ToString();
            }
            else
            {
                command.CommandText = $"SELECT {columns} FROM {tableInfo.FullName} WHERE {tableInfo.PkColumn.Name}=@{tableInfo.PkColumn.Name}";
                command.AddInParameter(tableInfo.PkColumn.Name, tableInfo.PkColumn.ColumnDbType, id);
            }
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

        internal virtual void CreateReadAllPagedCommand(IDbCommand command, string query, string orderBy, int pageNo, int pageSize, object parameters = null)
        {
            StringBuilder commandText = new StringBuilder();

            if (this is MsSqlDatabase)
            {
                if (GetDBVersion(command.Connection).Version.Major >= 11) //SQL server 2012 and above supports offset
                    commandText.Append($@"{query} 
                                    ORDER BY {orderBy} 
                                    OFFSET {((pageNo - 1) * pageSize)} ROWS 
                                    FETCH NEXT {pageSize} ROWS ONLY");
                else
                    commandText.Append($@"SELECT * FROM (
                        SELECT ROW_NUMBER() OVER(ORDER BY {orderBy}) AS rownumber, 
                        * FROM ({query}) as sq
                    ) AS q
                    WHERE (rownumber between {((pageNo - 1) * pageSize) + 1} AND {pageNo * pageSize})");
            }
            else
            {
                commandText.Append($@"{query} 
                                ORDER BY {orderBy}
                                LIMIT {pageSize}
                                OFFSET {((pageNo - 1) * pageSize)}");
            }
            command.CommandType = CommandType.Text;
            command.CommandText = commandText.ToString();

            if (parameters != null)
                ParameterCache.GetFromCache(parameters, command).Invoke(parameters, command);
        }

        internal virtual void CreateReadAllPagedNoOffsetCommand<T>(IDbCommand command, string query, string orderBy, int pageSize, PageNavigationEnum navigation, object[] lastOrderByColumnValues = null, object lastKeyId = null, object parameters = null)
        {
            string[] orderByColumns = orderBy.Split(',');
            string[] orderByDirection = new string[orderByColumns.Length];
            for (int i = 0; i < orderByColumns.Length; i++)
            {
                if (orderByColumns[i].ToLowerInvariant().Contains("desc"))
                {
                    orderByDirection[i] = "DESC";
                    orderByColumns[i] = orderByColumns[i].ToLowerInvariant().Replace("desc", "").Trim();
                }
                else
                {
                    orderByDirection[i] = "ASC";
                    orderByColumns[i] = orderByColumns[i].ToLowerInvariant().Replace("asc", "").Trim();
                }
            }
            if (orderByColumns.Length == 0)
                throw new MissingMemberException("Orderby column(s) is missing");
            if ((navigation == PageNavigationEnum.Next || navigation == PageNavigationEnum.Previous) && lastOrderByColumnValues.Length != orderByColumns.Length)
                throw new MissingMemberException("For Next and Previous Navigation Length of Last Values must be equal to orderby columns length");
            if ((navigation == PageNavigationEnum.Next || navigation == PageNavigationEnum.Previous) && lastKeyId == null)
                throw new MissingMemberException("For Next and Previous Navigation Last KeyId is required");

            TableAttribute tableInfo = EntityCache.Get(typeof(T));
            bool hasWhere = query.ToLowerInvariant().Contains("where");

            StringBuilder pagedCriteria = new StringBuilder();
            StringBuilder pagedOrderBy = new StringBuilder();

            if (!hasWhere)
                pagedCriteria.Append(" WHERE 1=1");

            for (int i = 0; i < orderByColumns.Length; i++)
            {
                string applyEquals = (i <= orderByColumns.Length - 2 ? "=" : "");

                if (navigation == PageNavigationEnum.Next)
                {
                    //when multiple orderbycolumn - apply '>=' or '<=' till second last column
                    if (orderByDirection[i] == "ASC")
                        pagedCriteria.Append($" AND (({orderByColumns[i]} = @p_{orderByColumns[i]} {GetPrimaryColumnCriteriaForPagedQuery(tableInfo,">")}) OR {orderByColumns[i]} >{applyEquals} @p_{orderByColumns[i]})");
                    else
                        pagedCriteria.Append($" AND (({orderByColumns[i]} = @p_{orderByColumns[i]} {GetPrimaryColumnCriteriaForPagedQuery(tableInfo, "<")}) OR ({orderByColumns[i]} IS NULL OR {orderByColumns[i]} <{applyEquals} @p_{orderByColumns[i]}))");
                }
                else if (navigation == PageNavigationEnum.Previous)
                {
                    if (orderByDirection[i] == "ASC")
                        pagedCriteria.Append($" AND (({orderByColumns[i]} = @p_{orderByColumns[i]} {GetPrimaryColumnCriteriaForPagedQuery(tableInfo, "<")}) OR ({orderByColumns[i]} IS NULL OR {orderByColumns[i]} <{applyEquals} @p_{orderByColumns[i]}))");
                    else
                        pagedCriteria.Append($" AND (({orderByColumns[i]} = @p_{orderByColumns[i]} {GetPrimaryColumnCriteriaForPagedQuery(tableInfo, ">")}) OR {orderByColumns[i]} >{applyEquals} @p_{orderByColumns[i]})");
                }

                if (navigation == PageNavigationEnum.Next || navigation == PageNavigationEnum.Previous)
                {
                    //add Parameter for Last value of ordered column 
                    DbType dbType;
                    //see if column exists in TableInfo
                    tableInfo.Columns.TryGetValue(orderByColumns[i], out ColumnAttribute orderByColumn);
                    if (orderByColumn != null)
                        dbType = orderByColumn.ColumnDbType;
                    else
                        TypeCache.TypeToDbType.TryGetValue(lastOrderByColumnValues[i].GetType(), out dbType);

                    command.AddInParameter("@p_" + orderByColumns[i], dbType, lastOrderByColumnValues[i]);
                }

                if (i > 0) pagedOrderBy.Append(",");

                if (navigation == PageNavigationEnum.Last || navigation == PageNavigationEnum.Previous)
                {
                    //reverse sort as we are going backward
                    pagedOrderBy.Append($"{orderByColumns[i]} {(orderByDirection[i] == "ASC" ? "DESC" : "ASC")}");
                }
                else
                {
                    pagedOrderBy.Append($"{orderByColumns[i]} {orderByDirection[i]}");
                }
            }

            //add keyfield parameter for Next and Previous navigation
            if (navigation == PageNavigationEnum.Next || navigation == PageNavigationEnum.Previous)
            {
                //add LastKeyId Parameter

                if (tableInfo.PkColumnList.Count > 1)
                {
                    if (!(lastKeyId is T))
                    {
                        throw new InvalidOperationException("Entity has multiple primary keys. Pass entity setting Primary Key attributes.");
                    }

                    int index = 0;
                    foreach (ColumnAttribute pkCol in tableInfo.PkColumnList)
                    {
                        command.AddInParameter("@p_" + pkCol.Name, pkCol.ColumnDbType, tableInfo.GetKeyId(lastKeyId, pkCol));
                        index++;
                    }
                }
                else
                {
                    command.AddInParameter("@p_" + tableInfo.PkColumn.Name, tableInfo.PkColumn.ColumnDbType, lastKeyId);
                }
            }

            //add keyfield in orderby clause. Direction will be taken from 1st orderby column
            if (navigation == PageNavigationEnum.Last || navigation == PageNavigationEnum.Previous)
            {
                //reverse sort as we are going backward
                if (tableInfo.PkColumnList.Count > 1)
                {
                    foreach (ColumnAttribute pkCol in tableInfo.PkColumnList)
                    {
                        pagedOrderBy.Append($",{pkCol.Name} {(orderByDirection[0] == "ASC" ? "DESC" : "ASC")}");
                    }
                }
                else
                {
                    pagedOrderBy.Append($",{tableInfo.PkColumn.Name} {(orderByDirection[0] == "ASC" ? "DESC" : "ASC")}");
                }
            }
            else
            {
                if (tableInfo.PkColumnList.Count > 1)
                {
                    foreach (ColumnAttribute pkCol in tableInfo.PkColumnList)
                    {
                        pagedOrderBy.Append($",{pkCol.Name} {orderByDirection[0]}");
                    }
                }
                else
                {
                    pagedOrderBy.Append($",{tableInfo.PkColumn.Name} {orderByDirection[0]}");
                }
            }

            command.CommandType = CommandType.Text;

            if (this is MsSqlDatabase)
                command.CommandText = $"SELECT * FROM (SELECT TOP {pageSize} * FROM ({query} {pagedCriteria.ToString()}) AS r1 ORDER BY {pagedOrderBy}) AS r2 ORDER BY {orderBy}";
            else
                command.CommandText = $"SELECT * FROM ({query} {pagedCriteria.ToString()} ORDER BY {pagedOrderBy} LIMIT {pageSize}) AS r ORDER BY {orderBy}";

            if (parameters != null)
                ParameterCache.GetFromCache(parameters, command).Invoke(parameters, command);
        }

        #endregion

        #region other methods

        internal string GetPrimaryColumnCriteriaForPagedQuery(TableAttribute tableInfo, string op)
        {
            StringBuilder result = new StringBuilder();
            foreach(ColumnAttribute pkCol in tableInfo.PkColumnList)
            {
                result.Append($" AND {pkCol.Name} {op} @p_{pkCol.Name} ");
            }
            return result.ToString();
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

        internal bool IsNullableType(Type type)
        {
            if (!type.IsGenericType)
            {
                return false;
            }
            if (type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

    }

    /// <summary>
    /// Database Version Information
    /// </summary>
    public class DBVersionInfo
    {
        /// <summary>
        /// Database Product Name like Microsoft SQL Server 2012
        /// </summary>
        public string ProductName { get; set; }

        /// <summary>
        /// Database Edition details like Standard, Enterprise, Express
        /// </summary>
        public string Edition { get; set; }

        /// <summary>
        /// Database Version Info
        /// </summary>
        public Version Version { get; set; }

        /// <summary>
        /// Gets or set whether database is 64bit or 32bit
        /// </summary>
        public bool Is64Bit { get; set; }

        /// <summary>
        /// SQL Server Only. To check whether OFFSET keyword is supported by sql version
        /// </summary>
        internal bool IsOffsetSupported
        {
            get
            {
                //SQL Server 2012 and above supports offset keyword
                return ProductName.ToLowerInvariant().Contains("sql server") && Version.Major >= 11;
            }
        }
    }
}
