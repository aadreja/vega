using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Vega;

namespace VegaTests
{

    [TestClass]
    public class UpdateTests
    {
        static UpdateTests()
        {
            Common.DropAndCreateTables();
        }

        [TestMethod]
        public void UpdateAllColumns()
        {
            City city = new City
            {
                Name = "Ahmedabad",
                State = "GU",
                Latitude = 10.65m,
                Longitude = 11.50m,
            };

            Repository<City> cityRepo = new Repository<City>(Common.GetConnection());
            var id = cityRepo.Add(city);

            city = cityRepo.ReadOne(id);

            city.Longitude = 12m;
            city.Latitude = 13m;

            cityRepo.Update(city);

            Assert.AreEqual(12m, cityRepo.ReadOne<decimal>(id, "Longitude"));
            Assert.AreEqual(13m, cityRepo.ReadOne<decimal>(id, "Latitude"));
        }

        [TestMethod]
        public void UpdateFewColumns()
        {
            City city = new City
            {
                Name = "Ahmedabad",
                State = "GU",
                Latitude = 10.65m,
                Longitude = 11.50m,
            };

            Repository<City> cityRepo = new Repository<City>(Common.GetConnection());
            var id = cityRepo.Add(city);

            city = cityRepo.ReadOne(id);

            city.CountryId = 1;

            cityRepo.Update(city, "countryid");

            Assert.AreEqual(1, cityRepo.ReadOne<int>(id, "countryid"));
        }

        [TestMethod]
        public void UpdateNullableColumn()
        {
            Country country = new Country
            {
                Name = "India",
                ShortCode = "IN",
                Independence = new DateTime(1947, 8, 15)
            };

            Repository<Country> countryRepo = new Repository<Country>(Common.GetConnection());
            var id = countryRepo.Add(country);

            country = countryRepo.ReadOne(id);

            country.Independence = null;

            countryRepo.Update(country, "independence");

            Assert.AreEqual(null, countryRepo.ReadOne<DateTime?>(id, "independence"));
        }

    }
}
