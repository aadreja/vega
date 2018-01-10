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

namespace Vega
{
    #region class attributes

    /// <summary>
    /// Table details attribute to be implemented on entity class
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class TableAttribute : Attribute
    {
        #region constructor

        public TableAttribute()
        {
            Columns = new Dictionary<string, ColumnAttribute>(StringComparer.OrdinalIgnoreCase); //ignore case 
            DefaultInsertColumns = new List<string>();
            DefaultUpdateColumns = new List<string>();
            DefaultReadColumns = new List<string>();
        }

        public TableAttribute(string tableName) : this()
        {
            Name = tableName;
        }

        public TableAttribute(string schema, string tableName) : this(tableName)
        {
            Schema = schema;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Database Schema holding this table
        /// </summary>
        public string Schema { get; set; }
        /// <summary>
        /// Table Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Set True when table name is generated dynamically. Useful when using table partitioning else False
        /// </summary>
        public bool IsDynmic { get; set; }

        /// <summary>
        /// Set True when table doesn't contain version no column else false
        /// </summary>
        public bool NoVersionNo { get; set; }

        /// <summary>
        /// Set True when table doesn't contain CreatedOn column else false
        /// </summary>
        public bool NoCreatedOn { get; set; }

        /// <summary>
        /// Set True when table doesn't contain CreatedBy column else false
        /// </summary>
        public bool NoCreatedBy { get; set; }

        /// <summary>
        /// Set True when table doesn't contain Updated On column else false
        /// </summary>
        public bool NoUpdatedOn { get; set; }

        /// <summary>
        /// Set True when table doesn't contain Updated By column else false
        /// </summary>
        public bool NoUpdatedBy { get; set; }

        /// <summary>
        /// Set True when table doesn't contain Is Active (soft delete) column else false
        /// </summary>
        public bool NoIsActive { get; set; }

        /// <summary>
        /// Set True when record change history is to be maintained for given table else false
        /// </summary>
        public bool NeedsHistory { get; set; }

        #endregion

        public string FullName
        {
            get
            {
                if (string.IsNullOrEmpty(Schema))
                    return Name;
                else
                    return Schema + "." + Name;
            }
        }

        public Dictionary<string, ColumnAttribute> Columns { get; set; }

        public List<string> DefaultInsertColumns { get; set; }

        public List<string> DefaultUpdateColumns { get; set; }

        public List<string> DefaultReadColumns { get; set; }

        public PrimaryKeyAttribute PrimaryKeyAttribute { get; set; }

        ColumnAttribute primaryKey; //do not use this
        public ColumnAttribute PrimaryKeyColumn
        {
            get
            {
                if (primaryKey == null)
                    throw new InvalidOperationException("Primary Key attribute not defined");

                return primaryKey;
            }
            set
            {
                primaryKey = value;
            }
        }

    }

    #endregion

    #region Property attributes

    /// <summary>
    /// Set this attribute on Primary Key Property
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class PrimaryKeyAttribute : Attribute
    {
        public PrimaryKeyAttribute()
        {
            IsIdentity = false;
        }
        /// <summary>
        /// Pass false if this is not a primary key in database
        /// </summary>
        /// <param name="isKeyField"></param>
        public PrimaryKeyAttribute(bool isIdentity)
        {
            IsIdentity = isIdentity;
        }

        /// <summary>
        /// True if key field. False if not key field and there is a secondary primary key field
        /// </summary>
        public bool IsIdentity { get; set; }

    }

    /// <summary>
    /// set this attribute on each property which needs to be ignored in insert, update or read
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class IgnoreColumnAttribute : Attribute
    {
        public IgnoreColumnAttribute() : this(true)
        {

        }

        public IgnoreColumnAttribute(bool all)
        {
            Insert = all;
            Update = all;
            Read = all;
        }

        public IgnoreColumnAttribute(bool insert, bool update, bool read)
        {
            Insert = insert;
            Update = update;
            Read = read;
        }

        public bool Read { get; set; }
        public bool Insert { get; set; }
        public bool Update { get; set; }

        public bool All()
        {
            return Insert && Update && Read;
        }
    }

    /// <summary>
    /// set this attribute on each property when column name is different from property name or column type is different from property type
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class ColumnAttribute : Attribute, IEquatable<ColumnAttribute>
    {

        #region constructor

        public ColumnAttribute()
        {
            IsColumnDbTypeDefined = false;
            Name = string.Empty;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Name of the column
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Title when displayed in Grid 
        /// </summary>
        public string Title { get; set; }
        
        /// <summary>
        /// Column Type Defined
        /// </summary>
        internal bool IsColumnDbTypeDefined { get; set; }

        /// <summary>
        /// Database Type of column
        /// </summary>
        public DbType ColumnDbType
        {
            get { return columnDbType; }
            set
            {
                IsColumnDbTypeDefined = true;
                columnDbType = value;
            }
        }

        DbType columnDbType;

        /// <summary>
        /// Numeric Precision i.e. Numeric(10)
        /// </summary>
        public int NumericPrecision { get; set; }

        /// <summary>
        /// Numeric Scale i.e. Numeric(10,2)
        /// </summary>
        public int NumericScale { get; set; }
        
        /// <summary>
        /// Allow Sorting on this column
        /// </summary>
        public bool IsAllowSorting { get; set; }

        /// <summary>
        /// Allow search on this column
        /// </summary>
        public bool IsAllowSearch { get; set; }

        /// <summary>
        /// Search operator
        /// </summary>
        public DbSearchOperatorEnum SearchOperator { get; set; }

        internal PropertyInfo Property { get; set; }
        internal MethodInfo SetMethod { get; set; }
        internal MethodInfo GetMethod { get; set; }
        internal IgnoreColumnAttribute IgnoreInfo { get; set; }

        #endregion

        #region methods

        internal string GetDBTypeWithPrecisionAndScale(IDbConnection con)
        {
            return DBCache.Get(con).DbTypeString[columnDbType] + "(" + NumericPrecision + "," + NumericScale + ")";
        }

        #endregion

        #region equality methods

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is ColumnAttribute && Equals((ColumnAttribute)obj);
        }

        public bool Equals(ColumnAttribute other)
        {
            if (!Name.Equals(other?.Name, StringComparison.OrdinalIgnoreCase) || ColumnDbType != other?.ColumnDbType) return false;
            else return true;
        }

        #endregion
    }

    #endregion
}
