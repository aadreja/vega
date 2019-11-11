using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Vega.Tests
{
    [Collection("DMLTest")]
    public class ReadOneTests : IClassFixture<DbConnectionFixuture>
    {
        DbConnectionFixuture Fixture;

        public ReadOneTests(DbConnectionFixuture fixture)
        {
            Fixture = fixture;
        }
    
        [Fact]
        public void ReadOneNullParameter()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            City city = new City()
            {
                Name = "ReadTests.ReadOneNullParameter",
                State = "RO1",
                CountryId = 1,
                Longitude = 1m,
                Latitude = 1m,
                CreatedBy = Fixture.CurrentUserId
            };
            //delete all records
            cityRepo.ExecuteNonQuery("TRUNCATE TABLE city");

            city.Id = (long)cityRepo.Add(city);

            City cityReadOne = cityRepo.ReadOne(null, "name");

            Assert.Equal(city.Name, cityReadOne.Name);
        }

        [Fact]
        public void ReadOneNullParameterWithoutCriteria()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            City city = new City()
            {
                Name = "ReadTests.ReadOneNullParameterWithoutCriteria",
                State = "RONPWC",
                CountryId = 1,
                Longitude = 1m,
                Latitude = 1m,
                CreatedBy = Fixture.CurrentUserId
            };
            city.Id = (long)cityRepo.Add(city);

            City cityReadOne = cityRepo.ReadOne(new { State = "RONPWC" });

            Assert.Equal(city.Name, cityReadOne.Name);
        }

        [Fact]
        public void ReadOneMultipleParameter()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            City city = new City()
            {
                Name = "ReadTests.ReadOneMultipleParameter",
                State = "RO3",
                CountryId = 1,
                Longitude = 1m,
                Latitude = 1m,
                CreatedBy = Fixture.CurrentUserId
            };
            city.Id = (long)cityRepo.Add(city);

            City result = cityRepo.ReadOne("state=@state AND countryid=@countryid", new { State = "RO3", CountryId = 1 }, "name");
        }

        [Fact]
        public void ReadOneMultipleParameterNoCriteria()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            City city = new City()
            {
                Name = "ReadTests.ReadOneMultipleParameter",
                State = "RO3",
                CountryId = 1,
                Longitude = 1m,
                Latitude = 1m,
                CreatedBy = Fixture.CurrentUserId
            };
            city.Id = (long)cityRepo.Add(city);

            City result = cityRepo.ReadOne(new { State = "RO3", CountryId = 1 }, "name");
        }

        [Fact]
        public void ReadOne()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            City city = new City()
            {
                Name = "ReadTests.ReadOne",
                State = "RO",
                CountryId = 1,
                Longitude = 1m,
                Latitude = 1m,
                CreatedBy = Fixture.CurrentUserId
            };
            city.Id = (long)cityRepo.Add(city);

            City cityReadOne = cityRepo.ReadOne(city.Id);

            Assert.Equal(city.Name, cityReadOne.Name);
            Assert.Equal(city.State, cityReadOne.State);
            Assert.Equal(city.CountryId, cityReadOne.CountryId);
            Assert.Equal(city.Longitude, cityReadOne.Longitude);
            Assert.Equal(city.Latitude, cityReadOne.Latitude);

            cityReadOne = cityRepo.ReadOne(new { State = "RO" });
            Assert.Equal("RO", cityReadOne.State);
        }

        [Fact]
        public void ReadOneNoCriteria()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            City city = new City()
            {
                Name = "ReadTests.ReadOne",
                State = "RO",
                CountryId = 1,
                Longitude = 1m,
                Latitude = 1m,
                CreatedBy = Fixture.CurrentUserId
            };
            city.Id = (long)cityRepo.Add(city);

            city = cityRepo.ReadOne(new { State = "RO" });
            Assert.Equal("RO", city.State);
        }

        [Fact]
        public void ReadOneCompositePrimaryKeys()
        {
            Address address = new Address()
            {
                AddressLine1 = "line 1",
                AddressLine2 = "line 2",
                AddressType = "Home",
                Latitude = "1.1",
                Longitude = "1.2",
                Town = "Ahmedabad"
            };

            Repository<Address> addRepo = new Repository<Address>(Fixture.Connection);
            long id = (long)addRepo.Add(address);

            //to read we need address object search as we have composite primary key
            address = new Address()
            {
                Id = id,
                AddressType = "Home"
            };
            Assert.Equal("line 1", addRepo.ReadOne<string>("AddressLine1", address));
        }

        [Fact]
        public void ReadOneQuery()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            City city = new City()
            {
                Name = "ReadTests.ReadOneQuery",
                State = "RQ",
                CountryId = 1,
                Longitude = 1m,
                Latitude = 1m,
                CityType = EnumCityType.Metro,
                CreatedBy = Fixture.CurrentUserId
            };
            //add
            city.Id = (long)cityRepo.Add(city);

            City cityResult;
            if (DbConnectionFixuture.IsAppVeyor)
            {
                //as SQL is use at AppVeyor (CI/CD)
                cityResult = cityRepo.QueryOne("SELECT TOP 1 * FROM city WHERE id=" + city.Id);
            }
            else
            {
#if MSSQL
                cityResult = cityRepo.QueryOne("SELECT TOP 1 * FROM city WHERE id=" + city.Id);
#else
                cityResult = cityRepo.QueryOne("SELECT * FROM city WHERE id=" + city.Id + " LIMIT 1");
#endif
            }
            Assert.Equal("ReadTests.ReadOneQuery", cityResult.Name);
        }

        [Fact]
        public void ReadOneIsActiveAfterDelete()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            City city = new City()
            {
                Name = "ReadTests.ReadWithoutIsActive",
                State = "RQ",
                CountryId = 1,
                Longitude = 1m,
                Latitude = 1m,
                CityType = EnumCityType.Metro,
                CreatedBy = Fixture.CurrentUserId
            };
            //add
            city.Id = (long)cityRepo.Add(city);

            //delete
            cityRepo.Delete(city.Id, city.VersionNo, Fixture.CurrentUserId);

            //get
            City deletedCity = cityRepo.ReadOne(city.Id);

            Assert.False(deletedCity.IsActive);
        }

        [Fact]
        public void ReadWithoutIsActiveAfterDelete()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            City city = new City()
            {
                Name = "ReadTests.ReadWithoutIsActive",
                State = "RQ",
                CountryId = 1,
                Longitude = 1m,
                Latitude = 1m,
                CityType = EnumCityType.Metro,
                CreatedBy = Fixture.CurrentUserId
            };
            //add
            city.Id = (long)cityRepo.Add(city);

            //delete
            cityRepo.Delete(city.Id, city.VersionNo, Fixture.CurrentUserId);

            //get
            City deletedCity = cityRepo.ReadOne(city.Id, "Name, State");

            Assert.Null(deletedCity.IsActive);
        }
    }
}
