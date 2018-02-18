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
using System.Globalization;
using System.Linq;
using System.Text;

namespace Vega
{
    /// <summary>
    /// Vega core library class
    /// </summary>
    /// <typeparam name="T">Type of entity class</typeparam>
    public class Repository<T> where T : EntityBase, new()
    {
        #region Constructors

        /// <summary>
        /// default constructor
        /// </summary>
        Repository()
        {
            TableInfo = EntityCache.Get(typeof(T));
        }

        /// <summary>
        /// Constructor with specific connection
        /// </summary>
        /// <param name="connection">provide DB connection</param>
        /// <param name="currentSession">Current Session</param>
        public Repository(IDbConnection connection, Session currentSession) : this()
        {
            Connection = connection;
            DB = DBCache.Get(Connection);
            CurrentSession = currentSession;
        }

        /// <summary>
        /// Constructor with specific transaction
        /// </summary>
        /// <param name="transaction">provide DB Transaction</param>
        /// <param name="currentSession">Current Session</param>
        public Repository(IDbTransaction transaction, Session currentSession) : this()
        {
            Connection = transaction.Connection;
            Transaction = transaction;
            CurrentSession = currentSession;
            DB = DBCache.Get(Connection);
        }

        #endregion

        #region fields

        internal TableAttribute TableInfo { get; private set; }

        /// <summary>
        /// Gets or set Database specific instance
        /// </summary>
        Database DB { get; set; }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or set connection string
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or set transaction object
        /// </summary>
        public IDbTransaction Transaction { get; set; }

        /// <summary>
        /// gets or set connection object
        /// </summary>
        public IDbConnection Connection { get; set; }

        /// <summary>
        /// gets or set session object
        /// </summary>
        public Session CurrentSession { get; set; }

        #endregion

        #region Connection & Transaction methods

        /// <summary>
        /// Checks whether connection is open or not
        /// </summary>
        /// <returns>returns status of the connection</returns>
        public bool IsConnectionOpen()
        {
            if (Connection == null)
                throw new NullReferenceException("Connection is null");

            return (Connection.State == ConnectionState.Open);
        }

        /// <summary>
        /// Begins transaction and sets current Transaction object
        /// </summary>
        /// <returns></returns>
        public bool BeginTransaction()
        {
            if (Connection == null)
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

        /// <summary>
        /// commits transaction
        /// </summary>
        public void Commit()
        {
            if (Transaction != null) {
                Transaction.Commit();
                Transaction = null;
            }

        }

        /// <summary>
        /// Rollback transaction
        /// </summary>
        public void Rollback()
        {
            if (Transaction != null)
            {
                Transaction.Rollback();
                Transaction = null;
            }
        }

        #endregion

        #region CRUD

        #region Create

        /// <summary>
        /// Insert Records
        /// </summary>
        /// <param name="entity">Entity to add</param>
        /// <returns>Id of inserted record</returns>
        public object Add(T entity)
        {
            return Add(entity, null);
        }

        /// <summary>
        /// Insert Records
        /// </summary>
        /// <param name="entity">Entity to add</param>
        /// <param name="columns">Columns to include in Insert</param>
        /// <returns>Id of inserted record</returns>
        public object Add(T entity, string columns)
        {
            bool isTransactHere = false;

            bool isConOpen = IsConnectionOpen();
            if (!isConOpen) Connection.Open();

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
                DB.CreateAddCommand(command, entity, this.CurrentSession, audit, columns, false);
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
                    AuditTrialRepository auditTrialRepo = new AuditTrialRepository(Transaction, CurrentSession);
                    auditTrialRepo.Add(entity, RecordOperationEnum.Insert, TableInfo, audit);
                    if (isTransactHere) Commit();
                }
                return entity.KeyId;
            }
            catch
            {
                if (isTransactHere) Rollback();
                throw;
            }
            finally
            {
                if (!isConOpen) Connection.Close();
            }
        }

        #endregion

        #region Update

        /// <summary>
        /// Performs update on current entity and returns status
        /// </summary>
        /// <param name="entity">entity object for update</param>
        /// <returns>True if update successful else false</returns>
        public bool Update(T entity)
        {
            return Update(entity, null);
        }

