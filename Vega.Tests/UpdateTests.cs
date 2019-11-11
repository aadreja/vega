using System;
using System.Collections.Generic;
using Vega;
using Xunit;
using System.Linq;

namespace Vega.Tests
{
    [Collection("DMLTest")]
    public class UpdateTests : IClassFixture<DbConnectionFixuture>
    {
        DbConnectionFixuture Fixture;

        public UpdateTests(DbConnectionFixuture fixture)
        {
            Fixture = fixture;
        }

        [Fact]
        public void UpdateFewColumnWithOldEntity()
        {
            City city = new City
            {
                Name = "Ahmedabad",
                State = "GU",
                Latitude = 10.65m,
                Longitude = 11.50m,
                CreatedBy = Fixture.CurrentUserId
            };

            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);
            var id = cityRepo.Add(city);

            city = cityRepo.ReadOne(id);

            City cityNew = (City)city.ShallowCopy();
            cityNew.CountryId = 1;

            cityRepo.Update(cityNew, "countryid", city);

            Assert.Equal(1, cityRepo.ReadOne<long>("CountryId", id));
        }

        [Fact]
        public void UpdateWithOldEntity()
        {
            City city = new City
            {
                Name = "Ahmedabad",
                State = "GU",
                Latitude = 10.65m,
                Longitude = 11.50m,
                CreatedBy = Fixture.CurrentUserId
            };

            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);
            var id = cityRepo.Add(city);

            city = cityRepo.ReadOne(id);

            City cityNew = (City)city.ShallowCopy();
            cityNew.Longitude = 12m;
            cityNew.Latitude = 13m;

            cityRepo.Update(cityNew, city);

