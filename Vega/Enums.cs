/*
 Description: Vega - Fastest ORM with enterprise features
 Author: Ritesh Sutaria
 Date: 9-Dec-2017
 Home Page: https://github.com/aadreja/vega
            http://www.vegaorm.com
*/
namespace Vega
{
    /// <summary>
    /// Flag for Active,InActive or All records
    /// </summary>
    public enum RecordStatusEnum
    {
        /// <summary>
        /// Flag: All Records
        /// </summary>
        All = -1,
        /// <summary>
        /// Flag Active Records
        /// </summary>
        Active = 1,
        /// <summary>
        /// Flag InActive Records
        /// </summary>
        InActive = 0
    }

    /// <summary>
    /// Flag for Insert, Update, Delete or Recover Operation
    /// </summary>
    public enum RecordOperationEnum
    {
        /// <summary>
        /// Inserted Record
        /// </summary>
        Insert = 0,
        /// <summary>
        /// Updated Record
        /// </summary>
        Update = 1,
        /// <summary>
        /// Deleted Record
        /// </summary>
        Delete = 2,
        /// <summary>
        /// Recovered Record
        /// </summary>
        Recover = 3
    }

    /// <summary>
    /// Flag: database object type
    /// </summary>
    public enum DBObjectTypeEnum
    {
        /// <summary>
        /// Flag: Database
        /// </summary>
        Database,
        /// <summary>
        /// Flag: Schema
        /// </summary>
        Schema,
        /// <summary>
        /// Flag: Table
        /// </summary>
        Table,
        /// <summary>
        /// Flag: View
        /// </summary>
        View,
        /// <summary>
        /// Flag: Function
        /// </summary>
        Function,
        /// <summary>
        /// Flag: Procedure
        /// </summary>
        Procedure
    }

    /// <summary>
    /// Page Navigator
    /// </summary>
    public enum PageNavigationEnum
    {
        /// <summary>
        /// Navigate to First Page
        /// </summary>
        First = 0,
        /// <summary>
        /// Navigate to Last Page
        /// </summary>
        Last = 1,
        /// <summary>
        /// Navigate to Next Page
        /// </summary>
        Next = 2,
        /// <summary>
        /// Navigate to Previous Page
        /// </summary>
        Previous = 3
    }

}
