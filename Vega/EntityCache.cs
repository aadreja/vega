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
using System.Reflection.Emit;
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

        /// <summary>
        /// Delegate handler that's used to compile the IL to.
        /// (This delegate is standard in .net 3.5)
        /// </summary>
        /// <typeparam name="T1">Parameter Type</typeparam>
        /// <typeparam name="TResult">Return Type</typeparam>
        /// <param name="arg1">Argument</param>
        /// <returns>Result</returns>
        public delegate TResult Func<T1, TResult>(T1 arg1);
        /// <summary>
        /// This dictionary caches the delegates for each 'to-clone' type.
        /// </summary>
        static Dictionary<Type, Delegate> CachedCloneIL;

        static EntityCache()
        {
            Entities = new Dictionary<Type, TableAttribute>();
            CachedCloneIL = new Dictionary<Type, Delegate>();
        }

        /// <summary>
        /// Clears all Entity cache. Can be used when switching database in runtime.
        /// </summary>
        public static void Clear()
        {
            Entities.Clear();
            CachedCloneIL.Clear();
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

            result = PrepareTableAttribute(entity);
            
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

        
        //to prepare TableAttribute 
        internal static TableAttribute PrepareTableAttribute(Type entity)
        {
            TableAttribute result = (TableAttribute)entity.GetCustomAttributes(typeof(TableAttribute), false).FirstOrDefault();
            if (result == null)
            {
                result = new TableAttribute
                {
                    Name = entity.Name, //assuming entity class name is table name
                    NeedsHistory = Config.NeedsHistory,
                    NoCreatedBy = Config.NoCreatedBy,
                    NoCreatedOn = Config.NoCreatedOn,
                    NoUpdatedBy = Config.NoUpdatedBy,
                    NoUpdatedOn = Config.NoUpdatedOn,
                    NoVersionNo = Config.NoVersionNo,
                    NoIsActive = Config.NoIsActive
                };
            }

            if (string.IsNullOrEmpty(result.Name)) result.Name = entity.Name;

            //find all properties
            var properties = entity.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo property in properties)
            {
                //TODO: check for valid property types to be added in list
                if ((property.Name.Equals("keyid", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("operation", StringComparison.OrdinalIgnoreCase)))
                    continue;

                //check for ignore property attribute
                var ignoreInfo = (IgnoreColumnAttribute)property.GetCustomAttribute(typeof(IgnoreColumnAttribute));
                var primaryKey = (PrimaryKeyAttribute)property.GetCustomAttribute(typeof(PrimaryKeyAttribute));
                var column = (ColumnAttribute)property.GetCustomAttribute(typeof(ColumnAttribute));


                if (column == null) column = new ColumnAttribute();

                if (string.IsNullOrEmpty(column.Name)) column.Name = property.Name;

                if (property.Name.Equals("CreatedBy", StringComparison.OrdinalIgnoreCase))
                    column.Name = Config.CreatedByColumnName;
                else if (property.Name.Equals("CreatedByName"))
                    column.Name = Config.CreatedByNameColumnName;
                else if (property.Name.Equals("CreatedOn"))
                    column.Name = Config.CreatedOnColumnName;
                else if (property.Name.Equals("UpdatedBy"))
                    column.Name = Config.UpdatedByColumnName;
                else if (property.Name.Equals("UpdatedByName"))
                    column.Name = Config.UpdatedByNameColumnName;
                else if (property.Name.Equals("UpdatedOn"))
                    column.Name = Config.UpdatedOnColumnName;
                else if (property.Name.Equals("VersionNo"))
                    column.Name = Config.VersionNoColumnName;
                else if (property.Name.Equals("IsActive"))
                    column.Name = Config.IsActiveColumnName;

                if (!column.IsColumnDbTypeDefined)
                {
                    if (column.Name.Equals(Config.CreatedByColumnName, StringComparison.OrdinalIgnoreCase) ||
                        column.Name.Equals(Config.UpdatedByColumnName, StringComparison.OrdinalIgnoreCase))
                        column.ColumnDbType = Config.CreatedUpdatedByColumnType;
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

                column.SetPropertyInfo(property, entity);

                column.IgnoreInfo = ignoreInfo ?? new IgnoreColumnAttribute(false);

                //Primary Key details
                if (primaryKey != null)
                {
                    column.PrimaryKeyInfo = primaryKey;

                    var virtualForeignKeys = (IEnumerable<ForeignKey>)property.GetCustomAttributes(typeof(ForeignKey));
                    if (virtualForeignKeys != null && virtualForeignKeys.Count() > 0)
                    {
                        if (result.VirtualForeignKeys == null) result.VirtualForeignKeys = new List<ForeignKey>();
                        result.VirtualForeignKeys.AddRange(virtualForeignKeys);
                    }
                }

                if (result.NoCreatedBy && (column.Name.Equals(Config.CreatedByColumnName, StringComparison.OrdinalIgnoreCase)
                    || column.Name.Equals(Config.CreatedByNameColumnName, StringComparison.OrdinalIgnoreCase)))
                    continue;
                else if (result.NoCreatedOn && column.Name.Equals(Config.CreatedOnColumnName, StringComparison.OrdinalIgnoreCase))
                    continue;
                else if (result.NoUpdatedBy && ((column.Name.Equals(Config.UpdatedByColumnName, StringComparison.OrdinalIgnoreCase)
                    || column.Name.Equals(Config.UpdatedByNameColumnName, StringComparison.OrdinalIgnoreCase))))
                    continue;
                else if (result.NoUpdatedOn && column.Name.Equals(Config.UpdatedOnColumnName, StringComparison.OrdinalIgnoreCase))
                    continue;
                else if (result.NoIsActive && column.Name.Equals(Config.IsActiveColumnName, StringComparison.OrdinalIgnoreCase))
                    continue;
                else if (result.NoVersionNo && column.Name.Equals(Config.VersionNoColumnName, StringComparison.OrdinalIgnoreCase))
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

            if(result.Columns.LongCount(p=>p.Value.IsPrimaryKey && p.Value.PrimaryKeyInfo.IsIdentity) > 1)
            {
                throw new NotSupportedException("Primary key with multiple Identity is not supported on " + result.Name);
            }

            if (result.Columns.LongCount(p => p.Value.IsPrimaryKey) > 1 && result.NeedsHistory)
            {
                throw new NotSupportedException($"History for {result.Name} is not supported as it has composite Primary key");
            }

            return result;
        }

        //TODO: Remove this method Later as AuditTrail will be based on interface IAuditTrail
        //to prepare AuditTableAttribute 
        /* internal static TableAttribute PrepareAuditTrailTableAttribute()
        {
            TableAttribute result = new TableAttribute
            {
                Name = "audittrail",
                NeedsHistory = false,
                NoCreatedBy = false,
                NoCreatedOn = false,
                NoUpdatedBy = true,
                NoUpdatedOn = true,
                NoVersionNo = true,
                NoIsActive = true
            };

            var type = typeof(AuditTrail);

            foreach (PropertyInfo property in type.GetProperties())
            {
                var column = (ColumnAttribute)property.GetCustomAttribute(typeof(ColumnAttribute));
                column = column ?? new ColumnAttribute();

                if (property.Name == "AuditTrailId")
                    column.Name = "audittrailid";
                else if (property.Name == "OperationType")
                    column.Name = "operationtype";
                else if (property.Name == "TableName")
                    column.Name = "tablename";
                else if (property.Name == "RecordId")
                    column.Name = "recordid";
                else if (property.Name == "Details")
                    column.Name = "details";
                else if (property.Name == "RecordVersionNo")
                    column.Name = "recordversionno";
                else if (property.Name.Equals("CreatedBy", StringComparison.OrdinalIgnoreCase))
                    column.Name = Config.CreatedByColumnName;
                else if (property.Name.Equals("CreatedOn"))
                    column.Name = Config.CreatedOnColumnName;
                else
                    column.Name = property.Name;

                if (!column.IsColumnDbTypeDefined)
                {
                    if (column.Name.Equals(Config.CreatedByColumnName, StringComparison.OrdinalIgnoreCase))
                        column.ColumnDbType = Config.CreatedUpdatedByColumnType;
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

                column.SetPropertyInfo(property, typeof(AuditTrail));
                result.DefaultInsertColumns.Add(column.Name);
                result.DefaultReadColumns.Add(column.Name);

                if(property.Name == "AuditTrailId")
                {
                    column.PrimaryKeyInfo = (PrimaryKeyAttribute)property.
                        GetCustomAttributes(typeof(PrimaryKeyAttribute)).
                        FirstOrDefault();
                }

                result.Columns[column.Name] = column;
            }
            return result;
        }
        */

        #region clone object using IL

        /// <summary>
        /// http://whizzodev.blogspot.com/2008/03/object-cloning-using-il-in-c.html
        /// Generic cloning method that clones an object using IL.
        /// Only the first call of a certain type will hold back performance.
        /// After the first call, the compiled IL is executed.
        /// </summary>
        /// <typeparam name="T">Type of object to clone</typeparam>
        /// <param name="entity">Object to clone</param>
        /// <returns>Cloned object</returns>
        internal static T CloneObjectWithIL<T>(T entity)
        {
            if (!CachedCloneIL.TryGetValue(typeof(T), out Delegate cloneIL))
            {
                // Create ILGenerator
                DynamicMethod dymMethod = new DynamicMethod("DoClone", typeof(T), new Type[] { typeof(T) }, true);
                ConstructorInfo cInfo = entity.GetType().GetConstructor(new Type[] { });

                ILGenerator generator = dymMethod.GetILGenerator();

                LocalBuilder lbf = generator.DeclareLocal(typeof(T));
                //lbf.SetLocalSymInfo("_temp");

                generator.Emit(OpCodes.Newobj, cInfo);
                generator.Emit(OpCodes.Stloc_0);
                foreach (FieldInfo field in entity.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
                {
                    // Load the new object on the eval stack... (currently 1 item on eval stack)
                    generator.Emit(OpCodes.Ldloc_0);
                    // Load initial object (parameter)          (currently 2 items on eval stack)
                    generator.Emit(OpCodes.Ldarg_0);
                    // Replace value by field value             (still currently 2 items on eval stack)
                    generator.Emit(OpCodes.Ldfld, field);
                    // Store the value of the top on the eval stack into the object underneath that value on the value stack.
                    //  (0 items on eval stack)
                    generator.Emit(OpCodes.Stfld, field);
                }

                // Load new constructed obj on eval stack -> 1 item on stack
                generator.Emit(OpCodes.Ldloc_0);
                // Return constructed object.   --> 0 items on stack
                generator.Emit(OpCodes.Ret);

                cloneIL = dymMethod.CreateDelegate(typeof(Func<T, T>));
                CachedCloneIL.Add(typeof(T), cloneIL);
            }
            return ((Func<T, T>)cloneIL)(entity);
        }

        #endregion

    }
}
