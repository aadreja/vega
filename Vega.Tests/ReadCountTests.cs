using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Vega.Tests
{
    [Collection("DMLTest")]
    public class ReadCountTests : IClassFixture<DbConnectionFixuture>
    {
        DbConnectionFixuture Fixture;

        public ReadCountTests(DbConnectionFixuture fixture)
        {
            Fixture = fixture;
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
                    State = i % 2==0 ? "RC" : "GU",
                    CountryId = 1,
                    Longitude = 1m,
                    Latitude = 1m,
                    CreatedBy = Fixture.CurrentUserId
                };
                city.Id = (long)cityRepo.Add(city);
            }

            Assert.Equal(counter, cityRepo.Count());
            Assert.Equal(counter/2, cityRepo.Count(new { State = "GU" }));
            Assert.Equal(counter/2, cityRepo.Count("State=@State AND CountryId=@Countryid", new { State = "RC", CountryId = 1 }));
            Assert.Equal(counter/2, cityRepo.Count("State=@state", new { State = "RC" }));
        }
    }
}
