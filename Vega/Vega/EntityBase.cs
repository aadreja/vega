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

        #region fields

        private int versionNo = 1;
        private int pastVersionNo;

        #endregion

        #region properties

        [IgnoreColumn(true)]
        internal object KeyId
        {
            get
            {
                return KeyIdGetSetCache.GetKeyId(GetType()).Invoke(this);

            }
            set
            {
                KeyIdGetSetCache.SetKeyId(GetType()).Invoke(this, value);
            }
        }

        //TODO: Remove this property
        //[IgnoreColumn(true)]
        //public object KeyIdRef
        //{
        //    get
        //    {
        //        return EntityCache.Get(this.GetType()).PrimaryKeyColumn.Property.GetValue(this);
        //    }
        //    set
        //    {
        //        EntityCache.Get(this.GetType()).PrimaryKeyColumn.Property.SetValue(this, value);
        //    }
        //}

        [Column(Name = Config.CREATEDBY_COLUMNNAME, Title = "Created By")]
        public Int32 CreatedBy { get; set; }

        [IgnoreColumn(true)]
        [Column(Name = Config.CREATEDBYNAME_COLUMNNAME, Title = "Created By")]
        public string CreatedByName { get; set; }

        [Column(Name = Config.CREATEDON_COLUMNNAME, Title = "Created On", IsAllowSorting = true)]
        public DateTime CreatedOn { get; set; }

        [Column(Name = Config.UPDATEDBY_COLUMNNAME, Title = "Updated By")]
        public Int32 UpdatedBy { get; set; }

        [IgnoreColumn(true)]
        [Column(Name = Config.UPDATEDBYNAME_COLUMNNAME, Title = "Updated By")]
        public string UpdatedByName { get; set; }

        [Column(Name = Config.UPDATEDON_COLUMNNAME, Title = "Updated On", IsAllowSorting = true)]
        public DateTime UpdatedOn { get; set; }

        [Column(Name = Config.VERSIONNO_COLUMNNAME, Title = "Version")]
        public int VersionNo
        {
            get { return versionNo; }
            set
            {
                pastVersionNo = versionNo;
                versionNo = value;
            }
        }

        [Column(Name = Config.ISACTIVE_COLUMNNAME, Title = "Is Active")]
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

        public bool IsKeyIdEmpty()
        {
            var id = KeyId;

            if (id is null)
                return true;
            else if (id.IsNumber())
            {
                if (Equals(id, Convert.ChangeType(0, id.GetType()))) return true;
                else return false;
            }
            else if (id is Guid)
            {
                if (Equals(id, Guid.Empty)) return true;
                else return false;
            }
            else
                throw new Exception(id.GetType().Name + " data type not supported for Primary Key");
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

                //KeyId get/set delegates
                
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

    //https://www.codeproject.com/Articles/9927/Fast-Dynamic-Property-Access-with-C
    public static class KeyIdGetSetCache
    {
        static Hashtable mTypeHash;
        static Dictionary<Type, Func<object, object>> EntityKeyIdGetFunc;
        static Dictionary<Type, Action<object, object>> EntityKeyIdSetFunc;

        static KeyIdGetSetCache()
        {
            EntityKeyIdGetFunc = new Dictionary<Type, Func<object, object>>();
            EntityKeyIdSetFunc = new Dictionary<Type, Action<object, object>>();

            mTypeHash = new Hashtable
            {
                [typeof(sbyte)] = OpCodes.Ldind_I1,
                [typeof(byte)] = OpCodes.Ldind_U1,
                [typeof(char)] = OpCodes.Ldind_U2,
                [typeof(short)] = OpCodes.Ldind_I2,
                [typeof(ushort)] = OpCodes.Ldind_U2,
                [typeof(int)] = OpCodes.Ldind_I4,
                [typeof(uint)] = OpCodes.Ldind_U4,
                [typeof(long)] = OpCodes.Ldind_I8,
                [typeof(ulong)] = OpCodes.Ldind_I8,
                [typeof(bool)] = OpCodes.Ldind_I1,
                [typeof(double)] = OpCodes.Ldind_R8,
                [typeof(float)] = OpCodes.Ldind_R4
            };
        }

        public static void Clear()
        {
            EntityKeyIdGetFunc.Clear();
            EntityKeyIdSetFunc.Clear();
        }

        public static Func<object, object> GetKeyId(Type entityType)
        {
            Func<object, object> result;

            lock (EntityKeyIdGetFunc)
            {
                if (EntityKeyIdGetFunc.TryGetValue(entityType, out result)) return result;
            }

            result = CreateGetProperty(entityType);
            //save in cache
            lock (EntityKeyIdGetFunc)
            {
                EntityKeyIdGetFunc[entityType] = result;
            }

            return result;
        }

        public static Action<object, object> SetKeyId(Type entityType)
        {
            Action<object, object> result;

            lock (EntityKeyIdSetFunc)
            {
                if (EntityKeyIdSetFunc.TryGetValue(entityType, out result)) return result;
            }

            result = CreateSetProperty(entityType);
            //save in cache
            lock (EntityKeyIdSetFunc)
            {
                EntityKeyIdSetFunc[entityType] = result;
            }

            return result;
        }

        static Func<object, object> CreateGetProperty(Type entityType)
        {
            MethodInfo mi = EntityCache.Get(entityType).PrimaryKeyColumn.GetMethod;

            Type[] args = new Type[] { typeof(object) };

            DynamicMethod method = new DynamicMethod("Get_" + entityType.Name + "KeyId", typeof(object), args, entityType.Module, true);
            ILGenerator getIL = method.GetILGenerator();

            getIL.DeclareLocal(typeof(object));
            getIL.Emit(OpCodes.Ldarg_0); //Load the first argument

            //(target object)
            //Cast to the source type
            getIL.Emit(OpCodes.Castclass, entityType);

            //Get the property value
            getIL.EmitCall(OpCodes.Call, mi, null);
            if (mi.ReturnType.IsValueType)
            {
                getIL.Emit(OpCodes.Box, mi.ReturnType);
                //Box if necessary
            }
            getIL.Emit(OpCodes.Stloc_0); //Store it

            getIL.Emit(OpCodes.Ldloc_0);
            getIL.Emit(OpCodes.Ret);

            var funcType = System.Linq.Expressions.Expression.GetFuncType(typeof(object), typeof(object));
            return (Func<object, object>)method.CreateDelegate(funcType);
        }

        static Action<object, object> CreateSetProperty(Type entityType)
        {
            MethodInfo mi = EntityCache.Get(entityType).PrimaryKeyColumn.SetMethod;

            Type[] args = new Type[] { typeof(object), typeof(object) };

            DynamicMethod method = new DynamicMethod("Set_" + entityType.Name + "KeyId", null, args, entityType.Module, true);
            ILGenerator setIL = method.GetILGenerator();

            Type paramType = mi.GetParameters()[0].ParameterType;

            //setIL.DeclareLocal(typeof(object));
            setIL.Emit(OpCodes.Ldarg_0); //Load the first argument [Entity]
            //(target object)
            //Cast to the source type
            setIL.Emit(OpCodes.Castclass, entityType);
            setIL.Emit(OpCodes.Ldarg_1); //Load the second argument [Value]
            //(value object)
            if (paramType.IsValueType)
            {
                setIL.Emit(OpCodes.Unbox, paramType); //Unbox it 
                if (mTypeHash[paramType] != null) //and load
                {
                    OpCode load = (OpCode)mTypeHash[paramType];
                    setIL.Emit(load);
                }
                else
                {
                    setIL.Emit(OpCodes.Ldobj, paramType);
                }
            }
            else
            {
                setIL.Emit(OpCodes.Castclass, paramType); //Cast class
            }

            setIL.EmitCall(OpCodes.Callvirt, mi, null); //Set the property value
            setIL.Emit(OpCodes.Ret);

            var actionType = System.Linq.Expressions.Expression.GetActionType(typeof(object), typeof(object));
            return (Action<object, object>)method.CreateDelegate(actionType);
        }

        
    }
}