            Assert.Equal(12m, cityRepo.ReadOne<decimal>("Longitude", id));
            Assert.Equal(13m, cityRepo.ReadOne<decimal>("Latitude", id));
        }

        [Fact]
        public void UpdateAllColumns()
        {
            City city = new City
            {
                Name = "Ahmedabad",
                State = "GU",
                Latitude = 10.65m,
                Longitude = 11.50m,
                CreatedBy =Fixture.CurrentUserId
            };

            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);
            var id = cityRepo.Add(city);

            city = cityRepo.ReadOne(id);

            city.Longitude = 12m;
            city.Latitude = 13m;

            cityRepo.Update(city);

            Assert.Equal(12m, cityRepo.ReadOne<decimal>("Longitude", id));
            Assert.Equal(13m, cityRepo.ReadOne<decimal>("Latitude", id));
        }

        [Fact]
        public void UpdateFewColumns()
        {
            City city = new City
            {
                Name = "Ahmedabad",
                State = "GU",
                Latitude = 10.65m,
                Longitude = 11.50m,
                CreatedBy =Fixture.CurrentUserId
            };

            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);
            var id = cityRepo.Add(city);

            city = cityRepo.ReadOne(id);

            city.CountryId = 1;

            cityRepo.Update(city, "countryid");

            Assert.Equal(1, cityRepo.ReadOne<int>("countryid", id));
        }

        [Fact]
        public void UpdateNullableColumn()
        {
            Country country = new Country
            {
                Name = "India",
                ShortCode = "IN",
                Independence = new DateTime(1947, 8, 15),
                CreatedBy =Fixture.CurrentUserId
            };

            Repository<Country> countryRepo = new Repository<Country>(Fixture.Connection);
            var id = countryRepo.Add(country);

            country = countryRepo.ReadOne(id);

            country.Independence = null;

            countryRepo.Update(country, "independence");

            Assert.Null(countryRepo.ReadOne<DateTime?>("independence", id));
        }

        [Fact]
        public void UpdateDefaultUpdatedOn()
        {
            Country country = new Country
            {
                Name = "India",
                ShortCode = "IN",
                Independence = new DateTime(1947, 8, 15),
                CreatedBy = Fixture.CurrentUserId
            };

            Repository<Country> countryRepo = new Repository<Country>(Fixture.Connection);
            var id = countryRepo.Add(country);
            country = countryRepo.ReadOne(id);
            country.Independence = null;
            countryRepo.Update(country, "independence");
            Assert.Equal(DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss"), countryRepo.ReadOne<DateTime>("UpdatedOn", id).ToString("dd-MM-yyyy hh:mm:ss"));
        }

        [Fact]
        public void UpdateOverrideUpdatedOn()
        {
            DateTime overrideDate = new DateTime(2019, 04, 18, 1, 1, 1);
            Country country = new Country
            {
                Name = "India",
                ShortCode = "IN",
                Independence = new DateTime(1947, 8, 15),
                CreatedBy = Fixture.CurrentUserId,
            };

            Repository<Country> countryRepo = new Repository<Country>(Fixture.Connection);
            var id = countryRepo.Add(country);

            country = countryRepo.ReadOne(id);

            country.Independence = null;
            country.UpdatedOn = overrideDate;

            countryRepo.Update(country, "independence", overrideCreatedUpdatedOn:true);
            Assert.Equal(overrideDate, countryRepo.ReadOne<DateTime>("UpdatedOn", id));
        }

        [Fact]
        public void UpdateNoEntityBase()
        {
            Employee employee = new Employee
            {
                EmployeeName = "Ramesh",
                Department = "Accounts",
                DOB = new DateTime(1980, 7, 22)
            };

            Repository<Employee> empRepo = new Repository<Employee>(Fixture.Connection);

            var id = empRepo.Add(employee);

            employee = empRepo.ReadOne(id);

            employee.Department = "IT";

            empRepo.Update(employee, "department");
            Assert.Equal("IT", empRepo.ReadOne<string>("department", id));
        }

        [Fact]
        public void UpdateWhenCompositePrimaryKey()
        {
            Address address1 = new Address()
            {
                AddressType = "Home",
                AddressLine1 = "Address 1 line 1",
                AddressLine2 = "Address 1 line 2",
            };

            Address address2 = new Address()
            {
                AddressType = "Home",
                AddressLine1 = "Address 2 line 1",
                AddressLine2 = "Address 2 line 2",
            };

            Repository<Address> addRepo = new Repository<Address>(Fixture.Connection);
            address1.Id = (long)addRepo.Add(address1);
            address2.Id = (long)addRepo.Add(address2);

            //update
            address1.AddressLine1 = "Updated Address 1 line 1";
            address2.AddressLine1 = "Updated Address 2 line 1";

            Assert.True(addRepo.Update(address1));
            Assert.True(addRepo.Update(address2));

            Assert.Equal("Updated Address 1 line 1", addRepo.ReadOne<string>("AddressLine1", address1));
            Assert.Equal("Updated Address 2 line 1", addRepo.ReadOne<string>("AddressLine1", address2));
        }

        [Fact]
        public void UpdateEntityWithoutAttributes()
        {
            EntityWithoutTableInfo ewa = new EntityWithoutTableInfo()
            {
                Attribute1 = "Attribute1",
                Attribute2 = "Attribute2"
            };

            Repository<EntityWithoutTableInfo> ewaRepo = new Repository<EntityWithoutTableInfo>(Fixture.Connection);
            int id = (int)ewaRepo.Add(ewa);

            ewa = ewaRepo.ReadOne(id);

            ewa.Attribute1 = "Updated Attribute1";

            ewaRepo.Update(ewa);
            Assert.Equal("Updated Attribute1", ewaRepo.ReadOne(id).Attribute1);
        }

        [Fact]
        public void UpdateNoVersion()
        {
            EntityWithIsActive soc = new EntityWithIsActive
            {
                Attribute1 = "attribute 1",
                Attribute2 = "attribute 2",
            };

            Repository<EntityWithIsActive> socRepo = new Repository<EntityWithIsActive>(Fixture.Connection);
            var id = socRepo.Add(soc);

            soc = socRepo.ReadOne(id);

            soc.Attribute1 = "Updated Attribute1";

            socRepo.Update(soc);
            Assert.Equal("Updated Attribute1", socRepo.ReadOne(id).Attribute1);
        }

        [Fact]
        public void UpdateWithoutPassingIsActive()
        {
            Repository<Country> countryRepo = new Repository<Country>(Fixture.Connection);

            Country country = new Country()
            {
                Name = "India",
                ShortCode = "IN",
                CreatedBy = Fixture.CurrentUserId
            };
            var id = (long)countryRepo.Add(country);

            country = new Country()
            {
                Id = id,
                Name = "India",
                ShortCode = "IND",
                UpdatedBy = Fixture.CurrentUserId,
                VersionNo = 1
            };

            Assert.True(countryRepo.Update(country));
            
            Assert.True(countryRepo.ReadOne(id).IsActive);

            List<Country> lstAudit = countryRepo.ReadHistory(id).ToList();

            Assert.Equal(Fixture.CurrentUserId, lstAudit[1].CreatedBy);

        }

        [Fact]
        public void UpdateOneColumnWithHistory()
        {
            Repository<Country> countryRepo = new Repository<Country>(Fixture.Connection);

            Country country = new Country()
            {
                Name = "India",
                ShortCode = "IN",
                CreatedBy = Fixture.CurrentUserId
            };
            var id = (long)countryRepo.Add(country);

            Assert.True(countryRepo.Update(id, "ShortCode", "IND", Fixture.CurrentUserId));
            Assert.Equal("IND", countryRepo.ReadOne(id).ShortCode);

            List<Country> lstAudit = countryRepo.ReadHistory(id).ToList();
            Assert.Equal(Fixture.CurrentUserId, lstAudit[1].CreatedBy);
            Assert.Equal("IND", lstAudit[1].ShortCode);
        }

        [Fact]
        public void UpdateOneColumnWithVersionNo()
        {
            Repository<User> userRepo = new Repository<User>(Fixture.Connection);

            User user = new User()
            {
                Username = "admin",
                CreatedBy = Fixture.CurrentUserId
            };
            var id = (int)userRepo.Add(user);

            Assert.True(userRepo.Update(id, "Username", "super", Fixture.CurrentUserId));
            Assert.Equal("super", userRepo.ReadOne(id).Username);
        }

        [Fact]
        public void UpdateOneColumnWithoutHistoryVersionNo()
        {
            Repository<Society> societyRepo = new Repository<Society>(Fixture.Connection);

            Society country = new Society()
            {
                Name = "Society 1"
            };
            var id = (int)societyRepo.Add(country);

            Assert.True(societyRepo.Update(id, "Name", "Updated society 1", Fixture.CurrentUserId));
            Assert.Equal("Updated society 1", societyRepo.ReadOne(id).Name);
        }
    }
}
