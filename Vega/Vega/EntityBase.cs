/*
 Description: Vega - Fastest ORM with enterprise features
 Author: Ritesh Sutaria
 Date: 9-Dec-2017
 Home Page: https://github.com/aadreja/vega
            http://www.vegaorm.com
*/
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Vega
{
    /// <summary>
    /// Entity parent class. All entities must be inherited from this abstract class
    /// </summary>
    [Serializable]
    public abstract class EntityBase
    {

        public EntityBase()
        {

        }

        #region properties

        [IgnoreColumn(true)]
        public object KeyId
        {
            get
            {
                return EntityCache.Get(GetType()).PrimaryKeyColumn.GetMethod.Invoke(this, null);
            }
            set
            {
                EntityCache.Get(GetType()).PrimaryKeyColumn.SetMethod.Invoke(this, new object[] { value });
            }
        }

        public Int32 CreatedBy { get; set; }

        [IgnoreColumn(true)]
        public string CreatedByName { get; set; }

        public DateTime CreatedOn { get; set; }

        public Int32 UpdatedBy { get; set; }

        [IgnoreColumn(true)]
        public string UpdatedByName { get; set; }

        public DateTime UpdatedOn { get; set; }

        private int versionNo = 1;
        private int pastVersionNo;

        public int VersionNo
        {
            get { return versionNo; }
            set
            {
                pastVersionNo = versionNo;
                versionNo = value;
            }
        }

        public bool IsActive { get; set; }

        [IgnoreColumn(true)]
        public string Operation
        {
            get
            {
                if (!IsActive) return "In Active";
                else if (VersionNo == 0 || VersionNo == 1) return "Add";
                else if (VersionNo > 1) return "Update";
                else return "Unknown";
            }
        }

        #endregion

        #region methods

        public virtual EntityBase ShallowCopy()
        {
            return (EntityBase)MemberwiseClone();
        }

        public virtual void RevertVersionNo()
        {
            if (pastVersionNo > 0)
            {
                VersionNo = pastVersionNo;
            }
        }

        public bool IsPrimaryKeyEmpty()
        {
            var Id = EntityCache.Get(GetType()).PrimaryKeyColumn.GetMethod.Invoke(this, null);

            if (Id is null) return true;
            else if (Id.IsNumber()) if (Equals(Id, Convert.ChangeType(0, Id.GetType()))) return true; else return false;
            else if (Id is Guid) if (Equals(Id, Guid.Empty)) return true; else return false;
            else throw new Exception(Id.GetType().Name + " data type not supported for Primary Key");
        }

        #endregion
    }

    public static class EntityCache
    {

        static Dictionary<Type, TableAttribute> Entities;

        static EntityCache()
        {
            Entities = new Dictionary<Type, TableAttribute>();
        }

        public static void Clear()
        {
            Entities.Clear();
        }

        public static TableAttribute Get(Type entity)
        {
            TableAttribute result;

            lock (Entities)
            {
                if (Entities.TryGetValue(entity, out result)) return result;
            }

            result = (TableAttribute)entity.GetCustomAttributes(typeof(TableAttribute), false).FirstOrDefault();
            if (result == null)
            {
                result = new TableAttribute
                {
                    Name = entity.Name, //assuming entity class name is table name
                    NeedsHistory = Config.DEFAULT_NEEDS_HISTORY,
                    NoCreatedBy = Config.DEFAULT_NO_CREATEDBY,
                    NoCreatedOn = Config.DEFAULT_NO_CREATEDON,
                    NoUpdatedBy = Config.DEFAULT_NO_UPDATEDBY,
                    NoUpdatedOn = Config.DEFAULT_NO_UPDATEDON,
                    NoVersionNo = Config.DEFAULT_NO_VERSIONNO,
                    NoIsActive = Config.DEFAULT_NO_ISACTIVE
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

                result.PrimaryKeyColumn = primaryKeyColumn;
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

                if (!column.IsColumnDbTypeDefined)
                {
                    if (property.PropertyType.IsEnum)
                        column.ColumnDbType = TypeCache.TypeToDbType[property.PropertyType.GetEnumUnderlyingType()];
                    else if (property.PropertyType.IsValueType)
                        column.ColumnDbType = TypeCache.TypeToDbType[property.PropertyType];
                }
                
                column.Property = property;
                column.SetMethod = property.GetSetMethod();
                column.GetMethod = property.GetGetMethod();
                column.IgnoreInfo = ignoreInfo ?? new IgnoreColumnAttribute(false);

                //TODO: create columnattribute equals method
                if (result.NoCreatedBy && column.Equals(Config.CREATEDBY_COLUMN)
                    || column.Equals(Config.CREATEDBYNAME_COLUMN.Name))
                    continue;
                else if (result.NoCreatedOn && column.Equals(Config.CREATEDON_COLUMN.Name))
                    continue;
                else if (result.NoUpdatedBy && (column.Equals(Config.UPDATEDBY_COLUMN)
                    || column.Equals(Config.UPDATEDBYNAME_COLUMN)))
                    continue;
                else if (result.NoUpdatedOn && column.Equals(Config.UPDATEDON_COLUMN))
                    continue;
                else if (result.NoIsActive && column.Equals(Config.ISACTIVE_COLUMN))
                    continue;
                else if (result.NoVersionNo && column.Equals(Config.VERSIONNO_COLUMN))
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

            lock (Entities)
            {
                Entities[entity] = result;
            }

            return result;
        }

    }
}