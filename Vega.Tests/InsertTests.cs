using System;
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
        public void InsertNoIdentity()
        {
            User usr = new User
            {
                Id = 1,
                Username = "admin",
            };

            Repository<User> usrRepo = new Repository<User>(Fixture.Connection, Fixture.CurrentSession);

            if (usrRepo.Exists(usr.Id)) usrRepo.HardDelete(usr.Id);

            usrRepo.Add(usr);

            Assert.Equal(1, usrRepo.ReadOne<short>(1, "id"));
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
                CityType = EnumCityType.Metro
            };

            Repository<City> cityRepo = new Repository<City>(Fixture.Connection, Fixture.CurrentSession);
            var id = cityRepo.Add(city);

            Assert.Equal("Ahmedabad", cityRepo.ReadOne<string>(id, "Name"));
            Assert.Equal(EnumCityType.Metro, cityRepo.ReadOne<EnumCityType>(id, "CityType"));
        }

        [Fact]
        public void InsertWithNullable()
        {
            Country country = new Country
            {
                Name = "India",
                ShortCode = "IN",
                Independence = new DateTime(1947, 8, 15)//15th August, 1947
            };

            Repository<Country> countryRepo = new Repository<Country>(Fixture.Connection, Fixture.CurrentSession);
            var id = countryRepo.Add(country);

            Assert.Equal("India", countryRepo.ReadOne<string>(id, "Name"));
        }

        [Fact]
        public void InsertNonNumericEnum()
        {
            Country country = new Country
            {
                Name = "India",
                ShortCode = "IN",
                Independence = new DateTime(1947, 8, 15),
                Continent = EnumContinent.America
            };

            Repository<Country> countryRepo = new Repository<Country>(Fixture.Connection, Fixture.CurrentSession);
            var id = countryRepo.Add(country);

            Assert.Equal("India", countryRepo.ReadOne<string>(id, "Name"));
            Assert.Equal(EnumContinent.America, countryRepo.ReadOne<EnumContinent>(id, "Continent"));
        }

    }
}
