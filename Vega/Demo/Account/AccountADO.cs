using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace Demo.Account
{
    public class AccountADO
    {
        public List<Account> ReadAll(int count)
        {
            List<Account> lstAccounts = new List<Account>();
            using (Npgsql.NpgsqlConnection con = new Npgsql.NpgsqlConnection(ConfigurationManager.ConnectionStrings["test"].ConnectionString))
            {
                con.Open();

                Npgsql.NpgsqlCommand cmd = con.CreateCommand();
                cmd.CommandText = $"SELECT accountid, accountname from master.account";
                Npgsql.NpgsqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    lstAccounts.Add(ReaderToObj(rdr));
                }

                con.Close();
                con.Dispose();
            }
            return lstAccounts;
        }

        private Account ReaderToObj(IDataReader rdr)
        {
            Account account = new Account();

            int index = -1;
            object value = null;

            try
            {
                index = 0;
                value = rdr[index];
                if (!(value is DBNull))
                    account.AccountId = (int)value;

                index = 1;
                value = rdr[index];
                if (!(value is DBNull))
                    account.AccountName = (string)value;

                //index = 2;
                //value = rdr[index];
                //if (!(value is DBNull))
                //    account.Country = (string)value;

                //index = 3;
                //value = rdr[index];
                //if (!(value is DBNull))
                //    account.Region = (string)value;

                //index = 4;
                //value = rdr[index];
                //if (!(value is DBNull))
                //    account.Longitude = (decimal)value;

                //index = 5;
                //value = rdr[index];
                //if (!(value is DBNull))
                //    account.Latitude = (decimal)value;

                //index = 6;
                //value = rdr[index];
                //if (!(value is DBNull))
                //    city.Continent = (EnumContinent)(Int16)value;
            }
            catch(Exception ex)
            {
                ThrowDataException(ex, index, rdr, value);
            }
            return account;
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
