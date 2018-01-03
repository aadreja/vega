using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vega
{
    [Table(Schema = "prompt", Name = "audittrial", NoUpdatedBy =true, NoUpdatedOn =true, NoIsActive =true, NoVersionNo =true)]
    public class AuditTrial : EntityBase
    {

        //public override PrimaryKey KeyId
        //{
        //    get { return AuditTrailId; }
        //    set { AuditTrailId = value; }
        //}

        [Column(Title = "Id", Name = "audittrialid")]
        [PrimaryKey(true)]
        public long AuditTrailId { get; set; }

        [Column(Title = "Operation", Name = "operationtype")]
        public RecordOperationEnum OperationType { get; set; }

        [Column(Name = "tablename")]
        public String TableName { get; set; }

        [Column(Name = "recordid")]
        public object RecordId { get; set; }

        [Column(Name = "details")]
        public String Details { get; set; }

        [Column(Name = "recordversionno")]
        public Int32 RecordVersionNo { get; set; }

    }
}
