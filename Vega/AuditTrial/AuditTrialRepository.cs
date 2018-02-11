using System;
using System.Collections.Generic;
using System.Data;

namespace Vega
{
    internal class AuditTrialRepository : Repository<AuditTrial>
    {
        #region constructors

        public AuditTrialRepository(IDbConnection con, Session currentSession) : base(con, currentSession) { }
        public AuditTrialRepository(IDbTransaction tran, Session currentSession) : base(tran, currentSession) { }

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

            //check for index on RecordId field
            if (IsAuditTableExists)
            {
                CreateIndex(Config.AUDIT_RECORDIDINDEXNAME, $"{Config.AUDIT_TABLENAMECOLUMNNAME},{Config.AUDIT_RECORDIDCOLUMNNAME}", false);
            }
            
            IsAuditTableExistsCheckDone = true;
        }

        internal bool Add(EntityBase entity, RecordOperationEnum operation, TableAttribute tableInfo, AuditTrial audit)
        {
            CreateTableIfNotExist();

            audit.OperationType = operation;
            audit.RecordId = entity.KeyId.ToString();
            audit.RecordVersionNo = (operation == RecordOperationEnum.Insert ? 1 : entity.VersionNo); //always 1 for new insert
            audit.TableName = tableInfo.Name;
            audit.Details = audit.GenerateString();

            audit.KeyId = Add(audit);

            return true;
        }

        //for delete & restore
        internal bool Add(object recordId, int recordVersionNo, RecordOperationEnum operation, TableAttribute tableInfo)
        {
            if (operation != RecordOperationEnum.Delete && operation != RecordOperationEnum.Recover)
                throw new InvalidOperationException("Invalid call to this method. This method shall be call for Delete and Recover operation only.");

            CreateTableIfNotExist();

            AuditTrial audit = new AuditTrial
            {
                OperationType = operation,
                RecordId = recordId.ToString(),
                RecordVersionNo = recordVersionNo+1,
                TableName = tableInfo.Name,
            };
            audit.AppendDetail(Config.ISACTIVE_COLUMN.Name, !(operation == RecordOperationEnum.Delete), DbType.Boolean);
            audit.Details = audit.GenerateString();

            Add(audit);

            return true;
        }

        internal IEnumerable<T> ReadAll<T>(string tableName, object id) where T : EntityBase, new()
        {
            TableAttribute tableInfo = EntityCache.Get(typeof(T));

            var lstAudit = ReadAll(null, $"{Config.AUDIT_TABLENAMECOLUMNNAME}=@TableName AND {Config.AUDIT_RECORDIDCOLUMNNAME}=@RecordId", new { TableName = tableName, RecordId = id.ToString() }, Config.CREATEDON_COLUMNNAME + " ASC");

            T current = null;

            foreach (AuditTrial audit in lstAudit)
            {
                audit.Split();

                if (current == null)
                {
                    //create new object
                    current = new T
                    {
                        CreatedBy = audit.CreatedBy,
                        CreatedOn = audit.CreatedOn,
                    };

                    current.KeyId = audit.RecordId.ConvertTo(tableInfo.PrimaryKeyColumn.Property.PropertyType);
                }
                else current = (T)current.ShallowCopy(); //create copy of current object

                //render modified values
                foreach (AuditTrailDetail detail in audit.lstAuditDetails)
                {
                    if (detail.Value == null) continue;

                    //find column
                    tableInfo.Columns.TryGetValue(detail.Column, out ColumnAttribute col);
                    if (col == null) continue;

                    object convertedValue = null;

                    if (col.Property.PropertyType == typeof(bool))
                        convertedValue = (detail.Value.ToString() == "1" ? true : false);
                    else if (col.Property.PropertyType == typeof(DateTime))
                        convertedValue = detail.Value.ToString().FromSQLDateTime();
                    else
                        convertedValue = detail.Value.ConvertTo(col.Property.PropertyType);

                    col.SetMethod.Invoke(current, new object[] { convertedValue });
                }

                current.VersionNo = audit.RecordVersionNo;
                current.UpdatedOn = audit.CreatedOn;
                current.UpdatedBy = audit.CreatedBy;

                yield return current;
            }
        }
    }
}
