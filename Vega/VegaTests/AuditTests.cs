using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vega;
using Xunit;

namespace VegaTests
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
            };

            //cleanup audittrial table
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);
            cityRepo.ExecuteNonQuery("DELETE FROM " + Config.AUDIT_TABLENAME);

            //add record

            var id = cityRepo.Add(city);

            //now update record
            city.State = "MH";
            cityRepo.Update(city);

            //update again
            city.CountryId = 1;
            cityRepo.Update(city);

            //read history
            List<City> cityHistory = cityRepo.ReadHistory(id);

            Assert.Equal(3, cityHistory.Count);
            Assert.Equal(0, cityHistory[0].CountryId);
            Assert.Equal("GU", cityHistory[0].State);

            Assert.Equal(0, cityHistory[1].CountryId);
            Assert.Equal("MH", cityHistory[1].State);

            Assert.Equal(1, cityHistory[2].CountryId);
            Assert.Equal("MH", cityHistory[2].State);
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
            };

            
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            //cleanup audit table
            cityRepo.ExecuteNonQuery("DELETE FROM " + Config.AUDIT_TABLENAME);

            //add record
            var id = cityRepo.Add(city);

            //read history
            List<City> cityHistory = cityRepo.ReadHistory(id);

            Assert.Single(cityHistory);
            Assert.Equal(strComplex, cityHistory[0].Name);
            Assert.Equal("RJ", cityHistory[0].State);
        }
    }
}
