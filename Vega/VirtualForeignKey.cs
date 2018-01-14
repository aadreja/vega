/*
 Description: Vega - Fastest ORM with enterprise features
 Author: Ritesh Sutaria
 Date: 9-Dec-2017
 Home Page: https://github.com/aadreja/vega
            http://www.vegaorm.com
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Vega
{
    internal class VirtualForeignKey
    {

        #region constructors

        /// <summary>
        /// Default Constructor
        /// </summary>
        public VirtualForeignKey()
        {

        }

        /// <summary>
        /// Overloaded Constructor that initializes current object with TableName and ColumnName.
        /// </summary>
        /// <param name="tableName">Name of the table containing foreign key.</param>
        /// <param name="columnName">Name of the foreign key column.</param>
        /// <param name="containsIsActive">Is table contains IsActive.</param>
        /// <param name="recordOperationType">Operation Add, Update or Delete</param>
        public VirtualForeignKey(string tableName, string columnName, bool containsIsActive = true, RecordOperationEnum recordOperationType = RecordOperationEnum.Delete)
        {
            this.TableName = tableName;
            this.ColumnName = columnName;
            this.ContainsIsActive = containsIsActive;
            this.OperationType = recordOperationType;
        }

        /// <summary>
        /// Overloaded Constructor that initializes current object with SchemaName, TableName and ColumnName.
        /// </summary>
        /// <param name="schemaName">Name of the schema containing foreign key.</param>
        /// <param name="tableName">Name of the table containing foreign key.</param>
        /// <param name="columnName">Name of the foreign key column.</param>
        /// <param name="containsIsActive">Is table contains IsActive.</param>
        /// <param name="recordOperationType">Operation Add, Update or Delete</param>
        public VirtualForeignKey(string schemaName, string tableName, string columnName, bool containsIsActive = true, RecordOperationEnum recordOperationType = RecordOperationEnum.Delete)
            : this(tableName, columnName, containsIsActive, recordOperationType)
        {
            this.SchemaName = schemaName;
        }

        /// <summary>
        /// Overloaded Constructor that intializes current object with DisplayName
        /// </summary>
        /// <param name="schemaName"></param>
        /// <param name="refTableName"></param>
        /// <param name="columnName"></param>
        /// <param name="displayName"></param>
        /// <param name="containsIsActive"></param>
        /// <param name="recordOperationType"></param>
        public VirtualForeignKey(string schemaName, string refTableName, string columnName, string displayName, bool containsIsActive = true, RecordOperationEnum recordOperationType = RecordOperationEnum.Delete)
            : this(schemaName, refTableName, columnName, containsIsActive, recordOperationType)
        {
            this.DisplayName = displayName;
        }

        /// <summary>
        /// Overloaded Constructor that intializes current object with DisplayName and DatabaseName
        /// </summary>
        /// <param name="databaseName"></param>
        /// <param name="schemaName"></param>
        /// <param name="refTableName"></param>
        /// <param name="columnName"></param>
        /// <param name="displayName"></param>
        /// <param name="containsIsActive"></param>
        /// <param name="recordOperationType"></param>
        public VirtualForeignKey(string databaseName, string schemaName, string refTableName, string columnName, string displayName, bool containsIsActive = true, RecordOperationEnum recordOperationType = RecordOperationEnum.Delete)
            : this(schemaName, refTableName, columnName, displayName, containsIsActive, recordOperationType)
        {
            this.DatabaseName = databaseName;
        }


        #endregion

        #region properties

        /// <summary>
        /// Gets / sets name of the schema containing foregn key.
        /// </summary>
        public string SchemaName { get; set; }

        /// <summary>
        /// Gets / sets name of the database containing foreign key.
        /// </summary>
        public string DatabaseName { get; set; }

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
        /// Add, Update or Delete Operation
        /// </summary>
        public RecordOperationEnum OperationType { get; set; }

        /// <summary>
        /// Table has IsActive Column
        /// </summary>
        public bool ContainsIsActive { get; set; }

        #endregion

    }
}
