/*
 Description: Vega - Fastest ORM with enterprise features
 Author: Ritesh Sutaria
 Date: 9-Dec-2017
 Home Page: https://github.com/aadreja/vega
            http://www.vegaorm.com
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;

namespace Vega
{
    /// <summary>
    /// Cache of Entity
    /// </summary>
    public static class EntityCache
    {
        static ReaderWriterLockSlim cacheLock = new ReaderWriterLockSlim();
        static Dictionary<Type, TableAttribute> Entities;

        static EntityCache()
        {
            Entities = new Dictionary<Type, TableAttribute>();
        }

        /// <summary>
        /// Clears all Entity cache. Can be used when switching database in runtime.
        /// </summary>
        public static void Clear()
        {
            Entities.Clear();
        }

        internal static TableAttribute Get(Type entity)
        {
            TableAttribute result;

            try
            {
                cacheLock.EnterReadLock();
                if (Entities.TryGetValue(entity, out result)) return result;
            }
            finally
            {
                cacheLock.ExitReadLock();
            }

            //AuditTrial table attribute is configured based on configuration 
            if (entity == typeof(AuditTrial))
            {
                result = GetAuditTrialTableAttribute();

                try
                {
                    cacheLock.EnterWriteLock();
                    Entities[entity] = result;
                }
                finally
                {
                    cacheLock.ExitWriteLock();
                }

                return result;
            }
            
            result = (TableAttribute)entity.GetCustomAttributes(typeof(TableAttribute), false).FirstOrDefault();
            if (result == null)
            {
                result = new TableAttribute
                {
                    Name = entity.Name, //assuming entity class name is table name
                    NeedsHistory = Config.VegaConfig.NeedsHistory,
                    NoCreatedBy = Config.VegaConfig.NoCreatedBy,
                    NoCreatedOn = Config.VegaConfig.NoCreatedOn,
                    NoUpdatedBy = Config.VegaConfig.NoUpdatedBy,
                    NoUpdatedOn = Config.VegaConfig.NoUpdatedOn,
                    NoVersionNo = Config.VegaConfig.NoVersionNo,
                    NoIsActive = Config.VegaConfig.NoIsActive
                };
            }

            if (string.IsNullOrEmpty(result.Name)) result.Name = entity.Name;

            //find all properties
            var properties = entity.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            //find primary key column attribute
            var primaryKeyProperty = properties.FirstOrDefault(p => p.GetCustomAttributes(typeof(PrimaryKeyAttribute), false).Count() == 1);

            ColumnAttribute primaryKeyColumn = null;
            if (primaryKeyProperty != null)
            {
                result.PrimaryKeyAttribute = (PrimaryKeyAttribute)primaryKeyProperty.GetCustomAttributes(typeof(PrimaryKeyAttribute)).FirstOrDefault();

                //find column attribute on this property
                primaryKeyColumn = (ColumnAttribute)primaryKeyProperty.GetCustomAttributes(typeof(ColumnAttribute)).FirstOrDefault();
                if (primaryKeyColumn == null)
                {
                    //if no column attribute defined assume Propertyname is key column name
                    primaryKeyColumn = new ColumnAttribute
                    {
                        Name = primaryKeyProperty.Name,
                        ColumnDbType = TypeCache.TypeToDbType[primaryKeyProperty.PropertyType]
                    };
                }

                primaryKeyColumn.Property = primaryKeyProperty;
                primaryKeyColumn.SetMethod = primaryKeyProperty.GetSetMethod();
                primaryKeyColumn.GetMethod = primaryKeyProperty.GetGetMethod();
                primaryKeyColumn.SetAction = Helper.CreateSetProperty(entity, primaryKeyProperty.Name);
                primaryKeyColumn.GetAction = Helper.CreateGetProperty(entity, primaryKeyProperty.Name);

                result.PrimaryKeyColumn = primaryKeyColumn;

                //check for virtual foreign key
                var virtualForeignKeys = (IEnumerable<ForeignKey>)primaryKeyProperty.GetCustomAttributes(typeof(ForeignKey));
                if(virtualForeignKeys != null && virtualForeignKeys.Count() > 0)
                {
                    if (result.VirtualForeignKeys == null) result.VirtualForeignKeys = new List<ForeignKey>();
                    result.VirtualForeignKeys.AddRange(virtualForeignKeys);
                }
            }

            foreach (PropertyInfo property in properties)
            {
                //TODO: check for valid property types to be added in list
                if ((property.Name.Equals("keyid", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("operation", StringComparison.OrdinalIgnoreCase)))
                    continue;

                //check for ignore property attribute
                var ignoreInfo = (IgnoreColumnAttribute)property.GetCustomAttribute(typeof(IgnoreColumnAttribute));
                var column = (ColumnAttribute)property.GetCustomAttribute(typeof(ColumnAttribute));

                if (column == null) column = new ColumnAttribute();

                if (string.IsNullOrEmpty(column.Name)) column.Name = property.Name;

                if (property.Name.Equals("CreatedBy", StringComparison.OrdinalIgnoreCase))
                    column.Name = Config.VegaConfig.CreatedByColumnName;
                else if (property.Name.Equals("CreatedByName"))
                    column.Name = Config.VegaConfig.CreatedByNameColumnName;
                else if (property.Name.Equals("CreatedOn"))
                    column.Name = Config.VegaConfig.CreatedOnColumnName;
                else if (property.Name.Equals("UpdatedBy"))
                    column.Name = Config.VegaConfig.UpdatedByColumnName;
                else if (property.Name.Equals("UpdatedByName"))
                    column.Name = Config.VegaConfig.UpdatedByNameColumnName;
                else if (property.Name.Equals("UpdatedOn"))
                    column.Name = Config.VegaConfig.UpdatedOnColumnName;
                else if (property.Name.Equals("VersionNo"))
                    column.Name = Config.VegaConfig.VersionNoColumnName;
                else if (property.Name.Equals("IsActive"))
                    column.Name = Config.VegaConfig.IsActiveColumnName;

                if (!column.IsColumnDbTypeDefined)
                {
                    if (column.Name.Equals(Config.VegaConfig.CreatedByColumnName, StringComparison.OrdinalIgnoreCase) ||
                        column.Name.Equals(Config.VegaConfig.UpdatedByColumnName, StringComparison.OrdinalIgnoreCase))
                        column.ColumnDbType = Config.VegaConfig.CreatedUpdatedByColumnType;
                    else if (property.PropertyType.IsEnum)
                        column.ColumnDbType = TypeCache.TypeToDbType[property.PropertyType.GetEnumUnderlyingType()];
                    else if (property.PropertyType.IsValueType)
                        column.ColumnDbType = TypeCache.TypeToDbType[property.PropertyType];
                    else
                    {
                        TypeCache.TypeToDbType.TryGetValue(property.PropertyType, out DbType columnDbType);
                        column.ColumnDbType = columnDbType;
                    }
                }

                column.Property = property;
                column.SetMethod = property.GetSetMethod();
                column.GetMethod = property.GetGetMethod();
                column.SetAction = Helper.CreateSetProperty(entity, property.Name);
                column.GetAction = Helper.CreateGetProperty(entity, property.Name);

                column.IgnoreInfo = ignoreInfo ?? new IgnoreColumnAttribute(false);

                if (result.NoCreatedBy && (column.Name.Equals(Config.VegaConfig.CreatedByColumnName, StringComparison.OrdinalIgnoreCase)
                    || column.Name.Equals(Config.VegaConfig.CreatedByNameColumnName, StringComparison.OrdinalIgnoreCase)))
                    continue;
                else if (result.NoCreatedOn && column.Name.Equals(Config.VegaConfig.CreatedOnColumnName, StringComparison.OrdinalIgnoreCase))
                    continue;
                else if (result.NoUpdatedBy && ((column.Name.Equals(Config.VegaConfig.UpdatedByColumnName, StringComparison.OrdinalIgnoreCase)
                    || column.Name.Equals(Config.VegaConfig.UpdatedByNameColumnName, StringComparison.OrdinalIgnoreCase))))
                    continue;
                else if (result.NoUpdatedOn && column.Name.Equals(Config.VegaConfig.UpdatedOnColumnName, StringComparison.OrdinalIgnoreCase))
                    continue;
                else if (result.NoIsActive && column.Name.Equals(Config.VegaConfig.IsActiveColumnName, StringComparison.OrdinalIgnoreCase))
                    continue;
                else if (result.NoVersionNo && column.Name.Equals(Config.VegaConfig.VersionNoColumnName, StringComparison.OrdinalIgnoreCase))
                    continue;
                else
                {
                    if (!column.IgnoreInfo.Insert)
                        result.DefaultInsertColumns.Add(column.Name);

                    if (!column.IgnoreInfo.Update)
                        result.DefaultUpdateColumns.Add(column.Name);

                    if (!column.IgnoreInfo.Read)
                        result.DefaultReadColumns.Add(column.Name);

                    result.Columns[column.Name] = column;
                }
            }

            try
            {
                cacheLock.EnterWriteLock();
                Entities[entity] = result;
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }

            return result;
        }

        internal static TableAttribute GetAuditTrialTableAttribute()
        {
            TableAttribute result = new TableAttribute
            {
                Name = Config.VegaConfig.AuditTableName,
                NeedsHistory = false,
                NoCreatedBy = false,
                NoCreatedOn = false,
                NoUpdatedBy = true,
                NoUpdatedOn = true,
                NoVersionNo = true,
                NoIsActive = true
            };

            var type = typeof(AuditTrial);
            var pkProperty = typeof(AuditTrial).GetProperty("AuditTrailId");

            result.PrimaryKeyAttribute = (PrimaryKeyAttribute)pkProperty.GetCustomAttributes(typeof(PrimaryKeyAttribute)).FirstOrDefault();
            result.PrimaryKeyColumn = new ColumnAttribute()
            {
                Name = Config.VegaConfig.AuditKeyColumnName,
                ColumnDbType = DbType.Int64,
                Property = pkProperty,
                SetMethod = pkProperty.GetSetMethod(),
                GetMethod = pkProperty.GetGetMethod(),
                SetAction = Helper.CreateSetProperty(type, pkProperty.Name),
                GetAction = Helper.CreateGetProperty(type, pkProperty.Name),
            };

            foreach (PropertyInfo property in type.GetProperties())
            {
                //TODO: check for valid property types to be added in list
                if ((property.Name.Equals("keyid", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("operation", StringComparison.OrdinalIgnoreCase)))
                    continue;

                //check for ignore property attribute
                var ignoreInfo = (IgnoreColumnAttribute)property.GetCustomAttribute(typeof(IgnoreColumnAttribute));
                var column = (ColumnAttribute)property.GetCustomAttribute(typeof(ColumnAttribute));

                if (column == null) column = new ColumnAttribute();

                if (property.Name == "AuditTrailId")
                    column.Name = Config.VegaConfig.AuditKeyColumnName;
                else if (property.Name == "OperationType")
                    column.Name = Config.VegaConfig.AuditOperationTypeColumnName;
                else if (property.Name == "TableName")
                    column.Name = Config.VegaConfig.AuditTableNameColumnName;
                else if (property.Name == "RecordId")
                    column.Name = Config.VegaConfig.AuditRecordIdColumnName;
                else if (property.Name == "Details")
                    column.Name = Config.VegaConfig.AuditDetailsColumnName;
                else if (property.Name == "RecordVersionNo")
                    column.Name = Config.VegaConfig.AuditRecordVersionColumnName;
                else if (property.Name.Equals("CreatedBy", StringComparison.OrdinalIgnoreCase))
                    column.Name = Config.VegaConfig.CreatedByColumnName;
                else if (property.Name.Equals("CreatedOn"))
                    column.Name = Config.VegaConfig.CreatedOnColumnName;
                else
                    column.Name = property.Name;

                if (!column.IsColumnDbTypeDefined)
                {
                    if (column.Name.Equals(Config.VegaConfig.CreatedByColumnName, StringComparison.OrdinalIgnoreCase) ||
                        column.Name.Equals(Config.VegaConfig.UpdatedByColumnName, StringComparison.OrdinalIgnoreCase))
                        column.ColumnDbType = Config.VegaConfig.CreatedUpdatedByColumnType;
                    else if (property.PropertyType.IsEnum)
                        column.ColumnDbType = TypeCache.TypeToDbType[property.PropertyType.GetEnumUnderlyingType()];
                    else if (property.PropertyType.IsValueType)
                        column.ColumnDbType = TypeCache.TypeToDbType[property.PropertyType];
                    else
                    {
                        TypeCache.TypeToDbType.TryGetValue(property.PropertyType, out DbType columnDbType);
                        column.ColumnDbType = columnDbType;
                    }
                }

                column.Property = property;
                column.SetMethod = property.GetSetMethod();
                column.GetMethod = property.GetGetMethod();
                column.SetAction = Helper.CreateSetProperty(type, property.Name);
                column.GetAction = Helper.CreateGetProperty(type, property.Name);

                column.IgnoreInfo = ignoreInfo ?? new IgnoreColumnAttribute(false);

                if (result.NoCreatedBy && (column.Name.Equals(Config.VegaConfig.CreatedByColumnName, StringComparison.OrdinalIgnoreCase)
                    || column.Name.Equals(Config.VegaConfig.CreatedByNameColumnName, StringComparison.OrdinalIgnoreCase)))
                    continue;
                else if (result.NoCreatedOn && column.Name.Equals(Config.VegaConfig.CreatedOnColumnName, StringComparison.OrdinalIgnoreCase))
                    continue;
                else if (result.NoUpdatedBy && ((column.Name.Equals(Config.VegaConfig.UpdatedByColumnName, StringComparison.OrdinalIgnoreCase)
                    || column.Name.Equals(Config.VegaConfig.UpdatedByNameColumnName, StringComparison.OrdinalIgnoreCase))))
                    continue;
                else if (result.NoUpdatedOn && column.Name.Equals(Config.VegaConfig.UpdatedOnColumnName, StringComparison.OrdinalIgnoreCase))
                    continue;
                else if (result.NoIsActive && column.Name.Equals(Config.VegaConfig.IsActiveColumnName, StringComparison.OrdinalIgnoreCase))
                    continue;
                else if (result.NoVersionNo && column.Name.Equals(Config.VegaConfig.VersionNoColumnName, StringComparison.OrdinalIgnoreCase))
                    continue;
                else
                {
                    if (!column.IgnoreInfo.Insert)
                        result.DefaultInsertColumns.Add(column.Name);

                    if (!column.IgnoreInfo.Update)
                        result.DefaultUpdateColumns.Add(column.Name);

                    if (!column.IgnoreInfo.Read)
                        result.DefaultReadColumns.Add(column.Name);

                    result.Columns[column.Name] = column;
                }
            }
            return result;
        }
    }    
}
