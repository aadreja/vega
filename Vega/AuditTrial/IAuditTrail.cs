using System;
using System.Collections.Generic;
using System.Data;

namespace Vega
{
    /// <summary>
    /// 
    /// </summary>
    public interface IAuditTrail
    {
        /// <summary>
        /// 
        /// </summary>
        long AuditTrailId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        RecordOperationEnum OperationType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        string TableName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        string RecordId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        int? RecordVersionNo { get; set; }

        /// <summary>
        /// 
        /// </summary>
        object CreatedBy { get; set; }

        /// <summary>
        /// 
        /// </summary>
        DateTime CreatedOn { get; set; }

        /// <summary>
        /// 
        /// </summary>
        List<IAuditTrailDetail> lstAuditTrailDetail { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="column"></param>
        /// <param name="newValue"></param>
        /// <param name="type"></param>
        /// <param name="oldValue"></param>
        void AppendDetail(string column, object newValue, DbType type, object oldValue);
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IAuditTrailDetail
    {
        /// <summary>
        /// 
        /// </summary>
        long AuditTrailDetailId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        long AuditTrailId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        string ColumnName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        int ColumnType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        string OldValue { get; set; }

        /// <summary>
        /// 
        /// </summary>
        string NewValue { get; set; }
    }
}
