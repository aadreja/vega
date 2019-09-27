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

    [Table(IsNoDefaultFields=true, NeedsHistory =true)]
    public class Employee
    {
        [PrimaryKey(true)]
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string Department { get; set; }
        public DateTime? DOB { get; set; }
    }

    [Table(NeedsHistory = true)]
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
        [Column(ColumnDbType = System.Data.DbType.String, Size =4000)]
        public string Name { get; set; }
        [Column(ColumnDbType = System.Data.DbType.String, Size = 50)]
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

    [Table(IsNoDefaultFields = true, NeedsHistory = false)]
    public class Organization
    {
        [PrimaryKey(false)]
        [Column(Size = 50)]
        [ForeignKey("address", "customercode", false)]
        public string CustomerCode { get; set; }

        public string Name { get; set; }
        public string ItcontactPerson { get; set; }
        public string ItcontactPersonEmail { get; set; }
        public string ItcontactPersonMobile { get; set; }
        public int AccountNum { get; set; }

        [IgnoreColumn]
        public Address Address { get; set; }
    }

    [Table(NeedsHistory = false, NoVersionNo =false, NoIsActive =true, NoCreatedBy =true, NoCreatedOn =true, NoUpdatedBy =true, NoUpdatedOn =true)]
    public class Address
    {
        [PrimaryKey(true)]
        public long Id { get; set; }
        [PrimaryKey]
        [Column(Size = 100)]
        public string AddressType { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string Town { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string CustomerCode { get; set; }
        public int VersionNo { get; set; }
    }

    [Table(IsNoDefaultFields = true, NeedsHistory = false)]
    public class Society
    {
        [PrimaryKey(true)]
        [Column]
        public int Id { get; set; }

        [Column(Size = 50)]
        public string Name { get; set; }
    }

    [Table(IsNoDefaultFields = false, NeedsHistory = false)]
    public class Center
    {
        [PrimaryKey(true)]
        [Column]
        public int Id { get; set; }

        [PrimaryKey]
        [Column(Size = 20)]
        public string CenterType { get; set; }

        [Column(Size = 50)]
        public string CenterName { get; set; }

        public bool IsActive { get; set; }
        public int VersionNo { get; set; }
        public int CreatedBy { get; set; }
        public int UpdatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
    }

    public class EntityWithoutTableInfo
    {
        [PrimaryKey(true)]
        public int Id { get; set; }
        public string Attribute1 { get; set; }
        public string Attribute2 { get; set; }
    }

    [Table(IsNoDefaultFields = true, NoIsActive = false, NeedsHistory = true)]
    public class EntityWithIsActive
    {
        [PrimaryKey(true)]
        public int Id { get; set; }
        public string Attribute1 { get; set; }
        public string Attribute2 { get; set; }
        public bool IsActive { get; set; }
    }

}
