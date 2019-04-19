using System;
using System.Collections.Generic;
using System.Data;

namespace Vega
{
    internal class AuditTrialRepository<Entity> : Repository<AuditTrial>
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

            //check for index on RecordId field
            if (IsAuditTableExists)
            {
                CreateIndex(Config.VegaConfig.AuditRecordIdIndexName, $"{Config.VegaConfig.AuditTableNameColumnName},{Config.VegaConfig.AuditRecordIdColumnName}", false);
            }
            
            IsAuditTableExistsCheckDone = true;
        }

        internal bool Add(Entity entity, RecordOperationEnum operation, TableAttribute tableInfo, AuditTrial audit)
        {
            CreateTableIfNotExist();

            audit.OperationType = operation;
            //Remove EntityBase 12-Apr-19
            audit.RecordId = tableInfo.GetKeyId(entity).ToString();
            //Remove EntityBase 12-Apr-19
            if (!tableInfo.NoVersionNo)
                audit.RecordVersionNo = (operation == RecordOperationEnum.Insert ? 1 : tableInfo.GetVersionNo(entity)); //always 1 for new insert

            audit.TableName = tableInfo.Name;
            audit.Details = audit.GenerateString();
            audit.AuditTrailId = (long)Add(audit);

            return true;
        }

        //for delete & restore
        internal bool Add(object recordId, int? recordVersionNo, object updatedBy, RecordOperationEnum operation, TableAttribute tableInfo)
        {
            if (operation != RecordOperationEnum.Delete && operation != RecordOperationEnum.Recover)
                throw new InvalidOperationException("Invalid call to this method. This method shall be call for Delete and Recover operation only.");

            CreateTableIfNotExist();

            AuditTrial audit = new AuditTrial
            {
                OperationType = operation,
                RecordId = recordId.ToString(),
                RecordVersionNo =   (recordVersionNo??0) + 1,
                TableName = tableInfo.Name,
                CreatedBy = updatedBy
            };

            audit.AppendDetail(Config.ISACTIVE_COLUMN.Name, !(operation == RecordOperationEnum.Delete), DbType.Boolean);
            audit.Details = audit.GenerateString();

            Add(audit);

            return true;
        }

        internal IEnumerable<T> ReadAll<T>(string tableName, object id) where T : new()
        {
            TableAttribute tableInfo = EntityCache.Get(typeof(T));

            var lstAudit = ReadAll(null, $"{Config.VegaConfig.AuditTableNameColumnName}=@TableName AND {Config.VegaConfig.AuditRecordIdColumnName}=@RecordId", new { TableName = tableName, RecordId = id.ToString() }, Config.VegaConfig.CreatedOnColumnName + " ASC");

            T current = default;

            foreach (AuditTrial audit in lstAudit)
            {
                audit.Split();

                if (current == null)
                {
                    //create new object
                    current = new T();

                    //Remove EntityBase 12-Apr-19
                    if(!tableInfo.NoCreatedBy)
                        tableInfo.SetCreatedBy(current, audit.CreatedBy);

                    //Remove EntityBase 12-Apr-19
                    if (!tableInfo.NoCreatedOn)
                        tableInfo.SetCreatedOn(current, audit.CreatedOn);

                    //Remove EntityBase 12-Apr-19
                    tableInfo.PrimaryKeyColumn.SetAction(current, audit.RecordId.ConvertTo(tableInfo.PrimaryKeyColumn.Property.PropertyType));
                }
                else
                {
                    //Remove EntityBase 12-Apr-19
                    //TODO: Test case pending
                    current = EntityCache.CloneObjectWithIL(current);
                }

                //render modified values
                foreach (AuditTrailDetail detail in audit.lstAuditDetails)
                {
                    if (detail.Value == null) continue;

                    //find column
                    tableInfo.Columns.TryGetValue(detail.Column, out ColumnAttribute col);
                    if (col == null) continue;

                    object convertedValue = null;

                    if (col.Property.PropertyType == typeof(bool) || col.Property.PropertyType == typeof(bool?))
                        convertedValue = (detail.Value.ToString() == "1" ? true : false);
                    else if (col.Property.PropertyType == typeof(DateTime) || col.Property.PropertyType == typeof(DateTime?))
                        convertedValue = detail.Value.ToString().FromSQLDateTime();
                    else
                        convertedValue = detail.Value.ConvertTo(col.Property.PropertyType);

                    col.SetAction(current, convertedValue);
                }

                //Remove EntityBase 12-Apr-19
                tableInfo.SetVersionNo(current, audit.RecordVersionNo);
                tableInfo.SetUpdatedOn(current, audit.CreatedOn);
                tableInfo.SetUpdatedBy(current, audit.CreatedBy);

                yield return current;
            }
        }
    }
}
