using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Vega.Tests
{
    [Collection("DMLTest")]
    public class QueryTests : IClassFixture<DbConnectionFixuture>
    {
        DbConnectionFixuture Fixture;

        public QueryTests(DbConnectionFixuture fixture)
        {
            Fixture = fixture;
        }
    
        [Fact]
        public void Query()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            City city = new City()
            {
                Name = "ReadTests.Query",
                State = "Q",
                CountryId = 1,
                Longitude = 1m,
                Latitude = 1m,
                CreatedBy = Fixture.CurrentUserId
            };
            city.Id = (long)cityRepo.Add(city);

            long id = cityRepo.Query<long>("SELECT id FROM City WHERE Name='ReadTests.Query'");
            Assert.Equal(city.Id, id);

            id = cityRepo.Query<long>("SELECT id FROM City WHERE Id=@Id", new { Id = city.Id });
            Assert.Equal(city.Id, id);
        }

        [Fact]
        public void QueryOneWithNullParameterValue()
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

            int? country = null;

            City cityResult = cityRepo.QueryOne("SELECT * from city WHERE countryid=@countryid", new { countryid = country });

            Assert.Null(cityResult);
        }
    }
}
