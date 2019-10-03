using System;
using System.Linq;
using Xunit;

namespace Vega.Tests
{
    [Collection("DMLTest")]
    public class ReadAllTests : IClassFixture<DbConnectionFixuture>
    {
        DbConnectionFixuture Fixture;

        public ReadAllTests(DbConnectionFixuture fixture)
        {
            Fixture = fixture;
        }

        [Fact]
        public void ReadAll()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            var cityList = cityRepo.ReadAll();

            Assert.Equal(cityList.Count(), cityRepo.Count());
        }

        [Fact]
        public void ReadAllActive()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            var cityList = cityRepo.ReadAll(RecordStatusEnum.Active);

            Assert.Equal(cityList.Count(), (int)cityRepo.Count(RecordStatusEnum.Active));
        }

        [Fact]
        public void ReadAllInActive()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            var cityList = cityRepo.ReadAll(RecordStatusEnum.InActive);

            Assert.Equal(cityList.Count(), (int)cityRepo.Count(RecordStatusEnum.InActive));
        }

        

        [Fact]
        public void ReadAllSort()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            for (int i = 0; i < 10; i++)
            {
                City city = new City()
                {
                    Name = "ReadTests.ReadAllSort" + i,
                    State = "RS",
                    CountryId = i,
                    Longitude = 1m,
                    Latitude = 1m,
                    CreatedBy = Fixture.CurrentUserId
                };
                city.Id = (long)cityRepo.Add(city);
            }

            var cityList = cityRepo.ReadAll(null, "State=@State", new { State = "RS" }, "countryid");

            Assert.Equal((int)cityRepo.Count("State=@State", new { State = "RS" }), cityList.Count());
        }

        [Fact]
        public void ReadAllQuery()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            for (int i = 0; i < 10; i++)
            {
                City city = new City()
                {
                    Name = "ReadTests.ReadAllQuery" + i,
                    State = "RQ",
                    CountryId = i,
                    Longitude = 1m,
                    Latitude = 1m,
                    CreatedBy = Fixture.CurrentUserId
                };
                city.Id = (long)cityRepo.Add(city);
            }

            var cityList = cityRepo.ReadAllQuery("SELECT * FROM city WHERE state=@state", new { State = "RQ" });

            Assert.Equal((int)cityRepo.Count("state=@state", new { State = "RQ" }), cityList.Count());
        }

        [Fact]
        public void ReadAllEnumCriteria()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            for (int i = 0; i < 10; i++)
            {
                City city = new City()
                {
                    Name = "ReadTests.ReadAllQuery" + i,
                    State = "RQ",
                    CountryId = i,
                    Longitude = 1m,
                    Latitude = 1m,
                    CityType = EnumCityType.Metro,
                    CreatedBy = Fixture.CurrentUserId
                };
                city.Id = (long)cityRepo.Add(city);
            }

            var cityList = cityRepo.ReadAllQuery("SELECT * FROM city WHERE CityType=@CityType", new { CityType = EnumCityType.Metro });

            Assert.Equal((int)cityRepo.Count("CityType=@CityType", new { CityType = EnumCityType.Metro }), cityList.Count());
        }


        


        
    }
}
