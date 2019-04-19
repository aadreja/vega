﻿using System;
using Vega;
using Xunit;

namespace Vega.Tests
{
    [Collection("DMLTest")]
    public class InsertTests : IClassFixture<DbConnectionFixuture>
    {
        DbConnectionFixuture Fixture;

        public InsertTests(DbConnectionFixuture fixture)
        {
            Fixture = fixture;
        }

        [Fact]
        public void InsertNoVersionNoIsActive()
        {
            Department department = new Department
            {
                DepartmentName = "Accounts",
            };

            Repository<Department> deptRepo = new Repository<Department>(Fixture.Connection);

            //must throw updated by required error and set CreatedBy
            Exception ex = Assert.Throws<MissingFieldException>(() => deptRepo.Add(department));

            Assert.Equal("CreatedBy is required when Audit Trail is enabled", ex.Message);

            //set CreatedBy
            department.CreatedBy = Fixture.CurrentUserId;
            //try to add again
            var id = deptRepo.Add(department);

            Assert.Equal((int)id, deptRepo.ReadOne<int>(id, "DepartmentId"));
            Assert.Equal("Accounts", deptRepo.ReadOne<string>(id, "DepartmentName"));
        }

        [Fact]
        public void InsertNoCreatedByOnAndNoUpdatedByOn()
        {
            Job job = new Job
            {
                JobName = "Accountant",
            };

            Repository<Job> jobRepo = new Repository<Job>(Fixture.Connection);

            var id = jobRepo.Add(job);

            Assert.Equal((int)id, jobRepo.ReadOne<int>(id, "JobId"));
            Assert.Equal("Accountant", jobRepo.ReadOne<string>(id, "JobName"));
        }

        [Fact]
        public void InsertNoEntityBase()
        {
            Employee employee = new Employee
            {
                EmployeeName = "Ramesh",
                Department = "Accounts",
                DOB = new DateTime(1980, 7, 22)
            };

            Repository<Employee> empRepo = new Repository<Employee>(Fixture.Connection);

            var id = empRepo.Add(employee);

            Assert.Equal((int)id, empRepo.ReadOne<int>(id, "EmployeeId"));
            Assert.Equal("Ramesh", empRepo.ReadOne<string>(id, "EmployeeName"));
        }

        [Fact]
        public void InsertNoIdentity()
        {
            User usr = new User
            {
                Id = Fixture.CurrentUserId,
                Username = "admin",
                CreatedBy = Fixture.CurrentUserId
            };

            Repository<User> usrRepo = new Repository<User>(Fixture.Connection);

            if (usrRepo.Exists(usr.Id)) usrRepo.HardDelete(usr.Id, Fixture.CurrentUserId);

            usrRepo.Add(usr);

            //Assert.Equal<Guid>(Fixture.CurrentUserId, usrRepo.ReadOne<Guid>(Fixture.CurrentUserId, "id"));
            Assert.Equal<int>(Fixture.CurrentUserId, usrRepo.ReadOne<int>(Fixture.CurrentUserId, "id"));
        }

        [Fact]
        public void InsertIdentity()
        {
            City city = new City
            {
                Name = "Ahmedabad",
                State = "GU",
                Latitude = 10.65m,
                Longitude = 11.50m,
                CityType = EnumCityType.Metro,
                CreatedBy =Fixture.CurrentUserId
            };

            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);
            var id = cityRepo.Add(city);

            Assert.Equal("Ahmedabad", cityRepo.ReadOne<string>(id, "Name"));
            Assert.Equal(EnumCityType.Metro, cityRepo.ReadOne<EnumCityType>(id, "CityType"));
        }

        [Fact]
        public void InsertWithNullable()
        {
            Country country = new Country
            {
                Name = "India",
                ShortCode = "IN",
                Independence = new DateTime(1947, 8, 15),//15th August, 1947
                CreatedBy =Fixture.CurrentUserId
            };

            Repository<Country> countryRepo = new Repository<Country>(Fixture.Connection);
            var id = countryRepo.Add(country);

            country.UpdatedBy = Fixture.CurrentUserId;
            var id1 = countryRepo.Update(country);

            Assert.Equal("India", countryRepo.ReadOne<string>(id, "Name"));
        }

        [Fact]
        public void InsertNonNumericEnum()
        {
            Country country = new Country
            {
                Name = "India",
                ShortCode = "IN",
                Independence = new DateTime(1947, 8, 15),
                Continent = EnumContinent.America,
                CreatedBy =Fixture.CurrentUserId
            };

            Repository<Country> countryRepo = new Repository<Country>(Fixture.Connection);
            var id = countryRepo.Add(country);

            Assert.Equal("India", countryRepo.ReadOne<string>(id, "Name"));
            Assert.Equal(EnumContinent.America, countryRepo.ReadOne<EnumContinent>(id, "Continent"));
        }

