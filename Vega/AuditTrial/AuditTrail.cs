using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;

namespace Vega
{
    /// <summary>
    /// 
    /// </summary>
    public class AuditTrailDetail : IAuditTrailDetail
    {
        /// <summary>
        /// 
        /// </summary>
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

    /// <summary>
    /// 
    /// </summary>
    [Table(Name = "audittrail", NoUpdatedBy =true, NoUpdatedOn =true, NoIsActive =true, NoVersionNo =true)]
    public class AuditTrail : IAuditTrail
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
        public string Details { get; set; }

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

        #region methods

        //limitation: any &quot; in string will be replaced by " [double quotes]
        static readonly char ESC_QUOTED = '"';
        static Regex columnSepRegEx = new Regex(",(?=(?:(?:[^\"]*\"){2})*[^\"]*$)", RegexOptions.Compiled); //column seperator Regular Expression
        static Regex newValueSepRegEx = new Regex("=(?=(?:(?:[^\"]*\"){2})*[^\"]*$)", RegexOptions.Compiled);  //New value seperator Regular Expression
        static Regex oldValueSepRegEx = new Regex("\\|(?=(?:(?:[^\"]*\"){2})*[^\"]*$)", RegexOptions.Compiled);  //New value seperator Regular Expression

        /// <summary>
        /// 
        /// </summary>
        /// <param name="column"></param>
        /// <param name="newValue"></param>
        /// <param name="type"></param>
        /// <param name="oldValue"></param>
        public void AppendDetail(string column, object newValue, DbType type, object oldValue)
        {
            if (newValue == null) return; //null values don't go in history
            if (lstAuditTrailDetail == null) lstAuditTrailDetail = new List<IAuditTrailDetail>();

            lstAuditTrailDetail.Add(new AuditTrailDetail()
            {
                ColumnName = column,
                NewValue = GetStringForDb(newValue, type),
                ColumnType = (int)type,
                OldValue = GetStringForDb(oldValue, type)
            });
        }

        internal string GetStringForDb(object value, DbType type)
        {
            if (value == null)
                return null;

            string strValue;
            if (type == DbType.Boolean)
                strValue = (bool)value ? "1" : "0";
            else if (type == DbType.Date)
                strValue = ((DateTime)value).ToSQLDate();
            else if (type == DbType.DateTime)
                strValue = ((DateTime)value).ToSQLDateTime();
            else if (Helper.IsNumber(value))
                strValue = value.ToString();
            else
                //escap string for proper split, replace all quotes inside string with &quot;
                strValue = ESC_QUOTED + value.ToString().Replace("\"", "&quot;") + ESC_QUOTED;

            return strValue;
        }

        internal string GenerateString()
        {
            if (lstAuditTrailDetail == null)
                return string.Empty;
            else
                return string.Join(",", 
                    lstAuditTrailDetail.Select(p => $"{p.ColumnName}={p.NewValue}{(!string.IsNullOrEmpty(p.OldValue) ? "|" + p.OldValue : "")}"));
        }

        internal void Split()
        {
            lstAuditTrailDetail = new List<IAuditTrailDetail>();

            string[] columns = columnSepRegEx.Split(Details);

            foreach (string strColumn in columns)
            {
                string[] values = newValueSepRegEx.Split(strColumn);

                if (values.Length <= 1)
                    continue;

                string[] oldNewValue = oldValueSepRegEx.Split(values[1]);

                string newValue = null;
                string oldValue = null;

                if (oldNewValue.Length == 0)
                    newValue = values[1];
                else
                {
                    newValue = oldNewValue[0];
                    if(oldNewValue.Length > 1)
                        oldValue = oldNewValue[1];
                }

                lstAuditTrailDetail.Add(new AuditTrailDetail()
                {
                    ColumnName = values[0],
                    //remove ESC_QUOTES and add quotes present in string
                    NewValue = newValue?.Replace("\"", string.Empty).Replace("&quot;", "\""), 
                    OldValue = oldValue?.Replace("\"", string.Empty).Replace("&quot;", "\"")
                });
            }
        }

        #endregion

    }
}
