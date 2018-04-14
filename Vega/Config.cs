/*
 Description: Vega - Fastest ORM with enterprise features
 Author: Ritesh Sutaria
 Date: 9-Dec-2017
 Home Page: https://github.com/aadreja/vega
            http://www.vegaorm.com
*/

using System.Data;

namespace Vega
{
    /// <summary>
    /// Vega Configuration Abstract Class
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// Default values
        /// </summary>
        public Configuration()
        {
            //Set default values

            //1. Framework configuration
            DbConcurrencyCheck = true;
            NoVersionNo = false;
            NoCreatedBy = false;
            NoCreatedOn = false;
            NoUpdatedBy = false;
            NoUpdatedOn = false;
            NoIsActive = false;
            NeedsHistory = true;

            //2. Common Fields
            VersionNoColumnName = "versionno";
            CreatedUpdatedByColumnType = DbType.Int32;
            CreatedByColumnName = "createdby";
            CreatedOnColumnName = "createdon";
            CreatedByNameColumnName = "updatedbyname";
            UpdatedByColumnName = "updatedby";
            UpdatedOnColumnName = "updatedon";
            UpdatedByNameColumnName = "updatedbyname";
            IsActiveColumnName = "isactive";

            //3. User table configuration
            UserTableSchema = "dbo";
            UserTableName = "users";
            UserKeyField = "userid";
            UserFullNameDbColumn = "fullname";

            //4. Audit table configuration
            AuditTableName = "audittrial";
            AuditKeyColumnName = "audittrialid";
            AuditOperationTypeColumnName = "operationtype";
            AuditTableNameColumnName = "tablename";
            AuditRecordIdColumnName = "recordid";
            AuditRecordVersionColumnName = "recordversionno";
            AuditDetailsColumnName = "details";
            AuditRecordIdIndexName = "idx_recordid";
        }

        #region 1. Framework Configuration

        /// <summary>
        /// Implement framework database Concurrency check before updating/deleting records
        /// </summary>
        public bool DbConcurrencyCheck { get; set; }

        /// <summary>
        /// Global flags - all tables doesn't contains VersionNo column
        /// </summary>
        public bool NoVersionNo { get; set; }
        /// <summary>
        /// Global flags - all tables doesn't contains CreatedBy column
        /// </summary>
        public bool NoCreatedBy { get; set; }
        /// <summary>
        /// Global flags - all tables doesn't contains CreatedOn column
        /// </summary>
        public bool NoCreatedOn { get; set; }
        /// <summary>
        /// Global flags - all tables doesn't contains UpdatedBy column
        /// </summary>
        public bool NoUpdatedOn { get; set; }
        /// <summary>
        /// Global flags - all tables doesn't contains UpdatedOn column
        /// </summary>
        public bool NoUpdatedBy { get; set; }
        /// <summary>
        /// Global flags - all tables doesn't contains IsActive column
        /// </summary>
        public bool NoIsActive { get; set; }
        /// <summary>
        /// Global flags - all tables needs history
        /// </summary>
        public bool NeedsHistory { get; set; }

        #endregion

        #region 2. Common Fields Configuration

        /// <summary>
        /// Default versionno column name
        /// </summary>
        public string VersionNoColumnName { get; set; }
        /// <summary>
        /// Default CreatedBy column name
        /// </summary>
        public string CreatedByColumnName { get; set; }
        /// <summary>
        /// Default CreatedBy column Type
        /// </summary>
        public DbType CreatedUpdatedByColumnType { get; set; }
        /// <summary>
        /// Default CreatedByName column name
        /// </summary>
        public string CreatedByNameColumnName { get; set; }
        /// <summary>
        /// Default CreatedOn column name
        /// </summary>
        public string CreatedOnColumnName { get; set; }
        /// <summary>
        /// Default UpdatedBy column name
        /// </summary>
        public string UpdatedByColumnName { get; set; }
        /// <summary>
        /// Default UpdatedByName column name
        /// </summary>
        public string UpdatedByNameColumnName { get; set; }
        /// <summary>
        /// Default UpdatedOn column name
        /// </summary>
        public string UpdatedOnColumnName { get; set; }
        /// <summary>
        /// Default IsActive column name
        /// </summary>
        public string IsActiveColumnName { get; set; }

