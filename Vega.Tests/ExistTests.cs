using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Vega.Tests
{
    [Collection("DMLTest")]
    public class ExistTests : IClassFixture<DbConnectionFixuture>
    {
        DbConnectionFixuture Fixture;

        public ExistTests(DbConnectionFixuture fixture)
        {
            Fixture = fixture;
        }

        [Fact]
        public void Exists()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            City city = new City()
            {
                Name = "ReadTests.Exists",
                State = "RO",
                CountryId = 1,
                Longitude = 1m,
                Latitude = 1m,
                CreatedBy = Fixture.CurrentUserId
            };
            city.Id = (long)cityRepo.Add(city);

            Assert.True(cityRepo.Exists(city.Id));
            Assert.True(cityRepo.Exists("State=@State", new { State = "RO" }));
            Assert.False(cityRepo.Exists("State=@State", new { State = "R1" }));
        }

        [Fact]
        public void ExistsWhere()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            City city = new City()
            {
                Name = "ReadTests.ExistsNew",
                State = "RO",
                CountryId = 1,
                Longitude = 1m,
                Latitude = 1m,
                CreatedBy = Fixture.CurrentUserId
            };
            city.Id = (long)cityRepo.Add(city);

            Assert.True(cityRepo.Exists(new { State = "RO" }));
            Assert.False(cityRepo.Exists(new { State = "R1" }));
        }

        [Fact]
        public void ExistsOneParameterUsedMultiple()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            City city = new City()
            {
                Name = "ReadTests.ExistsNew1",
                State = "RO",
                CountryId = 1,
                Longitude = 1m,
                Latitude = 1m,
                CreatedBy = Fixture.CurrentUserId
            };
            city.Id = (long)cityRepo.Add(city);

            city.Id = 0;
            city.Name = "ReadTests.ExistsNew2";
            city.CountryId = 2;
            city.Id = (long)cityRepo.Add(city);

            Assert.True(cityRepo.Exists("(countryid=@countryid AND state=@state) OR (state=@state and countryid=1)", 
                new { countryid = 2, state = "RO" }));

            Assert.False(cityRepo.Exists("(countryid=@countryid AND state=@state) OR (state=@state and countryid=1)",
                new { countryid = 2, state = "GU" }));
        }

    }
}