        /// <summary>
        /// Performs update on current entity and returns status
        /// </summary>
        /// <param name="entity">entity object for update</param>
        /// <param name="columns">specific columns to update, comma seperated</param>
        /// <param name="oldEntity">previous entity object for audit trial. Default null.</param>
        /// <returns>True if update successful else false</returns>
        public bool Update(T entity, string columns, T oldEntity = null)
        {
            bool isTransactHere = false;
            bool isConOpen = IsConnectionOpen();
            if (!isConOpen) Connection.Open();
            try
            {
                AuditTrial audit = null;
                if (TableInfo.NeedsHistory)
                {
                    audit = new AuditTrial();
                    isTransactHere = BeginTransaction();

                    if (oldEntity == null)
                    {
                        oldEntity = ReadOne(entity.KeyId);
                    }
                }

                IDbCommand command = Connection.CreateCommand();

                bool isUpdateNeeded = DB.CreateUpdateCommand(command, entity, oldEntity, CurrentSession, audit, columns);

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
                        AuditTrialRepository auditTrialRepo = new AuditTrialRepository(Transaction, CurrentSession);
                        auditTrialRepo.Add(entity, RecordOperationEnum.Update, TableInfo, audit);
                        if (isTransactHere) Commit();
                    }
                }
                return true;
            }
            catch
            {
                if (isTransactHere) Rollback();
                throw;
            }
            finally
            {
                if (!isConOpen) Connection.Close();
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
        /// <param name="versionNo">Version of deleting record</param>
        /// <returns>deletion status</returns>
        public bool Delete(object id, Int32 versionNo)
        {
            return Delete(id, versionNo, false);
        }

        /// <summary>
        /// Hard delete irrelevant of NoIsActive flag
        /// </summary>
        /// <param name="id">Record Id</param>
        /// <returns>deletion status</returns>
        public bool HardDelete(object id)
        {
            return Delete(id, 0, true);
        }

        /// <summary>
        /// Hard delete irrelevant of NoIsActive flag
        /// </summary>
        /// <param name="id">Record Id</param>
        /// <param name="versionNo">Version of deleting record</param>
        /// <returns>deletion status</returns>
        public bool HardDelete(object id, Int32 versionNo)
        {
            return Delete(id, versionNo, true);
        }

        /// <summary>
        /// Delete record
        /// </summary>
        /// <param name="id">id of record</param>
        /// <param name="versionNo">version no for concurrency check</param>
        /// <param name="isHardDelete">true to perform harddelete else false for soft delete(i.e mark isactive=false)</param>
        /// <returns>deletion status</returns>
        bool Delete(object id, Int32 versionNo, bool isHardDelete)
        {
            bool isTransactHere = false;
            bool isConOpen = IsConnectionOpen();
            if (!isConOpen) Connection.Open();

            try
            {
                //check virtual foreign key violation
                IsVirtualForeignKeyViolation(id);

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
                        command.AddInParameter(Config.UPDATEDBY_COLUMN.Name, Config.UPDATEDBY_COLUMN.ColumnDbType, CurrentSession.CurrentUserId);
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
                    AuditTrialRepository auditTrialRepo = new AuditTrialRepository(Transaction, CurrentSession);

                    auditTrialRepo.Add(id, versionNo, RecordOperationEnum.Delete, TableInfo);

                    if (isTransactHere) Commit();
                }
                return true;
            }
            catch
            {
                if (isTransactHere) Rollback();
                throw;
            }
            finally
            {
                if (!isConOpen) Connection.Close();
            }
        }

        #endregion

        #region Recover

        /// <summary>
        /// Recovers Soft Deleted Record with specific id. i.e. marks isactive=true
        /// </summary>
        /// <param name="id">record id</param>
        /// <returns>true if recovered else false</returns>
        public bool Recover(object id)
        {
            return Recover(id, 0);
        }

        /// <summary>
        /// Recovers Soft Deleted Record with specific id. i.e. marks isactive=true
        /// </summary>
        /// <param name="id">record id</param>
        /// <param name="versionNo">rowversion for concurrency check</param>
        /// <returns></returns>
        public bool Recover(object id, Int32 versionNo)
        {
            if (TableInfo.NoIsActive)
            {
                throw new InvalidOperationException("Recover can be used for entities with soft delete ability");
            }

            bool isTransactHere = false;
            bool isConOpen = IsConnectionOpen();
            if (!isConOpen) Connection.Open();

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
                    command.AddInParameter(Config.UPDATEDBY_COLUMN.Name, Config.UPDATEDBY_COLUMN.ColumnDbType, CurrentSession.CurrentUserId);
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
                    AuditTrialRepository auditTrialRepo = new AuditTrialRepository(Transaction, CurrentSession);

                    auditTrialRepo.Add(id, versionNo, RecordOperationEnum.Recover, TableInfo);

                    if (isTransactHere) Commit();
                }
                return true;
            }
            catch
            {
                if (isTransactHere) Rollback();
                throw;
            }
            finally
            {
                if (!isConOpen) Connection.Close();
            }
        }

        #endregion

        #region Read

        /// <summary>
        /// To Check record exists for a given Record Id
        /// </summary>
        /// <param name="id">Record Id</param>
        /// <returns>True if Record exists otherwise False</returns>
        public bool Exists(object id)
        {
            IDbCommand command = Connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = $"SELECT 1 FROM {TableInfo.FullName} WHERE {TableInfo.PrimaryKeyColumn.Name}=@{TableInfo.PrimaryKeyColumn.Name}";
            command.AddInParameter(TableInfo.PrimaryKeyColumn.Name, TableInfo.PrimaryKeyColumn.ColumnDbType, id);
            return ExecuteScalar(command) != null;
        }

        /// <summary>
        /// To Check record exists for a given Criteria
        /// </summary>
        /// <param name="criteria">parameterised criteria e.g. "department=@Department"</param>
        /// <param name="parameters">dynamic parameter object e.g. new {Department = "IT"} </param>
        /// <returns>True if Record exists otherwise False</returns>
        public bool Exists(string criteria, object parameters)
        {
            bool hasWhere = criteria.ToLowerInvariant().Contains("where");
            
            IDbCommand command = Connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = $"SELECT 1 FROM {TableInfo.FullName} {(!hasWhere ? " WHERE " : "")} {criteria}";
            ParameterCache.GetFromCache(parameters, command).Invoke(parameters, command);
            return ExecuteScalar(command) != null;
        }

        /// <summary>
        /// Read First record with specific ID
        /// </summary>
        /// <param name="id">record id</param>
        /// <param name="columns">optional specific columns to retrieve. Default: all columns</param>
        /// <returns>Entity if record found otherwise null</returns>
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
        /// Returns First Record with specific criteria
        /// </summary>
        /// <param name="columns">optional specific columns to retrieve. Default: all columns</param>
        /// <param name="criteria">parameterised criteria e.g. "department=@Department"</param>
        /// <param name="parameters">dynamic parameter object e.g. new {Department = "IT"} </param>
        /// <returns>Entity if record found otherwise null</returns>
        public T ReadOne(string criteria=null, object parameters=null, string columns = null)
        {
            bool hasWhere = criteria.ToLowerInvariant().Contains("where");

            //Get columns from Entity attributes loaded in TableInfo
            if (string.IsNullOrEmpty(columns)) columns = String.Join(",", TableInfo.DefaultReadColumns);

            IDbCommand cmd = Connection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = $"SELECT {columns} FROM {TableInfo.FullName} {(!hasWhere ? " WHERE " : "")} {criteria}";

            ParameterCache.GetFromCache(parameters, cmd).Invoke(parameters, cmd);

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
        /// <returns>Value if record found otherwise null or default</returns>
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
        /// <param name="columns">specific columns to retrieve.</param>
        /// <param name="status">get Active, InActive or all Records Default: All records</param>
        /// <returns>IEnumerable list of entities</returns>
        public IEnumerable<T> ReadAll(string columns, RecordStatusEnum status)
        {
            return ReadAll(columns, null, null, null, status);
        }

        /// <summary>
        /// Read all records: fastest
        /// </summary>
        /// <param name="columns">specific columns to retrieve. Default: all columns</param>
        /// <param name="criteria">parameterised criteria e.g. "department=@Department"</param>
        /// <param name="parameters">dynamic parameter object e.g. new {Department = "IT"} </param>
        /// <param name="status">get Active, InActive or all Records Default: All records</param>
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
        public IEnumerable<T> ReadAll(string columns = null, string criteria = null, object parameters = null, string orderBy = null, RecordStatusEnum status = RecordStatusEnum.All)
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
        /// Count Number of Records in Table
        /// </summary>
        /// <param name="status">optional get Active, InActive or all Records Default: All records</param>
        /// <returns>no of records for a given criteria</returns>
        public long Count(RecordStatusEnum status)
        {
            return Count(null, null, status);
        }

        /// <summary>
        /// Count Number of records in table with given query or criteria on current entity table
        /// </summary>
        /// <param name="queryorCriteria">optional Custom query or criteria for current entity table. e.g. "SELECT * FROM City" OR "Department=@Department" </param>
        /// <param name="parameters">optional dynamic parameter object e.g. new {Department = "IT"} </param>
        /// <param name="status">optional get Active, InActive or all Records Default: All records</param>
        /// <returns>no of records for a given query or criteria</returns>
        public long Count(string queryorCriteria = null, object parameters = null, RecordStatusEnum status = RecordStatusEnum.All)
        {
            bool isQuery = !string.IsNullOrEmpty(queryorCriteria) && queryorCriteria.ToLowerInvariant().Contains("select");

            StringBuilder query = new StringBuilder();

            if (!isQuery)
            {
                query.Append($"SELECT COUNT({(TableInfo.PrimaryKeyColumn != null ? TableInfo.PrimaryKeyColumn.Name : "0")}) FROM {TableInfo.FullName}");
                if (!string.IsNullOrEmpty(queryorCriteria)) query.Append(" WHERE " + queryorCriteria);
                if (!TableInfo.NoIsActive) DB.AppendStatusCriteria(query, status);
            }
            else
                query.Append(queryorCriteria);

            IDbCommand cmd = Connection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = DB.CreateSelectCommand(cmd, query.ToString(), parameters).ToString();

            bool isConOpen = IsConnectionOpen();
            if (!isConOpen) Connection.Open();

            long result = cmd.ExecuteScalar().Parse<long>();

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
            AuditTrialRepository auditRepo = new AuditTrialRepository(Connection, CurrentSession);

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

        #region Read Paged

        /// <summary>
        /// Read all without query, sorted for specific Page No and Page Size
        /// </summary>
        /// <param name="orderBy">Sort Columns. e.g. "department" or "department DESC"</param>
        /// <param name="pageNo">Page No to retrieve. e.g. 1</param>
        /// <param name="pageSize">Page Size. e.g. 50</param>
        /// <param name="columns">optional specific columns to retrieve. Default: all columns</param>
        /// <param name="criteria">optional parameterised criteria e.g. "department=@Department"</param>
        /// <param name="parameters">optional dynamic parameter object e.g. new {Department = "IT"} </param>
        /// <returns></returns>
        public IEnumerable<T> ReadAllPaged(string orderBy, int pageNo, int pageSize, string columns = null, string criteria=null, object parameters = null)
        {
            if (string.IsNullOrEmpty(orderBy))
                throw new MissingMemberException("Missing orderBy parameter");

            //Get columns from Entity attributes loaded in TableInfo
            if (string.IsNullOrEmpty(columns)) columns = String.Join(",", TableInfo.DefaultReadColumns);

            IDbCommand cmd = Connection.CreateCommand();
            cmd.CommandType = CommandType.Text;

            string query = $"SELECT {columns} FROM {TableInfo.FullName} ";
            StringBuilder commandText = DB.CreateSelectCommand(cmd, query, criteria); //don't pass parameter as it will be added in ReadAllPaged() function

            return ReadAllPaged(commandText.ToString(), orderBy, pageNo, pageSize, parameters);
        }

        /// <summary>
        /// Read all with Query, Sorted for specific Page No and Page Size
        /// </summary>
        /// <param name="query">parameterized query</param>
        /// <param name="orderBy">Sort Columns. e.g. "department" or "department DESC"</param>
        /// <param name="pageNo">Page No to retrieve. e.g. 1</param>
        /// <param name="pageSize">Page Size. e.g. 50</param>
        /// <param name="parameters">optional dynamic parameter object e.g. new {Department = "IT"}</param>
        /// <returns></returns>
        public IEnumerable<T> ReadAllPaged(string query, string orderBy, int pageNo, int pageSize, object parameters = null)
        {
            if (string.IsNullOrEmpty(orderBy))
                throw new MissingMemberException("Missing orderBy parameter");

            //Get columns from Entity attributes loaded in TableInfo
            IDbCommand cmd = Connection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            DB.CreateReadAllPagedCommand(cmd, query, orderBy, pageNo, pageSize, parameters);
            
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

        /// <summary>
        /// Fastest Paged ReadAll without query and OFFSET
        /// </summary>
        /// <param name="orderBy">Sort Columns. e.g. "department" or "department DESC"</param>
        /// <param name="pageSize">Page Size. e.g. 50</param>
        /// <param name="navigation">Navigation Next,Previous,First or Last</param>
        /// <param name="columns">optional specific columns to retrieve. Default: all columns</param>
        /// <param name="criteria">optional parameterised criteria e.g. "department=@Department"</param>
        /// <param name="lastOrderByColumnValues">Required for Next and Previous Navigation only. Next - Last value of all orderby column(s). Previous - First Value of all order by column(s)</param>
        /// <param name="lastKeyId">Required for Next and Previous Navigation only. Next - Last KeyId Value. Previous - First KeyId value</param>
        /// <param name="parameters">optional dynamic parameter object e.g. new {Department = "IT"}</param>
        /// <returns></returns>
        public IEnumerable<T> ReadAllPaged(string orderBy, int pageSize, PageNavigationEnum navigation, string columns = null, string criteria = null, object[] lastOrderByColumnValues = null, object lastKeyId = null, object parameters = null)
        {
            //Get columns from Entity attributes loaded in TableInfo
            if (string.IsNullOrEmpty(columns)) columns = String.Join(",", TableInfo.DefaultReadColumns);

            IDbCommand cmd = Connection.CreateCommand();
            cmd.CommandType = CommandType.Text;

            string query = $"SELECT {columns} FROM {TableInfo.FullName} ";
            StringBuilder commandText = DB.CreateSelectCommand(cmd, query, criteria); //don't pass parameter as it will be added in ReadAllPaged() function

            return ReadAllPaged(commandText.ToString(), orderBy, pageSize, navigation, lastOrderByColumnValues, lastKeyId, parameters);
        }

        /// <summary>
        /// Fastest Paged Read without OFFSET. With Query, Sorted
        /// </summary>
        /// <param name="query">parameterized query</param>
        /// <param name="orderBy">Sort Columns. e.g. "department" or "department DESC"</param>
        /// <param name="pageSize">Page Size. e.g. 50</param>
        /// <param name="navigation">Navigation Next,Previous,First or Last</param>
        /// <param name="lastOrderByColumnValues">Required for Next and Previous Navigation only. Next - Last value of all orderby column(s). Previous - First Value of all order by column(s)</param>
        /// <param name="lastKeyId">Required for Next and Previous Navigation only. Next - Last KeyId Value. Previous - First KeyId value</param>
        /// <param name="parameters">optional dynamic parameter object e.g. new {Department = "IT"}</param>
        /// <returns></returns>
        public IEnumerable<T> ReadAllPaged(string query, string orderBy, int pageSize, PageNavigationEnum navigation, object[] lastOrderByColumnValues=null, object lastKeyId=null, object parameters = null)
        {
            //Get columns from Entity attributes loaded in TableInfo
            IDbCommand cmd = Connection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            DB.CreateReadAllPagedNoOffsetCommand<T>(cmd, query, orderBy, pageSize, navigation, lastOrderByColumnValues, lastKeyId, parameters);

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

        #endregion

        #region Execute methods

        /// <summary>
        /// Performs query with parameters and returns first column of first retrieved row in a given type
        /// </summary>
        /// <typeparam name="R">Type of Value to Return</typeparam>
        /// <param name="query">SQL Query</param>
        /// <param name="parameters">Dynamic parameter(s)</param>
        /// <returns>retrieved value in given type</returns>
        public R Query<R>(string query, object parameters)
        {
            IDbCommand cmd = Connection.CreateCommand();
            cmd.CommandText = query;
            cmd.CommandType = CommandType.Text;
            ParameterCache.GetFromCache(parameters, cmd).Invoke(parameters, cmd);
            return ExecuteScalar(cmd).Parse<R>();
        }

        /// <summary>
        /// Performs query and returns first column of first retrieved row in a given type
        /// </summary>
        /// <typeparam name="R">type of value to return</typeparam>
        /// <param name="query">sql query</param>
        /// <returns>retrieved value in given type</returns>
        public R Query<R>(string query)
        {
            return ExecuteScalar(query).Parse<R>();
        }

        /// <summary>
        /// Executes command returns first column of first retrieved row in a given type
        /// </summary>
        /// <typeparam name="R">type of value to return</typeparam>
        /// <param name="cmd">IDbCommand object</param>
        /// <returns>retrieved value in given type</returns>
        public R Query<R>(IDbCommand cmd)
        {
            return ExecuteScalar(cmd).Parse<R>();
        }

#if NET461
        /// <summary>
        /// Creates dataadapter
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public DbDataAdapter CreateDataAdapter(IDbCommand command)
        {
            //TODO: DbProviderFactories is not available in .net core
            DbProviderFactory factory = DbProviderFactories.GetFactory((DbConnection)command.Connection);
            DbDataAdapter adapter = factory.CreateDataAdapter();
            adapter.SelectCommand = (DbCommand)command;
            return adapter;
        }

        /// <summary>
        /// Executes DataSet for a given query and returns DataSet
        /// </summary>
        /// <param name="query">SQL query</param>
        /// <param name="tableNames">optional name of tables</param>
        /// <returns>DataSet</returns>
        public DataSet ExecuteDataSet(string query, string tableNames = null)
        {
            IDbCommand command = Connection.CreateCommand();
            command.CommandText = query;
            command.CommandType = CommandType.Text;

            return ExecuteDataSet(command, tableNames);
        }

        /// <summary>
        /// Executes DataSet for a given Command and returns DataSet
        /// </summary>
        /// <param name="command">IDbCommand object</param>
        /// <param name="tableNames">optional name of tables</param>
        /// <returns>DataSet</returns>
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
#endif

        /// <summary>
        /// Executes Reader for a given SQL query
        /// </summary>
        /// <param name="query">SQL Query</param>
        /// <returns>IDataReader</returns>
        public IDataReader ExecuteReader(string query)
        {
            IDbCommand command = Connection.CreateCommand();
            command.CommandText = query;
            command.CommandType = CommandType.Text;

            return ExecuteReader(command);
        }

        /// <summary>
        /// Executes Reader for a given command
        /// </summary>
        /// <param name="command">IDbCommand object</param>
        /// <returns>IDataReader</returns>
        public IDataReader ExecuteReader(IDbCommand command)
        {
            bool isConOpen = IsConnectionOpen();

            if (!isConOpen) Connection.Open();

            if (Transaction != null) command.Transaction = Transaction;

            return command.ExecuteReader();

            //can't close connection when executing datareader
        }

        /// <summary>
        /// Executes NonQuery on a given query and returns no of records affected
        /// </summary>
        /// <param name="query">SQL Query</param>
        /// <returns>no of affected records</returns>
        public int ExecuteNonQuery(string query)
        {
            IDbCommand command = Connection.CreateCommand();
            command.CommandText = query;
            command.CommandType = CommandType.Text;

            return ExecuteNonQuery(command);
        }

        /// <summary>
        /// Executes NonQuery on a given command and returns no of records affected
        /// </summary>
        /// <param name="command">IDbCommand object</param>
        /// <returns>no of affected records</returns>
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

        /// <summary>
        /// Executes scalar on a given query and returns scalar value
        /// </summary>
        /// <param name="query">SQL Query</param>
        /// <returns>Returns scalar value</returns>
        public object ExecuteScalar(string query)
        {
            IDbCommand command = Connection.CreateCommand();
            command.CommandText = query;
            command.CommandType = CommandType.Text;

            return ExecuteScalar(command);
        }

        /// <summary>
        /// Executes scalar on a given command and returns scalar value
        /// </summary>
        /// <param name="command">IDbCommand object</param>
        /// <returns>Returns scalar value</returns>
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

        /// <summary>
        /// Checks whether database table exits for current entity table
        /// </summary>
        /// <returns>true if table exists else false</returns>
        public bool IsTableExists()
        {
            return DBObjectExists(TableInfo.Name, DBObjectTypeEnum.Table, TableInfo.Schema);
        }

        /// <summary>
        /// checks whether index exists on current entity table
        /// </summary>
        /// <param name="indexName">Name of index</param>
        /// <returns>true if index exits else false</returns>
        public bool IsIndexExists(string indexName)
        {
            if (ExecuteScalar(DB.IndexExistsQuery(TableInfo.Name, indexName)) != null)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Checks specific database object exists
        /// </summary>
        /// <param name="name">Name of databae object</param>
        /// <param name="objectType">type of database object</param>
        /// <param name="schema">optional schema name of database object</param>
        /// <returns>true of object exist else false</returns>
        public bool DBObjectExists(string name, DBObjectTypeEnum objectType, string schema = null)
        {
            if (ExecuteScalar(DB.DBObjectExistsQuery(name, objectType, schema)) != null)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Create Table for a given entity type
        /// </summary>
        /// <returns>true if table is created else false</returns>
        public bool CreateTable()
        {
            if (!IsTableExists()) ExecuteNonQuery(DB.CreateTableQuery(typeof(T)));

            return true;
        }

        /// <summary>
        /// Drop table of current entity type
        /// </summary>
        /// <returns>true if table is dropped else false</returns>
        public bool DropTable()
        {
            if (IsTableExists()) ExecuteNonQuery(DB.DropTableQuery(typeof(T)));

            return true;
        }

        /// <summary>
        /// Creates index and returns status
        /// </summary>
        /// <param name="indexName">Name of index</param>
        /// <param name="columns">columns to create index on. comma seperated values</param>
        /// <param name="isUnique">is unique index</param>
        /// <returns>true if index is created else false</returns>
        public bool CreateIndex(string indexName, string columns, bool isUnique)
        {
            if (!IsIndexExists(indexName)) ExecuteNonQuery(DB.CreateIndexQuery(TableInfo.Name, indexName, columns, isUnique));

            return true;
        }

        /// <summary>
        /// Gets Database Version
        /// </summary>
        public DBVersionInfo DBVersion
        {
            get { return DB.GetDBVersion(Connection); }
        }
        
        #endregion

        #region Referene Integrity Methods

        /// <summary>
        /// To check foreign key violation against this table
        /// </summary>
        /// <param name="keyId">Master Record Id</param>
        /// <returns>True i</returns>
        public bool IsVirtualForeignKeyViolation(object keyId)
        {
            if (TableInfo.VirtualForeignKeys == null || TableInfo.VirtualForeignKeys.Count == 0)
                return false;

            foreach (ForeignKey vfk in TableInfo.VirtualForeignKeys)
            {
                string query = DB.VirtualForeignKeyCheckQuery(vfk);

                if (Count(query, new { Id = keyId }) > 0)
                {
                    throw new Exception($"Virtual Foreign Key Violation. Table:{vfk.FullTableName} Column:{(!string.IsNullOrEmpty(vfk.DisplayName) ? vfk.DisplayName : vfk.ColumnName)}");
                }
            }
            return false;
        }

        #endregion
    }
}
