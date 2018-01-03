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

        #region 2. Common Fields configuration

        /// <summary>
        /// Version No database column
        /// </summary>
        public static readonly ColumnAttribute VERSIONNO_COLUMN = 
            new ColumnAttribute()
            {
                ColumnDbType = DbType.Int32,
                Name = "versionno",
                Title = "Version"
            };

        /// <summary>
        /// Created By database column
        /// </summary>
        public static readonly ColumnAttribute CREATEDBY_COLUMN = 
            new ColumnAttribute()
            {
                ColumnDbType = DbType.Int32,
                Name = "createdby"
            };

        public static readonly ColumnAttribute CREATEDBYNAME_COLUMN = 
            new ColumnAttribute()
            {
                ColumnDbType = DbType.String,
                Name = "createdbyname",
                Title = "Created By"
            };

        /// <summary>
        /// Created On database column
        /// </summary>
        public static readonly ColumnAttribute CREATEDON_COLUMN =
            new ColumnAttribute()
            {
                ColumnDbType = DbType.DateTime,
                Name = "createdon",
                Title = "Created On",
                IsAllowSorting = true
            };

        /// <summary>
        /// Updated By database column
        /// </summary>
        public static readonly ColumnAttribute UPDATEDBY_COLUMN =
            new ColumnAttribute()
            {
                ColumnDbType = DbType.Int32,
                Name = "updatedby"
            };

        public static readonly ColumnAttribute UPDATEDBYNAME_COLUMN =
           new ColumnAttribute()
           {
               ColumnDbType = DbType.String,
               Name = "updatedbyname",
               Title = "Updated By"
           };

        /// <summary>
        /// Updated On database column
        /// </summary>
        public static readonly ColumnAttribute UPDATEDON_COLUMN =
            new ColumnAttribute()
            {
                ColumnDbType = DbType.DateTime,
                Name = "updatedon",
                Title = "Updated On",
                IsAllowSorting = true
            };

        /// <summary>
        /// Is Active database column
        /// </summary>
        public static readonly ColumnAttribute ISACTIVE_COLUMN =
            new ColumnAttribute()
            {
                ColumnDbType = DbType.Boolean,
                Name = "isactive",
                Title = "Is Active",
            };

        #endregion

        #region 3. User table configuration

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
    }
}
