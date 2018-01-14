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
    /// <summary>
    /// Paging Parameter Class
    /// </summary>
    public class PagedListParameter
    {
        #region fields

        private string _searchString;

        #endregion

        #region constructors

        public PagedListParameter()
        {
            RecordStatus = RecordStatusEnum.Active;
            this.PageSize = -1; //set -1 to get all pages i.e. all records
        }

        public PagedListParameter(int pageSize) : this()
        {
            this.PageSize = pageSize;
        }

        public PagedListParameter(string searchString, SortParameter sortBy) : this()
        {
            this.SearchString = searchString;
            this.SortByColumn = sortBy;
        }

        public PagedListParameter(string searchString, SortParameter sortBy, int pageSize, ListPagerEnum navigateTo) : this(searchString, sortBy)
        {
            this.PageSize = pageSize;
            this.NavigateTo = navigateTo;
        }

        public PagedListParameter(string searchString, SortParameter sortBy, int pageSize, ListPagerEnum pageAction, string lastValue, object lastId) : this(searchString, sortBy, pageSize, pageAction)
        {
            this.LastValue = lastValue;
            this.LastId = lastId;
        }

        #endregion

        #region properties

        /// <summary>
        /// Page Size
        /// -1 = All Pages
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total Pages in Result
        /// </summary>
        public int TotalPages
        {
            get
            {
                if (PageSize > 0 && RecordCount > 0)
                    return (int)Math.Ceiling((decimal)RecordCount / (decimal)PageSize);
                else
                    return 1;
            }
        }

        /// <summary>
        /// Navigate to Page
        /// </summary>
        public ListPagerEnum NavigateTo { get; set; }

        /// <summary>
        /// Set lastvalue of sorted column. 
        /// Navigating to Next Page - Last sorted column value 
        /// Navigating to Previous Page - First Sorted Column Value. 
        /// First and Last Page - Property is Ignored
        /// </summary>
        public String LastValue { get; set; }

        /// <summary>
        /// Set Last RecordId (Primary Key)
        /// Navigating to Next Page - Last RecordId
        /// Navigating to Previous Page - First RecordId
        /// First and Last Page - Property is Ignored
        /// </summary>
        public object LastId { get; set; }

        /// <summary>
        /// Total Records for the list
        /// </summary>
        public Int64 RecordCount { get; set; }

        /// <summary>
        /// Sort By Columns
        /// </summary>
        public SortParameter SortByColumn { get; set; }

        /// <summary>
        /// Search Value
        /// </summary>
        public String SearchString
        {
            get { return _searchString; }
            set { _searchString = value; }
        }

        /// <summary>
        /// Search On Columns
        /// </summary>
        public Dictionary<string,SearchParameter> SearchOnColumns { get; set; }

        /// <summary>
        /// Filters
        /// </summary>
        public Dictionary<string,FilterParameter> Filters { get; set; }

        /// <summary>
        /// Records with Status to be retrieved. All, Active, InActive
        /// </summary>
        public RecordStatusEnum RecordStatus { get; set; }

        #endregion

        #region methods

        /// <summary>
        /// Add search columns(s)
        /// Pass single column or comma seperated column names
        /// </summary>
        /// <param name="columnNames"></param>
        /// <param name="dbOperator"></param>
        public void AddSearchColumn(string columnNames, DbSearchOperatorEnum dbOperator = DbSearchOperatorEnum.Equals)
        {
            if (SearchOnColumns == null)
            {
                SearchOnColumns = new Dictionary<string, SearchParameter>();
            }

            if (columnNames.Contains(","))
            {
                AddSearchColumn(columnNames.Split(','), dbOperator);
            }
            else
            {
                //add parameter if doesn't exists in the dictionary
                if (!SearchOnColumns.ContainsKey(columnNames))
                {
                    SearchOnColumns[columnNames] = new SearchParameter(columnNames, dbOperator);
                }
            }
        }

        /// <summary>
        /// Add search Columns
        /// </summary>
        /// <param name="columnNames"></param>
        /// <param name="dbOperator"></param>
        public void AddSearchColumn(string[] columnNames, DbSearchOperatorEnum dbOperator = DbSearchOperatorEnum.Equals)
        {
            if (SearchOnColumns == null)
            {
                SearchOnColumns = new Dictionary<string, SearchParameter>();
            }
            foreach (string columnName in columnNames)
            {
                //add parameter if doesn't exists in the dictionary
                if (!SearchOnColumns.ContainsKey(columnName))
                {
                    SearchOnColumns[columnName] = new SearchParameter(columnName, dbOperator);
                }

            }
        }

        /// <summary>
        /// Add filter parameters
        /// </summary>
        /// <param name="columnName">Column Name</param>
        /// <param name="value">Filtered Value</param>
        public void AddFilterParameter(string columnName, object value)
        {
            AddFilterParameter(columnName, DbSearchOperatorEnum.Equals, value);
        }

        /// <summary>
        /// Add filter parameters
        /// </summary>
        /// <param name="columnName">Column Name</param>
        /// <param name="dbOperator">Filter Operator</param>
        /// <param name="value">Filtred value</param>
        public void AddFilterParameter(string columnName, DbSearchOperatorEnum dbOperator, object value)
        {
            if (Filters == null)
            {
                Filters = new Dictionary<string, FilterParameter>();
            }

            //add Parameter if doesn't exists in the list
            if (!Filters.ContainsKey(columnName))
            {
                Filters[columnName] = new FilterParameter(columnName, dbOperator, value);
            }
            else
            {
                Filters[columnName].ParameterValue = value;
            }
        }

        #endregion

        #region static methods

        public static string GetDBOperatorString(DbSearchOperatorEnum filterOperator)
        {
            switch (filterOperator)
            {
                case DbSearchOperatorEnum.Between:
                    return "BETWEEN";
                case DbSearchOperatorEnum.Equals:
                    return "=";
                case DbSearchOperatorEnum.NotEquals:
                    return "!=";
                case DbSearchOperatorEnum.GreaterThen:
                    return ">";
                case DbSearchOperatorEnum.GreaterThenEquals:
                    return ">=";
                case DbSearchOperatorEnum.LessThen:
                    return "<";
                case DbSearchOperatorEnum.LessThenEquals:
                    return "<=";
                case DbSearchOperatorEnum.Like:
                    //TODO: remove pragma
#if PGSQL
                        return "ILIKE";
#else
                    return "LIKE";
#endif
                default:
                    return "";
            }
        }

        #endregion
    }

    public class SortParameter
    {
        #region constructors

        public SortParameter()
        {

        }

        public SortParameter(string columnName, SortDirectionEnum sortOrder)
        {
            this.ColumnName = columnName;
            this.SortOrder = sortOrder;
        }

        #endregion

        #region properties

        public string ColumnName { get; set; }
        public SortDirectionEnum SortOrder { get; set; }

        #endregion
    }

    public class SearchParameter
    {
        #region constructors

        public SearchParameter(string columnName, DbSearchOperatorEnum searchOperator)
        {
            this.ColumnName = columnName;
            this.Operator = searchOperator;
        }

        public SearchParameter()
        {

        }

        #endregion

        #region properties

        public string ColumnName { get; set; }

        public DbSearchOperatorEnum Operator { get; set; }

        #endregion
    }

    public class FilterParameter
    {
        #region constructor

        public FilterParameter()
        {

        }

        public FilterParameter(string columnName, DbSearchOperatorEnum filterOperator, object parameterValue)
        {
            ColumnName = columnName;
            FilterOperator = filterOperator;
            ParameterValue = parameterValue;
        }

        public FilterParameter(string columnName, string filterOperator, object parameterValue)
        {
            ColumnName = columnName;
            FilterOperatorString = filterOperator;
            ParameterValue = parameterValue;
        }

        public FilterParameter(string columnName, DbSearchOperatorEnum filterOperator, object parameterValue, DbType columnType)
        {
            ColumnName = columnName;
            ColumnType = columnType;
            FilterOperator = filterOperator;
            ParameterValue = parameterValue;
        }

        public FilterParameter(string columnName, DbSearchOperatorEnum filterOperator, object parameterValue, DbType columnType, bool isCustomparameter) 
            : this(columnName, filterOperator, parameterValue, columnType)
        {
            IsCustomParameter = isCustomparameter;
        }

        #endregion

        #region properties

        string columnName;
        public string ColumnName
        {
            get
            {
                string[] col = columnName.Split('.');

                if (col.Length > 1)
                    return col[1];
                else
                    return col[0];
            }
            set
            {
                columnName = value;
            }
        }

        public DbType ColumnType { get; set; }

        public DbSearchOperatorEnum FilterOperator { get; set; }

        public string FilterOperatorString { get; set; }

        public object ParameterValue { get; set; }

        public bool IsCustomParameter { get; set; }

        #endregion
    }
}
