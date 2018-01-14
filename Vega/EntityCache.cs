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
using System.Threading.Tasks;

namespace Vega
{
    /// <summary>
    /// Cache of Entity
    /// </summary>
    public static class EntityCache
    {
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
                primaryKeyColumn.SetAction = Helper.CreateSetProperty(entity, primaryKeyProperty.Name);
                primaryKeyColumn.GetAction = Helper.CreateGetProperty(entity, primaryKeyProperty.Name);

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

                if (result.NoCreatedBy && (column.Name.Equals(Config.CREATEDBY_COLUMNNAME, StringComparison.OrdinalIgnoreCase)
                    || column.Name.Equals(Config.CREATEDBYNAME_COLUMNNAME, StringComparison.OrdinalIgnoreCase)))
                    continue;
                else if (result.NoCreatedOn && column.Name.Equals(Config.CREATEDON_COLUMNNAME, StringComparison.OrdinalIgnoreCase))
                    continue;
                else if (result.NoUpdatedBy && ((column.Name.Equals(Config.UPDATEDBY_COLUMNNAME, StringComparison.OrdinalIgnoreCase)
                    || column.Name.Equals(Config.UPDATEDBYNAME_COLUMNNAME, StringComparison.OrdinalIgnoreCase))))
                    continue;
                else if (result.NoUpdatedOn && column.Name.Equals(Config.UPDATEDON_COLUMNNAME, StringComparison.OrdinalIgnoreCase))
                    continue;
                else if (result.NoIsActive && column.Name.Equals(Config.ISACTIVE_COLUMNNAME, StringComparison.OrdinalIgnoreCase))
                    continue;
                else if (result.NoVersionNo && column.Name.Equals(Config.VERSIONNO_COLUMNNAME, StringComparison.OrdinalIgnoreCase))
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
