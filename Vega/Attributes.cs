﻿/*
 Description: Vega - Fastest ORM with enterprise features
 Author: Ritesh Sutaria
 Date: 9-Dec-2017
 Home Page: https://github.com/aadreja/vega
            http://www.vegaorm.com
*/
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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

        #region Public Properties

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
        public bool IsDynamic { get; set; }

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

        /// <summary>
        /// Set True when no default fields IsActive, VersionNo, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn are not needed
        /// </summary>
        public bool IsNoDefaultFields
        {
            get
            {
                return NoVersionNo && NoIsActive && NoCreatedBy && NoCreatedOn && NoUpdatedBy && NoUpdatedOn;
            }
            set
            {
                NoVersionNo = value;
                NoIsActive = value;
                NoCreatedOn = value;
                NoCreatedBy = value;
                NoUpdatedOn = value;
                NoUpdatedBy = value;
            }
        }

        #endregion

        #region Internal Properties

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

        internal List<ForeignKey> VirtualForeignKeys { get; set; }

        internal List<string> DefaultInsertColumns { get; set; }

        internal List<string> DefaultUpdateColumns { get; set; }

        internal List<string> DefaultReadColumns { get; set; }

        internal ColumnAttribute PkColumn
        {
            get
            {
                long pkCount = Columns.LongCount(p => p.Value.IsPrimaryKey);
                if (pkCount == 0)
                {
                    throw new InvalidOperationException("This method cannot be used as no primary defined on this entity");
                }
                else if (pkCount > 1)
                {
                    throw new InvalidOperationException("This method cannot be used as more then one primary exists on this entity");
                }
                else
                {
                    return Columns.FirstOrDefault(p => p.Value.IsPrimaryKey).Value;
                }
            }
        }

        internal ColumnAttribute PkIdentityColumn
        {
            get
            {
                long pkCount = Columns.LongCount(p => p.Value.IsPrimaryKey && p.Value.PrimaryKeyInfo.IsIdentity);
                if (pkCount == 0)
                {
                    throw new InvalidOperationException("This method cannot be used as no primary identity is defined on this entity");
                }
                else if (pkCount > 1)
                {
                    throw new InvalidOperationException("This method cannot be used as multiple primary identity is defined on this entity");
                }
                else
                {
                    return Columns.Where(p => p.Value.IsPrimaryKey && p.Value.PrimaryKeyInfo.IsIdentity).
                        Select(p => p.Value).
                        FirstOrDefault();
                }
            }
        }

        internal List<ColumnAttribute> PkColumnList
        {
            get
            {
                if (Columns.LongCount(p => p.Value.IsPrimaryKey) == 0)
                    return new List<ColumnAttribute>();
                else
                    return Columns.Where(p => p.Value.IsPrimaryKey).Select(p => p.Value).ToList();
            }
        }

        #endregion

        #region helper to replace entity base

        bool IsKeyFieldEmpty(object id, string fieldName)
        {
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
                throw new Exception(id.GetType().Name + " data type not supported for " + fieldName);
        }

        internal bool IsKeyIdEmpty(object entity, ColumnAttribute pkColumn)
        {
            return IsKeyFieldEmpty(pkColumn.GetAction(entity), pkColumn.Title);
        }

        internal bool IsKeyIdEmpty(object entity)
        {
            if (Columns.Where(p=>p.Value.IsPrimaryKey).Count()==0)
                return true;

            bool result = false;
            foreach (ColumnAttribute c in Columns.Where(p => p.Value.IsPrimaryKey).Select(a=>a.Value))
            {
                result = IsKeyFieldEmpty(c.GetAction(entity), c.Title);

                if (result)
                    break;
            }
            return result;
        }

        internal bool IsIdentityKeyIdEmpty(object entity)
        {
            if (Columns.Where(p => p.Value.IsPrimaryKey && p.Value.PrimaryKeyInfo.IsIdentity).Count() == 0)
                return true;

            ColumnAttribute pkIdentityCol = Columns.Where(p => p.Value.IsPrimaryKey && p.Value.PrimaryKeyInfo.IsIdentity).Select(p=>p.Value).FirstOrDefault();
            return IsKeyFieldEmpty(pkIdentityCol.GetAction(entity), pkIdentityCol.Title);
        }

        internal bool IsKeyIdentity()
        {
            //TODO: Test Pending
            return Columns.Where(p => p.Value.IsPrimaryKey).LongCount(p => p.Value.PrimaryKeyInfo.IsIdentity) > 0;
        }

        internal bool IsCreatedByEmpty(object entity)
        {
            return NoCreatedBy ? false : IsKeyFieldEmpty(Columns[Config.CREATEDBY_COLUMN.Name].GetAction(entity), "Created By");
        }

        internal bool IsUpdatedByEmpty(object entity)
        {
            return NoUpdatedBy ? false : IsKeyFieldEmpty(Columns[Config.UPDATEDBY_COLUMN.Name].GetAction(entity), "Updated By");
        }

        internal object GetKeyId(object entity)
        {
            return PkColumn.GetAction(entity);
        }

        internal object GetKeyId(object entity, ColumnAttribute pkColumn)
        {
            return pkColumn.GetAction(entity);
        }

        internal void SetKeyId(object entity, object id)
        {
            PkColumn.SetAction(entity, id);
        }

        internal void SetKeyId(object entity, ColumnAttribute pkColumn, object id)
        {
            pkColumn.SetAction(entity, id);
        }

        internal object GetCreatedBy(object entity)
        {
            return Columns[Config.CreatedByColumnName].GetAction(entity);
        }

        internal void SetCreatedBy(object entity, object createdBy)
        {
            Columns[Config.CreatedByColumnName].SetAction(entity, createdBy);
        }

        internal void SetCreatedOn(object entity, DateTime? createdOn)
        {
            Columns[Config.CreatedOnColumnName].SetAction(entity, createdOn);
        }

        internal DateTime? GetCreatedOn(object entity)
        {
            return (DateTime?)Columns[Config.CreatedOnColumnName].GetAction(entity);
        }

        internal object GetUpdatedBy(object entity)
        {
            return Columns[Config.UpdatedByColumnName].GetAction(entity);
        }

        internal void SetUpdatedBy(object entity, object updatedBy)
        {
            Columns[Config.UpdatedByColumnName].SetAction(entity, updatedBy);
        }

        internal DateTime? GetUpdatedOn(object entity)
        {
            return (DateTime?)Columns[Config.UpdatedOnColumnName].GetAction(entity);
        }

        internal void SetUpdatedOn(object entity, DateTime? updatedOn)
        {
            Columns[Config.UpdatedOnColumnName].SetAction(entity, updatedOn);
        }

        internal int? GetVersionNo(object entity)
        {
            return (int?)Columns[Config.VersionNoColumnName].GetAction(entity);
        }

        internal void SetVersionNo(object entity, int? versionNo)
        {
            Columns[Config.VersionNoColumnName].SetAction(entity, versionNo);
        }

        internal bool? GetIsActive(object entity)
        {
            return (bool?)Columns[Config.IsActiveColumnName].GetAction(entity);
        }

        internal void SetIsActive(object entity, bool? isActive)
        {
            Columns[Config.IsActiveColumnName].SetAction(entity, isActive);
        }

        #endregion

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
        /// Size of column for varchar, nvarchar and text, Numeric
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Numeric Scale i.e. Numeric(10,2)
        /// </summary>
        public int NumericScale { get; set; }

        #endregion

        #region Internal Properties

        internal PropertyInfo Property { get; set; }
        internal MethodInfo SetMethod { get; set; }
        internal MethodInfo GetMethod { get; set; }
        internal IgnoreColumnAttribute IgnoreInfo { get; set; }
        internal PrimaryKeyAttribute PrimaryKeyInfo { get; set; }
        internal bool IsPrimaryKey
        {
            get { return PrimaryKeyInfo != null; }
        }
        internal Action<object, object> SetAction { get; set; }
        internal Func<object, object> GetAction { get; set; }

        #endregion

        #region methods

        internal void SetPropertyInfo(PropertyInfo property, Type entityType)
        {
            Property = property;
            SetMethod = property.GetSetMethod();
            GetMethod = property.GetGetMethod();
            SetAction = Helper.CreateSetProperty(entityType, property.Name);
            GetAction = Helper.CreateGetProperty(entityType, property.Name);
        }

        internal string GetDBTypeWithPrecisionAndScale(IDbConnection con)
        {
            return DBCache.Get(con).DbTypeString[columnDbType] + "(" + Size + "," + NumericScale + ")";
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

    #region Foreign Key

    /// <summary>
    /// Define on PrimaryKey Properties. Foreign Keys which are not defined at database level but still wants to check database integrity on HardDelete or SoftDelete.
    /// Note: Attribute defined on NonPrimaryKey properties will be ignored
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    public class ForeignKey : Attribute
    {
        #region constructors

        /// <summary>
        /// Default Constructor
        /// </summary>
        public ForeignKey()
        {

        }

        /// <summary>
        /// Overloaded Constructor that initializes current object with TableName and ColumnName.
        /// </summary>
        /// <param name="tableName">Name of the table containing foreign key.</param>
        /// <param name="columnName">Name of the foreign key column.</param>
        /// <param name="containsIsActive">Is table contains IsActive.</param>
        public ForeignKey(string tableName, string columnName, bool containsIsActive = true)
        {
            TableName = tableName;
            ColumnName = columnName;
            ContainsIsActive = containsIsActive;
        }

        /// <summary>
        /// Overloaded Constructor that initializes current object with SchemaName, TableName and ColumnName.
        /// </summary>
        /// <param name="schemaName">Name of the schema containing foreign key.</param>
        /// <param name="tableName">Name of the table containing foreign key.</param>
        /// <param name="columnName">Name of the foreign key column.</param>
        /// <param name="containsIsActive">Is table contains IsActive.</param>
        public ForeignKey(string schemaName, string tableName, string columnName, bool containsIsActive = true)
            : this(tableName, columnName, containsIsActive)
        {
            SchemaName = schemaName;
        }

        /// <summary>
        /// Overloaded Constructor that intializes current object with DisplayName
        /// </summary>
        /// <param name="schemaName"></param>
        /// <param name="refTableName"></param>
        /// <param name="columnName"></param>
        /// <param name="displayName"></param>
        /// <param name="containsIsActive"></param>
        public ForeignKey(string schemaName, string refTableName, string columnName, string displayName, bool containsIsActive = true)
            : this(schemaName, refTableName, columnName, containsIsActive)
        {
            DisplayName = displayName;
        }


        #endregion

        #region properties

        /// <summary>
        /// Gets / sets name of the schema containing foregn key.
        /// </summary>
        public string SchemaName { get; set; }

        /// <summary>
        /// Gets / sets name of the table containing foregn key.
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Gets / sets name of the foreign key column.
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// Gets / sets name of column which you want to display to user on reference exists.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Table has IsActive Column
        /// </summary>
        public bool ContainsIsActive { get; set; }

        #endregion

        /// <summary>
        /// Fullname of table
        /// </summary>
        internal string FullTableName
        {
            get
            {
                if (string.IsNullOrEmpty(SchemaName))
                    return TableName;
                else
                    return SchemaName + "." + TableName;
            }
        }
    }

    #endregion
}
