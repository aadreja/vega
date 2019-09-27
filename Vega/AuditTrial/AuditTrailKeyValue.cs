using System;
using System.Collections.Generic;
using System.Data;

namespace Vega
{
    /// <summary>
    /// KeyValue Implementation of AuditTrail
    /// </summary>
    [Table( Name = "audittrail", NoUpdatedBy = true, NoUpdatedOn = true, NoIsActive = true, NoVersionNo = true)]
    public class AuditTrailKeyValue : IAuditTrail
    {
        /// <summary>
        /// Primary Key
        /// </summary>
        [PrimaryKey(true)]
        public long AuditTrailId { get; set; }

        /// <summary>
        /// Operation Type - Add, Update, Delete, Recover
        /// </summary>
        public RecordOperationEnum OperationType { get; set; }

        /// <summary>
        /// Name of table whose audit trail is being recorded
        /// </summary>
        [Column(ColumnDbType = DbType.String, Size = 255)]
        public string TableName { get; set; }

        /// <summary>
        /// Record Id whose audit trail is being recorded. Its primary key id of specified table
        /// </summary>
        [Column(ColumnDbType = DbType.String, Size = 50)]
        public string RecordId { get; set; }

        /// <summary>
        /// Optional if maintaining VersionNo for each table record
        /// </summary>
        public int? RecordVersionNo { get; set; }

        /// <summary>
        /// User who modified record
        /// </summary>
        public object CreatedBy { get; set; }

        /// <summary>
        /// DateTime when record was modified
        /// </summary>
        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// List of changes to the record. Each columns New and Old Values
        /// </summary>
        [IgnoreColumn]
        public List<IAuditTrailDetail> lstAuditTrailDetail { get; set; }

        /// <summary>
        /// Append modified details to this list
        /// </summary>
        /// <param name="column">Column which was modified</param>
        /// <param name="newValue">New value of column</param>
        /// <param name="type">DbType of Column</param>
        /// <param name="oldValue">Old value of column</param>
        public void AppendDetail(string column, object newValue, DbType type, object oldValue)
        {
            if (newValue == null && newValue == null) return; //null values don't go in history

            if (lstAuditTrailDetail == null) lstAuditTrailDetail = new List<IAuditTrailDetail>();

            string strNewValue = null;
            string strOldValue = null;

            if(newValue != null)
            {
                if (type == DbType.Boolean)
                    strNewValue = (bool)newValue ? "1" : "0";
                else if (type == DbType.Date)
                    strNewValue = ((DateTime)newValue).ToSQLDate();
                else if (type == DbType.DateTime)
                    strNewValue = ((DateTime)newValue).ToSQLDateTime();
                else
                    strNewValue = newValue.ToString();
            }

            if (oldValue != null)
            {
                if (type == DbType.Boolean)
                    strOldValue = (bool)oldValue ? "1" : "0";
                else if (type == DbType.Date)
                    strOldValue = ((DateTime)oldValue).ToSQLDate();
                else if (type == DbType.DateTime)
                    strOldValue = ((DateTime)oldValue).ToSQLDateTime();
                else
                    strOldValue = oldValue.ToString();
            }

            lstAuditTrailDetail.Add(new AuditTrailKeyValueDetail()
            {
                ColumnName = column,
                NewValue = strNewValue,
                OldValue = strOldValue,
                ColumnType = (int)type
            });
        }
    }

    /// <summary>
    /// Implementatin of KeyValue to track List of changes to the record. Each columns New and Old Values
    /// </summary>
    [Table(Name = "audittraildetail", NoCreatedBy = true, NoCreatedOn = true, NoUpdatedBy = true, NoUpdatedOn = true, NoIsActive = true, NoVersionNo = true)]
    public class AuditTrailKeyValueDetail : IAuditTrailDetail
    {
        /// <summary>
        /// Primary key
        /// </summary>
        [PrimaryKey(true)]
        public long AuditTrailDetailId { get; set; }
        /// <summary>
        /// Foreign key to AuditTrail primary key
        /// </summary>
        public long AuditTrailId { get; set; }
        /// <summary>
        /// Column which was modified
        /// </summary>
        public string ColumnName { get; set; }
        /// <summary>
        /// DbType of column
        /// </summary>
        public int ColumnType { get; set; }
        /// <summary>
        /// Old Value for column. It will be NULL for inserts
        /// </summary>
        public string OldValue { get; set; }
        /// <summary>
        /// New Value for column
        /// </summary>
        public string NewValue { get; set; }
    }

}
