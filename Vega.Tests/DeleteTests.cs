﻿using System;
using System.Data;
using Vega;
using Xunit;

namespace Vega.Tests
{
    [Collection("DMLTest")]
    public class DeleteTests : IClassFixture<DbConnectionFixuture>
    {
        DbConnectionFixuture Fixture;

        public DeleteTests(DbConnectionFixuture fixture)
        {
            Fixture = fixture;
        }

        [Fact]
        public void DeleteWithoutVersionNo()
        {
            City city = new City
            {
                Name = "Ahmedabad",
                State = "GU",
                Latitude = 10.65m,
                Longitude = 11.50m,
                CreatedBy =Fixture.CurrentUserId
            };

            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);
            var id = cityRepo.Add(city);

            Assert.True(cityRepo.Delete(id, Fixture.CurrentUserId));
        }

        [Fact]
        public void DeleteWithVersionNo()
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

            Assert.True(cityRepo.Delete(id, 1, Fixture.CurrentUserId));
        }

        [Fact]
        public void DeleteWithInvalidVersionNo()
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

            Exception ex = Assert.Throws<VersionNotFoundException>(() => cityRepo.Delete(id, 2, Fixture.CurrentUserId));

            Assert.Equal("Record doesn't exists or modified by another user", ex.Message);
        }

        [Fact]
        public void HardDeleteWithoutVersionNo()
        {
            User user = new User
            {
                Username="super",
                CreatedBy = Fixture.CurrentUserId
            };

            Repository<User> userRepo = new Repository<User>(Fixture.Connection);
            var id = userRepo.Add(user);

            Assert.True(userRepo.Delete(id, Fixture.CurrentUserId));
        }

        [Fact]
        public void HardDeleteWithVersionNo()
        {
            User user = new User
            {
                Username = "super",
                CreatedBy = Fixture.CurrentUserId
            };

            Repository<User> userRepo = new Repository<User>(Fixture.Connection);
            var id = userRepo.Add(user);

            Assert.True(userRepo.Delete(id, 1, Fixture.CurrentUserId));
        }

        [Fact]
        public void HardDeleteWithInvalidVersionNo()
        {
            User user = new User
            {
                Username = "super",
                CreatedBy = Fixture.CurrentUserId
            };

            Repository<User> userRepo = new Repository<User>(Fixture.Connection);
            var id = userRepo.Add(user);

            Exception ex = Assert.Throws<VersionNotFoundException>(() => userRepo.Delete(id, 2, Fixture.CurrentUserId));

            Assert.Equal("Record doesn't exists or modified by another user", ex.Message);
        }

        [Fact]
        public void HardDeleteWithoutVersionNoCompositePrimaryKey()
        {
            Address address = new Address
            {
                AddressLine1 = "Line1",
                AddressType="Home",
                CustomerCode = "D0001"
            };

            Repository<Address> addressRepo = new Repository<Address>(Fixture.Connection);
            var id = (long)addressRepo.Add(address);

            Assert.True(addressRepo.Delete( new Address { Id= id, AddressType="Home" }, Fixture.CurrentUserId));
        }

        [Fact]
        public void HardDeleteWithVersionNoCompositePrimaryKey()
        {
            Address address = new Address
            {
                AddressLine1 = "Line1",
                AddressType = "Home",
                CustomerCode = "D0001"
            };

            Repository<Address> addressRepo = new Repository<Address>(Fixture.Connection);
            var id = (long)addressRepo.Add(address);

            Assert.True(addressRepo.Delete(new Address { Id = id, AddressType = "Home" }, 1, Fixture.CurrentUserId));
        }

        [Fact]
        public void HardDeleteWithInvalidVersionNoCompositePrimaryKey()
        {
            Address address = new Address
            {
                AddressLine1 = "Line1",
                AddressType = "Home",
                CustomerCode = "D0001"
            };

            Repository<Address> addressRepo = new Repository<Address>(Fixture.Connection);
            var id = (long)addressRepo.Add(address);

            Exception ex = Assert.Throws<VersionNotFoundException>(() => addressRepo.Delete(new Address { Id = id, AddressType = "Home" }, 2, Fixture.CurrentUserId));

            Assert.Equal("Record doesn't exists or modified by another user", ex.Message);
        }
    }
}