        #endregion

        #region 3. User Table Configuration

        /// <summary>
        /// User Table database schema
        /// </summary>
        public string UserTableSchema { get; set; }
        /// <summary>
        /// User Table Name
        /// </summary>
        public string UserTableName { get; set; }
        /// <summary>
        /// User KeyId column name
        /// </summary>
        public string UserKeyField { get; set; }
        /// <summary>
        /// User Fullname database column
        /// </summary>
        public string UserFullNameDbColumn { get; set; }

        #endregion

        #region 4. Audit Table Configuration

        /// <summary>
        /// Audit Table name
        /// </summary>
        public string AuditTableName { get; set; }
        /// <summary>
        /// Audit keyfield column name
        /// </summary>
        public string AuditKeyColumnName { get; set; }
        /// <summary>
        /// Audit OperationType column name
        /// </summary>
        public string AuditOperationTypeColumnName { get; set; }
        /// <summary>
        /// Audit TableName column name
        /// </summary>
        public string AuditTableNameColumnName { get; set; }
        /// <summary>
        /// Audit RecordId column name
        /// </summary>
        public string AuditRecordIdColumnName { get; set; }
        /// <summary>
        /// Audit RecordVersionNo column name
        /// </summary>
        public string AuditRecordVersionColumnName { get; set; }
        /// <summary>
        /// Audit details column name
        /// </summary>
        public string AuditDetailsColumnName { get; set; }
        /// <summary>
        /// Audit indexname
        /// </summary>
        public string AuditRecordIdIndexName { get; set; }

        #endregion

    }

    /// <summary>
    /// Vega configuration class
    /// </summary>
    public static class Config
    {
        
        #region Static Constructor & Properties

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration"></param>
        public static void Configure(Configuration configuration)
        {
            VegaConfig = configuration;
            Parse();
        }

        static Config()
        {
            VegaConfig = new Configuration();
            Parse();
        }

        static void Parse()
        {
            VERSIONNO_COLUMN = EntityCache.Get(typeof(EntityBase)).Columns[VegaConfig.VersionNoColumnName];

            CREATEDBY_COLUMN = EntityCache.Get(typeof(EntityBase)).Columns[VegaConfig.CreatedByColumnName];
            CREATEDBY_COLUMN.ColumnDbType = VegaConfig.CreatedUpdatedByColumnType;
            CREATEDBYNAME_COLUMN = EntityCache.Get(typeof(EntityBase)).Columns[VegaConfig.CreatedByNameColumnName];
            CREATEDON_COLUMN = EntityCache.Get(typeof(EntityBase)).Columns[VegaConfig.CreatedOnColumnName];

            UPDATEDBY_COLUMN = EntityCache.Get(typeof(EntityBase)).Columns[VegaConfig.UpdatedByColumnName];
            UPDATEDBY_COLUMN.ColumnDbType = VegaConfig.CreatedUpdatedByColumnType;
            UPDATEDBYNAME_COLUMN = EntityCache.Get(typeof(EntityBase)).Columns[VegaConfig.UpdatedByNameColumnName];
            UPDATEDON_COLUMN = EntityCache.Get(typeof(EntityBase)).Columns[VegaConfig.UpdatedOnColumnName];

            ISACTIVE_COLUMN = EntityCache.Get(typeof(EntityBase)).Columns[VegaConfig.IsActiveColumnName];
        }

        /// <summary>
        /// Vega Configuration Parameters
        /// </summary>
        public static Configuration VegaConfig { get; internal set; }

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
