/*
 Description: Vega - Fastest ORM with enterprise features
 Author: Ritesh Sutaria
 Date: 9-Dec-2017
 Home Page: https://github.com/aadreja/vega
            http://www.vegaorm.com
*/

using System;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Vega
{
    /// <summary>
    /// Vega Configuration Abstract Class
    /// </summary>
    public static class Config
    {
        /// <summary>
        /// Default values
        /// </summary>
        static Config()
        {
            //Set default values

            //1. Framework configuration
            DbConcurrencyCheck = true;
            NoVersionNo = true;
            NoCreatedBy = true;
            NoCreatedOn = true;
            NoUpdatedBy = true;
            NoUpdatedOn = true;
            NoIsActive = true;
            NeedsHistory = false;

            //2. Common Fields
            VersionNoColumnName = "versionno";
            CreatedUpdatedByColumnType = DbType.Int32;
            CreatedByColumnName = "createdby";
            CreatedOnColumnName = "createdon";
            CreatedByNameColumnName = "createdbyname";
            UpdatedByColumnName = "updatedby";
            UpdatedOnColumnName = "updatedon";
            UpdatedByNameColumnName = "updatedbyname";
            IsActiveColumnName = "isactive";

            //3. User table configuration
            UserTableSchema = "dbo";
            UserTableName = "users";
            UserKeyField = "userid";
            UserFullNameDbColumn = "fullname";

            //4. Audit table configuration. Default is AuditTrailKeyValue
            AuditType = AuditTypeEnum.CSV;
        }

        #region 1. Framework Configuration

        /// <summary>
        /// Implement framework database Concurrency check before updating/deleting records
        /// </summary>
        public static bool DbConcurrencyCheck { get; set; }

        /// <summary>
        /// Global flags - all tables doesn't contains VersionNo column
        /// </summary>
        public static bool NoVersionNo { get; set; }
        /// <summary>
        /// Global flags - all tables doesn't contains CreatedBy column
        /// </summary>
        public static bool NoCreatedBy { get; set; }
        /// <summary>
        /// Global flags - all tables doesn't contains CreatedOn column
        /// </summary>
        public static bool NoCreatedOn { get; set; }
        /// <summary>
        /// Global flags - all tables doesn't contains UpdatedBy column
        /// </summary>
        public static bool NoUpdatedOn { get; set; }
        /// <summary>
        /// Global flags - all tables doesn't contains UpdatedOn column
        /// </summary>
        public static bool NoUpdatedBy { get; set; }
        /// <summary>
        /// Global flags - all tables doesn't contains IsActive column
        /// </summary>
        public static bool NoIsActive { get; set; }
        /// <summary>
        /// Global flags - all tables needs history
        /// </summary>
        public static bool NeedsHistory { get; set; }

        #endregion

        #region 2. Common Fields Configuration
        internal static ColumnAttribute VERSIONNO_COLUMN { get; set; }
        /// <summary>
        /// Default versionno column name
        /// </summary>
        public static string VersionNoColumnName
        {
            get { return VERSIONNO_COLUMN.Name; }
            set { VERSIONNO_COLUMN = ParseColumn("VersionNo", value, DbType.Int32, "Version No"); }
        }

        internal static ColumnAttribute CREATEDBY_COLUMN { get; set; }

        /// <summary>
        /// Default CreatedBy column name
        /// </summary>
        public static string CreatedByColumnName
        {
            get { return CREATEDBY_COLUMN.Name; }
            set { CREATEDBY_COLUMN = ParseColumn("CreatedBy", value, CreatedUpdatedByColumnType, "Created By"); }
        }

        private static DbType _createdUpdatedByColumnType;

        /// <summary>
        /// Default PrimaryKey column Type
        /// </summary>
        public static DbType CreatedUpdatedByColumnType
        {
            get
            {
                return _createdUpdatedByColumnType;
            }
            set
            {
                _createdUpdatedByColumnType = value;


                //Required to change the CreatedBy and UpdatedBy column type once again on change of 
                //Config.CreatedUpdatedByColumnType to reflect the changes in the Add, Update and Delete command
                //
                //WARNING: 
                //Kindly, keep in mind that this is required when datatype of PrimaryKey column is the same as that of CreatedBy and UpdatedBy column
                if (CREATEDBY_COLUMN != null)
                    CREATEDBY_COLUMN.ColumnDbType = value;

                if (UPDATEDBY_COLUMN != null)
                    UPDATEDBY_COLUMN.ColumnDbType = value;
            }
        }

        internal static ColumnAttribute CREATEDBYNAME_COLUMN { get; set; }

        /// <summary>
        /// Default CreatedByName column name
        /// </summary>
        public static string CreatedByNameColumnName
        {
            get { return CREATEDBYNAME_COLUMN.Name; }
            set { CREATEDBYNAME_COLUMN = ParseColumn("CreatedByName", value, DbType.String, "Created By Name"); }
        }

        internal static ColumnAttribute CREATEDON_COLUMN { get; set; }

        /// <summary>
        /// Default CreatedOn column name
        /// </summary>
        public static string CreatedOnColumnName
        {
            get { return CREATEDON_COLUMN.Name; }
            set { CREATEDON_COLUMN = ParseColumn("CreatedOn", value, DbType.DateTime, "Created On"); }
        }

        internal static ColumnAttribute UPDATEDBY_COLUMN { get; set; }

        /// <summary>
        /// Default UpdatedBy column name
        /// </summary>
        public static string UpdatedByColumnName
        {
            get { return UPDATEDBY_COLUMN.Name; }
            set { UPDATEDBY_COLUMN = ParseColumn("UpdatedBy", value, CreatedUpdatedByColumnType, "Updated By"); }
        }

        internal static ColumnAttribute UPDATEDBYNAME_COLUMN { get; set; }

        /// <summary>
        /// Default UpdatedByName column name
        /// </summary>
        public static string UpdatedByNameColumnName
        {
            get { return UPDATEDBYNAME_COLUMN.Name; }
            set { UPDATEDBYNAME_COLUMN = ParseColumn("UpdatedByName", value, DbType.String, "Updated By Name"); }
        }

        internal static ColumnAttribute UPDATEDON_COLUMN { get; set; }

        /// <summary>
        /// Default UpdatedOn column name
        /// </summary>
        public static string UpdatedOnColumnName
        {
            get { return UPDATEDON_COLUMN.Name; }
            set { UPDATEDON_COLUMN = ParseColumn("UpdatedOn", value, DbType.DateTime, "Updated On"); }
        }

        internal static ColumnAttribute ISACTIVE_COLUMN { get; set; }

        /// <summary>
        /// Default IsActive column name
        /// </summary>
        public static string IsActiveColumnName
        {
            get { return ISACTIVE_COLUMN.Name; }
            set { ISACTIVE_COLUMN = ParseColumn("IsActive", value, DbType.Boolean, "Is Active"); }
        }

        static ColumnAttribute ParseColumn(string propertyName, string columnName, DbType columnDbType, string title)
        {
            PropertyInfo property = typeof(EntityDefault).GetProperty(propertyName);
            ColumnAttribute column = new ColumnAttribute()
            {
                Name = columnName,
                ColumnDbType = columnDbType,
                IsColumnDbTypeDefined = true,
                Title = title,
            };
            column.SetPropertyInfo(property, typeof(EntityDefault));
            return column;
        }
        #endregion

        #region 3. User Table Configuration

        /// <summary>
        /// User Table database schema
        /// </summary>
        public static string UserTableSchema { get; set; }
        /// <summary>
        /// User Table Name
        /// </summary>
        public static string UserTableName { get; set; }
        /// <summary>
        /// User KeyId column name
        /// </summary>
        public static string UserKeyField { get; set; }
        /// <summary>
        /// User Fullname database column
        /// </summary>
        public static string UserFullNameDbColumn { get; set; }

        static AuditTypeEnum auditType;
        /// <summary>
        /// To configure AuditType CSV, KeyValue or Custom
        /// </summary>
        public static AuditTypeEnum AuditType { get => auditType; set { auditType = value; SetAuditTrailType(); } }

        static Type auditTrailType;
        /// <summary>
        /// Type of AuditTrail entity. Default is AuditTrailKeyValue
        /// You can override using Dependency Injection by creating your own entity using IAuditTrail and IAuditTrailRepository interface 
        /// e.g.  config.AuditTrailType = typeof(AuditTrailKeyValue)
        /// </summary>
        public static Type AuditTrailType
        {
            get
            {
                return auditTrailType;
            }
            set
            {
                if (!typeof(IAuditTrail).IsAssignableFrom(value))
                {
                    throw new InvalidCastException(value.Name + " must implement IAuditTrail");
                }
                auditTrailType = value;
            }
        }

        static Type auditTrailRepositoryType;
        /// <summary>
        /// Type of AuditTrailRepository. Default is AuditTrailKeyValueRepository
        /// You can override using Dependency Injection by creating your own entity using IAuditTrailRepository interface
        /// Since their is dynamic Entity for AuditTrailRepository use empty Entity.
        /// e.g. config.AuditTrailRepositoryType = typeof(AuditTrailKeyValueRepository&lt;&gt;)
        /// </summary>
        public static Type AuditTrailRepositoryType
        {
            get { return auditTrailRepositoryType; }
            set
            {
                if (!(value.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAuditTrailRepository<>))))
                {
                    throw new InvalidCastException(value.Name + " must implement IAuditTrailRepository");
                }
                auditTrailRepositoryType = value;
            }
        }

        internal static Type AuditTrialRepositoryGenericType<T>()
        {
            return AuditTrailRepositoryType.MakeGenericType(typeof(T));
        }

        internal static void SetAuditTrailType()
        {
            if (AuditType == AuditTypeEnum.CSV)
            {
                AuditTrailType = typeof(AuditTrail);
                AuditTrailRepositoryType = typeof(AuditTrailRepository<>);
            }
            else if (AuditType == AuditTypeEnum.KeyValue)
            {
                AuditTrailType = typeof(AuditTrailKeyValue);
                AuditTrailRepositoryType = typeof(AuditTrailKeyValueRepository<>);
            }
            else
            {
                //clear types when audittype is NULL
                AuditTrailType = null;
                AuditTrailRepositoryType = null;
            }
        }

        #endregion
    }
}
