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
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;

namespace Vega
{
    internal struct ReaderKey : IEquatable<ReaderKey>
    {
        private readonly int length;
        private readonly IDataReader reader;
        private readonly string[] names;
        private readonly Type[] types;
        private readonly int hashCode;

        public override int GetHashCode()
        {
            return hashCode;
        }

        internal ReaderKey(int hashCode, IDataReader reader)
        {
            this.hashCode = hashCode;
            length = reader.FieldCount;
            this.reader = reader;

            names = new string[length];
            types = new Type[length];
            for (int i = 0; i < length; i++)
            {
                names[i] = reader.GetName(i);
                types[i] = reader.GetFieldType(i);
            }
        }

        public override string ToString()
        { 
            // to be used in the debugger
            if (names != null)
            {
                return string.Join(", ", names);
            }
            if (reader != null)
            {
                var sb = new StringBuilder();
                int index = 0;
                for (int i = 0; i < length; i++)
                {
                    if (i != 0) sb.Append(", ");
                    sb.Append(reader.GetName(index++));
                }
                return sb.ToString();
            }
            return base.ToString();
        }

        public override bool Equals(object obj)
        {
            return obj is ReaderKey && Equals((ReaderKey)obj);
        }

        public bool Equals(ReaderKey other)
        {
            if (hashCode != other.hashCode || length != other.length)
            {
                return false; //clearly different
            }
            for (int i = 0; i < length; i++)
            {
                if ((names?[i] ?? reader?.GetName(i)) != (other.names?[i] ?? other.reader?.GetName(i)) 
                    ||
                    (types?[i] ?? reader?.GetFieldType(i)) != (other.types?[i] ?? other.reader?.GetFieldType(i))
                    )
                {
                    return false; // different column name or type
                }
            }
            return true;
        }
    }

    /// <summary>
    /// Reader Cache
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class ReaderCache<T> where T : new()
    {
        static ReaderWriterLockSlim cacheLock = new ReaderWriterLockSlim();
        static Dictionary<ReaderKey, Func<IDataReader, T>> readers = new Dictionary<ReaderKey, Func<IDataReader, T>>();

        static int GetReaderHash(IDataReader reader)
        {
            unchecked
            {
                int hash = 31; //any prime number
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    object fieldName = reader.GetName(i);
                    object fieldType = reader.GetFieldType(i);

                    //prime numbers to generate hash
                    hash = (-97 * ((hash * 29) + fieldName.GetHashCode())) + fieldType.GetHashCode();
                }
                return hash;
            }
        }

