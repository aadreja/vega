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

        /// <summary>
        /// Default constructor
        /// </summary>
        public TableAttribute()
        {
            Columns = new Dictionary<string, ColumnAttribute>(StringComparer.OrdinalIgnoreCase); //ignore case 
            DefaultInsertColumns = new List<string>();
            DefaultUpdateColumns = new List<string>();
            DefaultReadColumns = new List<string>();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tableName">Table name for the entity</param>
        public TableAttribute(string tableName) : this()
        {
            Name = tableName;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="schema">Schema name for the entity</param>
        /// <param name="tableName">Table Name for the entity</param>
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

        /// <summary>
        /// Fullname of table
        /// </summary>
        internal string FullName
        {
            get
            {
                if (string.IsNullOrEmpty(Schema))
                    return Name;
                else
                    return Schema + "." + Name;
            }
        }

        internal Dictionary<string, ColumnAttribute> Columns { get; set; }

        internal List<string> DefaultInsertColumns { get; set; }

        internal List<string> DefaultUpdateColumns { get; set; }

        internal List<string> DefaultReadColumns { get; set; }

        internal PrimaryKeyAttribute PrimaryKeyAttribute { get; set; }

        ColumnAttribute primaryKey; //do not use this
        internal ColumnAttribute PrimaryKeyColumn
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
        /// <summary>
        /// Default constructor
        /// </summary>
        public PrimaryKeyAttribute()
        {
            IsIdentity = false;
        }
        /// <summary>
        /// Pass false if this is not a primary key in database
        /// </summary>
        /// <param name="isIdentity"></param>
        public PrimaryKeyAttribute(bool isIdentity)
        {
            IsIdentity = isIdentity;
        }

        /// <summary>
        /// True if key field. False if not key field and there is a secondary primary key field
        /// </summary>
        internal bool IsIdentity { get; set; }

    }

    /// <summary>
    /// Set this attribute on each property which needs to be ignored in insert, update or read
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class IgnoreColumnAttribute : Attribute
    {
        /// <summary>
        /// Default Constructor, Set ignore all true
        /// </summary>
        public IgnoreColumnAttribute() : this(true)
        {

        }

        /// <summary>
        /// Default Sets Ignore Insert, Update, Read to true or false
        /// </summary>
        /// <param name="all">boolean</param>
        public IgnoreColumnAttribute(bool all)
        {
            Insert = all;
            Update = all;
            Read = all;
        }

        /// <summary>
        /// Default Sets Ignore Insert, Update, Read to respective parameters
        /// </summary>
        /// <param name="insert">true to ignore column on insert</param>
        /// <param name="update">true to ignore column on update</param>
        /// <param name="read">true to ignore column on read</param>
        public IgnoreColumnAttribute(bool insert, bool update, bool read)
        {
            Insert = insert;
            Update = update;
            Read = read;
        }

        internal bool Read { get; set; }
        internal bool Insert { get; set; }
        internal bool Update { get; set; }

        internal bool All()
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

        /// <summary>
        /// Default Constructor
        /// </summary>
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
        //public DbSearchOperatorEnum SearchOperator { get; set; }

        #endregion

        #region Internal Properties

        internal PropertyInfo Property { get; set; }
        internal MethodInfo SetMethod { get; set; }
        internal MethodInfo GetMethod { get; set; }
        internal IgnoreColumnAttribute IgnoreInfo { get; set; }

        internal Action<object, object> SetAction { get; set; }
        internal Func<object, object> GetAction { get; set; }

        #endregion

        #region methods

        internal string GetDBTypeWithPrecisionAndScale(IDbConnection con)
        {
            return DBCache.Get(con).DbTypeString[columnDbType] + "(" + NumericPrecision + "," + NumericScale + ")";
        }

        #endregion

        #region equality methods

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer hash code.
        /// </returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        ///<summary>
        ///Determines whether this ColumnAttribute and a specified ColumnAttribute object are same.
        ///</summary>
        ///<param name="obj">The ColumnAttribute to compare to this instance.</param>
        ///<returns> true if the value of the value parameter is the same as this ColumnAttribute; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return obj is ColumnAttribute && Equals((ColumnAttribute)obj);
        }

        ///<summary>
        ///Determines whether this ColumnAttribute and a specified ColumnAttribute object are same.
        ///</summary>
        ///<param name="other">The ColumnAttribute to compare to this instance.</param>
        ///<returns> true if the value of the value parameter is the same as this ColumnAttribute; otherwise, false.</returns>
        public bool Equals(ColumnAttribute other)
        {
            if (!Name.Equals(other?.Name, StringComparison.OrdinalIgnoreCase) || ColumnDbType != other?.ColumnDbType) return false;
            else return true;
        }

        #endregion
    }

    #endregion
}
