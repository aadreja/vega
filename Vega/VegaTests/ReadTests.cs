using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Vega;
using System.Data;

namespace VegaTests
{
    [Collection("DMLTest")]
    public class ReadTests : IClassFixture<DbConnectionFixuture>
    {
        DbConnectionFixuture Fixture;

        public ReadTests(DbConnectionFixuture fixture)
        {
            Fixture = fixture;
        }

        [Fact]
        public void ExecuteDataSet()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            //Bulk Insert Data
            City[] cities = new City[20];
            for (int i = 0; i < 20; i++)
            {
                cities[i] = new City()
                {
                    Name = "ReadTests.ExecuteDataSet " + i,
                    State = "DS"
                };

                cities[i].Id = (long)cityRepo.Add(cities[i]);
            }

            //Execute dataset
            DataSet ds= cityRepo.ExecuteDataSet("SELECT * FROM city WHERE state='DS'");

            Assert.Equal(20, ds.Tables[0].Rows.Count);
        }

        [Fact]
        public void ReadCount()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            int counter = 10;

            for (int i = 0; i < counter; i++)
            {
                City city = new City()
                {
                    Name = "ReadTests.ReadCount " + i,
                    State = "RC",
                    CountryId = 1,
                    Longitude = 1m,
                    Latitude = 1m
                };
                city.Id = (long)cityRepo.Add(city);
            }

            Assert.Equal(counter, cityRepo.Count("State=@state AND CountryId=@countryid", new { State = "RC", CountryId = 1 }));
            Assert.Equal(counter, cityRepo.Count("State=@state", new { State = "RC" }));
        }

        [Fact]
        public void ReadOne()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            City city = new City()
            {
                Name = "ReadTests.ReadOne",
                State = "RO",
                CountryId = 1,
                Longitude = 1m,
                Latitude = 1m
            };
            city.Id = (long)cityRepo.Add(city);

            City cityReadOne = cityRepo.ReadOne(city.Id);

            Assert.Equal(city.Name, cityReadOne.Name);
            Assert.Equal(city.State, cityReadOne.State);
            Assert.Equal(city.CountryId, cityReadOne.CountryId);
            Assert.Equal(city.Longitude, cityReadOne.Longitude);
            Assert.Equal(city.Latitude, cityReadOne.Latitude);
        }

        [Fact]
        public void ReadAll()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            var cityList = cityRepo.ReadAll();

            Assert.Equal(cityList.Count(), (int)cityRepo.Count());
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
        public void Query()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            City city = new City()
            {
                Name = "ReadTests.Query",
                State = "Q",
                CountryId = 1,
                Longitude = 1m,
                Latitude = 1m
            };
            city.Id = (long)cityRepo.Add(city);

            cityRepo.Query<long>("SELECT id FROM City");
        }

    }
}
