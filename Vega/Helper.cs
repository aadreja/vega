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
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;

namespace Vega
{
    /// <summary>
    /// Vega extension and helper methods 
    /// </summary>
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

        /// <summary>
        /// Adds out parameter to the command
        /// </summary>
        /// <param name="cmd">IDbCommand object</param>
        /// <param name="name">Name of parameter</param>
        /// <param name="dbType">DbType of parameter</param>
        public static void AddOutParameter(this IDbCommand cmd,
                                   string name,
                                   DbType dbType)
        {
            AddParameter(cmd, name, dbType, ParameterDirection.Output, name, DataRowVersion.Default, null);
        }

        /// <summary>
        /// Adds In parameter to the command
        /// </summary>
        /// <param name="cmd">IDbCommand object</param>
        /// <param name="name">Name of parameter</param>
        /// <param name="dbType">DbType of parameter</param>
        /// <param name="value">Value of parameter</param>
        public static void AddInParameter(this IDbCommand cmd,
                                   string name,
                                   DbType dbType,
                                   object value)
        {
            AddParameter(cmd, name, dbType, ParameterDirection.Input, String.Empty, DataRowVersion.Default, value);
        }

        /// <summary>
        /// Adds Parameter to the command
        /// </summary>
        /// <param name="cmd">IDbCommand object</param>
        /// <param name="name">Name of parameter</param>
        /// <param name="dbType">DbType of parameter</param>
        /// <param name="direction">Director: In or Out</param>
        /// <param name="sourceColumn">Name of the source column for loading or returning the System.Data.IDataParameter.Value.</param>
        /// <param name="sourceVersion">System.Data.DataRowVersion to use when loading System.Data.IDataParameter.Value</param>
        /// <param name="value">Value of parameter</param>
        public static void AddParameter(this IDbCommand cmd,
                                 string name,
                                 DbType dbType,
                                 ParameterDirection direction,
                                 string sourceColumn,
                                 DataRowVersion sourceVersion,
                                 object value)
        {
            AddParameter(cmd, name, dbType, 0, direction, false, 0, 0, sourceColumn, sourceVersion, value);
        }

        /// <summary>
        /// Adds Parameter to the command
        /// </summary>
        /// <param name="cmd">IDbCommand object</param>
        /// <param name="name">Name of parameter</param>
        /// <param name="dbType">DbType of parameter</param>
        /// <param name="direction">Director: In or Out</param>
        /// <param name="sourceColumn">Name of the source column for loading or returning the System.Data.IDataParameter.Value.</param>
        /// <param name="sourceVersion">System.Data.DataRowVersion to use when loading System.Data.IDataParameter.Value</param>
        /// <param name="size">Size of column</param>
        /// <param name="nullable">Is Null values allowed</param>
        /// <param name="precision">For Numeric data size e.g. Decimal(10)</param>
        /// <param name="scale">For Numeric data scale size e.g. Decimal(10,2)</param>
        /// <param name="value">Value of parameter</param>
        public static void AddParameter(this IDbCommand cmd,
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
            if (cmd == null) throw new ArgumentNullException("command");

            IDbDataParameter parameter = cmd.CreateParameter();

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

            cmd.Parameters.Add(parameter);
        }

        #endregion

        #region object extension 

        /// <summary>
        /// Box Parameter Value to type as per database
        /// </summary>
        /// <param name="value">object of value</param>
        /// <returns>boxed value</returns>
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

        /// <summary>
        /// Change escaped characters in XML and retuns XML format string
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <param name="dbType">dbtype of column</param>
        /// <returns>xml string</returns>
        internal static string ToXMLValue(this object value, DbType dbType)
        {
            if (value == null) return string.Empty;

            string strValue = (dbType == DbType.DateTime || 
                dbType == DbType.Date) ? strValue = ((DateTime)value).ToSQLDateTime() : value.ToString();            

            //replace special characters in XML //" ' & < >
            strValue = strValue.Replace("'", "&apos;").Replace("\"", "&quot;").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

            return strValue;
        }

        /// <summary>
        /// Checks whether value is number returns true if number else false
        /// </summary>
        /// <param name="value">any object</param>
        /// <returns>true or false</returns>
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

