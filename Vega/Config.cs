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
    /// <summary>
    /// Vega configuration class
    /// </summary>
    public static class Config
    {

        #region 1. Framework Configuration

        /// <summary>
        /// Implement framework database Concurrency check before updating/deleting records
        /// </summary>
        public const bool DB_CONCURRENCY_CHECK = true;

        /// <summary>
        /// Global flags - all tables doesn't contains VersionNo column
        /// </summary>
        public const bool DEFAULT_NO_VERSIONNO = false;
        /// <summary>
        /// Global flags - all tables doesn't contains CreatedBy column
        /// </summary>
        public const bool DEFAULT_NO_CREATEDBY = false;
        /// <summary>
        /// Global flags - all tables doesn't contains CreatedOn column
        /// </summary>
        public const bool DEFAULT_NO_CREATEDON = false;
        /// <summary>
        /// Global flags - all tables doesn't contains UpdatedBy column
        /// </summary>
        public const bool DEFAULT_NO_UPDATEDON = false;
        /// <summary>
        /// Global flags - all tables doesn't contains UpdatedOn column
        /// </summary>
        public const bool DEFAULT_NO_UPDATEDBY = false;
        /// <summary>
        /// Global flags - all tables doesn't contains IsActive column
        /// </summary>
        public const bool DEFAULT_NO_ISACTIVE = false;
        /// <summary>
        /// Global flags - all tables needs history
        /// </summary>
        public const bool DEFAULT_NEEDS_HISTORY = true;

        #endregion

        #region 2. Common Fields Configuration

        /// <summary>
        /// Default versionno column name
        /// </summary>
        public const string VERSIONNO_COLUMNNAME = "versionno";
        /// <summary>
        /// Default CreatedBy column name
        /// </summary>
        public const string CREATEDBY_COLUMNNAME = "createdby";
        /// <summary>
        /// Default CreatedByName column name
        /// </summary>
        public const string CREATEDBYNAME_COLUMNNAME = "createdbyname";
        /// <summary>
        /// Default CreatedOn column name
        /// </summary>
        public const string CREATEDON_COLUMNNAME = "createdon";
        /// <summary>
        /// Default UpdatedBy column name
        /// </summary>
        public const string UPDATEDBY_COLUMNNAME = "updatedby";
        /// <summary>
        /// Default UpdatedByName column name
        /// </summary>
        public const string UPDATEDBYNAME_COLUMNNAME = "updatedbyname";
        /// <summary>
        /// Default UpdatedOn column name
        /// </summary>
        public const string UPDATEDON_COLUMNNAME = "updatedon";
        /// <summary>
        /// Default IsActive column name
        /// </summary>
        public const string ISACTIVE_COLUMNNAME = "isactive";

        #endregion

        #region 3. User Table Configuration

        /// <summary>
        /// User Table database schema
        /// </summary>
        public const string USER_TABLESCHEMA = "dbo";
        /// <summary>
        /// User Table Name
        /// </summary>
        public const string USER_TABLENAME = "users";
        /// <summary>
        /// User KeyId column name
        /// </summary>
        public const string USER_KEYFIELD = "userid";
        /// <summary>
        /// User Fullname database column
        /// </summary>
        public const string USER_FULLNAME_DBCOLUMN = "fullname";

        #endregion

        #region 4. Audit Table Configuration

        /// <summary>
        /// Audit Table name
        /// </summary>
        public const string AUDIT_TABLENAME = "audittrial";
        /// <summary>
        /// Audit keyfield column name
        /// </summary>
        public const string AUDIT_KEYCOLUMNNAME = "audittrialid";
        /// <summary>
        /// Audit OperationType column name
        /// </summary>
        public const string AUDIT_OPERATIONTYPECOLUMNNAME = "operationtype";
        /// <summary>
        /// Audit TableName column name
        /// </summary>
        public const string AUDIT_TABLENAMECOLUMNNAME = "tablename";
        /// <summary>
        /// Audit RecordId column name
        /// </summary>
        public const string AUDIT_RECORDIDCOLUMNNAME = "recordid";
        /// <summary>
        /// Audit RecordVersionNo column name
        /// </summary>
        public const string AUDIT_RECORDVERSIONCOLUMNNAME = "recordversionno";
        /// <summary>
        /// Audit details column name
        /// </summary>
        public const string AUDIT_DETAILSCOLUMNNAME = "details";
        /// <summary>
        /// Audit indexname
        /// </summary>
        public const string AUDIT_RECORDIDINDEXNAME = "idx_recordid";

        #endregion

        #region Static Constructor & Properties

        static Config()
        {
            VERSIONNO_COLUMN = EntityCache.Get(typeof(EntityBase)).Columns[VERSIONNO_COLUMNNAME];
            CREATEDBY_COLUMN = EntityCache.Get(typeof(EntityBase)).Columns[CREATEDBY_COLUMNNAME];
            CREATEDBYNAME_COLUMN = EntityCache.Get(typeof(EntityBase)).Columns[CREATEDBYNAME_COLUMNNAME];
            CREATEDON_COLUMN = EntityCache.Get(typeof(EntityBase)).Columns[CREATEDON_COLUMNNAME];
            UPDATEDBY_COLUMN = EntityCache.Get(typeof(EntityBase)).Columns[UPDATEDBY_COLUMNNAME];
            UPDATEDBYNAME_COLUMN = EntityCache.Get(typeof(EntityBase)).Columns[UPDATEDBYNAME_COLUMNNAME];
            UPDATEDON_COLUMN = EntityCache.Get(typeof(EntityBase)).Columns[UPDATEDON_COLUMNNAME];
            ISACTIVE_COLUMN = EntityCache.Get(typeof(EntityBase)).Columns[ISACTIVE_COLUMNNAME];
        }

        internal static ColumnAttribute VERSIONNO_COLUMN { get; set; }
        internal static ColumnAttribute CREATEDBY_COLUMN { get; set; }
        internal static ColumnAttribute CREATEDBYNAME_COLUMN { get; set; }
        internal static ColumnAttribute CREATEDON_COLUMN { get; set; }
        internal static ColumnAttribute UPDATEDBY_COLUMN { get; set; }
        internal static ColumnAttribute UPDATEDBYNAME_COLUMN { get; set; }
        internal static ColumnAttribute UPDATEDON_COLUMN { get; set; }
        internal static ColumnAttribute ISACTIVE_COLUMN { get; set; }

        #endregion
    }
}
