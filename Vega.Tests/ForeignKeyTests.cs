using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Vega.Tests
{
    public class ForeignKeyTests : IClassFixture<DbConnectionFixuture>
    {
        DbConnectionFixuture Fixture;

        public ForeignKeyTests(DbConnectionFixuture fixture)
        {
            Fixture = fixture;
        }

        [Fact]
        public void ViolateTest()
        {
            Country country = new Country
            {
                Name = "India",
                Continent = EnumContinent.Asia,
                CreatedBy =Fixture.CurrentUserId
            };

            Repository<Country> countryRepo = new Repository<Country>(Fixture.Connection);
            //add master record
            country.Id = (long)countryRepo.Add(country);

            City city = new City
            {
                Name = "Ahmedabad",
                State = "GU",
                Latitude = 10.65m,
                Longitude = 11.50m,
                CountryId = country.Id,
                CreatedBy=Fixture.CurrentUserId
            };

            //add child record
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);
            city.Id = (long)cityRepo.Add(city);

            //now try to delete country record;
            Exception ex = Assert.Throws<Exception>(() => countryRepo.Delete(country.Id, Fixture.CurrentUserId));

            Assert.Contains("Virtual Foreign Key", ex.Message);
        }

        [Fact]
        public void NoViolationTest()
        {
            Country country = new Country
            {
                Name = "India",
                Continent = EnumContinent.Asia,
                CreatedBy= Fixture.CurrentUserId
            };

            Repository<Country> countryRepo = new Repository<Country>(Fixture.Connection);
            //add master record
            country.Id = (long)countryRepo.Add(country);

            //now try to delete country record
            Assert.True(countryRepo.Delete(country.Id, Fixture.CurrentUserId));
        }

        [Fact]
        public void MultipleForeignKeysTest()
        {
            User usr = new User
            {
                Id = Fixture.CurrentUserId,
                Username = "super",
                CreatedBy = Fixture.CurrentUserId
            };

            Repository<User> userRepo = new Repository<User>(Fixture.Connection);
            //add master record
            //usr.Id = (Guid)userRepo.Add(usr);
            usr.Id = (int)userRepo.Add(usr);


            //perform insert/update operations
            Country country = new Country
            {
                Name = "India",
                Continent = EnumContinent.Asia,
                CreatedBy = Fixture.CurrentUserId
            };

            Repository<Country> countryRepo = new Repository<Country>(Fixture.Connection);
            //add master record
            country.Id = (long)countryRepo.Add(country);

            City city = new City
            {
                Name = "Ahmedabad",
                State = "GU",
                Latitude = 10.65m,
                Longitude = 11.50m,
                CountryId = country.Id,
                CreatedBy = Fixture.CurrentUserId
            };

            //add child record
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);
            city.Id = (long)cityRepo.Add(city);

            Exception ex = Assert.Throws<Exception>(() => userRepo.Delete(usr.Id, Fixture.CurrentUserId));
            Assert.Contains("Virtual Foreign Key", ex.Message);

        }
    }
}
