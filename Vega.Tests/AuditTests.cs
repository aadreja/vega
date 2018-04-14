using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vega;
using Xunit;

namespace Vega.Tests
{
    
    public class AuditTests : IClassFixture<DbConnectionFixuture>
    {
        DbConnectionFixuture Fixture;

        public AuditTests(DbConnectionFixuture fixture)
        {
            Fixture = fixture;
        }

        [Fact]
        public void AuditTrial()
        {
            City city = new City
            {
                Name = "Ahmedabad",
                State = "GU",
                Latitude = 10.65m,
                Longitude = 11.50m,
                CreatedBy =Fixture.CurrentUserId
            };

            //cleanup audittrial table
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            var id = cityRepo.Add(city);
            cityRepo.ExecuteNonQuery("DELETE FROM " + Config.VegaConfig.AuditTableName);

            //add record
            city.Id = 0;
            id = cityRepo.Add(city);

            //now update record
            city.State = "MH";
            city.UpdatedBy =Fixture.CurrentUserId;
            cityRepo.Update(city);

            //update again
            city.CountryId = 1;
            city.UpdatedBy =Fixture.CurrentUserId;
            cityRepo.Update(city);

            //read history
            var cityHistory = cityRepo.ReadHistory(id);

            Assert.Equal(3, cityHistory.Count());
            Assert.Equal(0, cityHistory.First().CountryId);
            Assert.Equal("GU", cityHistory.First().State);

            Assert.Equal(0, cityHistory.ElementAt(1).CountryId);
            Assert.Equal("MH", cityHistory.ElementAt(1).State);

            Assert.Equal(1, cityHistory.Last().CountryId);
            Assert.Equal("MH", cityHistory.Last().State);
        }

        [Fact]
        public void AuditEscapeString()
        {
            string strComplex = "So far, we've been writing regular ex&nbsp;pressions that partially match pieces across all the text. " +
                        "Sometimes this isn't desirable, imagine for example we wanted to match the word \"success\"" +
                        "in a log file. We certainly don't want that pattern to match a line that says \"Error: unsuccessful operation\"! " +
                        "That is why it is often best practice to write as specific regular expressions as possible to ensure that we don't get false " +
                        "positives when matching against real world text. One way to tighten our patterns is to define a pattern that describes both the" +
                        "start and the end of the line using the special ^ (hat)and $ (dollar sign) metacharacters." +
                        "In the example above, we can use the pattern ^ success to match only a line that begins with the word \"success\", but not the line " +
                        "Error: unsuccessful operation\". And if you combine both the hat and the dollar sign, you create a pattern that matches the whole line " +
                        "completely at the beginning and end.";

            City city = new City
            {
                Name = strComplex,
                State = "RJ",
                Latitude = 56.65m,
                Longitude = 16.50m,
                CreatedBy =Fixture.CurrentUserId
            };

            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            var id = cityRepo.Add(city);

            //cleanup audit table
            cityRepo.ExecuteNonQuery("DELETE FROM " + Config.VegaConfig.AuditTableName);

            //add record
            city.Id = 0;
            id = cityRepo.Add(city);

            //read history
            var cityHistory = cityRepo.ReadHistory(id);

            Assert.Single(cityHistory);
            Assert.Equal(strComplex, cityHistory.First().Name);
            Assert.Equal("RJ", cityHistory.First().State);
        }
    }
}
