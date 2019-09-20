using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Vega
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="Entity"></typeparam>
    public class AuditTrailRepository<Entity> : Repository<AuditTrail>, IAuditTrailRepository<Entity> where Entity : new()
    {
        #region constructors
        /// <summary>
        /// 
        /// </summary>
        /// <param name="con"></param>
        public AuditTrailRepository(IDbConnection con) : base(con)
        {
            entityTableInfo = EntityCache.Get(typeof(Entity));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tran"></param>
        public AuditTrailRepository(IDbTransaction tran) : base(tran)
        {
            entityTableInfo = EntityCache.Get(typeof(Entity));
        }

        #endregion

        readonly TableAttribute entityTableInfo;

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
                CreateIndex("recordid", $"tablename, recordid", false);
            }
            
            IsAuditTableExistsCheckDone = true;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="operation"></param>
        /// <param name="audit"></param>
        /// <returns></returns>
        public bool Add(Entity entity, RecordOperationEnum operation, IAuditTrail audit)
        {
            CreateTableIfNotExist();

            AuditTrail auditTrail = (AuditTrail)audit;

            auditTrail.OperationType = operation;
            //Remove EntityBase 12-Apr-19
            auditTrail.RecordId = entityTableInfo.GetKeyId(entity).ToString();
            //Remove EntityBase 12-Apr-19
            if (!entityTableInfo.NoVersionNo)
                auditTrail.RecordVersionNo = (operation == RecordOperationEnum.Insert ? 1 : entityTableInfo.GetVersionNo(entity)); //always 1 for new insert

            auditTrail.TableName = entityTableInfo.Name;
            auditTrail.Details = auditTrail.GenerateString();
            auditTrail.AuditTrailId = (long)Add(auditTrail);

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="recordId"></param>
        /// <param name="recordVersionNo"></param>
        /// <param name="updatedBy"></param>
        /// <param name="operation"></param>
        /// <returns></returns>
        //for delete & restore
        public bool Add(object recordId, int? recordVersionNo, object updatedBy, RecordOperationEnum operation)
        {
            if (operation != RecordOperationEnum.Delete && operation != RecordOperationEnum.Recover)
                throw new InvalidOperationException("Invalid call to this method. This method shall be call for Delete and Recover operation only.");

            CreateTableIfNotExist();

            AuditTrail audit = new AuditTrail
            {
                OperationType = operation,
                RecordId = recordId.ToString(),
                RecordVersionNo =   (recordVersionNo??0) + 1,
                TableName = entityTableInfo.Name,
                CreatedBy = updatedBy
            };

            audit.AppendDetail(Config.ISACTIVE_COLUMN.Name, !(operation == RecordOperationEnum.Delete), DbType.Boolean, (operation == RecordOperationEnum.Delete));
            audit.Details = audit.GenerateString();

            Add(audit);

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        //public IEnumerable<T> ReadAll<T>(string tableName, object id) where T : new()
        public IEnumerable<Entity> ReadAll(object id)
        {
            var lstAudit = ReadAll("*", $"tablename=@TableName AND recordid=@RecordId", new { TableName = entityTableInfo.Name, RecordId = id.ToString() }, Config.VegaConfig.CreatedOnColumnName + " ASC");

            Entity current = default;

            foreach (AuditTrail audit in lstAudit)
            {
                audit.Split();

                if (current == null)
                {
                    //create new object
                    current = new Entity();

                    if(!entityTableInfo.NoCreatedBy)
                        entityTableInfo.SetCreatedBy(current, audit.CreatedBy);

                    if (!entityTableInfo.NoCreatedOn)
                        entityTableInfo.SetCreatedOn(current, audit.CreatedOn);

                    entityTableInfo.PkColumn.SetAction(current, audit.RecordId.ConvertTo(entityTableInfo.PkColumn.Property.PropertyType));
                }
                else
                {
                    current = EntityCache.CloneObjectWithIL(current);
                }

                //render modified values
                foreach (AuditTrailDetail detail in audit.lstAuditTrailDetail)
                {
                    if (detail.NewValue == null) continue;

                    //find column
                    entityTableInfo.Columns.TryGetValue(detail.ColumnName, out ColumnAttribute col);
                    if (col == null) continue;

                    object convertedValue = null;

                    if (col.Property.PropertyType == typeof(bool) || col.Property.PropertyType == typeof(bool?))
                        convertedValue = detail.NewValue.ToString() == "1" ? true : false;
                    else if (col.Property.PropertyType == typeof(DateTime) || col.Property.PropertyType == typeof(DateTime?))
                        convertedValue = detail.NewValue.ToString().FromSQLDateTime();
                    else
                        convertedValue = detail.NewValue.ConvertTo(col.Property.PropertyType);

                    col.SetAction(current, convertedValue);
                }

                if (!entityTableInfo.NoVersionNo)
                    entityTableInfo.SetVersionNo(current, audit.RecordVersionNo);

                if (!entityTableInfo.NoUpdatedBy)
                    entityTableInfo.SetUpdatedOn(current, audit.CreatedOn);

                if (!entityTableInfo.NoUpdatedBy)
                    entityTableInfo.SetUpdatedBy(current, audit.CreatedBy);

                yield return current;
            }
        }

        /// <summary>
        /// Read all AuditTrail for a given record of a table
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public List<IAuditTrail> ReadAllAuditTrail(object id)
        {
            var lstAudit = ReadAll("*", $"tablename=@TableName AND recordid=@RecordId", 
                new { TableName = entityTableInfo.Name, RecordId = id.ToString() }, 
                Config.VegaConfig.CreatedOnColumnName + " ASC");

            var result = lstAudit.ToList();

            result.ForEach(p => p.Split());

            return result.Cast<IAuditTrail>().ToList();

        }
    }
}