        /// <summary>
        /// Checks whether value is of AnonymousType
        /// </summary>
        /// <param name="value">any object</param>
        /// <returns>true or false</returns>
        public static bool IsAnonymousType(this object value)
        {
            if (value == null)
                return false;

            Type type = value.GetType();

            return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
               && type.IsGenericType && type.Name.Contains("AnonymousType")
               && (type.Name.StartsWith("<>", StringComparison.OrdinalIgnoreCase) ||
                   type.Name.StartsWith("VB$", StringComparison.OrdinalIgnoreCase))
               && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
        }

        #endregion

        #region datetime extension

        /// <summary>
        /// From SQL format datetime (yyyy-MM-dd) to datetime
        /// </summary>
        /// <param name="pDate">String date</param>
        /// <returns></returns>
        public static DateTime FromSQLDateTime(this string pDate)
        {
            DateTime.TryParse(pDate, out DateTime result);
            return result;
        }

        /// <summary>
        /// To SQL format datetime (yyyy-MM-dd HH:mm:ss)
        /// </summary>
        /// <param name="pDate">DateTime</param>
        /// <returns></returns>
        public static string ToSQLDateTime(this DateTime pDate)
        {
            return pDate.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// To SQL format datetime (yyyy-MM-dd HH:mm:ss)
        /// </summary>
        /// <param name="pDate">DateTime Nullable</param>
        /// <returns></returns>
        public static string ToSQLDateTime(this DateTime? pDate)
        {
            if (pDate == null) return string.Empty;
            else return ToSQLDateTime((DateTime)pDate);
        }

        /// <summary>
        /// From SQL format datetime (yyyy-MM-dd) to datetime
        /// </summary>
        /// <param name="pDate">String date</param>
        /// <returns></returns>
        public static DateTime FromSQLDate(this string pDate)
        {
            DateTime.TryParse(pDate, out DateTime result);
            return result;
        }

        /// <summary>
        /// To SQL format date (yyyy-MM-dd)
        /// </summary>
        /// <param name="pDate">DateTime</param>
        /// <returns></returns>
        public static string ToSQLDate(this DateTime pDate)
        {
            return pDate.ToString("yyyy-MM-dd");
        }

        /// <summary>
        /// To SQL format date (yyyy-MM-dd)
        /// </summary>
        /// <param name="pDate">Nullable DateTime</param>
        /// <returns></returns>
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
                pString = pString.Remove(pString.Length - 1, 1);
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

        internal static object GetDateTimeOrDatabaseDateTimeSQL(DateTime? dateTime, Database db, bool overRideCreatedUpdatedOn)
        {
            if(!overRideCreatedUpdatedOn || dateTime == null || dateTime == default(DateTime))
                return db.CURRENTDATETIMESQL;
            else
                return dateTime;
        }

        #endregion

        #region Configuration Helper Methods

        ///// <summary>
        ///// Reads value from AppSettings sections from App.Config or Web.Config file
        ///// </summary>
        ///// <param name="pKey">String value indicating Key.</param>
        ///// <returns>if found returns value of the specified key from the .config files, otherwise returns Empty string.</returns>
        //public static string GetAppSetting(String pKey)
        //{
        //    if (ConfigurationManager.AppSettings[pKey] != null)
        //        return ConfigurationManager.AppSettings[pKey].ToString();
        //    else
        //        return string.Empty;
        //}

        ///// <summary>
        ///// Reads value from ConnectionStrings sections from App.Config or Web.Config file
        ///// </summary>
        ///// <param name="pKey">String value indicating Key.</param>
        ///// <returns>if found returns value of the specified key from the .config files, otherwise returns Empty string.</returns>
        //public static string GetConnectionString(String pKey)
        //{
        //    if (ConfigurationManager.ConnectionStrings[pKey] != null)
        //        return ConfigurationManager.ConnectionStrings[pKey].ToString();
        //    else
        //        return string.Empty;
        //}

        #endregion

        #region Emit Helper

        //https://www.codeproject.com/Articles/9927/Fast-Dynamic-Property-Access-with-C

        static Hashtable mTypeHash;

        internal static Func<object, object> CreateGetProperty(Type entityType, string propertyName)
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
