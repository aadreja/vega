using System;
using System.Collections.Generic;
using System.Linq;
using Vega;
using Xunit;

namespace Vega.Tests
{
    [Collection("DMLTest")]
    public class InsertTests : IClassFixture<DbConnectionFixuture>
    {
        DbConnectionFixuture Fixture;

        public InsertTests(DbConnectionFixuture fixture)
        {
            Fixture = fixture;
        }

        [Fact]
        public void InsertNoVersionNoIsActive()
        {
            Department department = new Department
            {
                DepartmentName = "Accounts",
            };

            Repository<Department> deptRepo = new Repository<Department>(Fixture.Connection);

            //must throw updated by required error and set CreatedBy
            Exception ex = Assert.Throws<MissingFieldException>(() => deptRepo.Add(department));

            Assert.Equal("CreatedBy is required when Audit Trail is enabled", ex.Message);

            //set CreatedBy
            department.CreatedBy = Fixture.CurrentUserId;
            //try to add again
            var id = deptRepo.Add(department);

            Assert.Equal((int)id, deptRepo.ReadOne<int>("DepartmentId", id));
            Assert.Equal("Accounts", deptRepo.ReadOne<string>("DepartmentName", id));
        }

        [Fact]
        public void InsertNoCreatedByOnAndNoUpdatedByOn()
        {
            Job job = new Job
            {
                JobName = "Accountant",
            };

            Repository<Job> jobRepo = new Repository<Job>(Fixture.Connection);

            var id = jobRepo.Add(job);

            Assert.Equal((int)id, jobRepo.ReadOne<int>("JobId", id));
            Assert.Equal("Accountant", jobRepo.ReadOne<string>("JobName", id));
        }

        [Fact]
        public void InsertNoEntityBase()
        {
            Employee employee = new Employee
            {
                EmployeeName = "Ramesh",
                Department = "Accounts",
                DOB = new DateTime(1980, 7, 22)
            };

            Repository<Employee> empRepo = new Repository<Employee>(Fixture.Connection);

            var id = empRepo.Add(employee);

            Assert.Equal((int)id, empRepo.ReadOne<int>("EmployeeId", id));
            Assert.Equal("Ramesh", empRepo.ReadOne<string>("EmployeeName", id));
        }

        [Fact]
        public void InsertNoIdentity()
        {
            User usr = new User
            {
                Id = Fixture.CurrentUserId,
                Username = "admin",
                CreatedBy = Fixture.CurrentUserId
            };

            Repository<User> usrRepo = new Repository<User>(Fixture.Connection);

            if (usrRepo.Exists(usr.Id)) usrRepo.HardDelete(usr.Id, Fixture.CurrentUserId);

            usrRepo.Add(usr);

            Assert.Equal(Fixture.CurrentUserId, usrRepo.ReadOne<int>("id", Fixture.CurrentUserId));
        }

        [Fact]
        public void InsertIdentity()
        {
            City city = new City
            {
                Name = "Ahmedabad",
                State = "GU",
                Latitude = 10.65m,
                Longitude = 11.50m,
                CityType = EnumCityType.Metro,
                CreatedBy =Fixture.CurrentUserId
            };

            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);
            var id = cityRepo.Add(city);

            Assert.Equal("Ahmedabad", cityRepo.ReadOne<string>("Name", id));
            Assert.Equal(EnumCityType.Metro, cityRepo.ReadOne<EnumCityType>("CityType", id));
        }

        [Fact]
        public void InsertWithNullable()
        {
            Country country = new Country
            {
                Name = "India",
                ShortCode = "IN",
                Independence = new DateTime(1947, 8, 15),//15th August, 1947
                CreatedBy =Fixture.CurrentUserId
            };

            Repository<Country> countryRepo = new Repository<Country>(Fixture.Connection);
            var id = countryRepo.Add(country);

            country.UpdatedBy = Fixture.CurrentUserId;
            var id1 = countryRepo.Update(country);

            Assert.Equal("India", countryRepo.ReadOne<string>("Name", id));
        }

        [Fact]
        public void InsertNonNumericEnum()
        {
            Country country = new Country
            {
                Name = "India",
                ShortCode = "IN",
                Independence = new DateTime(1947, 8, 15),
                Continent = EnumContinent.America,
                CreatedBy =Fixture.CurrentUserId
            };

            Repository<Country> countryRepo = new Repository<Country>(Fixture.Connection);
            var id = countryRepo.Add(country);

            Assert.Equal("India", countryRepo.ReadOne<string>("Name", id));
            Assert.Equal(EnumContinent.America, countryRepo.ReadOne<EnumContinent>("Continent", id));
        }

