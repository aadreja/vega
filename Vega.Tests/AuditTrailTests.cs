using System;
using System.Linq;
using Xunit;

namespace Vega.Tests
{
    [Collection("DMLTest")]
    public class AuditTrailTests : IClassFixture<DbConnectionFixuture>
    {
        DbConnectionFixuture Fixture;

        public AuditTrailTests(DbConnectionFixuture fixture)
        {
            fixture.SetAuditTrailType(false); //in one table as json 
            Fixture = fixture;
        }

        [Fact]
        public void AuditTrail()
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

            Fixture.CleanupAuditTable();

            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            //add record
            var id = cityRepo.Add(city);

            //read history
            var cityHistory = cityRepo.ReadHistory(id);

            Assert.Single(cityHistory);
            Assert.Equal(strComplex, cityHistory.First().Name);
            Assert.Equal("RJ", cityHistory.First().State);
        }


        [Fact]
        public void AuditTrailNoEntityBase()
        {
            Employee emp = new Employee
            {
                EmployeeName = "Rajesh",
                Department = "AC",
                DOB = null,
            };

            Fixture.CleanupAuditTable();

            Repository<Employee> empRepo = new Repository<Employee>(Fixture.Connection);

            //add record
            var id = empRepo.Add(emp);

            //read history
            var empHistory = empRepo.ReadHistory(id);

            Assert.Single(empHistory);
            Assert.Equal("Rajesh", empHistory.First().EmployeeName);
            Assert.Equal("AC", empHistory.First().Department);
            Assert.Null(empHistory.First().DOB);

            AuditTrailRepository<Employee> auditRepo = new AuditTrailRepository<Employee>(Fixture.Connection);

            //read history
            var history = auditRepo.ReadAllAuditTrail(id);

            Assert.Single(history);
            Assert.Equal("Rajesh", history.First().lstAuditTrailDetail.Find(p => p.ColumnName == "EmployeeName").NewValue);
            Assert.Equal("AC", history.First().lstAuditTrailDetail.Find(p => p.ColumnName == "Department").NewValue);
            Assert.Null(history.First().lstAuditTrailDetail.Find(p => p.ColumnName == "DOB"));
        }

        [Fact]
        public void AuditTrailPipeInText()
        {
            Fixture.CleanupAuditTable();

            Repository<Employee> empRepo = new Repository<Employee>(Fixture.Connection);
            Employee emp = new Employee
            {
                EmployeeName = "Rajesh",
                Department = "AC|IT",
                DOB = new DateTime(1980, 7, 22),
            };

            //add record
            var id = empRepo.Add(emp);

            //update record
            emp.Department = "IT";
            emp.DOB = new DateTime(1983, 7, 22);
            empRepo.Update(emp);

            //read history
            var empHistory = empRepo.ReadHistory(id);
            Assert.Equal(2, empHistory.Count());
            Assert.Equal("Rajesh", empHistory.First().EmployeeName);
            Assert.Equal("AC|IT", empHistory.First().Department);
            Assert.Equal(new DateTime(1980, 7, 22), empHistory.First().DOB);

            Assert.Equal("IT", empHistory.ElementAt(1).Department);
            Assert.Equal(new DateTime(1983, 7, 22), empHistory.ElementAt(1).DOB);

            AuditTrailRepository<Employee> auditRepo = new AuditTrailRepository<Employee>(Fixture.Connection);
            //read audittrail
            var history = auditRepo.ReadAllAuditTrail(id);

            Assert.Equal(2, history.Count);

            Assert.Equal("Rajesh", history.First().lstAuditTrailDetail.Find(p => p.ColumnName == "EmployeeName").NewValue);
            Assert.Equal("AC|IT", history.First().lstAuditTrailDetail.Find(p => p.ColumnName == "Department").NewValue);
            Assert.Equal(new DateTime(1980, 7, 22), DateTime.Parse(history.First().lstAuditTrailDetail.Find(p => p.ColumnName == "DOB").NewValue));

            Assert.Equal("IT", history.ElementAt(1).lstAuditTrailDetail.Find(p => p.ColumnName == "Department").NewValue);
            Assert.Equal("AC|IT", history.ElementAt(1).lstAuditTrailDetail.Find(p => p.ColumnName == "Department").OldValue);

            Assert.Equal(new DateTime(1983, 7, 22), DateTime.Parse(history.ElementAt(1).lstAuditTrailDetail.Find(p => p.ColumnName == "DOB").NewValue));
            Assert.Equal(new DateTime(1980, 7, 22), DateTime.Parse(history.ElementAt(1).lstAuditTrailDetail.Find(p => p.ColumnName == "DOB").OldValue));
        }

        [Fact]
        public void AuditTrailOldNewforExistingRecords()
        {
            Fixture.CleanupAuditTable();

            Employee emp = new Employee
            {
                EmployeeId = 1,
                EmployeeName = "Rajesh",
                Department = "AC",
                DOB = null,
            };

            //add values as it was earlier
            AuditTrail audit = new AuditTrail
            {
                lstAuditTrailDetail = new System.Collections.Generic.List<IAuditTrailDetail>()
                {
                    new AuditTrailDetail()
                    {
                        ColumnName = "EmployeeName",
                        NewValue = "\"Rajesh\"",
                    },
                    new AuditTrailDetail()
                    {
                        ColumnName = "Department",
                        NewValue = "\"AC\"",
                    }
                }
            };

            AuditTrailRepository<Employee> auditRepo = new AuditTrailRepository<Employee>(Fixture.Connection);
            auditRepo.Add(emp, RecordOperationEnum.Insert, audit);

            audit = new AuditTrail
            {
                lstAuditTrailDetail = new System.Collections.Generic.List<IAuditTrailDetail>()
                {
                    new AuditTrailDetail()
                    {
                        ColumnName = "EmployeeName",
                        NewValue = "\"Rakesh\"",
                        OldValue = "\"Rajesh\"",
                    },
                    new AuditTrailDetail()
                    {
                        ColumnName = "Department",
                        NewValue = "\"IT\"",
                        OldValue = "\"AC\"",
                    },
                    new AuditTrailDetail()
                    {
                        ColumnName = "DOB",
                        NewValue = "1980-7-22"
                    }
                }
            };
            auditRepo.Add(emp, RecordOperationEnum.Update, audit);

            //read history
            var history = auditRepo.ReadAllAuditTrail(1);

            Assert.Equal(2, history.Count);
            Assert.Equal("Rajesh", history.First().lstAuditTrailDetail.Find(p => p.ColumnName == "EmployeeName").NewValue);
            Assert.Equal("AC", history.First().lstAuditTrailDetail.Find(p => p.ColumnName == "Department").NewValue);
            Assert.Null(history.First().lstAuditTrailDetail.Find(p => p.ColumnName == "DOB"));
        }
    }
}
