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

namespace Vega
{
    public struct ReaderKey : IEquatable<ReaderKey>
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

        public ReaderKey(int hashCode, IDataReader reader)
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

        public static bool operator ==(ReaderKey obj1, ReaderKey obj2)
        {
            //TODO: check this operator
            return Equals(obj1, obj2);
        }

        public static bool operator !=(ReaderKey obj1, ReaderKey obj2)
        {
            //TODO: check this operator
            return !Equals(obj1, obj2);
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

    public class ReaderCache<T> where T : EntityBase, new()
    {
        private static Dictionary<ReaderKey, Func<IDataReader, T>> readers = new Dictionary<ReaderKey, Func<IDataReader, T>>();

        private static int GetReaderHash(IDataReader reader)
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

        public static Func<IDataReader, T> GetFromCache(IDataReader reader)
        {
            int hash = GetReaderHash(reader);

            ReaderKey key = new ReaderKey(hash, reader);

            Func<IDataReader, T> func;
            lock (readers)
            {
                if (readers.TryGetValue(key, out func)) return func;
            }
            func = ReaderToObject(reader);
            lock (readers)
            {
                return readers[key] = func;
            }
        }

        private static Func<IDataReader, T> ReaderToObject(IDataReader rdr) 
        {
            MethodInfo GetValueMethod = rdr.GetType().GetMethod("get_Item", new Type[] { typeof(int) });

            Type[] args = { typeof(IDataReader) };
            DynamicMethod method = new DynamicMethod("DynamicRead" + Guid.NewGuid().ToString(), typeof(T), args, typeof(Repository<T>).Module, true);
            ILGenerator il = method.GetILGenerator();

            LocalBuilder result = il.DeclareLocal(typeof(T)); //loc_0
            il.Emit(OpCodes.Newobj, typeof(T).GetConstructor(Type.EmptyTypes));
            il.Emit(OpCodes.Stloc_0, result); //Pops the current value from the top of the evaluation stack and stores it in a the local variable list at a specified index.

            Label tryBlock = il.BeginExceptionBlock();

            LocalBuilder valueCopy = il.DeclareLocal(typeof(object)); //declare local variable to store object value. loc_1
            int valueCopyLocal = valueCopy.LocalIndex; 

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
                    il.Emit(OpCodes.Callvirt, GetValueMethod); //Call rdr[i] method - Calls a late - bound method on an object, pushing the return value onto the evaluation stack.

                    //TODO: dynamic location using valueCopyLocal
                    il.Emit(OpCodes.Stloc_1); //pop the value and push in stack location 1
                    il.Emit(OpCodes.Ldloc_1); //load the variable in location 1

                    il.Emit(OpCodes.Isinst, typeof(DBNull)); //check whether value is null - Tests whether an object reference (type O) is an instance of a particular class.
                    il.Emit(OpCodes.Brtrue, endIfLabel); //go to end block if value is null

                    il.Emit(OpCodes.Ldloc_0); //load T result
                    il.Emit(OpCodes.Ldloc_1); //TODO: dynamic location using valueCopyLocal

                    il.Emit(OpCodes.Unbox_Any, rdr.GetFieldType(i)); //type cast
                    //if (columnInfo.Property.PropertyType.FullName == typeof(PrimaryKey).FullName)
                    //{
                    //    il.Emit(OpCodes.Newobj, typeof(PrimaryKey).GetConstructor(new Type[] { typeof(int) })); //Create new Primary Key object in stack
                    //}
                    // for nullable type fields
                    if (columnInfo.Property.PropertyType.IsGenericType && columnInfo.Property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        var underlyingType = Nullable.GetUnderlyingType(columnInfo.Property.PropertyType);
                        il.Emit(OpCodes.Newobj, columnInfo.Property.PropertyType.GetConstructor(new Type[] { underlyingType }));
                    }
                    //else
                    //{
                    //}
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

}
