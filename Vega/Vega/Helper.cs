/*
 Description: Vega - Fastest ORM with enterprise features
 Author: Ritesh Sutaria
 Date: 9-Dec-2017
 Home Page: https://github.com/aadreja/vega
            http://www.vegaorm.com
*/
using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Vega
{
    public static class Helper
    {
        #region constructor

        static Helper()
        {
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

        #endregion

        #region type extension

        #region idbcommand extension

        public static void AddOutParameter(this IDbCommand command,
                                   string name,
                                   DbType dbType)
        {
            AddParameter(command, name, dbType, ParameterDirection.Output, name, DataRowVersion.Default, null);
        }

        public static void AddInParameter(this IDbCommand command,
                                   string name,
                                   DbType dbType,
                                   object value)
        {
            AddParameter(command, name, dbType, ParameterDirection.Input, String.Empty, DataRowVersion.Default, value);
        }

        public static void AddParameter(this IDbCommand command,
                                 string name,
                                 DbType dbType,
                                 ParameterDirection direction,
                                 string sourceColumn,
                                 DataRowVersion sourceVersion,
                                 object value)
        {
            AddParameter(command, name, dbType, 0, direction, false, 0, 0, sourceColumn, sourceVersion, value);
        }

        public static void AddParameter(this IDbCommand command,
                                         string name,
                                         DbType dbType,
                                         int size,
                                         ParameterDirection direction,
                                         bool nullable,
                                         byte precision,
                                         byte scale,
                                         string sourceColumn,
                                         DataRowVersion sourceVersion,
                                         object value)
        {
            if (command == null) throw new ArgumentNullException("command");

            IDbDataParameter parameter = command.CreateParameter();

            parameter.ParameterName = name;
            parameter.DbType = dbType;
            parameter.Size = size;
            parameter.Direction = direction;
            //parameter.IsNullable = nullable; //TODO
            parameter.Precision = precision;
            parameter.Scale = scale;
            parameter.SourceColumn = sourceColumn;
            parameter.SourceVersion = sourceVersion;
            parameter.Value = value.ToParameterValue();

            command.Parameters.Add(parameter);
        }

        #endregion

        #region object extension 

        internal static object ToParameterValue(this object value)
        {
            if (value == null)
            {
                return DBNull.Value;
            }

            //TODO: not all needs mindatetime to be stored as null
            //if (value is DateTime && (DateTime)value == DateTime.MinValue) return DBNull.Value;

            if (value is Guid) if (Equals(value, Guid.Empty)) return DBNull.Value; else return value;
            else if (value.GetType().IsEnum) return Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType()));
            else return value;
        }

        internal static string ToXMLValue(this object value, DbType dbType)
        {
            if (value == null) return string.Empty;

            string strValue = (dbType == DbType.DateTime || 
                dbType == DbType.Date) ? strValue = ((DateTime)value).ToSQLDateTime() : value.ToString();            

            //replace special characters in XML //" ' & < >
            strValue = strValue.Replace("'", "&apos;").Replace("\"", "&quot;").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

            return strValue;
        }

        public static bool IsNumber(this object value)
        {
            return value is sbyte
                    || value is byte
                    || value is short
                    || value is ushort
                    || value is int
                    || value is uint
                    || value is long
                    || value is ulong
                    || value is float
                    || value is double
                    || value is decimal;
        }

        #endregion

        #region datetime extension

        public static DateTime FromSQLDateTime(this string pDate)
        {
            DateTime.TryParse(pDate, out DateTime result);
            return result;
        }

        public static string ToSQLDateTime(this DateTime pDate)
        {
            return pDate.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static string ToSQLDateTime(this DateTime? pDate)
        {
            if (pDate == null) return string.Empty;
            else return ToSQLDateTime((DateTime)pDate);
        }

        public static DateTime FromSQLDate(this string pDate)
        {
            DateTime.TryParse(pDate, out DateTime result);
            return result;
        }

        public static string ToSQLDate(this DateTime pDate)
        {
            return pDate.ToString("yyyy-MM-dd");
        }

        public static string ToSQLDate(this DateTime? pDate)
        {
            if (pDate == null) return string.Empty;
            else return ToSQLDate((DateTime)pDate);
        }

        #endregion

        #region string & stringbuilder extension

        internal static string RemoveLastComma(this string pString)
        {
            if (pString[pString.Length - 1] == ',')
            {
                pString.Remove(pString.Length - 1, 1);
            }
            return pString;
        }

        internal static StringBuilder RemoveLastComma(this StringBuilder pString)
        {
            if (pString[pString.Length - 1] == ',')
            {
                pString.Remove(pString.Length - 1, 1);
            }
            return pString;
        }

        #endregion

        #endregion

        #region converters

        internal static R Parse<R>(this object value)
        {
            if (value == null || value is DBNull) return default(R);
            if (value is R) return (R)value;

            Type type = typeof(R);
            if (type.IsEnum)
            {
                value = Convert.ChangeType(value, Enum.GetUnderlyingType(type), CultureInfo.InvariantCulture);
                return (R)Enum.ToObject(type, value);
            }
            else
            {
                return (R)Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
            }
        }

        internal static object ConvertTo(this object value, Type type)
        {
            var converter = TypeDescriptor.GetConverter(type);

            return converter.ConvertFrom(value);
        }

        internal static object ConvertTo(this string value, Type type)
        {
            var converter = TypeDescriptor.GetConverter(type);

            return converter.ConvertFromString(value);
        }

        #endregion

        #region Configuration Helper Methods

        /// <summary>
        /// Reads value from AppSettings sections from App.Config or Web.Config file
        /// </summary>
        /// <param name="pKey">String value indicating Key.</param>
        /// <returns>if found returns value of the specified key from the .config files, otherwise returns Empty string.</returns>
        public static string GetAppSetting(String pKey)
        {
            if (ConfigurationManager.AppSettings[pKey] != null)
                return ConfigurationManager.AppSettings[pKey].ToString();
            else
                return string.Empty;
        }

        /// <summary>
        /// Reads value from ConnectionStrings sections from App.Config or Web.Config file
        /// </summary>
        /// <param name="pKey">String value indicating Key.</param>
        /// <returns>if found returns value of the specified key from the .config files, otherwise returns Empty string.</returns>
        public static string GetConnectionString(String pKey)
        {
            if (ConfigurationManager.ConnectionStrings[pKey] != null)
                return ConfigurationManager.ConnectionStrings[pKey].ToString();
            else
                return string.Empty;
        }

        #endregion

        #region Emit Helper

        //https://www.codeproject.com/Articles/9927/Fast-Dynamic-Property-Access-with-C

        static Hashtable mTypeHash;

        //TODO: Make it internal later
        public static Func<object, object> CreateGetProperty(Type entityType, string propertyName)
        {
            MethodInfo mi = entityType.GetProperty(propertyName).GetMethod;

            if (mi == null) return null; //no get property

            Type[] args = new Type[] { typeof(object) };

            DynamicMethod method = new DynamicMethod("Get_" + entityType.Name + "_" + propertyName, typeof(object), args, entityType.Module, true);
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

        internal static Action<object, object> CreateSetProperty(Type entityType, string propertyName)
        {
            MethodInfo mi = entityType.GetProperty(propertyName).SetMethod;

            if (mi == null) return null; //no set property

            Type[] args = new Type[] { typeof(object), typeof(object) };

            DynamicMethod method = new DynamicMethod("Set_" + entityType.Name + "_" + propertyName, null, args, entityType.Module, true);
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

        #endregion
    }
}
