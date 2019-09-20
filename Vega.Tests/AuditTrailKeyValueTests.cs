using System.Linq;
using Xunit;

namespace Vega.Tests
{

    public class AuditTrailKeyValueTests : IClassFixture<DbConnectionFixuture>
    {
        DbConnectionFixuture Fixture;

        public AuditTrailKeyValueTests(DbConnectionFixuture fixture)
        {
            fixture.SetAuditTrailType(true); //as keyvalue 
            Fixture = fixture;
        }

        [Fact]
        public void AuditTrailKeyValue()
        {
            City city = new City
            {
                Name = "Ahmedabad",
                State = "GU",
                Latitude = 10.65m,
                Longitude = 11.50m,
                CreatedBy = Fixture.CurrentUserId
            };

            //cleanup auditTrail table
            Fixture.CleanupAuditTable();

            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);
           
            //add record
            var id = cityRepo.Add(city);

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

            var auditRepo = new AuditTrailKeyValueRepository<City>(Fixture.Connection);
            var history = auditRepo.ReadAllAuditTrail(id);
            Assert.Equal(3, history.Count());
            Assert.Equal("GU", history.First().lstAuditTrailDetail.Find(h => h.ColumnName == "State").NewValue.ToString());
            Assert.Null(history.First().lstAuditTrailDetail.Find(h => h.ColumnName == "State").OldValue);

            Assert.Equal("MH", history.ElementAt(1).lstAuditTrailDetail.Find(h => h.ColumnName == "State").NewValue.ToString());
            Assert.Equal("GU", history.ElementAt(1).lstAuditTrailDetail.Find(h => h.ColumnName == "State").OldValue.ToString());

        }

        [Fact]
        public void AuditTrailKeyValueNoEntityBase()
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

            Employee emp = new Employee
            {
                EmployeeName = strComplex,
                Department = "RJ",
                DOB = null,
            };

            Fixture.CleanupAuditTable();

            Repository<Employee> empRepo = new Repository<Employee>(Fixture.Connection);

            //add record
            var id = empRepo.Add(emp);

            //read history
            var empHistory = empRepo.ReadHistory(id);

            Assert.Single(empHistory);
            Assert.Equal(strComplex, empHistory.First().EmployeeName);
            Assert.Equal("RJ", empHistory.First().Department);
            Assert.Null(empHistory.First().DOB);

            AuditTrailKeyValueRepository<Employee> auditRepo = new AuditTrailKeyValueRepository<Employee>(Fixture.Connection);

            //read history
            var history = auditRepo.ReadAllAuditTrail(id);

            Assert.Single(history);
            Assert.Equal(strComplex, history.First().lstAuditTrailDetail.Find(p=>p.ColumnName== "EmployeeName").NewValue.ToString());
            Assert.Equal("RJ", history.First().lstAuditTrailDetail.Find(p => p.ColumnName == "Department").NewValue.ToString());
            Assert.Null(history.First().lstAuditTrailDetail.Find(p => p.ColumnName == "DOB"));
        }
    }
}
