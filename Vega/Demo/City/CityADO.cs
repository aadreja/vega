using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Vega;

namespace Demo.City
{
    public class CityADO
    {

        public int Add(City city)
        {
            using (Npgsql.NpgsqlConnection con = new Npgsql.NpgsqlConnection(ConfigurationManager.ConnectionStrings["test"].ConnectionString))
            {
                con.Open();

                IDbCommand cmd = con.CreateCommand();

                cmd.CommandText = $@"INSERT INTO prompt.citynew(cityname,country,region,longitude,latitude,continent,accentcity,versionno,isactive,createdon,createdby,updatedon,updatedby)
                                        VALUES(@cityname,@country,@region,@longitude,@latitude,@continent,@accentcity,@versionno,@isactive,now(),@createdby,now(),@updatedby)";

                cmd.AddInParameter("@cityname", DbType.String, city.CityName);
                cmd.AddInParameter("@country", DbType.String, city.Country);
                cmd.AddInParameter("@region", DbType.String, city.Region);
                cmd.AddInParameter("@longitude", DbType.Decimal, city.Longitude);
                cmd.AddInParameter("@latitude", DbType.Decimal, city.Latitude);
                cmd.AddInParameter("@continent", DbType.Int32, city.Continent);
                cmd.AddInParameter("@accentcity", DbType.String, city.AccentCity);
                cmd.AddInParameter("@versionno", DbType.Int32, 1);
                cmd.AddInParameter("@isactive", DbType.Boolean, true);
                cmd.AddInParameter("@createdby", DbType.Int32, 1);
                cmd.AddInParameter("@updatedby", DbType.Int32, 1);

                int rdr = cmd.ExecuteNonQuery();

                con.Close();
                con.Dispose();
            }

            return 1;
        }

        public List<City> ReadAll(int count)
        {
            List<City> lstCitys = new List<City>();
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["test"].ConnectionString))
            {
                con.Open();

                SqlCommand cmd = con.CreateCommand();
                cmd.CommandText = $"SELECT {(count > 0 ? "TOP " + count : "")} cityid, cityname, country, region, longitude, latitude,continent from city";
                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    lstCitys.Add(ReaderToObj(rdr));
                }

                con.Close();
                con.Dispose();
            }
            return lstCitys;
        }

        private City ReaderToObj(IDataReader rdr)
        {
            City city = new City();

            int index = -1;
            object value = null;

            try
            {
                index = 0;
                value = rdr[index];
                //if (!(value is DBNull))
                    //city.CityId = (Guid)value;

                index = 1;
                value = rdr[index];
                if (!(value is DBNull))
                    city.CityName = (string)value;

                index = 2;
                value = rdr[index];
                if (!(value is DBNull))
                    city.Country = (string)value;

                index = 3;
                value = rdr[index];
                if (!(value is DBNull))
                    city.Region = (string)value;

                index = 4;
                value = rdr[index];
                if (!(value is DBNull))
                    city.Longitude = (decimal)value;

                index = 5;
                value = rdr[index];
                if (!(value is DBNull))
                    city.Latitude = (decimal)value;

                //index = 6;
                //value = rdr[index];
                //if (!(value is DBNull))
                //    city.Continent = (EnumContinent)(Int16)value;

            }
            catch(Exception ex)
            {
                ThrowDataException(ex, index, rdr, value);
            }
            return city;
        }

        public static void ThrowDataException(Exception ex, int index, IDataReader reader, object value)
        {
            Exception toThrow;
            try
            {
                string name = "(n/a)", formattedValue = "(n/a)";
                if (reader != null && index >= 0 && index < reader.FieldCount)
                {
                    name = reader.GetName(index);
                    try
                    {
                        if (value == null || value is DBNull)
                        {
                            formattedValue = "<null>";
                        }
                        else
                        {
                            formattedValue = Convert.ToString(value);
                        }
                    }
                    catch (Exception valEx)
                    {
                        formattedValue = valEx.Message;
                    }
                }
                toThrow = new DataException($"Error parsing column {index} ({name}={formattedValue})", ex);
            }
            catch
            { 
                toThrow = new DataException(ex.Message, ex);
            }
            throw toThrow;
        }
    }
}
