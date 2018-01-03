using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using Vega;

namespace VegaTests
{
    public class Common
    {
        public static IDbConnection GetConnection()
        {
#if PGSQL
            return cons["pgsql"];
#elif SQLITE
            return cons["sqlite"];
#else
            return cons["mssql"];
#endif
        }

        static Common()
        {
            cons = new Dictionary<string, IDbConnection>
            {
                ["mssql"] = new SqlConnection("Data Source=.;Initial Catalog=tempdb;Integrated Security=True"),
                ["pgsql"] = new NpgsqlConnection("server=localhost;port=5432;Database=postgres;User Id=postgres;Password=postgres;timeout=1024;"),
                ["sqlite"] = new SQLiteConnection("Data Source=.\\test.db;Version=3;")
            };

            //Set session
            Session.CurrentUserId = 1;
        }

        public static Dictionary<string, IDbConnection> cons;

        public static void DropAndCreateTables()
        {
            Repository<City> cityRepo = new Repository<City>(GetConnection());
            cityRepo.DropTable();
            cityRepo.CreateTable();

            Repository<Country> countryRepo = new Repository<Country>(GetConnection());
            countryRepo.DropTable();
            countryRepo.CreateTable();

            Repository<User> userRepo = new Repository<User>(GetConnection());
            userRepo.DropTable();
            userRepo.CreateTable();
        }
    }
}