        [Fact]
        public void InsertWithFalseInActive()
        {
            Country country = new Country
            {
                Name = "India",
                ShortCode = "IN",
                Independence = new DateTime(1947, 8, 15),//15th August, 1947
                CreatedBy = Fixture.CurrentUserId,
                IsActive=false
            };

            Repository<Country> countryRepo = new Repository<Country>(Fixture.Connection);
            var id = countryRepo.Add(country);

            Assert.False(countryRepo.ReadOne<bool>(id, "IsActive"));
        }

        [Fact]
        public void InsertWithDefaultInActive()
        {
            Country country = new Country
            {
                Name = "India",
                ShortCode = "IN",
                Independence = new DateTime(1947, 8, 15),//15th August, 1947
                CreatedBy = Fixture.CurrentUserId,
            };

            Repository<Country> countryRepo = new Repository<Country>(Fixture.Connection);
            var id = countryRepo.Add(country);

            Assert.True(countryRepo.ReadOne<bool>(id, "IsActive"));
        }

        [Fact]
        public void InsertWithTrueInActive()
        {
            Country country = new Country
            {
                Name = "India",
                ShortCode = "IN",
                Independence = new DateTime(1947, 8, 15),//15th August, 1947
                CreatedBy = Fixture.CurrentUserId,
                IsActive=true
            };

            Repository<Country> countryRepo = new Repository<Country>(Fixture.Connection);
            var id = countryRepo.Add(country);

            Assert.True(countryRepo.ReadOne<bool>(id, "IsActive"));
        }

        [Fact]
        public void InsertWithOverrideVersionNo()
        {
            Country country = new Country
            {
                Name = "India",
                ShortCode = "IN",
                Independence = new DateTime(1947, 8, 15),//15th August, 1947
                CreatedBy = Fixture.CurrentUserId,
                VersionNo = 5
            };

            Repository<Country> countryRepo = new Repository<Country>(Fixture.Connection);
            var id = countryRepo.Add(country);

            Assert.Equal(5,countryRepo.ReadOne<int>(id, "VersionNo"));
        }

        [Fact]
        public void InsertWithDefaultVersionNo()
        {
            Country country = new Country
            {
                Name = "India",
                ShortCode = "IN",
                Independence = new DateTime(1947, 8, 15),//15th August, 1947
                CreatedBy = Fixture.CurrentUserId,
            };

            Repository<Country> countryRepo = new Repository<Country>(Fixture.Connection);
            var id = countryRepo.Add(country);

            Assert.Equal(1, countryRepo.ReadOne<int>(id, "VersionNo"));
        }

        [Fact]
        public void InsertWithDefaultCreatedUpdatedOn()
        {
            Country country = new Country
            {
                Name = "India",
                ShortCode = "IN",
                Independence = new DateTime(1947, 8, 15),//15th August, 1947
                CreatedBy = Fixture.CurrentUserId,
            };

            Repository<Country> countryRepo = new Repository<Country>(Fixture.Connection);
            var id = countryRepo.Add(country);

            Assert.Equal(DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss"), countryRepo.ReadOne<DateTime>(id, "CreatedOn").ToString("dd-MM-yyyy hh:mm:ss"));
            Assert.Equal(DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss"), countryRepo.ReadOne<DateTime>(id, "UpdatedOn").ToString("dd-MM-yyyy hh:mm:ss"));
        }

        [Fact]
        public void InsertWithOverrideCreatedUpdatedOn()
        {
            DateTime overrideDate = new DateTime(2019, 04, 18, 1, 1, 1);
            Country country = new Country
            {
                Name = "India",
                ShortCode = "IN",
                Independence = new DateTime(1947, 8, 15),//15th August, 1947
                CreatedBy = Fixture.CurrentUserId,
                CreatedOn = overrideDate,
                UpdatedOn = overrideDate
            };

            Repository<Country> countryRepo = new Repository<Country>(Fixture.Connection);
            var id = countryRepo.Add(country, overrideCreatedUpdatedOn:true);

            Assert.Equal(overrideDate, countryRepo.ReadOne<DateTime>(id, "CreatedOn"));
            Assert.Equal(overrideDate, countryRepo.ReadOne<DateTime>(id, "UpdatedOn"));
        }
    }
}
