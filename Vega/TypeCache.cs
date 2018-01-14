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

namespace Vega
{
    internal static class TypeCache
    {

        static Dictionary<Type, DbType> typeToDbType;

        internal static Dictionary<Type, DbType> TypeToDbType
        {
            get
            {
                if (typeToDbType != null)
                    return typeToDbType;

                typeToDbType = new Dictionary<Type, DbType>
                {
                    [typeof(byte)] = DbType.Byte,
                    [typeof(sbyte)] = DbType.SByte,
                    [typeof(short)] = DbType.Int16,
                    [typeof(ushort)] = DbType.UInt16,
                    [typeof(int)] = DbType.Int32,
                    [typeof(uint)] = DbType.UInt32,
                    [typeof(long)] = DbType.Int64,
                    [typeof(ulong)] = DbType.UInt64,
                    [typeof(float)] = DbType.Single,
                    [typeof(double)] = DbType.Double,
                    [typeof(decimal)] = DbType.Decimal,
                    [typeof(bool)] = DbType.Boolean,
                    [typeof(string)] = DbType.String,
                    [typeof(char)] = DbType.StringFixedLength,
                    [typeof(Guid)] = DbType.Guid,
                    [typeof(DateTime)] = DbType.DateTime,
                    [typeof(DateTimeOffset)] = DbType.DateTimeOffset,
                    [typeof(byte[])] = DbType.Binary,
                    [typeof(byte?)] = DbType.Byte,
                    [typeof(sbyte?)] = DbType.SByte,
                    [typeof(short?)] = DbType.Int16,
                    [typeof(ushort?)] = DbType.UInt16,
                    [typeof(int?)] = DbType.Int32,
                    [typeof(uint?)] = DbType.UInt32,
                    [typeof(long?)] = DbType.Int64,
                    [typeof(ulong?)] = DbType.UInt64,
                    [typeof(float?)] = DbType.Single,
                    [typeof(double?)] = DbType.Double,
                    [typeof(decimal?)] = DbType.Decimal,
                    [typeof(bool?)] = DbType.Boolean,
                    [typeof(char?)] = DbType.StringFixedLength,
                    [typeof(Guid?)] = DbType.Guid,
                    [typeof(DateTime?)] = DbType.DateTime,
                    [typeof(DateTimeOffset?)] = DbType.DateTimeOffset,
                    [typeof(Enum)] = DbType.Int16
                };

                return typeToDbType;
            }
        }
    }
}
