using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Vega.Tests
{
    [Collection("DMLTest")]
    public class DataSetTests : IClassFixture<DbConnectionFixuture>
    {
        DbConnectionFixuture Fixture;

        public DataSetTests(DbConnectionFixuture fixture)
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
    }
}
