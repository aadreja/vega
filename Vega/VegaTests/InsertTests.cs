using System;
using Vega;
using Xunit;

namespace VegaTests
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

            Repository<User> usrRepo = new Repository<User>(Fixture.Connection);

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
            };

            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);
            var id = cityRepo.Add(city);

            Assert.Equal("Ahmedabad", cityRepo.ReadOne<string>(id, "Name"));
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

            Repository<Country> countryRepo = new Repository<Country>(Fixture.Connection);
            var id = countryRepo.Add(country);

            Assert.Equal("India", countryRepo.ReadOne<string>(id, "Name"));
        }

        

    }
}
