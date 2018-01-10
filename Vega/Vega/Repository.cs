/*
 Description: Vega - Fastest ORM with enterprise features
 Author: Ritesh Sutaria
 Date: 9-Dec-2017
 Home Page: https://github.com/aadreja/vega
            http://www.vegaorm.com
*/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace Vega
{
    public class Repository<T> where T : EntityBase, new()
    {
        #region Constructors

        public Repository()
        {
            TableInfo = EntityCache.Get(typeof(T));
        }

        /// <summary>
        /// Constructor with specific connection
        /// </summary>
        /// <param name="connection">provide DB connection</param>
        public Repository(IDbConnection connection) : this() 
        {
            Connection = connection;
            DB = DBCache.Get(Connection);
        }

        /// <summary>
        /// Constructor with specific transaction
        /// </summary>
        /// <param name="transaction">provide DB Transaction</param>
        public Repository(IDbTransaction transaction) : this()
        {
            Connection = transaction.Connection;
            Transaction = transaction;
            DB = DBCache.Get(Connection);
        }

        #endregion

        #region fields

        internal TableAttribute TableInfo { get; private set; }
        
        public Database DB { get; set; }

        #endregion

        #region Properties

        public string ConnectionString { get; set; }

        public IDbTransaction Transaction { get; set; }

        public IDbConnection Connection { get; set; }

        #endregion

        #region Connection & Transaction methods

        public bool IsConnectionOpen()
        {
            if (Connection == null)
                throw new NullReferenceException("Connection is null");

            return (Connection.State == ConnectionState.Open);
        }

        public bool BeginTransaction()
        {
            if(Connection == null)
                throw new NullReferenceException("Connection is null");

            if (Transaction == null || Transaction.Connection == null)
            {
                if (!IsConnectionOpen())
                    Connection.Open();

                Transaction = Connection.BeginTransaction();
                return true; //is transacted here
            }
            else
            {
                return false; 
            }
        }

        public void Commit()
        {
            if(Transaction!=null) Transaction.Commit();
        }

        public void Rollback()
        {
            if (Transaction != null) Transaction.Rollback();
        }

        #endregion

        #region CRUD

        #region Create

        public object Add(T entity)
        {
            return Add(entity, null);
        }

        /// <summary>
        /// Insert Records
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>Id of inserted record</returns>
        public object Add(T entity, string columns)
        {
            bool isTransactHere = false;
            try
            {
                if (!entity.IsActive) entity.IsActive = true; //bydefault record will be active

                AuditTrial audit = null; //to save audit details
                if (TableInfo.NeedsHistory)
                {
                    //begin transaction
                    isTransactHere = BeginTransaction();
                    audit = new AuditTrial();
                }

                IDbCommand command = Connection.CreateCommand();
                DB.CreateAddCommand(command, entity, audit, columns);
                var keyId = ExecuteScalar(command);
                //get identity
                if (TableInfo.PrimaryKeyAttribute.IsIdentity && entity.IsKeyIdEmpty())
                {
                    //ExecuteScalar with Identity will always return long, hence converting it to type of Primary Key
                    entity.KeyId = Convert.ChangeType(keyId, entity.KeyId.GetType());
                }
                if (TableInfo.NeedsHistory)
                {
                    //Save Audit Trial
                    AuditTrialRepository auditTrialRepo = new AuditTrialRepository(Transaction);
                    auditTrialRepo.Add(entity, RecordOperationEnum.Add, TableInfo, audit);
                    if (isTransactHere) Commit();
                }
                return entity.KeyId;
            }
            catch
            {
                if (isTransactHere) Rollback();
                throw;
            }
        }

        #endregion

        #region Update

        public bool Update(T entity)
        {
            return Update(entity, null);
        }

        public bool Update(T entity, string columns, T oldEntity=null)
        {
            bool isTransactHere = false;

            try
            {
                AuditTrial audit = null;
                if (TableInfo.NeedsHistory)
                {
                    audit = new AuditTrial();
                    isTransactHere = BeginTransaction();

                    if(oldEntity == null)
                    {
                        oldEntity = ReadOne(entity.KeyId);
                    }
                }

                IDbCommand command = Connection.CreateCommand();

                bool isUpdateNeeded = DB.CreateUpdateCommand(command, entity, oldEntity, audit, columns);

                if (isUpdateNeeded)
                {
                    int result = ExecuteNonQuery(command);

                    if (result <= 0)
                    {
                        //record not found or concurrency violation
                        throw new VersionNotFoundException("Record doesn't exists or modified by another user");
                    }
                    entity.VersionNo++; //increment versionno when save is successful

                    if (TableInfo.NeedsHistory)
                    {
                        //Save History
                        AuditTrialRepository auditTrialRepo = new AuditTrialRepository(Transaction);
                        auditTrialRepo.Add(entity, RecordOperationEnum.Update, TableInfo, audit);
                        if(isTransactHere) Commit();
                    }
                }
                return true;
            }
            catch
            {
                if(isTransactHere) Rollback();
                throw;
            }
        }

        #endregion

        #region Delete

        /// <summary>
        /// Hard delete for entity with NoIsActive flag else Soft Delete
        /// </summary>
        /// <param name="id">Record Id</param>
        /// <returns>deletion status</returns>
        public bool Delete(object id)
        {
            return Delete(id, 0, false);
        }

        /// <summary>
        /// Hard delete for entity with NoIsActive flag else Soft Delete
        /// </summary>
        /// <param name="id">Record Id</param>
        /// <param name="versionno">Version of deleting record</param>
        /// <returns>deletion status</returns>
        public bool Delete(object id, Int32 versionNo)
        {
            return Delete(id, versionNo, false);
        }

        /// <summary>
        /// Hard delete irrelevant of NoIsActive flag
        /// </summary>
        /// <param name="id">Record Id</param>
        public bool HardDelete(object id)
        {
            return Delete(id, 0, true);
        }

        /// <summary>
        /// Hard delete irrelevant of NoIsActive flag
        /// </summary>
        /// <param name="id">Record Id</param>
        /// <param name="versionno">Version of deleting record</param>
        public bool HardDelete(object id, Int32 versionNo)
        {
            return Delete(id, versionNo, true);
        }

        bool Delete(object id, Int32 versionNo, bool isHardDelete)
        {
            bool isTransactHere = false;

            try
            {
                if (TableInfo.NeedsHistory)
                {
                    isTransactHere = BeginTransaction();
                }

                IDbCommand command = Connection.CreateCommand();

                StringBuilder commandText = new StringBuilder();
                if (TableInfo.NoIsActive || isHardDelete)
                {
                    commandText.Append($"DELETE FROM {TableInfo.FullName}");
                }
                else
                {
                    commandText.Append($"UPDATE {TableInfo.FullName} SET {Config.ISACTIVE_COLUMN.Name}={DB.BITFALSEVALUE}, {Config.VERSIONNO_COLUMN.Name}={Config.VERSIONNO_COLUMN.Name}+1");

                    if (!TableInfo.NoUpdatedOn)
                    {
                        commandText.Append($",{Config.UPDATEDON_COLUMN.Name}={DB.CURRENTDATETIMESQL}");
                    }
                    if (!TableInfo.NoUpdatedBy)
                    {
                        commandText.Append($",{Config.UPDATEDBY_COLUMN.Name}=@{Config.UPDATEDBY_COLUMN.Name}");
                        command.AddInParameter(Config.UPDATEDBY_COLUMN.Name, Config.UPDATEDBY_COLUMN.ColumnDbType, Session.CurrentUserId);
                    }
                }
                commandText.Append($" WHERE {TableInfo.PrimaryKeyColumn.Name}=@{TableInfo.PrimaryKeyColumn.Name}");
                command.AddInParameter(TableInfo.PrimaryKeyColumn.Name, TableInfo.PrimaryKeyColumn.ColumnDbType, id);

                if (versionNo > 0)
                {
                    commandText.Append($" AND {Config.VERSIONNO_COLUMN.Name}=@{Config.VERSIONNO_COLUMN.Name}");
                    command.AddInParameter(Config.VERSIONNO_COLUMN.Name, Config.VERSIONNO_COLUMN.ColumnDbType, versionNo);
                }

                command.CommandText = commandText.ToString();
                command.CommandType = CommandType.Text;
                int result = ExecuteNonQuery(command);

                if (result <= 0)
                {
                    //record not found or concurrency violation
                    throw new VersionNotFoundException("Record doesn't exists or modified by another user");
                }

                if (TableInfo.NeedsHistory)
                {
                    //Save History
                    AuditTrialRepository auditTrialRepo = new AuditTrialRepository(Transaction);

                    auditTrialRepo.Add(id, versionNo, RecordOperationEnum.Delete, TableInfo);

                    if(isTransactHere) Commit();
                }
                return true;
            }
            catch
            {
                if (isTransactHere) Rollback();
                throw;
            }
        }

        #endregion

        #region Recover

        public bool Recover(object id)
        {
            return Recover(id, 0);
        }

        public bool Recover(object id, Int32 versionNo)
        {
            if (TableInfo.NoIsActive)
            {
                throw new InvalidOperationException("Recover can be used for entities with soft delete ability");
            }

            bool isTransactHere = false;

            try
            {
                if (TableInfo.NeedsHistory)
                {
                    isTransactHere = BeginTransaction();
                }

                IDbCommand command = Connection.CreateCommand();

                StringBuilder commandText = new StringBuilder();
                commandText.Append($"UPDATE {TableInfo.FullName} SET {Config.ISACTIVE_COLUMN.Name}={DB.BITTRUEVALUE}, {Config.VERSIONNO_COLUMN.Name}={Config.VERSIONNO_COLUMN.Name}+1");

                if (!TableInfo.NoUpdatedOn)
                {
                    commandText.Append($",{Config.UPDATEDON_COLUMN.Name}={DB.CURRENTDATETIMESQL}");
                }
                if (!TableInfo.NoUpdatedBy)
                {
                    commandText.Append($",{Config.UPDATEDBY_COLUMN.Name}=@{Config.UPDATEDBY_COLUMN.Name}");
                    command.AddInParameter(Config.UPDATEDBY_COLUMN.Name, Config.UPDATEDBY_COLUMN.ColumnDbType, Session.CurrentUserId);
                }
                
                commandText.Append($" WHERE {TableInfo.PrimaryKeyColumn.Name}=@{TableInfo.PrimaryKeyColumn.Name}");
                command.AddInParameter(TableInfo.PrimaryKeyColumn.Name, TableInfo.PrimaryKeyColumn.ColumnDbType, id);

                if (versionNo > 0)
                {
                    commandText.Append($" AND {Config.VERSIONNO_COLUMN.Name}=@{Config.VERSIONNO_COLUMN.Name}");
                    command.AddInParameter(Config.VERSIONNO_COLUMN.Name, Config.VERSIONNO_COLUMN.ColumnDbType, versionNo);
                }

                command.CommandText = commandText.ToString();
                command.CommandType = CommandType.Text;
                int result = ExecuteNonQuery(command);

                if (result <= 0)
                {
                    //record not found or concurrency violation
                    throw new VersionNotFoundException("Record doesn't exists or modified by another user");
                }

                if (TableInfo.NeedsHistory)
                {
                    //Save History
                    AuditTrialRepository auditTrialRepo = new AuditTrialRepository(Transaction);

                    auditTrialRepo.Add(id, versionNo, RecordOperationEnum.Recover, TableInfo);

                    if(isTransactHere) Commit();
                }
                return true;
            }
            catch
            {
                if (isTransactHere) Rollback();
                throw;
            }
        }

        #endregion

        #region Read

        /// <summary>
        /// To check record exists 
        /// </summary>
        /// <param name="id">Record Id</param>
        /// <returns></returns>
        public bool Exists(object id)
        {
            IDbCommand command = Connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = $"SELECT 1 FROM {TableInfo.FullName} WHERE {TableInfo.PrimaryKeyColumn.Name}=@{TableInfo.PrimaryKeyColumn.Name}";
            command.AddInParameter(TableInfo.PrimaryKeyColumn.Name, TableInfo.PrimaryKeyColumn.ColumnDbType, id);
            return ExecuteScalar(command) != null;
        }

        /// <summary>
        /// Read one record with specific ID
        /// </summary>
        /// <param name="id">record id</param>
        /// <param name="columns">optional specific columns to retrieve. Default: all columns</param>
        /// <returns></returns>
        public T ReadOne(object id, string columns = null)
        {
            //Get columns from Entity attributes loaded in TableInfo
            if (string.IsNullOrEmpty(columns)) columns = String.Join(",", TableInfo.DefaultReadColumns);

            IDbCommand cmd = Connection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = $"SELECT {columns} FROM {TableInfo.FullName} WHERE {TableInfo.PrimaryKeyColumn.Name}=@{TableInfo.PrimaryKeyColumn.Name}";
            cmd.AddInParameter(TableInfo.PrimaryKeyColumn.Name, TableInfo.PrimaryKeyColumn.ColumnDbType, id);

            bool isConOpen = IsConnectionOpen();

            if (!isConOpen) Connection.Open();

            using (IDataReader rdr = ExecuteReader(cmd))
            {
                var func = ReaderCache<T>.GetFromCache(rdr);

                if (rdr != null && rdr.Read())
                {
                    return func(rdr);
                }
                rdr.Close();
                if (!isConOpen) Connection.Close();
            }

            return null;
        }

        /// <summary>
        /// Read value of one column for a given record
        /// </summary>
        /// <typeparam name="R">Type of value</typeparam>
        /// <param name="id">record id</param>
        /// <param name="column">column name</param>
        /// <returns></returns>
        public R ReadOne<R>(object id, string column)
        {
            IDbCommand cmd = Connection.CreateCommand();
            cmd.CommandText = $"SELECT {column} FROM {TableInfo.FullName} WHERE {TableInfo.PrimaryKeyColumn.Name}=@{TableInfo.PrimaryKeyColumn.Name}";
            cmd.AddInParameter(TableInfo.PrimaryKeyColumn.Name, TableInfo.PrimaryKeyColumn.ColumnDbType, id);
            return Query<R>(cmd);
        }

        /// <summary>
        /// Read all records: fastest
        /// </summary>
        /// <param name="status">optional get Active, InActive or all Records Default: All records</param>
        /// <returns>IEnumerable list of entities</returns>
        public IEnumerable<T> ReadAll(RecordStatusEnum status)
        {
            return ReadAll(null, null, null, null, status);
        }

        /// <summary>
        /// Read all records: fastest
        /// </summary>
        /// <param name="columns">optional specific columns to retrieve. Default: all columns</param>
        /// <returns>IEnumerable list of entities</returns>
        public IEnumerable<T> ReadAll(string columns, RecordStatusEnum status)
        {
            return ReadAll(columns, null, null, null, status);
        }

        /// <summary>
        /// Read all records: fastest
        /// </summary>
        /// <param name="columns">optional specific columns to retrieve. Default: all columns</param>
        /// <param name="criteria">optional parameterised criteria e.g. "department=@Department"</param>
        /// <param name="parameters">optional dynamic parameter object e.g. new {Department = "IT"} </param>
        /// <returns>IEnumerable list of entities</returns>
        public IEnumerable<T> ReadAll(string columns, string criteria, object parameters, RecordStatusEnum status)
        {
            return ReadAll(columns, criteria, parameters, null, status);
        }

        /// <summary>
        /// Read all records: fastest
        /// </summary>
        /// <param name="columns">optional specific columns to retrieve. Default: all columns</param>
        /// <param name="criteria">optional parameterised criteria e.g. "department=@Department"</param>
        /// <param name="parameters">optional dynamic parameter object e.g. new {Department = "IT"} </param>
        /// <param name="orderBy">optional order by e.g. "department ASC"</param>
        /// <param name="status">optional get Active, InActive or all Records Default: All records</param>
        /// <returns>IEnumerable list of entities</returns>
        public IEnumerable<T> ReadAll(string columns = null, string criteria = null, object parameters = null, string orderBy=null, RecordStatusEnum status = RecordStatusEnum.All)
        {
            //Get columns from Entity attributes loaded in TableInfo
            if (string.IsNullOrEmpty(columns)) columns = String.Join(",", TableInfo.DefaultReadColumns);

            IDbCommand cmd = Connection.CreateCommand();
            cmd.CommandType = CommandType.Text;

            string query = $"SELECT {columns} FROM {TableInfo.FullName} ";
            StringBuilder commandText = DB.CreateSelectCommand(cmd, query, criteria, parameters);
            if (!TableInfo.NoIsActive) DB.AppendStatusCriteria(commandText, status);
            if (orderBy != null)
            {
                if (!orderBy.ToLowerInvariant().Contains("orderby")) commandText.Append(" ORDER BY ");
                commandText.Append(orderBy);
            }
            cmd.CommandText = commandText.ToString();

            bool isConOpen = IsConnectionOpen();
            if (!isConOpen) Connection.Open();
            using (IDataReader rdr = ExecuteReader(cmd))
            {
                var func = ReaderCache<T>.GetFromCache(rdr);
                if (rdr != null)
                {
                    while (rdr.Read()) yield return func(rdr);
                }
                rdr.Close();
                rdr.Dispose();
                if (!isConOpen) Connection.Close();
            }
        }

        #endregion

        #region Record count

        /// <summary>
        /// Count Number of records in table
        /// </summary>
        /// <param name="status">optional get Active, InActive or all Records Default: All records</param>
        /// <returns></returns>
        public long Count(RecordStatusEnum status)
        {
            return Count(null, null, status);
        }

        /// <summary>
        /// Count Number of records in table with criteria
        /// </summary>
        /// <param name="criteria">Criteria as AnonymousType. e.g. new { Parameter1 = value } </param>
        /// <param name="status">optional get Active, InActive or all Records Default: All records</param>
        /// <returns></returns>
        public long Count(string criteria = null, object parameters = null, RecordStatusEnum status = RecordStatusEnum.All)
        {
            IDbCommand cmd = Connection.CreateCommand();

            string query = $"SELECT COUNT({(TableInfo.PrimaryKeyColumn != null ? TableInfo.PrimaryKeyColumn.Name : "0")}) FROM {TableInfo.FullName}";

            StringBuilder commandText = DB.CreateSelectCommand(cmd, query, criteria, parameters);

            if (!TableInfo.NoIsActive)DB.AppendStatusCriteria(commandText, status);

            cmd.CommandType = CommandType.Text;
            cmd.CommandText = commandText.ToString();

            bool isConOpen = IsConnectionOpen();
            if (!isConOpen) Connection.Open();
            long result = (long)cmd.ExecuteScalar();
            if (!isConOpen) Connection.Close();
            return result;
        }

        #endregion

        #region Read History

        /// <summary>
        /// Read all history of a given record
        /// </summary>
        /// <param name="id">Record Id</param>
        /// <returns>List of audit for this record</returns>
        public IEnumerable<T> ReadHistory(object id)
        {
            AuditTrialRepository auditRepo = new AuditTrialRepository(Connection);

            return auditRepo.ReadAll<T>(TableInfo.Name, id);
        }

        #endregion

        #region Read with Query

        /// <summary>
        /// Read All with Query
        /// </summary>
        /// <param name="query">SQL Query or procedure with parameters.e.g. SELECT * FROM Employee WHERE department=@Department</param>
        /// <param name="parameters">Dynamic parameter object e.g. new {Department = "IT"}</param>
        /// <param name="commandType">Text or Procedure or Table Direct</param>
        /// <returns>IEnumerable List of entities</returns>
        public IEnumerable<T> ReadAllQuery(string query, object parameters, CommandType commandType = CommandType.Text)
        {
            IDbCommand cmd = Connection.CreateCommand();
            cmd.CommandType = commandType;
            cmd.CommandText = DB.CreateSelectCommand(cmd, query, parameters).ToString();

            bool isConOpen = IsConnectionOpen();
            if (!isConOpen) Connection.Open();
            using (IDataReader rdr = ExecuteReader(cmd))
            {
                var func = ReaderCache<T>.GetFromCache(rdr);
                if (rdr != null)
                {
                    while (rdr.Read()) yield return func(rdr);
                }
                rdr.Close();
                rdr.Dispose();
                if (!isConOpen) Connection.Close();
            }
        }

        #endregion

        #region Read with paging

        #endregion

        #endregion

        #region Execute methods

        public R Query<R>(string query)
        {
            var result = ExecuteScalar(query);

            if (result is R)
                return (R)result;
            else
                return default(R);
        }

        public R Query<R>(IDbCommand cmd)
        {
            var result = ExecuteScalar(cmd);

            if (result is R)
                return (R)result;
            else
                return default(R);
        }

        public DbDataAdapter CreateDataAdapter(IDbCommand command)
        {
            DbProviderFactory factory = DbProviderFactories.GetFactory((DbConnection)command.Connection);
            DbDataAdapter adapter = factory.CreateDataAdapter();
            adapter.SelectCommand = (DbCommand)command;
            return adapter;
        }

        public DataSet ExecuteDataSet(string query, string tableNames = null)
        {
            IDbCommand command = Connection.CreateCommand();
            command.CommandText = query;
            command.CommandType = CommandType.Text;

            return ExecuteDataSet(command, tableNames);
        }

        public DataSet ExecuteDataSet(IDbCommand command, string tableNames = null)
        {
            if (string.IsNullOrEmpty(tableNames)) tableNames = "Table";

            bool isConOpen = IsConnectionOpen();

            try
            {
                if (!isConOpen) Connection.Open();

                if (Transaction != null) command.Transaction = Transaction;

                DataSet result = new DataSet();

                using (DbDataAdapter adapter = CreateDataAdapter(command))
                {
                    string[] tables = tableNames.Split(',');
                    string systemCreatedTableNameRoot = "Table";
                    for (int i = 0; i < tables.Length; i++)
                    {
                        string systemCreatedTableName = (i == 0)
                                                            ? systemCreatedTableNameRoot
                                                            : systemCreatedTableNameRoot + i;

                        adapter.TableMappings.Add(systemCreatedTableName, tables[i]);
                    }

                    adapter.Fill(result);
                }

                return result;
            }
            finally
            {
                if (!isConOpen) Connection.Close(); //close if connection was opened here
            }
        }

        public IDataReader ExecuteReader(string query)
        {
            IDbCommand command = Connection.CreateCommand();
            command.CommandText = query;
            command.CommandType = CommandType.Text;

            return ExecuteReader(command);
        }

        public IDataReader ExecuteReader(IDbCommand command)
        {
            bool isConOpen = IsConnectionOpen();

            if (!isConOpen) Connection.Open();

            if (Transaction != null) command.Transaction = Transaction;

            return command.ExecuteReader();

            //can't close connection when executing datareader
        }

        public int ExecuteNonQuery(string query)
        {
            IDbCommand command = Connection.CreateCommand();
            command.CommandText = query;
            command.CommandType = CommandType.Text;

            return ExecuteNonQuery(command);
        }

        public int ExecuteNonQuery(IDbCommand command)
        {
            bool isConOpen = IsConnectionOpen();

            try
            {
                if (!isConOpen) Connection.Open();

                if (Transaction != null) command.Transaction = Transaction;

                return command.ExecuteNonQuery();
            }
            finally
            {
                if (!isConOpen) Connection.Close(); //close if connection was opened here
            }
        }

        public object ExecuteScalar(string query)
        {
            IDbCommand command = Connection.CreateCommand();
            command.CommandText = query;
            command.CommandType = CommandType.Text;

            return ExecuteScalar(command);
        }

        public object ExecuteScalar(IDbCommand command)
        {
            bool isConOpen = IsConnectionOpen();

            try
            {
                if (!isConOpen) Connection.Open();

                if (Transaction != null) command.Transaction = Transaction;

                return command.ExecuteScalar();
            }
            catch
            {
                throw;
            }
            finally
            {
                if (!isConOpen) Connection.Close(); //close if connection was opened here
            }
        }
        #endregion

        #region DDL Methods

        public bool IsTableExists()
        {
            return DBObjectExists(TableInfo.Name, DBObjectTypeEnum.Table, TableInfo.Schema);
        }

        public bool IsIndexExists(string indexName)
        {
            if (ExecuteScalar(DB.IndexExistsQuery(TableInfo.Name, indexName)) != null)
                return true;
            else
                return false;
        }

        public bool DBObjectExists(string name, DBObjectTypeEnum objectType, string schema = null)
        {
            if (ExecuteScalar(DB.DBObjectExistsQuery(name, objectType, schema)) != null)
                return true;
            else
                return false;
        }

        public bool CreateTable()
        {
            if(!IsTableExists()) ExecuteNonQuery(DB.CreateTableQuery(typeof(T)));

            return true;
        }

        public bool DropTable()
        {
            if (IsTableExists()) ExecuteNonQuery(DB.DropTableQuery(typeof(T)));

            return true;
        }

        public bool CreateIndex(string indexName, string columns, bool isUnique)
        {
            if (!IsIndexExists(indexName)) ExecuteNonQuery(DB.CreateIndexQuery(TableInfo.Name, indexName, columns, isUnique));

            return true;
        }

        #endregion
    }
}
