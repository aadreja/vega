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
                Id = Fixture.CurrentUserId,
                Username = "admin",
                CreatedBy = Fixture.CurrentUserId
            };

            Repository<User> usrRepo = new Repository<User>(Fixture.Connection);

            if (usrRepo.Exists(usr.Id)) usrRepo.HardDelete(usr.Id, Fixture.CurrentUserId);

            usrRepo.Add(usr);

            //Assert.Equal<Guid>(Fixture.CurrentUserId, usrRepo.ReadOne<Guid>(Fixture.CurrentUserId, "id"));
            Assert.Equal<int>(Fixture.CurrentUserId, usrRepo.ReadOne<int>(Fixture.CurrentUserId, "id"));
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
                Independence = new DateTime(1947, 8, 15),//15th August, 1947
                CreatedBy =Fixture.CurrentUserId
            };

            Repository<Country> countryRepo = new Repository<Country>(Fixture.Connection);
            var id = countryRepo.Add(country);

            var id1 = countryRepo.Update(country);

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
                Continent = EnumContinent.America,
                CreatedBy =Fixture.CurrentUserId
            };

            Repository<Country> countryRepo = new Repository<Country>(Fixture.Connection);
            var id = countryRepo.Add(country);

            Assert.Equal("India", countryRepo.ReadOne<string>(id, "Name"));
            Assert.Equal(EnumContinent.America, countryRepo.ReadOne<EnumContinent>(id, "Continent"));
        }

    }
}