        /// <summary>
        /// Gets dynamic function for the given reader from cache
        /// </summary>
        /// <param name="reader">Reader</param>
        /// <returns>dynamic function to set entity values from reader</returns>
        public static Func<IDataReader, T> GetFromCache(IDataReader reader)
        {
            int hash = GetReaderHash(reader);

            ReaderKey key = new ReaderKey(hash, reader);

            Func<IDataReader, T> func;
            try
            {
                cacheLock.EnterReadLock();
                if (readers.TryGetValue(key, out func)) return func;
            }
            finally
            {
                cacheLock.ExitReadLock();
            }
            func = ReaderToObject(reader);
            try
            {
                cacheLock.EnterWriteLock();
                return readers[key] = func;
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }

        private static Func<IDataReader, T> ReaderToObject(IDataReader rdr)
        {
            MethodInfo rdrGetValueMethod = rdr.GetType().GetMethod("get_Item", new Type[] { typeof(int) });

            Type[] args = { typeof(IDataReader) };
            DynamicMethod method = new DynamicMethod("DynamicRead" + Guid.NewGuid().ToString(), typeof(T), args, typeof(Repository<T>).Module, true);
            ILGenerator il = method.GetILGenerator();

            LocalBuilder result = il.DeclareLocal(typeof(T)); //loc_0
            il.Emit(OpCodes.Newobj, typeof(T).GetConstructor(Type.EmptyTypes));
            il.Emit(OpCodes.Stloc_0, result); //Pops the current value from the top of the evaluation stack and stores it in a the local variable list at a specified index.

            Label tryBlock = il.BeginExceptionBlock();

            LocalBuilder valueCopy = il.DeclareLocal(typeof(object)); //declare local variable to store object value. loc_1

            il.DeclareLocal(typeof(int)); //declare local variable to store index //loc_2
            il.Emit(OpCodes.Ldc_I4_0); //load 0 in index
            il.Emit(OpCodes.Stloc_2); //pop and save to local variable loc 2

            //get FieldInfo of all properties
            TableAttribute tableInfo = EntityCache.Get(typeof(T));

            for (int i = 0; i < rdr.FieldCount; i++)
            {
                tableInfo.Columns.TryGetValue(rdr.GetName(i), out ColumnAttribute columnInfo);

                if (columnInfo != null && columnInfo.SetMethod != null)
                {
                    Label endIfLabel = il.DefineLabel();

                    il.Emit(OpCodes.Ldarg_0);//load the argument. Loads the argument at index 0 onto the evaluation stack.
                    il.Emit(OpCodes.Ldc_I4, i); //push field index as int32 to the stack. Pushes a supplied value of type int32 onto the evaluation stack as an int32.
                    il.Emit(OpCodes.Dup);//copy value
                    il.Emit(OpCodes.Stloc_2);//pop and save value to loc 2
                    il.Emit(OpCodes.Callvirt, rdrGetValueMethod); //Call rdr[i] method - Calls a late - bound method on an object, pushing the return value onto the evaluation stack.

                    //TODO: dynamic location using valueCopyLocal
                    il.Emit(OpCodes.Stloc_1); //pop the value and push in stack location 1
                    il.Emit(OpCodes.Ldloc_1); //load the variable in location 1

                    il.Emit(OpCodes.Isinst, typeof(DBNull)); //check whether value is null - Tests whether an object reference (type O) is an instance of a particular class.
                    il.Emit(OpCodes.Brtrue, endIfLabel); //go to end block if value is null

                    il.Emit(OpCodes.Ldloc_0); //load T result
                    il.Emit(OpCodes.Ldloc_1); //TODO: dynamic location using valueCopyLocal

                    //14-Apr-2018 - add an unbox instruction to convert the boxed value type into the actual value on the stack.
                    //Unbox only for ValueType
                    if (columnInfo.Property.PropertyType.IsValueType)
                        il.Emit(OpCodes.Unbox_Any, rdr.GetFieldType(i)); //type cast

                    // for nullable type fields
                    if (columnInfo.Property.PropertyType.IsGenericType && columnInfo.Property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        var underlyingType = Nullable.GetUnderlyingType(columnInfo.Property.PropertyType);
                        il.Emit(OpCodes.Newobj, columnInfo.Property.PropertyType.GetConstructor(new Type[] { underlyingType }));
                    }

                    il.Emit(OpCodes.Callvirt, columnInfo.SetMethod);
                    il.Emit(OpCodes.Nop);

                    il.MarkLabel(endIfLabel);
                }
            }

            il.BeginCatchBlock(typeof(Exception)); //begin try block. exception is in stack
            il.Emit(OpCodes.Ldloc_2); //load index
            il.Emit(OpCodes.Ldarg_0); //load argument reader
            il.Emit(OpCodes.Ldloc_1); //load value //TODO: dynamic location using valueCopyLocal
            il.EmitCall(OpCodes.Call, typeof(ReaderCache<T>).GetMethod(nameof(ReaderCache<T>.HandleException)), null); //call exception handler
            il.EndExceptionBlock();

            il.Emit(OpCodes.Ldloc, result);
            il.Emit(OpCodes.Ret);

            var funcType = System.Linq.Expressions.Expression.GetFuncType(typeof(IDataReader), typeof(T));
            return (Func<IDataReader, T>)method.CreateDelegate(funcType);
        }

        /// <summary>
        /// Handles exception occurend in mapping values from reader and returns error in readble form
        /// </summary>
        /// <param name="ex">Excaption object</param>
        /// <param name="index">index of column in reader</param>
        /// <param name="reader">reader object</param>
        /// <param name="value">value thrown error</param>
        public static void HandleException(Exception ex, int index, IDataReader reader, object value)
        {
            Exception toThrow;
            try
            {
                string name = "(n/a)", formattedValue = "(n/a)";
                if (reader != null && index >= 0 && index < reader.FieldCount)
                {
                    name = reader.GetName(index);
                    try
                    {
                        if (value == null || value is DBNull)
                        {
                            formattedValue = "<null>";
                        }
                        else
                        {
                            formattedValue = Convert.ToString(value) + " - " + value.GetType().Name;
                        }
                    }
                    catch (Exception valEx)
                    {
                        formattedValue = valEx.Message;
                    }
                }
                toThrow = new DataException($"Error parsing column {index} ({name}={formattedValue})", ex);
            }
            catch
            { // throw the **original** exception, wrapped as DataException
                toThrow = new DataException(ex.Message, ex);
            }
            throw toThrow;
        }
    }

    internal struct ParameterKey : IEquatable<ParameterKey>
    {
        private readonly int length;
        private readonly object dynamicObject;
        private readonly string[] names;
        private readonly PropertyInfo[] properytInfo;
        private readonly Type[] types;
        private readonly int hashCode;

        public override int GetHashCode()
        {
            return hashCode;
        }

        internal ParameterKey(int hashCode, object dynamicObject)
        {
            this.hashCode = hashCode;
            this.dynamicObject = dynamicObject;
            this.properytInfo = dynamicObject.GetType().GetProperties();
            this.length = properytInfo.Length;

            names = new string[length];
            types = new Type[length];
            for (int i = 0; i < length; i++)
            {
                names[i] = properytInfo[i].Name;
                types[i] = properytInfo[i].PropertyType;
            }
        }

