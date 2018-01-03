using System;
using System.Data;
using Vega;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VegaTests
{
    [TestClass]
    public class DDLTests
    {
        [TestMethod]
        public void CreateTableNoIdentity()
        {
            EntityCache.Clear(); //clear entity cache
            Repository<Country> countryRepo = new Repository<Country>(Common.GetConnection());
            //Drop table if exists
            countryRepo.DropTable();
            countryRepo.CreateTable();
            Assert.IsTrue(countryRepo.IsTableExists());
        }

        [TestMethod]
        public void CreateTableIdentity()
        {
            EntityCache.Clear(); //clear entity cache
            Repository<City> cityRepo = new Repository<City>(Common.GetConnection());
            cityRepo.DropTable();
            cityRepo.CreateTable();
            Assert.IsTrue(cityRepo.IsTableExists());
        }

        [TestMethod]
        public void CreateTableDifferentName()
        {
            EntityCache.Clear(); //clear entity cache
            Repository<User> userRepo = new Repository<User>(Common.GetConnection());
            userRepo.DropTable();
            userRepo.CreateTable();
            Assert.IsTrue(userRepo.IsTableExists());
        }
    }
}