        [Fact]
        public void InsertWithFalseInActive()
        {
            Country country = new Country
            {
                Name = "India",
                ShortCode = "IN",
                Independence = new DateTime(1947, 8, 15),//15th August, 1947
                CreatedBy = Fixture.CurrentUserId,
                IsActive=false
            };

            Repository<Country> countryRepo = new Repository<Country>(Fixture.Connection);
            var id = countryRepo.Add(country);

            Assert.False(countryRepo.ReadOne<bool>("IsActive", id));
        }

        [Fact]
        public void InsertWithDefaultInActive()
        {
            Country country = new Country
            {
                Name = "India",
                ShortCode = "IN",
                Independence = new DateTime(1947, 8, 15),//15th August, 1947
                CreatedBy = Fixture.CurrentUserId,
            };

            Repository<Country> countryRepo = new Repository<Country>(Fixture.Connection);
            var id = countryRepo.Add(country);

            Assert.True(countryRepo.ReadOne<bool>("IsActive", id));
        }

        [Fact]
        public void InsertWithTrueInActive()
        {
            Country country = new Country
            {
                Name = "India",
                ShortCode = "IN",
                Independence = new DateTime(1947, 8, 15),//15th August, 1947
                CreatedBy = Fixture.CurrentUserId,
                IsActive=true
            };

            Repository<Country> countryRepo = new Repository<Country>(Fixture.Connection);
            var id = countryRepo.Add(country);

            Assert.True(countryRepo.ReadOne<bool>("IsActive", id));
        }

        [Fact]
        public void InsertWithOverrideVersionNo()
        {
            Country country = new Country
            {
                Name = "India",
                ShortCode = "IN",
                Independence = new DateTime(1947, 8, 15),//15th August, 1947
                CreatedBy = Fixture.CurrentUserId,
                VersionNo = 5
            };

            Repository<Country> countryRepo = new Repository<Country>(Fixture.Connection);
            var id = countryRepo.Add(country);

            Assert.Equal(5,countryRepo.ReadOne<int>("VersionNo", id));
        }

        [Fact]
        public void InsertWithDefaultVersionNo()
        {
            Country country = new Country
            {
                Name = "India",
                ShortCode = "IN",
                Independence = new DateTime(1947, 8, 15),//15th August, 1947
                CreatedBy = Fixture.CurrentUserId,
            };

            Repository<Country> countryRepo = new Repository<Country>(Fixture.Connection);
            var id = countryRepo.Add(country);

            Assert.Equal(1, countryRepo.ReadOne<int>("VersionNo", id));
        }

