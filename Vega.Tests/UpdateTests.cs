using System;
using Vega;
using Xunit;

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

            Assert.Equal(12m, cityRepo.ReadOne<decimal>(id, "Longitude"));
            Assert.Equal(13m, cityRepo.ReadOne<decimal>(id, "Latitude"));
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

            Assert.Equal(1, cityRepo.ReadOne<int>(id, "countryid"));
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

            Assert.Null(countryRepo.ReadOne<DateTime?>(id, "independence"));
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
            Assert.Equal(DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss"), countryRepo.ReadOne<DateTime>(id, "UpdatedOn").ToString("dd-MM-yyyy hh:mm:ss"));
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
            Assert.Equal(overrideDate, countryRepo.ReadOne<DateTime>(id, "UpdatedOn"));
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
            Assert.Equal("IT", empRepo.ReadOne<string>(id, "department"));
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

            Assert.Equal("Updated Address 1 line 1", addRepo.ReadOne<string>(address1, "AddressLine1"));
            Assert.Equal("Updated Address 2 line 1", addRepo.ReadOne<string>(address2, "AddressLine1"));
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
    }
}
