using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vega;
using Xunit;

namespace Vega.Tests
{
    [Collection("DMLTest")]
    public class TransactionTests : IClassFixture<DbConnectionFixuture>
    {
        DbConnectionFixuture Fixture;

        public TransactionTests(DbConnectionFixuture fixture)
        {
            Fixture = fixture;
        }

        [Fact]
        public void SingleEntity()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            try
            {
                cityRepo.BeginTransaction();

                //add two records in transaction
                City[] cities = new City[2]
                {
                    new City()
                    {
                        Name = "Transaction City 1",
                        State = "T",
                        CreatedBy=1
                    },
                    new City()
                    {
                        Name = "Transaction City 2",
                        State = "T",
                        CreatedBy=1
                    }
                };

                cities[0].Id = (long)cityRepo.Add(cities[0]);
                cities[1].Id = (long)cityRepo.Add(cities[1]);

                //check before commit
                Assert.Equal(cities[0].Id, cityRepo.ReadOne<long>(cities[0].Id, "id"));
                Assert.Equal(cities[1].Id, cityRepo.ReadOne<long>(cities[1].Id, "id"));

                cityRepo.Commit();

                //check after commit
                Assert.Equal(cities[0].Id, cityRepo.ReadOne<long>(cities[0].Id, "id"));
                Assert.Equal(cities[1].Id, cityRepo.ReadOne<long>(cities[1].Id, "id"));
            }
            catch(Exception ex)
            {
                cityRepo.Rollback();
                Console.WriteLine(ex.Message);
                Assert.True(false);
            }
        }

        [Fact]
        public void MultipleEntity()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            try
            {
                //add country record
                Country country = new Country()
                {
                    Name = "TransactionTests.MultipleEntity",
                    CreatedBy = Fixture.CurrentUserId
                };

                //add city record
                City city = new City()
                {
                    Name = "TransactionTests.MultipleEntity",
                    State = "T",
                    CreatedBy = Fixture.CurrentUserId
                };

                cityRepo.BeginTransaction();

                Repository<Country> countryRepo = new Repository<Country>(cityRepo.Transaction);

                country.Id = (long)countryRepo.Add(country);

                city.CountryId = (int)country.Id;
                city.Id = (long)cityRepo.Add(city);

                //check before commit
                Assert.Equal(country.Id, cityRepo.ReadOne<long>(country.Id, "id"));
                Assert.Equal(city.Id, cityRepo.ReadOne<long>(city.Id, "id"));

                cityRepo.Commit();

                //check after commit
                Assert.Equal(country.Id, cityRepo.ReadOne<long>(country.Id, "id"));
                Assert.Equal(city.Id, cityRepo.ReadOne<long>(city.Id, "id"));
            }
            catch (Exception ex)
            {
                cityRepo.Rollback();
                Console.WriteLine(ex.Message);
                Assert.True(false);
            }
        }

        [Fact]
        public void Rollback()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            try
            {
                //add city record which fails as Id is given for Identity column insert for MSSQL
                City city = new City()
                {
                    Id = 1,
                    Name = "TransactionTests.Rollback",
                    State = null,
                    CreatedBy = Fixture.CurrentUserId
                };

                //Delete record if exists before transaction
                if (cityRepo.Exists(city.Id)) cityRepo.HardDelete(city.Id, Fixture.CurrentUserId);

                cityRepo.BeginTransaction();

                //add record
                city.Id = (long)cityRepo.Add(city);

                //add record with same key to generate error for PGSql
                city.Id = (long)cityRepo.Add(city);

                cityRepo.Commit();

                Assert.NotEqual("TransactionTests.Rollback", cityRepo.ReadOne<string>(1, "Name"));
            }
            catch (Exception ex)
            {
                cityRepo.Rollback();

                Assert.NotEqual("TransactionTests.Rollback", cityRepo.ReadOne<string>(1, "Name"));

                Console.WriteLine(ex.Message);
            }
        }
    }

}
