using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vega;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VegaTests
{

    [TestClass]
    public class InsertTests
    {
        static InsertTests()
        {
            Common.DropAndCreateTables();
        }

        [TestMethod]
        public void InsertNoIdentity()
        {
            User usr = new User
            {
                Id = 1,
                Username = "admin",
            };

            Repository<User> usrRepo = new Repository<User>(Common.GetConnection());

            usrRepo.Add(usr);

            Assert.AreEqual(1, usrRepo.ReadOne<short>(1, "id"));
        }

        [TestMethod]
        public void InsertIdentity()
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

            Assert.AreEqual("Ahmedabad", cityRepo.ReadOne<string>(id, "Name"));
        }

        [TestMethod]
        public void InsertWithNullable()
        {
            Country country = new Country
            {
                Name = "India",
                ShortCode = "IN",
                Independence = new DateTime(1947, 8, 15)//15th August, 1947
            };

            Repository<Country> countryRepo = new Repository<Country>(Common.GetConnection());
            var id = countryRepo.Add(country);

            Assert.AreEqual("India", countryRepo.ReadOne<string>(id, "Name"));
        }

    }
}