        public override string ToString()
        {
            // to be used in the debugger
            if (names != null)
            {
                return string.Join(", ", names);
            }
            if (dynamicObject != null)
            {
                var sb = new StringBuilder();
                int index = 0;
                for (int i = 0; i < length; i++)
                {
                    if (i != 0) sb.Append(", ");
                    sb.Append(properytInfo[index++].Name);
                }
                return sb.ToString();
            }
            return base.ToString();
        }

        public override bool Equals(object obj)
        {
            return obj is ParameterKey && Equals((ParameterKey)obj);
        }

        public bool Equals(ParameterKey other)
        {
            if (hashCode != other.hashCode || length != other.length)
            {
                return false; //clearly different
            }
            for (int i = 0; i < length; i++)
            {
                if ((names?[i] ?? properytInfo?[i].Name) != (other.names?[i] ?? other.properytInfo?[i].Name)
                    ||
                    (types?[i] ?? properytInfo?[i].PropertyType) != (other.types?[i] ?? properytInfo?[i].PropertyType)
                    )
                {
                    return false; // different column name or type
                }
            }
            return true;
        }
    }

    internal class ParameterCache
    {
        static Dictionary<ParameterKey, Action<object, IDbCommand>> dynamicParameters = new Dictionary<ParameterKey, Action<object, IDbCommand>>();
        static readonly MethodInfo addInParameter;
        static ReaderWriterLockSlim cacheLock = new ReaderWriterLockSlim();

        static ParameterCache()
        {
            //init addInParameter
            Type[] mParam = new Type[] { typeof(IDbCommand), typeof(string), typeof(DbType), typeof(object) };
            addInParameter = typeof(Helper).GetMethod("AddInParameter");
        }

        static int GetParameterHash(object dynamicObject)
        {
            unchecked
            {
                int hash = 31; //any prime number
                PropertyInfo[] propertyInfo = dynamicObject.GetType().GetProperties();
                for (int i = 0; i < propertyInfo.Length; i++)
                {
                    object propertyName = propertyInfo[i].Name;
                    object propertyType = propertyInfo[i].PropertyType;

                    //prime numbers to generate hash
                    hash = (-97 * ((hash * 29) + propertyName.GetHashCode())) + propertyType.GetHashCode();
                }
                return hash;
            }
        }

        internal static Action<object, IDbCommand> GetFromCache(object param, IDbCommand cmd)
        {
            ParameterKey key = new ParameterKey(GetParameterHash(param), param);
            
            Action<object, IDbCommand> action;
            try
            {
                cacheLock.EnterReadLock();
                if (dynamicParameters.TryGetValue(key, out action)) return action;
            }
            finally
            {
                cacheLock.ExitReadLock();
            }
            
            action = AddParameters(param, cmd);

            try
            {
                cacheLock.EnterWriteLock();
                return dynamicParameters[key] = action;
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }

        private static Action<object, IDbCommand> AddParameters(object param, IDbCommand cmd)
        {
            Type pType = param.GetType();
            Type[] args = { typeof(object), typeof(IDbCommand) };

            DynamicMethod method = new DynamicMethod("DynamicAddParam" + Guid.NewGuid().ToString(), null, args, typeof(ParameterCache).Module, true);
            ILGenerator il = method.GetILGenerator();

            foreach (PropertyInfo property in pType.GetProperties())
            {
                il.Emit(OpCodes.Ldarg_1);//load the idbcommand. Loads the argument at index 0 onto the evaluation stack.

                //name
                il.Emit(OpCodes.Ldstr, property.Name);

                //dbtype
                //dynamic property will always return System.Object as property type. Get Type from the value
                Type type = property.GetValue(param).GetType();
                if (type.IsEnum) type = Enum.GetUnderlyingType(type);

                if(TypeCache.TypeToDbType.TryGetValue(type, out DbType dbType))
                    il.Emit(OpCodes.Ldc_I4_S, (byte)dbType);
                else
                    il.Emit(OpCodes.Ldc_I4_S, (byte)DbType.String); //TODO: fix when unkown type

                //value
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Callvirt, property.GetMethod);

                //box if value type
                if (property.PropertyType.IsValueType)
                    il.Emit(OpCodes.Box, property.PropertyType);

                il.Emit(OpCodes.Call, addInParameter);

                il.Emit(OpCodes.Nop);
            }
            il.Emit(OpCodes.Ret);

            var actionType = System.Linq.Expressions.Expression.GetActionType(typeof(object), typeof(IDbCommand));
            return (Action<object, IDbCommand>)method.CreateDelegate(actionType);
        }
    }
}
