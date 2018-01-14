using Vega;
using Xunit;

namespace Vega.Tests
{
    [Collection("DDLTest")]
    public class DDLTests : IClassFixture<DbConnectionFixuture>
    {
        DbConnectionFixuture Fixture;

        public DDLTests(DbConnectionFixuture fixture)
        {
            Fixture = fixture;
        }

        [Fact]
        public void CreateTableNoIdentity()
        {
            EntityCache.Clear(); //clear entity cache
            Repository<Country> countryRepo = new Repository<Country>(Fixture.Connection);
            //Drop table if exists
            countryRepo.DropTable();
            countryRepo.CreateTable();

            Assert.True(countryRepo.IsTableExists());
        }

        [Fact]
        public void CreateTableIdentity()
        {
            EntityCache.Clear(); //clear entity cache
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);
            cityRepo.DropTable();
            cityRepo.CreateTable();
            Assert.True(cityRepo.IsTableExists());
        }

        [Fact]
        public void CreateTableDifferentName()
        {
            EntityCache.Clear(); //clear entity cache
            Repository<User> userRepo = new Repository<User>(Fixture.Connection);
            userRepo.DropTable();
            userRepo.CreateTable();
            Assert.True(userRepo.IsTableExists());
        }

        [Fact]
        public void CreateIndex()
        {
            EntityCache.Clear(); //clear entity cache
            Repository<City> cityRepo = new Repository<City>(Fixture.Connection);
            
            cityRepo.CreateTable();
            cityRepo.CreateIndex("idx_cityname", "countryid,state,name", false);

            Assert.True(cityRepo.IsIndexExists("idx_cityname"));
        }
    }
}