        [Fact]
        public void InsertWithDefaultCreatedUpdatedOn()
        {
            Country country = new Country
            {
                Name = "India",
                ShortCode = "IN",
                Independence = new DateTime(1947, 8, 15),//15th August, 1947
                CreatedBy = Fixture.CurrentUserId,
            };

            Repository<Country> countryRepo = new Repository<Country>(Fixture.Connection);
            var id = countryRepo.Add(country);

            Assert.Equal(DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss"), countryRepo.ReadOne<DateTime>("CreatedOn", id).ToString("dd-MM-yyyy hh:mm:ss"));
            Assert.Equal(DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss"), countryRepo.ReadOne<DateTime>("UpdatedOn", id).ToString("dd-MM-yyyy hh:mm:ss"));
        }

        [Fact]
        public void InsertWithOverrideCreatedUpdatedOn()
        {
            DateTime overrideDate = new DateTime(2019, 04, 18, 1, 1, 1);
            Country country = new Country
            {
                Name = "India",
                ShortCode = "IN",
                Independence = new DateTime(1947, 8, 15),//15th August, 1947
                CreatedBy = Fixture.CurrentUserId,
                CreatedOn = overrideDate,
                UpdatedOn = overrideDate
            };

            Repository<Country> countryRepo = new Repository<Country>(Fixture.Connection);
            var id = countryRepo.Add(country, overrideCreatedUpdatedOn:true);

            Assert.Equal(overrideDate, countryRepo.ReadOne<DateTime>("CreatedOn", id));
            Assert.Equal(overrideDate, countryRepo.ReadOne<DateTime>("UpdatedOn", id));
        }

        [Theory]
        [InlineData("India", "IN")]
        [InlineData("China", "CN")]
        public void MultipleInserts(string countryName, string shortCode)
        {
            Country country = new Country
            {
                Name = countryName,
                ShortCode = shortCode,
                Independence = new DateTime(1947, 8, 15),//15th August, 1947
                CreatedBy = Fixture.CurrentUserId,
            };

            Repository<Country> countryRepo = new Repository<Country>(Fixture.Connection);
            var id = countryRepo.Add(country);

            Assert.Equal(countryName, countryRepo.ReadOne<string>("Name", id));
        }


        [Fact]
        public void InsertWhenColumnAttributeIsWithoutPassingName()
        {
            Society soc = new Society()
            {
                Name = "Bajipura"
            };

            Repository<Society> socRepo = new Repository<Society>(Fixture.Connection);
            int id = (int)socRepo.Add(soc);

            Assert.Equal("Bajipura", socRepo.ReadOne<string>("Name", id));
        }

        [Fact]
        public void InsertWhenPrimaryKeyIsString()
        {
            //TODO: Pending - ReadOne with pk string is causing ambigious method call
            Organization org = new Organization()
            {
                CustomerCode = "005",
                Name = "Bajipura",
                AccountNum=123
            };

            Repository<Organization> orgRepo = new Repository<Organization>(Fixture.Connection);
            string id = (string)orgRepo.Add(org);

            Assert.Equal("Bajipura", orgRepo.ReadOne<string>("Name", id));
        }

        [Fact]
        public void InsertWhenCompositePrimaryKey()
        {
            Address address = new Address()
            {
                AddressType = "Home",
                AddressLine1 = "line 1",
                AddressLine2 = "line 2",
            };

            Repository<Address> addRepo = new Repository<Address>(Fixture.Connection);
            long id = (long)addRepo.Add(address);

            Exception ex = Assert.Throws<InvalidOperationException>(() => addRepo.ReadOne<string>("AddressLine1", id));

            //for search
            address = new Address()
            {
                Id = id,
                AddressType = "Home"
            };

            Assert.Equal("line 1", addRepo.ReadOne<string>("AddressLine1", address));
        }

        [Fact]
        public void InsertEntityWithoutAttributes()
        {
            EntityWithoutTableInfo ewa = new EntityWithoutTableInfo()
            {
                Attribute1 = "Attribute1",
                Attribute2 = "Attribute2"
            };

            Repository<EntityWithoutTableInfo> ewaRepo = new Repository<EntityWithoutTableInfo>(Fixture.Connection);
            int id = (int)ewaRepo.Add(ewa);

            Assert.Equal("Attribute1", ewaRepo.ReadOne<string>("Attribute1", id));
        }

        //Chetan found bugs in AuditTrail
        [Fact]
        public void InsertUpdateDeleteUpdateDelete()
        {

            //cleanup audit table
            
            Fixture.CleanupAuditTable();

            Country cnt = new Country()
            {
                Name = "India",
                ShortCode = "IN",
                Independence = new DateTime(1947, 8, 15),
                Continent= EnumContinent.Asia,
                CreatedBy = Fixture.CurrentUserId
            };

            Repository<Country> contryRepo = new Repository<Country>(Fixture.Connection);
            var id = contryRepo.Add(cnt);

            cnt = contryRepo.ReadOne(id);
            Assert.Equal(EnumContinent.Asia, cnt.Continent);

            cnt.UpdatedBy = 2;
            //should not update as no changes were made
            Assert.True(contryRepo.Update(cnt));

            //try to delete without UpdatedBy column
            Exception ex = Assert.Throws<MissingFieldException>(() => contryRepo.Delete(id));

            Assert.True(contryRepo.Delete(id,2));
            cnt.VersionNo++;
            Assert.True(contryRepo.Update(cnt)); //this will not be updated as isactive will not be updated
            Assert.True(contryRepo.Delete(id, 2));

            AuditTrailRepository<Country> adtRepo = new AuditTrailRepository<Country>(Fixture.Connection);
            Assert.Equal(3, adtRepo.Count());

            List<AuditTrail> lstAudit = adtRepo.ReadAll().Cast<AuditTrail>().ToList();

            Assert.Equal(1, lstAudit[0].RecordVersionNo);
            Assert.Equal(1, lstAudit[0].CreatedBy);

            Assert.Equal(2, lstAudit[1].RecordVersionNo);
            Assert.Equal(2, lstAudit[1].CreatedBy);

            Assert.Equal(3, lstAudit[2].RecordVersionNo);
            Assert.Equal(2, lstAudit[2].CreatedBy);
        }
    }

    
}
