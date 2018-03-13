using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Vega.Tests
{
    [Collection("DMLTest")]
    public class ReadPagedTests : IClassFixture<DbConnectionFixuture>
    {
        DbConnectionFixuture Fixture;

        public ReadPagedTests(DbConnectionFixuture fixture)
        {
            Fixture = fixture;
        }

        [Fact]
        public void NoQueryNoOffsetMultipleOrderByWithCriteriaTest()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            cityRepo.DropTable();
            cityRepo.CreateTable();

            int counter = 1000;

            for (int i = 1; i <= counter; i++)
            {
                City city = new City()
                {
                    Name = "PagedNoCriteriaTest." + i.ToString().PadLeft(5, '0'),
                    State = i % 5 == 0 ? "RC" : "DC",
                    CountryId = i % 2 == 0 ? 1 : 2,
                    Longitude = 1m,
                    Latitude = 1m,
                    CreatedBy =Fixture.CurrentUserId
                };
                city.Id = (long)cityRepo.Add(city);
            }

            var parameters = new { state = "DC" };

            //order by country ASC, name ASC
            //First Page
            List<City> cityList = cityRepo.ReadAllPaged("countryid,name", 50, PageNavigationEnum.First, null, "state=@state", null, null, parameters).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal(1, cityList[0].CountryId);
            Assert.Equal("PagedNoCriteriaTest.00002", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00124", cityList[49].Name);

            //Next Page
            cityList = cityRepo.ReadAllPaged("countryid,name", 50, PageNavigationEnum.Next, null, "state=@state", new object[] { cityList[49].CountryId, cityList[49].Name }, cityList[49].Id, parameters).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal(1, cityList[0].CountryId);
            Assert.Equal("PagedNoCriteriaTest.00126", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00248", cityList[49].Name);

            //Last Page
            cityList = cityRepo.ReadAllPaged("countryid,name", 50, PageNavigationEnum.Last, null, "state=@state", null, null, parameters).ToList();

            Assert.Equal(50, cityList.Count);
            Assert.Equal(2, cityList[0].CountryId);
            Assert.Equal("PagedNoCriteriaTest.00877", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00999", cityList[49].Name);

            //Previous Page
            cityList = cityRepo.ReadAllPaged("countryid,name", 50, PageNavigationEnum.Previous, null, "state=@state", new object[] { cityList[0].CountryId, cityList[0].Name }, cityList[0].Id, parameters).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal(2, cityList[0].CountryId);
            Assert.Equal("PagedNoCriteriaTest.00751", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00873", cityList[49].Name);

            //order by countryid desc, name asc
            //First Page
            cityList = cityRepo.ReadAllPaged("countryid desc, name", 50, PageNavigationEnum.First, null, "state=@state", null, null, parameters).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal(2, cityList[0].CountryId);
            Assert.Equal("PagedNoCriteriaTest.00001", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00123", cityList[49].Name);

            //Next Page
            cityList = cityRepo.ReadAllPaged("countryid desc,name", 50, PageNavigationEnum.Next, null, "state=@state", new object[] { cityList[49].CountryId, cityList[49].Name }, cityList[49].Id, parameters).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal(2, cityList[0].CountryId);
            Assert.Equal("PagedNoCriteriaTest.00127", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00249", cityList[49].Name);

            //Last Page
            cityList = cityRepo.ReadAllPaged("countryid desc,name", 50, PageNavigationEnum.Last, null, "state=@state", null, null, parameters).ToList();

            Assert.Equal(50, cityList.Count);
            Assert.Equal(1, cityList[0].CountryId);
            Assert.Equal("PagedNoCriteriaTest.00876", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00998", cityList[49].Name);

            //Previous Page
            cityList = cityRepo.ReadAllPaged("countryid desc,name", 50, PageNavigationEnum.Previous, null, "state=@state", new object[] { cityList[0].CountryId, cityList[0].Name }, cityList[0].Id, parameters).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal(1, cityList[0].CountryId);
            Assert.Equal("PagedNoCriteriaTest.00752", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00874", cityList[49].Name);
        }

        [Fact]
        public void NoQueryNoOffsetWithCriteriaTest()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            cityRepo.DropTable();
            cityRepo.CreateTable();

            int counter = 1000;

            for (int i = 1; i <= counter; i++)
            {
                City city = new City()
                {
                    Name = "PagedNoCriteriaTest." + i.ToString().PadLeft(5, '0'),
                    State = i % 2 == 0 ? "RC" : "DC",
                    CountryId = 1,
                    Longitude = 1m,
                    Latitude = 1m,
                    CreatedBy =Fixture.CurrentUserId
                };
                city.Id = (long)cityRepo.Add(city);
            }

            var parameters = new { state = "RC" };

            //order by ASC
            //First Page
            List<City> cityList = cityRepo.ReadAllPaged("name", 50, PageNavigationEnum.First, null, "state=@state", null, null, parameters).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.00002", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00100", cityList[49].Name);

            //Next Page
            cityList = cityRepo.ReadAllPaged("name", 50, PageNavigationEnum.Next, null, "state=@state", new[] { cityList[49].Name }, cityList[49].Id, parameters).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.00102", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00200", cityList[49].Name);

            //Last Page
            cityList = cityRepo.ReadAllPaged("name", 50, PageNavigationEnum.Last, null, "state=@state", null, null, parameters).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.00902", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.01000", cityList[49].Name);

            //Previous Page
            cityList = cityRepo.ReadAllPaged("name", 50, PageNavigationEnum.Previous, null, "state=@state", new[] { cityList[0].Name }, cityList[0].Id, parameters).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.00802", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00900", cityList[49].Name);

            //order by desc
            //First Page
            cityList = cityRepo.ReadAllPaged("name desc", 50, PageNavigationEnum.First, null, "state=@state", null, null, parameters).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.01000", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00902", cityList[49].Name);

            //Next Page
            cityList = cityRepo.ReadAllPaged("name desc", 50, PageNavigationEnum.Next, null, "state=@state", new[] { cityList[49].Name }, cityList[49].Id, parameters).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.00900", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00802", cityList[49].Name);

            //Last Page
            cityList = cityRepo.ReadAllPaged("name desc", 50, PageNavigationEnum.Last, null, "state=@state", null, null, parameters).ToList();

            Assert.Equal(50, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.00100", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00002", cityList[49].Name);

            //Previous Page
            cityList = cityRepo.ReadAllPaged("name desc", 50, PageNavigationEnum.Previous, null, "state=@state", new[] { cityList[0].Name }, cityList[0].Id, parameters).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.00200", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00102", cityList[49].Name);
        }

        [Fact]
        public void NoQueryNoOffsetMultipleOrderByNoCriteriaTest()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            cityRepo.DropTable();
            cityRepo.CreateTable();

            int counter = 1000;

            for (int i = 1; i <= counter; i++)
            {
                City city = new City()
                {
                    Name = "PagedNoCriteriaTest." + i.ToString().PadLeft(5, '0'),
                    State = i % 2 == 0 ? "RC" : "DC",
                    CountryId = i % 2 == 0 ? 1 : 2,
                    Longitude = 1m,
                    Latitude = 1m,
                    CreatedBy =Fixture.CurrentUserId
                };
                city.Id = (long)cityRepo.Add(city);
            }

            //order by country ASC, name ASC
            //First Page
            List<City> cityList = cityRepo.ReadAllPaged("countryid,name", 50, PageNavigationEnum.First).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal(1, cityList[0].CountryId);
            Assert.Equal("PagedNoCriteriaTest.00002", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00100", cityList[49].Name);

            //Next Page
            cityList = cityRepo.ReadAllPaged("countryid,name", 50, PageNavigationEnum.Next, null, null, new object[] { cityList[49].CountryId, cityList[49].Name }, cityList[49].Id).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal(1, cityList[0].CountryId);
            Assert.Equal("PagedNoCriteriaTest.00102", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00200", cityList[49].Name);

            //Last Page
            cityList = cityRepo.ReadAllPaged("countryid,name", 50, PageNavigationEnum.Last).ToList();

            Assert.Equal(50, cityList.Count);
            Assert.Equal(2, cityList[0].CountryId);
            Assert.Equal("PagedNoCriteriaTest.00901", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00999", cityList[49].Name);

            //Previous Page
            cityList = cityRepo.ReadAllPaged("countryid,name", 50, PageNavigationEnum.Previous, null, null, new object[] { cityList[0].CountryId, cityList[0].Name }, cityList[0].Id).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal(2, cityList[0].CountryId);
            Assert.Equal("PagedNoCriteriaTest.00801", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00899", cityList[49].Name);

            //order by countryid desc, name asc
            //First Page
            cityList = cityRepo.ReadAllPaged("countryid desc, name", 50, PageNavigationEnum.First).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal(2, cityList[0].CountryId);
            Assert.Equal("PagedNoCriteriaTest.00001", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00099", cityList[49].Name);

            //Next Page
            cityList = cityRepo.ReadAllPaged("countryid desc,name", 50, PageNavigationEnum.Next, null, null, new object[] { cityList[49].CountryId, cityList[49].Name }, cityList[49].Id).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal(2, cityList[0].CountryId);
            Assert.Equal("PagedNoCriteriaTest.00101", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00199", cityList[49].Name);

            //Last Page
            cityList = cityRepo.ReadAllPaged("countryid desc,name", 50, PageNavigationEnum.Last).ToList();

            Assert.Equal(50, cityList.Count);
            Assert.Equal(1, cityList[0].CountryId);
            Assert.Equal("PagedNoCriteriaTest.00902", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.01000", cityList[49].Name);

            //Previous Page
            cityList = cityRepo.ReadAllPaged("countryid desc,name", 50, PageNavigationEnum.Previous, null, null, new object[] { cityList[0].CountryId, cityList[0].Name }, cityList[0].Id).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal(1, cityList[0].CountryId);
            Assert.Equal("PagedNoCriteriaTest.00802", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00900", cityList[49].Name);
        }

        [Fact]
        public void NoQueryNoOffsetNoCriteriaTest()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            cityRepo.DropTable();
            cityRepo.CreateTable();

            int counter = 1000;

            for (int i = 1; i <= counter; i++)
            {
                City city = new City()
                {
                    Name = "PagedNoCriteriaTest." + i.ToString().PadLeft(5, '0'),
                    State = "RC",
                    CountryId = 1,
                    Longitude = 1m,
                    Latitude = 1m,
                    CreatedBy =Fixture.CurrentUserId
                };
                city.Id = (long)cityRepo.Add(city);
            }

            //order by ASC
            //First Page
            List<City> cityList = cityRepo.ReadAllPaged("name", 50, PageNavigationEnum.First).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.00001", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00050", cityList[49].Name);

            //Next Page
            cityList = cityRepo.ReadAllPaged("name", 50, PageNavigationEnum.Next, null, null, new[] { cityList[49].Name }, cityList[49].Id).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.00051", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00100", cityList[49].Name);

            //Last Page
            cityList = cityRepo.ReadAllPaged("name", 50, PageNavigationEnum.Last).ToList();

            Assert.Equal(50, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.00951", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.01000", cityList[49].Name);

            //Previous Page
            cityList = cityRepo.ReadAllPaged("name", 50, PageNavigationEnum.Previous, null, null, new[] { cityList[0].Name }, cityList[0].Id).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.00901", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00950", cityList[49].Name);

            //order by desc
            //First Page
            cityList = cityRepo.ReadAllPaged("name desc", 50, PageNavigationEnum.First).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.01000", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00951", cityList[49].Name);

            //Next Page
            cityList = cityRepo.ReadAllPaged("name desc", 50, PageNavigationEnum.Next, null, null, new[] { cityList[49].Name }, cityList[49].Id).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.00950", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00901", cityList[49].Name);

            //Last Page
            cityList = cityRepo.ReadAllPaged("name desc", 50, PageNavigationEnum.Last).ToList();

            Assert.Equal(50, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.00050", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00001", cityList[49].Name);

            //Previous Page
            cityList = cityRepo.ReadAllPaged("name desc", 50, PageNavigationEnum.Previous, null, null, new[] { cityList[0].Name }, cityList[0].Id).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.00100", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00051", cityList[49].Name);
        }

        [Fact]
        public void WithQueryNoOffsetMultipleOrderByWithCriteriaTest()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            cityRepo.DropTable();
            cityRepo.CreateTable();

            int counter = 1000;

            for (int i = 1; i <= counter; i++)
            {
                City city = new City()
                {
                    Name = "PagedNoCriteriaTest." + i.ToString().PadLeft(5, '0'),
                    State = i % 5 == 0 ? "RC" : "DC",
                    CountryId = i % 2 == 0 ? 1 : 2,
                    Longitude = 1m,
                    Latitude = 1m,
                    CreatedBy =Fixture.CurrentUserId
                };
                city.Id = (long)cityRepo.Add(city);
            }

            var parameters = new { state = "DC" };

            //order by country ASC, name ASC
            //First Page
            List<City> cityList = cityRepo.ReadAllPaged("SELECT * FROM city WHERE state=@state", "countryid,name", 50, PageNavigationEnum.First,null, null, parameters).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal(1, cityList[0].CountryId);
            Assert.Equal("PagedNoCriteriaTest.00002", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00124", cityList[49].Name);

            //Next Page
            cityList = cityRepo.ReadAllPaged("SELECT * FROM city WHERE state=@state", "countryid,name", 50, PageNavigationEnum.Next, new object[] { cityList[49].CountryId, cityList[49].Name }, cityList[49].Id, parameters).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal(1, cityList[0].CountryId);
            Assert.Equal("PagedNoCriteriaTest.00126", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00248", cityList[49].Name);

            //Last Page
            cityList = cityRepo.ReadAllPaged("SELECT * FROM city WHERE state=@state", "countryid,name", 50, PageNavigationEnum.Last, null, null, parameters).ToList();

            Assert.Equal(50, cityList.Count);
            Assert.Equal(2, cityList[0].CountryId);
            Assert.Equal("PagedNoCriteriaTest.00877", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00999", cityList[49].Name);

            //Previous Page
            cityList = cityRepo.ReadAllPaged("SELECT * FROM city WHERE state=@state", "countryid,name", 50, PageNavigationEnum.Previous, new object[] { cityList[0].CountryId, cityList[0].Name }, cityList[0].Id, parameters).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal(2, cityList[0].CountryId);
            Assert.Equal("PagedNoCriteriaTest.00751", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00873", cityList[49].Name);

            //order by countryid desc, name asc
            //First Page
            cityList = cityRepo.ReadAllPaged("SELECT * FROM city WHERE state=@state", "countryid desc, name", 50, PageNavigationEnum.First, null, null, parameters).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal(2, cityList[0].CountryId);
            Assert.Equal("PagedNoCriteriaTest.00001", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00123", cityList[49].Name);

            //Next Page
            cityList = cityRepo.ReadAllPaged("SELECT * FROM city WHERE state=@state", "countryid desc,name", 50, PageNavigationEnum.Next, new object[] { cityList[49].CountryId, cityList[49].Name }, cityList[49].Id, parameters).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal(2, cityList[0].CountryId);
            Assert.Equal("PagedNoCriteriaTest.00127", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00249", cityList[49].Name);

            //Last Page
            cityList = cityRepo.ReadAllPaged("SELECT * FROM city WHERE state=@state", "countryid desc,name", 50, PageNavigationEnum.Last, null, null, parameters).ToList();

            Assert.Equal(50, cityList.Count);
            Assert.Equal(1, cityList[0].CountryId);
            Assert.Equal("PagedNoCriteriaTest.00876", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00998", cityList[49].Name);

            //Previous Page
            cityList = cityRepo.ReadAllPaged("SELECT * FROM city WHERE state=@state", "countryid desc,name", 50, PageNavigationEnum.Previous, new object[] { cityList[0].CountryId, cityList[0].Name }, cityList[0].Id, parameters).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal(1, cityList[0].CountryId);
            Assert.Equal("PagedNoCriteriaTest.00752", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00874", cityList[49].Name);
        }

        [Fact]
        public void WithQueryNoOffsetWithCriteriaTest()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            cityRepo.DropTable();
            cityRepo.CreateTable();

            int counter = 1000;

            for (int i = 1; i <= counter; i++)
            {
                City city = new City()
                {
                    Name = "PagedNoCriteriaTest." + i.ToString().PadLeft(5, '0'),
                    State = i % 2 == 0 ? "RC" : "DC",
                    CountryId = 1,
                    Longitude = 1m,
                    Latitude = 1m,
                    CreatedBy =Fixture.CurrentUserId
                };
                city.Id = (long)cityRepo.Add(city);
            }

            var parameters = new { state = "RC" };

            //order by ASC
            //First Page
            List<City> cityList = cityRepo.ReadAllPaged("SELECT * FROM city WHERE state=@state", "name", 50, PageNavigationEnum.First, null, null, parameters).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.00002", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00100", cityList[49].Name);

            //Next Page
            cityList = cityRepo.ReadAllPaged("SELECT * FROM city WHERE state=@state", "name", 50, PageNavigationEnum.Next, new[] { cityList[49].Name }, cityList[49].Id, parameters).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.00102", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00200", cityList[49].Name);

            //Last Page
            cityList = cityRepo.ReadAllPaged("SELECT * FROM city WHERE state=@state", "name", 50, PageNavigationEnum.Last, null, null, parameters).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.00902", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.01000", cityList[49].Name);

            //Previous Page
            cityList = cityRepo.ReadAllPaged("SELECT * FROM city WHERE state=@state", "name", 50, PageNavigationEnum.Previous, new[] { cityList[0].Name }, cityList[0].Id, parameters).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.00802", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00900", cityList[49].Name);

            //order by desc
            //First Page
            cityList = cityRepo.ReadAllPaged("SELECT * FROM city WHERE state=@state", "name desc", 50, PageNavigationEnum.First, null, null, parameters).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.01000", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00902", cityList[49].Name);

            //Next Page
            cityList = cityRepo.ReadAllPaged("SELECT * FROM city WHERE state=@state", "name desc", 50, PageNavigationEnum.Next, new[] { cityList[49].Name }, cityList[49].Id, parameters).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.00900", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00802", cityList[49].Name);

            //Last Page
            cityList = cityRepo.ReadAllPaged("SELECT * FROM city WHERE state=@state", "name desc", 50, PageNavigationEnum.Last, null, null, parameters).ToList();

            Assert.Equal(50, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.00100", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00002", cityList[49].Name);

            //Previous Page
            cityList = cityRepo.ReadAllPaged("SELECT * FROM city WHERE state=@state", "name desc", 50, PageNavigationEnum.Previous, new[] { cityList[0].Name }, cityList[0].Id, parameters).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.00200", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00102", cityList[49].Name);
        }

        [Fact]
        public void WithQueryNoOffsetMultipleOrderByNoCriteriaTest()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            cityRepo.DropTable();
            cityRepo.CreateTable();

            int counter = 1000;

            for (int i = 1; i <= counter; i++)
            {
                City city = new City()
                {
                    Name = "PagedNoCriteriaTest." + i.ToString().PadLeft(5, '0'),
                    State = i % 2 == 0 ? "RC" : "DC",
                    CountryId = i % 2 == 0 ? 1 : 2,
                    Longitude = 1m,
                    Latitude = 1m,
                    CreatedBy =Fixture.CurrentUserId
                };
                city.Id = (long)cityRepo.Add(city);
            }

            //order by country ASC, name ASC
            //First Page
            List<City> cityList = cityRepo.ReadAllPaged("SELECT * FROM city", "countryid,name", 50, PageNavigationEnum.First).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal(1, cityList[0].CountryId);
            Assert.Equal("PagedNoCriteriaTest.00002", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00100", cityList[49].Name);

            //Next Page
            cityList = cityRepo.ReadAllPaged("SELECT * FROM city", "countryid,name", 50, PageNavigationEnum.Next, new object[] { cityList[49].CountryId, cityList[49].Name }, cityList[49].Id).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal(1, cityList[0].CountryId);
            Assert.Equal("PagedNoCriteriaTest.00102", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00200", cityList[49].Name);

            //Last Page
            cityList = cityRepo.ReadAllPaged("SELECT * FROM city", "countryid,name", 50, PageNavigationEnum.Last).ToList();

            Assert.Equal(50, cityList.Count);
            Assert.Equal(2, cityList[0].CountryId);
            Assert.Equal("PagedNoCriteriaTest.00901", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00999", cityList[49].Name);

            //Previous Page
            cityList = cityRepo.ReadAllPaged("SELECT * FROM city", "countryid,name", 50, PageNavigationEnum.Previous, new object[] { cityList[0].CountryId, cityList[0].Name }, cityList[0].Id).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal(2, cityList[0].CountryId);
            Assert.Equal("PagedNoCriteriaTest.00801", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00899", cityList[49].Name);

            //order by countryid desc, name asc
            //First Page
            cityList = cityRepo.ReadAllPaged("SELECT * FROM city", "countryid desc, name", 50, PageNavigationEnum.First).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal(2, cityList[0].CountryId);
            Assert.Equal("PagedNoCriteriaTest.00001", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00099", cityList[49].Name);

            //Next Page
            cityList = cityRepo.ReadAllPaged("SELECT * FROM city", "countryid desc,name", 50, PageNavigationEnum.Next, new object[] { cityList[49].CountryId, cityList[49].Name }, cityList[49].Id).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal(2, cityList[0].CountryId);
            Assert.Equal("PagedNoCriteriaTest.00101", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00199", cityList[49].Name);

            //Last Page
            cityList = cityRepo.ReadAllPaged("SELECT * FROM city", "countryid desc,name", 50, PageNavigationEnum.Last).ToList();

            Assert.Equal(50, cityList.Count);
            Assert.Equal(1, cityList[0].CountryId);
            Assert.Equal("PagedNoCriteriaTest.00902", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.01000", cityList[49].Name);

            //Previous Page
            cityList = cityRepo.ReadAllPaged("SELECT * FROM city", "countryid desc,name", 50, PageNavigationEnum.Previous, new object[] { cityList[0].CountryId, cityList[0].Name }, cityList[0].Id).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal(1, cityList[0].CountryId);
            Assert.Equal("PagedNoCriteriaTest.00802", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00900", cityList[49].Name);
        }

        [Fact]
        public void WithQueryNoOffsetNoCriteriaTest()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            cityRepo.DropTable();
            cityRepo.CreateTable();

            int counter = 1000;

            for (int i = 1; i <= counter; i++)
            {
                City city = new City()
                {
                    Name = "PagedNoCriteriaTest." + i.ToString().PadLeft(5, '0'),
                    State = "RC",
                    CountryId = 1,
                    Longitude = 1m,
                    Latitude = 1m,
                    CreatedBy =Fixture.CurrentUserId
                };
                city.Id = (long)cityRepo.Add(city);
            }

            //order by ASC
            //First Page
            List<City> cityList = cityRepo.ReadAllPaged("SELECT * FROM city", "name", 50, PageNavigationEnum.First).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.00001", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00050", cityList[49].Name);

            //Next Page
            cityList = cityRepo.ReadAllPaged("SELECT * FROM city", "name", 50, PageNavigationEnum.Next, new[] { cityList[49].Name }, cityList[49].Id).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.00051", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00100", cityList[49].Name);

            //Last Page
            cityList = cityRepo.ReadAllPaged("SELECT * FROM city", "name", 50, PageNavigationEnum.Last).ToList();

            Assert.Equal(50, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.00951", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.01000", cityList[49].Name);

            //Previous Page
            cityList = cityRepo.ReadAllPaged("SELECT * FROM city", "name", 50, PageNavigationEnum.Previous, new[] { cityList[0].Name }, cityList[0].Id).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.00901", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00950", cityList[49].Name);

            //order by desc
            //First Page
            cityList = cityRepo.ReadAllPaged("SELECT * FROM city", "name desc", 50, PageNavigationEnum.First).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.01000", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00951", cityList[49].Name);

            //Next Page
            cityList = cityRepo.ReadAllPaged("SELECT * FROM city", "name desc", 50, PageNavigationEnum.Next, new[] { cityList[49].Name }, cityList[49].Id).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.00950", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00901", cityList[49].Name);

            //Last Page
            cityList = cityRepo.ReadAllPaged("SELECT * FROM city", "name desc", 50, PageNavigationEnum.Last).ToList();

            Assert.Equal(50, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.00050", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00001", cityList[49].Name);

            //Previous Page
            cityList = cityRepo.ReadAllPaged("SELECT * FROM city", "name desc", 50, PageNavigationEnum.Previous, new[] { cityList[0].Name }, cityList[0].Id).ToList();
            Assert.Equal(50, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.00100", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.00051", cityList[49].Name);
        }

        [Fact]
        public void NoQueryNoCriteriaTest()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            cityRepo.DropTable();
            cityRepo.CreateTable();

            int counter = 100;

            for (int i = 1; i <= counter; i++)
            {
                City city = new City()
                {
                    Name = "PagedNoCriteriaTest." + i.ToString().PadLeft(4, '0'),
                    State = "RC",
                    CountryId = 1,
                    Longitude = 1m,
                    Latitude = 1m,
                    CreatedBy =Fixture.CurrentUserId
                };
                city.Id = (long)cityRepo.Add(city);
            }

            List<City> cityList = cityRepo.ReadAllPaged("name", 1, 10).ToList();

            Assert.Equal(10, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.0001", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.0010", cityList[9].Name);

            cityList = cityRepo.ReadAllPaged("name", 5, 10).ToList();

            Assert.Equal(10, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.0041", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.0050", cityList[9].Name);

        }

        [Fact]
        public void NoQueryWithCriteriaTest()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            cityRepo.DropTable();
            cityRepo.CreateTable();

            int counter = 100;

            for (int i = 1; i <= counter; i++)
            {
                City city = new City()
                {
                    Name = "PagedNoCriteriaTest." + i.ToString().PadLeft(4, '0'),
                    State = (i % 2 == 0 ? "RC" : "DC"),
                    CountryId = 1,
                    Longitude = 1m,
                    Latitude = 1m,
                    CreatedBy =Fixture.CurrentUserId
                };
                city.Id = (long)cityRepo.Add(city);
            }

            List<City> cityList = cityRepo.ReadAllPaged("name", 1, 10, null, "state=@state", new { state = "DC" }).ToList();

            Assert.Equal(10, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.0001", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.0019", cityList[9].Name);

            cityList = cityRepo.ReadAllPaged("name", 5, 10, null, "state=@state",  new { state = "RC" }).ToList();

            Assert.Equal(10, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.0082", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.0100", cityList[9].Name);
        }

        [Fact]
        public void WithQueryNoCriteriaTest()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            cityRepo.DropTable();
            cityRepo.CreateTable();

            int counter = 100;

            for (int i = 1; i <= counter; i++)
            {
                City city = new City()
                {
                    Name = "PagedNoCriteriaTest." + i.ToString().PadLeft(4,'0'),
                    State = "RC",
                    CountryId = 1,
                    Longitude = 1m,
                    Latitude = 1m,
                    CreatedBy =Fixture.CurrentUserId
                };
                city.Id = (long)cityRepo.Add(city);
            }

            List<City> cityList = cityRepo.ReadAllPaged("SELECT * FROM city", "name", 1, 10).ToList();

            Assert.Equal(10, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.0001", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.0010", cityList[9].Name);

            cityList = cityRepo.ReadAllPaged("SELECT * FROM city", "name", 5, 10).ToList();

            Assert.Equal(10, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.0041", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.0050", cityList[9].Name);

        }

        [Fact]
        public void WithQueryWithCriteriaTest()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            cityRepo.DropTable();
            cityRepo.CreateTable();

            int counter = 100;

            for (int i = 1; i <= counter; i++)
            {
                City city = new City()
                {
                    Name = "PagedNoCriteriaTest." + i.ToString().PadLeft(4, '0'),
                    State = (i % 2 == 0 ? "RC" : "DC"),
                    CountryId = 1,
                    Longitude = 1m,
                    Latitude = 1m,
                    CreatedBy =Fixture.CurrentUserId
                };
                city.Id = (long)cityRepo.Add(city);
            }

            List<City> cityList = cityRepo.ReadAllPaged("SELECT * FROM city WHERE state=@state", "name", 1, 10, new { state = "DC" }).ToList();

            Assert.Equal(10, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.0001", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.0019", cityList[9].Name);

            cityList = cityRepo.ReadAllPaged("SELECT * FROM city WHERE state=@state", "name", 5, 10, new { state = "RC" }).ToList();

            Assert.Equal(10, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.0082", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.0100", cityList[9].Name);
        }

