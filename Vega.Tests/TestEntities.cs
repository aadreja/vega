using System;

namespace Vega.Tests
{
    public enum EnumContinent
    {
        Asia,
        America,
        Africa
    }

    public enum EnumCityType
    {
        Metro = 1,
        NonMetro = 2,
        Town = 3
    }

    [Table(NoCreatedBy = true, NoCreatedOn = true, NoUpdatedBy =true, NoUpdatedOn =true, NeedsHistory = true)]
    public class Job : EntityBase
    {
        [PrimaryKey(true)]
        public int JobId { get; set; }
        public string JobName { get; set; }
    }

    [Table(NoIsActive =true, NoVersionNo =true, NeedsHistory =true)]
    public class Department : EntityBase
    {
        [PrimaryKey(true)]
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; }
    }

    [Table(IsNoDefaultFields=true)]
    public class Employee
    {
        [PrimaryKey(true)]
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string Department { get; set; }
        public DateTime? DOB { get; set; }
    }

    [Table(NeedsHistory = false)]
    public class Country : EntityBase
    {
        [PrimaryKey(true)]
        [ForeignKey("city","countryid",true)]
        public long Id { get; set; }
        public string Name { get; set; }
        public string ShortCode { get; set; }
        public DateTime? Independence { get; set; }
        [Column(ColumnDbType = System.Data.DbType.String)]
        public EnumContinent Continent { get; set; }
    }

    [Table(NeedsHistory = true)]
    public class City : EntityBase
    {
        [PrimaryKey(true)]
        public long Id { get; set; }
        [Column(ColumnDbType = System.Data.DbType.String, NumericPrecision =4000)]
        public string Name { get; set; }
        [Column(ColumnDbType = System.Data.DbType.String, NumericPrecision = 50)]
        public string State { get; set; }
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }
        public long CountryId { get; set; }
        [IgnoreColumn(true)]
        public string CountryName { get; set; }
        public EnumCityType CityType { get; set; }
    }

    [Table(Name = "Users", NoIsActive = true)]
    public class User : EntityBase
    {
        [PrimaryKey]
        [ForeignKey("city", "createdby", true)]
        [ForeignKey("city", "updatedby", true)]
        [ForeignKey("country", "createdby", true)]
        [ForeignKey("country", "updatedby", true)]
        public int Id { get; set; }
        public string Username { get; set; }
    }

}
