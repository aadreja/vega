using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Vega;
using System.Data;

namespace Vega.Tests
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
            //Repository<City> cityRepo = new Repository<City>(Fixture.Connection, Fixture.CurrentSession);

            ////Bulk Insert Data
            //City[] cities = new City[20];
            //for (int i = 0; i < 20; i++)
            //{
            //    cities[i] = new City()
            //    {
            //        Name = "ReadTests.ExecuteDataSet " + i,
            //        State = "DS"
            //    };

            //    cities[i].Id = (long)cityRepo.Add(cities[i]);
            //}

            ////Execute dataset
            //DataSet ds= cityRepo.ExecuteDataSet("SELECT * FROM city WHERE state='DS'");

            //Assert.Equal(20, ds.Tables[0].Rows.Count);
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
                    Latitude = 1m,
                    CreatedBy = Fixture.CurrentUserId
                };
                city.Id = (long)cityRepo.Add(city);
            }

            Assert.Equal(counter, cityRepo.Count("State=@State AND CountryId=@Countryid", new { State = "RC", CountryId = 1 }));
            Assert.Equal(counter, cityRepo.Count("State=@state", new { State = "RC" }));
        }

        [Fact]
        public void ReadOneNullParameter()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            City city = new City()
            {
                Name = "ReadTests.ReadOneNullParameter",
                State = "RO1",
                CountryId = 1,
                Longitude = 1m,
                Latitude = 1m,
                CreatedBy = Fixture.CurrentUserId
            };
            city.Id = (long)cityRepo.Add(city);

            City cityReadOne = cityRepo.ReadOne("State=@State", new { State = "RO1" }, "name");

            Assert.Equal(city.Name, cityReadOne.Name);
        }

        [Fact]
        public void ReadOneWithCriterialNoParameter()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            City city = new City()
            {
                Name = "ReadTests.ReadOneWithCriterialNoParameter",
                State = "RO2",
                CountryId = 1,
                Longitude = 1m,
                Latitude = 1m,
                CreatedBy = Fixture.CurrentUserId
            };
            city.Id = (long)cityRepo.Add(city);

            Assert.Throws<Exception>(() => cityRepo.ReadOne("State=@State", null, "name"));
        }

        [Fact]
        public void ReadOneMultipleParameter()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            City city = new City()
            {
                Name = "ReadTests.ReadOneMultipleParameter",
                State = "RO3",
                CountryId = 1,
                Longitude = 1m,
                Latitude = 1m,
                CreatedBy = Fixture.CurrentUserId
            };
            city.Id = (long)cityRepo.Add(city);

            City result = cityRepo.ReadOne("State=@state and CountryId=@countryid", new { State = "RO3", CountryId = 1 }, "name");
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
                Latitude = 1m,
                CreatedBy = Fixture.CurrentUserId
            };
            city.Id = (long)cityRepo.Add(city);

            City cityReadOne = cityRepo.ReadOne(city.Id);

            Assert.Equal(city.Name, cityReadOne.Name);
            Assert.Equal(city.State, cityReadOne.State);
            Assert.Equal(city.CountryId, cityReadOne.CountryId);
            Assert.Equal(city.Longitude, cityReadOne.Longitude);
            Assert.Equal(city.Latitude, cityReadOne.Latitude);

            cityReadOne = cityRepo.ReadOne("State=@State", new { State = "RO" });
            Assert.Equal("RO", cityReadOne.State);
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
        public void Query()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            City city = new City()
            {
                Name = "ReadTests.Query",
                State = "Q",
                CountryId = 1,
                Longitude = 1m,
                Latitude = 1m,
                CreatedBy = Fixture.CurrentUserId
            };
            city.Id = (long)cityRepo.Add(city);

            long id = cityRepo.Query<long>("SELECT id FROM City WHERE Name='ReadTests.Query'");
            Assert.Equal(city.Id, id);

            id = cityRepo.Query<long>("SELECT id FROM City WHERE Id=@Id", new { Id = city.Id });
            Assert.Equal(city.Id, id);
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


        [Fact]
        public void ReadWithoutIsActiveAfterDelete()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            City city = new City()
            {
                Name = "ReadTests.ReadWithoutIsActive",
                State = "RQ",
                CountryId = 1,
                Longitude = 1m,
                Latitude = 1m,
                CityType = EnumCityType.Metro,
                CreatedBy = Fixture.CurrentUserId
            };
            //add
            city.Id = (long)cityRepo.Add(city);

            //delete
            cityRepo.Delete(city.Id, city.VersionNo, Fixture.CurrentUserId);

            //get
            City deletedCity = cityRepo.ReadOne(city.Id, "Name, State");

            Assert.Null(deletedCity.IsActive);
        }

        [Fact]
        public void ReadIsActiveAfterDelete()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            City city = new City()
            {
                Name = "ReadTests.ReadWithoutIsActive",
                State = "RQ",
                CountryId = 1,
                Longitude = 1m,
                Latitude = 1m,
                CityType = EnumCityType.Metro,
                CreatedBy = Fixture.CurrentUserId
            };
            //add
            city.Id = (long)cityRepo.Add(city);

            //delete
            cityRepo.Delete(city.Id, city.VersionNo, Fixture.CurrentUserId);

            //get
            City deletedCity = cityRepo.ReadOne(city.Id);

            Assert.False(deletedCity.IsActive);
        }

        [Fact]
        public void ReadCompositePrimaryKeys()
        {
            Address address = new Address()
            {
                AddressLine1 = "line 1",
                AddressLine2 = "line 2",
                AddressType = "Home",
                Latitude = "1.1",
                Longitude = "1.2",
                Town = "Ahmedabad"
            };

            Repository<Address> addRepo = new Repository<Address>(Fixture.Connection);
            long id = (long)addRepo.Add(address);

            //to read we need address object search as we have composite primary key
            address = new Address()
            {
                Id = id,
                AddressType = "Home"
            };
            Assert.Equal("line 1", addRepo.ReadOne<string>(address, "AddressLine1"));
        }

        [Fact]
        public void ReadOneQuery()
        {
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);

            City city = new City()
            {
                Name = "ReadTests.ReadOneQuery",
                State = "RQ",
                CountryId = 1,
                Longitude = 1m,
                Latitude = 1m,
                CityType = EnumCityType.Metro,
                CreatedBy = Fixture.CurrentUserId
            };
            //add
            city.Id = (long)cityRepo.Add(city);
            
            City cityResult = cityRepo.ReadOne("select top 1 * from city WHERE id=" + city.Id);

            Assert.Equal("ReadTests.ReadOneQuery", cityResult.Name);
        }
    }
}