#if MSSQL
        [Fact]
        public void NoQueryNoCriteriaTestMSSQLBelow2012()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection, Fixture.CurrentSession);
            cityRepo.DropTable();
            cityRepo.CreateTable();

            int counter = 100;

            for (int i = 1; i <= counter; i++)
            {
                City city = new City()
                {
                    Name = "PagedNoCriteriaTest." + i.ToString().PadLeft(4, '0'),
                    State = "RC",
                    CountryId = 1,
                    Longitude = 1m,
                    Latitude = 1m
                };
                city.Id = (long)cityRepo.Add(city);
            }

            //Setting MSSQL Verion as 2008R2
            cityRepo.DBVersion.Version = new Version("10.50.6560");
            List<City> cityList = cityRepo.ReadAllPaged("name", 1, 10).ToList();

            Assert.Equal(10, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.0001", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.0010", cityList[9].Name);

            cityList = cityRepo.ReadAllPaged("name", 5, 10).ToList();

            Assert.Equal(10, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.0041", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.0050", cityList[9].Name);

        }
        [Fact]
        public void NoQueryWithCriteriaMSSQLBelow2012Test()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection, Fixture.CurrentSession);

            cityRepo.DropTable();
            cityRepo.CreateTable();

            int counter = 100;

            for (int i = 1; i <= counter; i++)
            {
                City city = new City()
                {
                    Name = "PagedNoCriteriaTest." + i.ToString().PadLeft(4, '0'),
                    State = (i % 2 == 0 ? "RC" : "DC"),
                    CountryId = 1,
                    Longitude = 1m,
                    Latitude = 1m
                };
                city.Id = (long)cityRepo.Add(city);
            }
            //Setting MSSQL Verion as 2008R2
            cityRepo.DBVersion.Version = new Version("10.50.6560");

            List<City> cityList = cityRepo.ReadAllPaged("name", 1, 10, null, "state=@state", new { state = "DC" }).ToList();

            Assert.Equal(10, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.0001", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.0019", cityList[9].Name);

            cityList = cityRepo.ReadAllPaged("name", 5, 10, null, "state=@state", new { state = "RC" }).ToList();

            Assert.Equal(10, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.0082", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.0100", cityList[9].Name);
        }
        [Fact]
        public void WithQueryNoCriteriaTestMSSQLBelow2012()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection, Fixture.CurrentSession);
            cityRepo.DropTable();
            cityRepo.CreateTable();

            int counter = 100;

            for (int i = 1; i <= counter; i++)
            {
                City city = new City()
                {
                    Name = "PagedNoCriteriaTest." + i.ToString().PadLeft(4, '0'),
                    State = "RC",
                    CountryId = 1,
                    Longitude = 1m,
                    Latitude = 1m
                };
                city.Id = (long)cityRepo.Add(city);
            }

            //Setting MSSQL Verion as 2008R2
            cityRepo.DBVersion.Version = new Version("10.50.6560");
            List<City> cityList = cityRepo.ReadAllPaged("SELECT * FROM city", "name", 1, 10).ToList();

            Assert.Equal(10, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.0001", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.0010", cityList[9].Name);

            cityList = cityRepo.ReadAllPaged("SELECT * FROM city", "name", 5, 10).ToList();

            Assert.Equal(10, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.0041", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.0050", cityList[9].Name);

        }
        [Fact]
        public void WithQueryWithCriteriaMSSQLBelow2012Test()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection, Fixture.CurrentSession);

            cityRepo.DropTable();
            cityRepo.CreateTable();

            int counter = 100;

            for (int i = 1; i <= counter; i++)
            {
                City city = new City()
                {
                    Name = "PagedNoCriteriaTest." + i.ToString().PadLeft(4, '0'),
                    State = (i % 2 == 0 ? "RC" : "DC"),
                    CountryId = 1,
                    Longitude = 1m,
                    Latitude = 1m
                };
                city.Id = (long)cityRepo.Add(city);
            }
            var parameters = new { state = "DC" };
            //Setting MSSQL Verion as 2008R2
            cityRepo.DBVersion.Version = new Version("10.50.6560");

            List<City> cityList = cityRepo.ReadAllPaged("SELECT * FROM city WHERE state=@state", "name", 1, 10, parameters).ToList();

            Assert.Equal(10, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.0001", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.0019", cityList[9].Name);

            parameters = new { state = "RC" };
            cityList = cityRepo.ReadAllPaged("SELECT * FROM city WHERE state=@state", "name", 5, 10, parameters).ToList();

            Assert.Equal(10, cityList.Count);
            Assert.Equal("PagedNoCriteriaTest.0082", cityList[0].Name);
            Assert.Equal("PagedNoCriteriaTest.0100", cityList[9].Name);
        }
#endif

    }

}
