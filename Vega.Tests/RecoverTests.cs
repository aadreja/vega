using System;
using System.Data;
using Vega;
using Xunit;

namespace Vega.Tests
{
    [Collection("DMLTest")]
    public class RecoverTests : IClassFixture<DbConnectionFixuture>
    {
        DbConnectionFixuture Fixture;

        public RecoverTests(DbConnectionFixuture fixture)
        {
            Fixture = fixture;
        }

        [Fact]
        public void RecoverWithoutVersionNo()
        {
            City city = new City
            {
                Name = "Ahmedabad",
                State = "GU",
                Latitude = 10.65m,
                Longitude = 11.50m,
                CreatedBy = Fixture.CurrentUserId
            };

            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);
            var id = cityRepo.Add(city);

            //Delete
            cityRepo.Delete(id, Fixture.CurrentUserId);

            //Recover
            Assert.True(cityRepo.Recover(id, Fixture.CurrentUserId));

            //Read
            city = cityRepo.ReadOne(id);

            //Check Status
            Assert.True(city.IsActive);
        }

        [Fact]
        public void RecoverWithVersionNo()
        {
            City city = new City
            {
                Name = "Baroda",
                State = "GU",
                Latitude = 10.65m,
                Longitude = 11.50m,
                CreatedBy = Fixture.CurrentUserId
            };

            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);
            var id = cityRepo.Add(city);

            cityRepo.Delete(id, 1, Fixture.CurrentUserId);

            //Recover
            Assert.True(cityRepo.Recover(id, 2, Fixture.CurrentUserId));

            //Read
            city = cityRepo.ReadOne(id);

            //Check Status
            Assert.True(city.IsActive);
            Assert.Equal(3, city.VersionNo);
        }

        [Fact]
        public void RecoverWithInvalidVersionNo()
        {
            City city = new City
            {
                Name = "Surat",
                State = "GU",
                Latitude = 10.65m,
                Longitude = 11.50m,
                CreatedBy = Fixture.CurrentUserId
            };

            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);
            var id = cityRepo.Add(city);

            cityRepo.Delete(id, 1, Fixture.CurrentUserId);

            Exception ex = Assert.Throws<VersionNotFoundException>(() => cityRepo.Recover(id, 1, Fixture.CurrentUserId));

            Assert.Equal("Record doesn't exists or modified by another user", ex.Message);
        }

        [Fact]
        public void RecoverWithoutVersionNoCompositePrimaryKey()
        {
            Center center = new Center
            {
                CenterName = "Centername",
                CenterType = "Default"
            };

            Repository<Center> centerRepo = new Repository<Center>(Fixture.Connection);
            var id = (int)centerRepo.Add(center);

            centerRepo.Delete( new Center { Id= id, CenterType = "Default" }, Fixture.CurrentUserId);

            Assert.True(centerRepo.Recover(new Center { Id = id, CenterType = "Default" }, Fixture.CurrentUserId));
        }

        [Fact]
        public void RecoverWithVersionNoCompositePrimaryKey()
        {
            Center center = new Center
            {
                CenterName = "Centername",
                CenterType = "Default"
            };

            Repository<Center> centerRepo = new Repository<Center>(Fixture.Connection);
            var id = (int)centerRepo.Add(center);

            centerRepo.Delete(new Center { Id = id, CenterType = "Default" }, 1, Fixture.CurrentUserId);

            Assert.True(centerRepo.Recover(new Center { Id = id, CenterType = "Default" },2, Fixture.CurrentUserId));
        }

        [Fact]
        public void RecoverWithInvalidVersionNoCompositePrimaryKey()
        {
            Center center = new Center
            {
                CenterName = "Centername",
                CenterType = "Default"
            };

            Repository<Center> centerRepo = new Repository<Center>(Fixture.Connection);
            var id = (int)centerRepo.Add(center);

            centerRepo.Delete(new Center { Id = id, CenterType = "Default" }, 1, Fixture.CurrentUserId);

            Exception ex = Assert.Throws<VersionNotFoundException>(() => centerRepo.Recover(new Center { Id = id, CenterType = "Default" }, 5, Fixture.CurrentUserId));

            Assert.Equal("Record doesn't exists or modified by another user", ex.Message);
        }
    }
}
