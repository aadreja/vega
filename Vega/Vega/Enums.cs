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
    /// Pass to method to get Active,InActive or All records
    /// </summary>
    public enum RecordStatusEnum
    {
        All = -1,
        Active = 1,
        InActive = 0,
    }

    /// <summary>
    /// Sort Direction
    /// </summary>
    public enum SortDirectionEnum
    {
        Ascending = 0,
        Decending = 1
    }

    /// <summary>
    /// Page Navigator
    /// </summary>
    public enum ListPagerEnum
    {
        FirstPage = 0,
        LastPage = 1,
        NextPage = 2,
        PrevPage = 3
    }

    /// <summary>
    /// Search Operator enum
    /// </summary>
    public enum DbSearchOperatorEnum
    {
        Equals = 0,
        Like = 1,
        GreaterThen = 2,
        GreaterThenEquals = 3,
        LessThen = 4,
        LessThenEquals = 5,
        Between = 6,
        NotEquals = 7
    }

    /// <summary>
    /// CRUD Operation
    /// </summary>
    public enum RecordOperationEnum
    {
        Add = 0,
        Update = 1,
        Delete = 2,
        Recover = 3
    }

    /// <summary>
    /// Database Object Type
    /// </summary>
    public enum DBObjectTypeEnum
    {
        Database,
        Schema,
        Table,
        View,
        Function,
        Procedure
    }

}
