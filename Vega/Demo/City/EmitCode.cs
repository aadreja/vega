using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Demo.City
{
    //https://stackoverflow.com/questions/24260673/populating-nullable-type-from-sqldatareader-using-reflection-emit
    //https://msdn.microsoft.com/en-us/library/system.reflection.emit.opcodes(v=vs.110).aspx
    class EmitCode
    {
        private static readonly MethodInfo GetValueMethod = typeof(SqlDataReader).GetMethod("get_Item", new Type[] { typeof(int) });
        private static readonly MethodInfo IsDBNullMethod = typeof(SqlDataReader).GetMethod("IsDBNull", new Type[] { typeof(int) });
        private static Func<IDataReader, City> func;
        private static string conString;

        public IEnumerable<City> ReadAll(int count)
        {
            if (string.IsNullOrEmpty(conString))
            {
                conString = ConfigurationManager.ConnectionStrings["test"].ConnectionString;
            }

            using (Npgsql.NpgsqlConnection con = new Npgsql.NpgsqlConnection(conString))
            {
                con.Open();

                Npgsql.NpgsqlCommand cmd = con.CreateCommand();
                cmd.CommandText = $"SELECT cityid, cityname FROM prompt.city";

                Npgsql.NpgsqlDataReader rdr = cmd.ExecuteReader();

                if (func == null)
                {
                    func = ReaderToObject<City>(rdr);
                }

                while (rdr.Read())
                {
                    object val = func(rdr);
                    if (val == null || val is City)
                    {
                        yield return (City)val;
                    }
                }

                rdr.Close();
                rdr = null;
            }
        }

        public Func<IDataReader, T> ReaderToObject<T>(IDataReader rdr) where T : new()
        {
            Type[] args = { typeof(IDataReader) };
            DynamicMethod method = new DynamicMethod("DynamicRead" + Guid.NewGuid().ToString(), typeof(T), args, typeof(T).Module, true);
            ILGenerator il = method.GetILGenerator();

            LocalBuilder result = il.DeclareLocal(typeof(T)); //loc_0
            il.Emit(OpCodes.Newobj, typeof(T).GetConstructor(Type.EmptyTypes));
            il.Emit(OpCodes.Stloc_0, result); //Pops the current value from the top of the evaluation stack and stores it in a the local variable list at a specified index.

            Label tryBlock = il.BeginExceptionBlock();

            int valueCopyLocal = il.DeclareLocal(typeof(object)).LocalIndex; //declare local variable to store object value. loc_1
            il.DeclareLocal(typeof(int)); //declare local variable to store index //loc_2
            il.Emit(OpCodes.Ldc_I4_0); //load 0 in index
            il.Emit(OpCodes.Stloc_2); //pop and save to local variable loc 2

            PropertyInfo[] allProps = typeof(T).GetProperties();
            for (int i = 0; i < rdr.FieldCount; i++)
            {
                PropertyInfo propertyInfo = allProps.Where(p => p.Name.Equals(rdr.GetName(i), StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                if (propertyInfo != null && propertyInfo.GetSetMethod() != null)
                {
                    Label endIfLabel = il.DefineLabel();

                    il.Emit(OpCodes.Ldarg_0); //load the argument. Loads the argument at index 0 onto the evaluation stack.
                    il.Emit(OpCodes.Ldc_I4, i); //push field index as int32 to the stack. Pushes a supplied value of type int32 onto the evaluation stack as an int32.
                    il.Emit(OpCodes.Dup);//copy value
                    il.Emit(OpCodes.Stloc_2);//pop and save value to loc 2

                    il.Emit(OpCodes.Callvirt, GetValueMethod); //Call rdr[i] method - Calls a late - bound method on an object, pushing the return value onto the evaluation stack.

                    //TODO: dynamic location using valueCopyLocal
                    il.Emit(OpCodes.Stloc_1); //pop the value and push in stack location 1
                    il.Emit(OpCodes.Ldloc_1); //load the variable in location 1

                    il.Emit(OpCodes.Isinst, typeof(DBNull)); //check whether value is null - Tests whether an object reference (type O) is an instance of a particular class.
                    il.Emit(OpCodes.Brtrue, endIfLabel); //go to end block if value is null

                    il.Emit(OpCodes.Ldloc_0);
                    il.Emit(OpCodes.Ldloc_1); //TODO: dynamic location using valueCopyLocal
                    il.Emit(OpCodes.Unbox_Any, rdr.GetFieldType(i));
                    il.Emit(OpCodes.Callvirt, propertyInfo.GetSetMethod());
                    il.Emit(OpCodes.Nop);

                    il.MarkLabel(endIfLabel);

                }
            }
            //il.ThrowException(typeof(Exception));

            il.BeginCatchBlock(typeof(Exception)); //begin try block. exception is in stack
            il.Emit(OpCodes.Ldloc_2); //load index
            il.Emit(OpCodes.Ldarg_0); //load argument reader
            il.Emit(OpCodes.Ldloc_1); //load value //TODO: dynamic location using valueCopyLocal
            il.EmitCall(OpCodes.Call, typeof(EmitCode).GetMethod(nameof(EmitCode.HandleException)), null); //call exception handler
            il.EndExceptionBlock();

            il.Emit(OpCodes.Ldloc, result);
            il.Emit(OpCodes.Ret);

            var funcType = System.Linq.Expressions.Expression.GetFuncType(typeof(IDataReader), typeof(T));
            return (Func<IDataReader, T>)method.CreateDelegate(funcType);
        }

        public Func<IDataReader, T> NewReaderToObject<T>(IDataReader rdr) where T : new()
        {
            Type[] args = { typeof(IDataReader) };
            DynamicMethod method = new DynamicMethod("ReadCity", typeof(T), args, typeof(T).Module, true);
            ILGenerator generator = method.GetILGenerator();

            LocalBuilder result = generator.DeclareLocal(typeof(T));
            generator.Emit(OpCodes.Newobj, typeof(T).GetConstructor(Type.EmptyTypes));
            generator.Emit(OpCodes.Stloc, result);

            PropertyInfo[] allProps = typeof(T).GetProperties();

            for (int i = 0; i < rdr.FieldCount; i++)
            {
                PropertyInfo propertyInfo = allProps.Where(p => p.Name.Equals(rdr.GetName(i), StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                Label endIfLabel = generator.DefineLabel();
                if (propertyInfo != null && propertyInfo.GetSetMethod() != null)
                {
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Ldc_I4, i);
                    generator.Emit(OpCodes.Callvirt, IsDBNullMethod);
                    generator.Emit(OpCodes.Brtrue, endIfLabel);

                    generator.Emit(OpCodes.Ldloc, result);
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Ldc_I4, i);
                    generator.Emit(OpCodes.Callvirt, GetValueMethod);
                    generator.Emit(OpCodes.Unbox_Any, rdr.GetFieldType(i));
                    generator.Emit(OpCodes.Callvirt, propertyInfo.GetSetMethod());

                    generator.MarkLabel(endIfLabel);
                }
            }
            generator.Emit(OpCodes.Ldloc, result);
            generator.Emit(OpCodes.Ret);

            var funcType = System.Linq.Expressions.Expression.GetFuncType(typeof(IDataReader), typeof(T));
            return (Func<IDataReader, T>)method.CreateDelegate(funcType);
        }

        private static void StoreLocal(ILGenerator il, int index)
        {
            if (index < 0 || index >= short.MaxValue) throw new ArgumentNullException(nameof(index));
            switch (index)
            {
                case 0: il.Emit(OpCodes.Stloc_0); break;
                case 1: il.Emit(OpCodes.Stloc_1); break;
                case 2: il.Emit(OpCodes.Stloc_2); break;
                case 3: il.Emit(OpCodes.Stloc_3); break;
                default:
                    if (index <= 255)
                    {
                        il.Emit(OpCodes.Stloc_S, (byte)index);
                    }
                    else
                    {
                        il.Emit(OpCodes.Stloc, (short)index);
                    }
                    break;
            }
        }

        private static void LoadLocal(ILGenerator il, int index)
        {
            if (index < 0 || index >= short.MaxValue) throw new ArgumentNullException(nameof(index));
            switch (index)
            {
                case 0: il.Emit(OpCodes.Ldloc_0); break;
                case 1: il.Emit(OpCodes.Ldloc_1); break;
                case 2: il.Emit(OpCodes.Ldloc_2); break;
                case 3: il.Emit(OpCodes.Ldloc_3); break;
                default:
                    if (index <= 255)
                    {
                        il.Emit(OpCodes.Ldloc_S, (byte)index);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldloc, (short)index);
                    }
                    break;
            }
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
