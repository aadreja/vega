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
    public class AuditTrailKeyValueRepository<Entity> : Repository<AuditTrailKeyValue>, IAuditTrailRepository<Entity> where Entity : new()
    {
        #region constructors

        /// <summary>
        /// 
        /// </summary>
        /// <param name="con"></param>
        public AuditTrailKeyValueRepository(IDbConnection con) : base(con)
        {
            entityTableInfo = EntityCache.Get(typeof(Entity));
            detailRepo = new Repository<AuditTrailKeyValueDetail>(con);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tran"></param>
        public AuditTrailKeyValueRepository(IDbTransaction tran) : base(tran)
        {
            entityTableInfo = EntityCache.Get(typeof(Entity));
            detailRepo = new Repository<AuditTrailKeyValueDetail>(tran);
        }

        #endregion

        #region static properties

        static bool IsAuditTableExists { get; set; }
        static bool IsAuditTableExistsCheckDone { get; set; }

        #endregion

        #region properties

        readonly TableAttribute entityTableInfo;
        readonly Repository<AuditTrailKeyValueDetail> detailRepo;

        #endregion

        void CreateTableIfNotExist()
        {
            if (IsAuditTableExistsCheckDone && IsAuditTableExists)
                return;

            IsAuditTableExists = CreateTable();

            //check for index on RecordId field
            if (IsAuditTableExists)
            {
                CreateIndex("idx_recordid", $"tablename,recordid", false);
            }

            IsAuditTableExists = detailRepo.CreateTable();
            //check for index on AuditTrailId field
            if (IsAuditTableExists)
            {
                detailRepo.CreateIndex("idx_audittrailid", "audittrailid", false);
            }

            IsAuditTableExistsCheckDone = true;
        }

        bool AddAuditTrail(AuditTrailKeyValue audit)
        {
            audit.AuditTrailId = (long)Add(audit);

            audit.lstAuditTrailDetail.ForEach(p => p.AuditTrailId = audit.AuditTrailId);
            //add child records in detail table
            if (audit.lstAuditTrailDetail != null)
            {
                foreach (AuditTrailKeyValueDetail detail in audit.lstAuditTrailDetail)
                {
                    detail.AuditTrailDetailId = (long)detailRepo.Add(detail);
                }
            }

            return true;
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

            audit.OperationType = operation;
            audit.RecordId = entityTableInfo.GetKeyId(entity).ToString();
            if (!entityTableInfo.NoVersionNo)
                audit.RecordVersionNo = (operation == RecordOperationEnum.Insert ? 1 : entityTableInfo.GetVersionNo(entity)); //always 1 for new insert

            audit.TableName = entityTableInfo.Name;

            return AddAuditTrail((AuditTrailKeyValue)audit);
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

            AuditTrailKeyValue audit = new AuditTrailKeyValue
            {
                OperationType = operation,
                RecordId = recordId.ToString(),
                RecordVersionNo =   (recordVersionNo??0) + 1,
                TableName = entityTableInfo.Name,
                CreatedBy = updatedBy,
                lstAuditTrailDetail = new List<IAuditTrailDetail>()
                {
                    new AuditTrailKeyValueDetail()
                    {
                        ColumnName = Config.ISACTIVE_COLUMN.Name,
                        ColumnType = (int)DbType.Boolean,
                        OldValue = (operation == RecordOperationEnum.Delete).ToString(),
                        NewValue = (!(operation == RecordOperationEnum.Delete)).ToString()
                    }
                }
            };

            return AddAuditTrail(audit);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // public IEnumerable<T> ReadAll<T>(object id) where T : new()
        public IEnumerable<Entity> ReadAll(object id)
        {
            var lstAudit = ReadAll("*", $"tablename=@TableName AND recordid=@RecordId", new { TableName = entityTableInfo.Name, RecordId = id.ToString() }, Config.VegaConfig.CreatedOnColumnName + " ASC").ToList();

            Entity current = default;

            foreach (AuditTrailKeyValue audit in lstAudit)
            {
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

                var lstAuditTrailDetail = detailRepo.ReadAll(null, "audittrailid=@audittrailid", new { audittrailid = audit.AuditTrailId }).ToList();

                audit.lstAuditTrailDetail = lstAuditTrailDetail.Cast<IAuditTrailDetail>().ToList();
                //render modified values
                foreach (AuditTrailKeyValueDetail detail in audit.lstAuditTrailDetail)
                {
                    if (detail.NewValue == null) continue;

                    //find column
                    entityTableInfo.Columns.TryGetValue(detail.ColumnName, out ColumnAttribute col);
                    if (col == null) continue;

                    object convertedValue = null;

                    if (col.Property.PropertyType == typeof(bool) || col.Property.PropertyType == typeof(bool?))
                        convertedValue = (detail.NewValue.ToString() == "1" ? true : false);
                    else if (col.Property.PropertyType == typeof(DateTime) || col.Property.PropertyType == typeof(DateTime?))
                        convertedValue = detail.NewValue.ToString().FromSQLDateTime();
                    else
                        convertedValue = detail.NewValue.ConvertTo(col.Property.PropertyType);

                    col.SetAction(current, convertedValue);
                }

                if(!entityTableInfo.NoVersionNo)
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
            var lstAudit = ReadAll("*", $"tablename=@TableName AND recordid=@RecordId", new { TableName = entityTableInfo.Name, RecordId = id.ToString() }, Config.VegaConfig.CreatedOnColumnName + " ASC").ToList();

            foreach (AuditTrailKeyValue audit in lstAudit)
            {
                var lstAuditTrailDetail = detailRepo.ReadAll(null, "audittrailid=@audittrailid", new { audittrailid = audit.AuditTrailId }).ToList();
                audit.lstAuditTrailDetail = lstAuditTrailDetail.Cast<IAuditTrailDetail>().ToList();
            }

            return lstAudit.Cast<IAuditTrail>().ToList();
        }
    }
}
