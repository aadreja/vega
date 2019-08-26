using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;

namespace Vega
{

    internal class AuditTrailDetail
    {
        internal string Column { get; set; }
        internal object Value { get; set; }
    }

    [Table(NoUpdatedBy =true, NoUpdatedOn =true, NoIsActive =true, NoVersionNo =true)]
    internal class AuditTrial
    {

        [PrimaryKey(true)]
        public long AuditTrailId { get; set; }

        public RecordOperationEnum OperationType { get; set; }

        [Column(ColumnDbType = DbType.String, Size = 255)]
        public string TableName { get; set; }

        [Column(ColumnDbType = DbType.String, Size = 50)]
        public string RecordId { get; set; }

        public string Details { get; set; }

        public int? RecordVersionNo { get; set; }

        public object CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }

        #region methods

        //limitation: any &quot; in string will be replaced by " [double quotes]
        static char ESC_QUOTED = '"';
        static Regex columnSepRegEx = new Regex(",(?=(?:(?:[^\"]*\"){2})*[^\"]*$)", RegexOptions.Compiled); //column seperator Regular Expression
        static Regex valueSepRegEx = new Regex("=(?=(?:(?:[^\"]*\"){2})*[^\"]*$)", RegexOptions.Compiled);  //value seperator Regular Expression

        internal List<AuditTrailDetail> lstAuditDetails;

        internal void AppendDetail(string column, object value, DbType type)
        {
            if (value == null) return; //null values don't go in history
            if (lstAuditDetails == null) lstAuditDetails = new List<AuditTrailDetail>();
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
                strValue = ESC_QUOTED + value.ToString().Replace("\"", "&quot;") + ESC_QUOTED; //escap string for proper split, replace all quotes inside string with &quot;

            lstAuditDetails.Add(new AuditTrailDetail()
            {
                Column = column,
                Value = strValue,
            });
        }

        internal string GenerateString()
        {
            if (lstAuditDetails == null)
                return string.Empty;
            else
                return string.Join(",", lstAuditDetails.Select(p => $"{p.Column}={p.Value}"));
        }

        internal void Split()
        {
            lstAuditDetails = new List<AuditTrailDetail>();

            string[] columns = columnSepRegEx.Split(Details);

            foreach (string strColumn in columns)
            {
                string[] values = valueSepRegEx.Split(strColumn);

                if (values.Length > 0)
                {
                    lstAuditDetails.Add(new AuditTrailDetail()
                    {
                        Column = values[0],
                        Value = values[1].Replace("\"", string.Empty).Replace("&quot;", "\"") //remove ESC_QUOTES and add quotes present in string
                    });
                }
            }
        }

        #endregion

    }
}
