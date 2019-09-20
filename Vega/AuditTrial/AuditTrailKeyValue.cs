using System;
using System.Collections.Generic;
using System.Data;

namespace Vega
{
    /// <summary>
    /// 
    /// </summary>
    [Table( Name = "audittrail", NoUpdatedBy = true, NoUpdatedOn = true, NoIsActive = true, NoVersionNo = true)]
    public class AuditTrailKeyValue : IAuditTrail
    {
        /// <summary>
        /// 
        /// </summary>
        [PrimaryKey(true)]
        public long AuditTrailId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public RecordOperationEnum OperationType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Column(ColumnDbType = DbType.String, Size = 255)]
        public string TableName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Column(ColumnDbType = DbType.String, Size = 50)]
        public string RecordId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int? RecordVersionNo { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public object CreatedBy { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [IgnoreColumn]
        public List<IAuditTrailDetail> lstAuditTrailDetail { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="column"></param>
        /// <param name="newValue"></param>
        /// <param name="type"></param>
        /// <param name="oldValue"></param>
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
    /// 
    /// </summary>
    [Table(Name = "audittraildetail", NoCreatedBy = true, NoCreatedOn = true, NoUpdatedBy = true, NoUpdatedOn = true, NoIsActive = true, NoVersionNo = true)]
    public class AuditTrailKeyValueDetail : IAuditTrailDetail
    {
        /// <summary>
        /// 
        /// </summary>
        [PrimaryKey(true)]
        public long AuditTrailDetailId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public long AuditTrailId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int ColumnType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string OldValue { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string NewValue { get; set; }
    }

}
