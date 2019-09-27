using System;
using System.Collections.Generic;
using System.Data;

namespace Vega
{
    /// <summary>
    /// Interface to implemente custom AuditTrail
    /// </summary>
    public interface IAuditTrail
    {
        /// <summary>
        /// Unique key
        /// </summary>
        long AuditTrailId { get; set; }
        /// <summary>
        /// Operation Type - Add, Update, Delete, Recover
        /// </summary>
        RecordOperationEnum OperationType { get; set; }
        /// <summary>
        /// Name of table whose audit trail is being recorded
        /// </summary>
        string TableName { get; set; }
        /// <summary>
        /// Record Id whose audit trail is being recorded. Its primary key id of specified table
        /// </summary>
        string RecordId { get; set; }
        /// <summary>
        /// Optional if maintaining VersionNo for each table record
        /// </summary>
        int? RecordVersionNo { get; set; }
        /// <summary>
        /// User who modified record
        /// </summary>
        object CreatedBy { get; set; }
        /// <summary>
        /// DateTime when record was modified
        /// </summary>
        DateTime CreatedOn { get; set; }
        /// <summary>
        /// List of changes to the record. Each columns New and Old Values
        /// </summary>
        List<IAuditTrailDetail> lstAuditTrailDetail { get; set; }
        /// <summary>
        /// Append modified details to this list
        /// </summary>
        /// <param name="column">Column which was modified</param>
        /// <param name="newValue">New value of column</param>
        /// <param name="type">DbType of Column</param>
        /// <param name="oldValue">Old value of column</param>
        void AppendDetail(string column, object newValue, DbType type, object oldValue);
    }

    /// <summary>
    /// Interface to track List of changes to the record. Each columns New and Old Values
    /// </summary>
    public interface IAuditTrailDetail
    {
        /// <summary>
        /// Primary Key
        /// </summary>
        long AuditTrailDetailId { get; set; }
        /// <summary>
        /// Foreign key to AuditTrail primary key
        /// </summary>
        long AuditTrailId { get; set; }
        /// <summary>
        /// Column which was modified
        /// </summary>
        string ColumnName { get; set; }
        /// <summary>
        /// DbType of column
        /// </summary>
        int ColumnType { get; set; }
        /// <summary>
        /// Old Value for column. It will be NULL for inserts
        /// </summary>
        string OldValue { get; set; }
        /// <summary>
        /// New Value for column
        /// </summary>
        string NewValue { get; set; }
    }
}
