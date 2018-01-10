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
    public static class Config
    {

        #region 1. Framework Configuration

        /// <summary>
        /// Implement framework database Concurrency check before updating/deleting records
        /// </summary>
        public const bool DB_CONCURRENCY_CHECK = true;

        /// <summary>
        /// Glbal flags - whether all table contains default fields on each table
        /// </summary>
        public const bool DEFAULT_NO_VERSIONNO = false;
        public const bool DEFAULT_NO_CREATEDBY = false;
        public const bool DEFAULT_NO_CREATEDON = false;
        public const bool DEFAULT_NO_UPDATEDON = false;
        public const bool DEFAULT_NO_UPDATEDBY = false;
        public const bool DEFAULT_NO_ISACTIVE = false;
        public const bool DEFAULT_NEEDS_HISTORY = true;

        #endregion

        #region 2. Common Fields Configuration

        public const string VERSIONNO_COLUMNNAME = "versionno";
        public const string CREATEDBY_COLUMNNAME = "createdby";
        public const string CREATEDBYNAME_COLUMNNAME = "createdbyname";
        public const string CREATEDON_COLUMNNAME = "createdon";
        public const string UPDATEDBY_COLUMNNAME = "updatedby";
        public const string UPDATEDBYNAME_COLUMNNAME = "updatedbyname";
        public const string UPDATEDON_COLUMNNAME = "updatedon";
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

        public const string AUDIT_TABLENAME = "audittrial";
        public const string AUDIT_KEYCOLUMNNAME = "audittrialid";
        public const string AUDIT_OPERATIONTYPECOLUMNNAME = "operationtype";
        public const string AUDIT_TABLENAMECOLUMNNAME = "tablename";
        public const string AUDIT_RECORDIDCOLUMNNAME = "recordid";
        public const string AUDIT_RECORDVERSIONCOLUMNNAME = "recordversionno";
        public const string AUDIT_DETAILSCOLUMNNAME = "details";
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
