using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Dapper;

namespace Demo.City
{
    public class CityDapper
    {
        public List<City> ReadAll(int count)
        {
            using (IDbConnection db = new SqlConnection(ConfigurationManager.ConnectionStrings["test"].ConnectionString))
            {
                return db.Query<City>($"SELECT {(count > 0 ? "TOP " + count : "")} cityid, cityname, country, region, longitude, latitude, continent FROM City").ToList();
            }
        }
    }
}
