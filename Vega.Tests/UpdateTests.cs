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
    }
}
