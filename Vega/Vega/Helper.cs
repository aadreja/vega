/*
 Description: Vega - Fastest ORM with enterprise features
 Author: Ritesh Sutaria
 Date: 9-Dec-2017
 Home Page: https://github.com/aadreja/vega
            http://www.vegaorm.com
*/
using System;
using System.Configuration;
using System.Data;
using System.Text;

namespace Vega
{
    public static class Helper
    {
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

        public static object ToParameterValue(this object value)
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

        public static string ToXMLValue(this object value, DbType dbType)
        {
            if (value == null) return string.Empty;

            string strValue = (dbType == DbType.DateTime || 
                dbType == DbType.Date) ? strValue = ((DateTime)value).ToSQLFormat() : value.ToString();            

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

        public static string ToSQLFormat(this DateTime pDate)
        {
            return pDate.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static string ToSQLFormat(this DateTime? pDate)
        {
            if (pDate == null) return string.Empty;
            else return ToSQLFormat((DateTime)pDate);
        }

        #endregion

        #region string & stringbuilder extension

        public static string RemoveLastComma(this string pString)
        {
            if (pString[pString.Length - 1] == ',')
            {
                pString.Remove(pString.Length - 1, 1);
            }
            return pString;
        }

        public static StringBuilder RemoveLastComma(this StringBuilder pString)
        {
            if (pString[pString.Length - 1] == ',')
            {
                pString.Remove(pString.Length - 1, 1);
            }
            return pString;
        }

        #endregion

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
    }
}
