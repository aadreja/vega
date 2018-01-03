using System;
using System.Data;
using System.Text;

namespace Vega
{
    public class AuditTrialRepository : Repository<AuditTrial>
    {
        #region constructors

        public AuditTrialRepository(IDbConnection con) : base(con) { }
        public AuditTrialRepository(IDbTransaction tran) : base(tran) { }

        #endregion

        #region static properties

        static bool IsAuditTableExists { get; set; }
        static bool IsAuditTableExistsCheckDone { get; set; }

        #endregion

        void CreateTableIfNotExist()
        {
            if (IsAuditTableExistsCheckDone && IsAuditTableExists)
                return;

            IsAuditTableExists = CreateTable();
            IsAuditTableExistsCheckDone = true;
        }

        public bool Add(EntityBase entity, RecordOperationEnum operation, TableAttribute tableInfo, StringBuilder auditXML)
        {
            CreateTableIfNotExist();

            AuditTrial audit = new AuditTrial
            {
                OperationType = operation,
                RecordId = entity.KeyId,
                RecordVersionNo = (operation == RecordOperationEnum.Add ? 1 : entity.VersionNo), //always 1 for new insert
                TableName = tableInfo.Name,
                Details = $"<{tableInfo.Name}>{auditXML.ToString()}</{tableInfo.Name}>" //XML
            };

            Add(audit);

            return true;
        }

        //for delete & restore
        public bool Add(object recordId, int recordVersionNo, RecordOperationEnum operation, TableAttribute tableInfo)
        {
            if (operation != RecordOperationEnum.Delete && operation != RecordOperationEnum.Recover)
                throw new InvalidOperationException("Invalid call to this method. This method shall be call for Delete and Recover operation only.");

            CreateTableIfNotExist();

            string auditXML = $"<{Config.ISACTIVE_COLUMN.Name}>{(operation== RecordOperationEnum.Delete? DB.BITFALSEVALUE : DB.BITTRUEVALUE)}</{Config.ISACTIVE_COLUMN.Name}>";

            AuditTrial audit = new AuditTrial
            {
                OperationType = operation,
                RecordId = recordId,
                RecordVersionNo = recordVersionNo+1,
                TableName = tableInfo.Name,
                Details = $"<{tableInfo.Name}>{auditXML}</{tableInfo.Name}>" //XML
            };

            Add(audit);

            return true;
        }
    }
}
