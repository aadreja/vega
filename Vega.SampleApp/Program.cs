using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace Vega.SampleApp
{
    class Program
    {
        static ConsoleColor defaultColor;

        static void Main(string[] args)
        {
            defaultColor = Console.ForegroundColor;

            WriteLine("-------------------------------------------", ConsoleColor.Green);
            WriteLine("Vega(Fastest .net ORM) - Sample Application", ConsoleColor.Green);
            WriteLine("-------------------------------------------", ConsoleColor.Green);

            WriteLine("Creating Database with Sample Data");

            CreateDBWithSampleData();

            //Set Session
            Common.Session = new Session(1);

            long id = InsertSample();
            UpdateSample(id);
            ReadById(id);
            DeleteSample(id);
            Read();
            ReadPaged();
            AuditTrial(id);

            Console.ReadKey();
        }

        static void AuditTrial(long id)
        {
            WriteLine("Read Audit Trial", ConsoleColor.Green);

            using (SqlConnection con = new SqlConnection(Common.ConnectionString))
            {
                Repository<City> cityRepo = new Repository<City>(con, Common.Session);

                List<City> cities = cityRepo.ReadHistory(id).ToList();

                WriteLine($"Found {cities.Count} Records in Audit Trial", ConsoleColor.Green);
                foreach (City city in cities)
                {
                    WriteLine($"City Id={city.Id},Name={city.Name},Type={city.CityType},IsActive={city.IsActive},ModifiedOn={city.UpdatedOn},ModifiedBy={city.UpdatedBy},Operation={city.Operation}");
                }
            }
            WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        static void ReadPaged()
        {
            WriteLine("Paging Sample", ConsoleColor.Green);

            using (SqlConnection con = new SqlConnection(Common.ConnectionString))
            {
                Repository<City> cityRepo = new Repository<City>(con, Common.Session);

                List<City> cities = cityRepo.ReadAllPaged("name", 1, 5).ToList();

                WriteLine($"Found {cities.Count} Records in Page 1", ConsoleColor.Green);
                foreach (City city in cities)
                {
                    WriteLine($"City Id={city.Id},Name={city.Name},Type={city.CityType},IsActive={city.IsActive}");
                }

                cities = cityRepo.ReadAllPaged("name", 2, 5).ToList();
                WriteLine($"Found {cities.Count} Records in Page 2", ConsoleColor.Green);
                foreach (City city in cities)
                {
                    WriteLine($"City Id={city.Id},Name={city.Name},Type={city.CityType},IsActive={city.IsActive}");
                }

                cities = cityRepo.ReadAllPaged("name", 1, 5, null, "State=@State", new { State = "GU" }).ToList();
                WriteLine($"Found {cities.Count} Records where State=GU in Page 1", ConsoleColor.Green);
                foreach (City city in cities)
                {
                    WriteLine($"City Id={city.Id},Name={city.Name},State={city.State},IsActive={city.IsActive}");
                }
            }

            WriteLine("Paging Sample No Offset", ConsoleColor.Green);

            using (SqlConnection con = new SqlConnection(Common.ConnectionString))
            {
                Repository<City> cityRepo = new Repository<City>(con, Common.Session);

                List<City> cities = cityRepo.ReadAllPaged("name", 5, PageNavigationEnum.First).ToList();

                WriteLine($"Found {cities.Count} Records in First Page", ConsoleColor.Green);
                foreach (City city in cities)
                {
                    WriteLine($"City Id={city.Id},Name={city.Name},Type={city.CityType},IsActive={city.IsActive}");
                }

                cities = cityRepo.ReadAllPaged("name", 5, PageNavigationEnum.Next, null, null, new object[] { cities.LastOrDefault().Name }, cities.LastOrDefault().Id).ToList();
                WriteLine($"Found {cities.Count} Records in Next Page", ConsoleColor.Green);
                foreach (City city in cities)
                {
                    WriteLine($"City Id={city.Id},Name={city.Name},Type={city.CityType},IsActive={city.IsActive}");
                }

                cities = cityRepo.ReadAllPaged("name", 5, PageNavigationEnum.First, null, "State=@State", null, null, new { State = "GU" }).ToList();
                WriteLine($"Found {cities.Count} Records where State=GU in First Page", ConsoleColor.Green);
                foreach (City city in cities)
                {
                    WriteLine($"City Id={city.Id},Name={city.Name},State={city.State},IsActive={city.IsActive}");
                }
            }
            WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        static void Read()
        {
            WriteLine("Read All", ConsoleColor.Green);
            using (SqlConnection con = new SqlConnection(Common.ConnectionString))
            {
                Repository<City> cityRepo = new Repository<City>(con, Common.Session);
                List<City> cities = cityRepo.ReadAll().ToList();

                WriteLine($"Found {cities.Count} Records", ConsoleColor.Green);
                foreach (City city in cities)
                {
                    WriteLine($"City Id={city.Id},Name={city.Name},Type={city.CityType},IsActive={city.IsActive}");
                }

                cities = cityRepo.ReadAll(RecordStatusEnum.Active).ToList();
                WriteLine($"Found {cities.Count} Active Records", ConsoleColor.Green);
                foreach (City city in cities)
                {
                    WriteLine($"City Id={city.Id},Name={city.Name},Type={city.CityType},IsActive={city.IsActive}");
                }
            }

            WriteLine("Read by Criteria", ConsoleColor.Green);
            using (SqlConnection con = new SqlConnection(Common.ConnectionString))
            {
                Repository<City> cityRepo = new Repository<City>(con, Common.Session);
                List<City> cities = cityRepo.ReadAll(null, "State=@State", new { State = "GU" }).ToList();
                WriteLine($"Found {cities.Count} Records", ConsoleColor.Green);
                foreach (City city in cities)
                {
                    WriteLine($"City Id={city.Id},Name={city.Name},State={city.State},IsActive={city.IsActive}");
                }
            }

            WriteLine("Read Name by Criteria", ConsoleColor.Green);
            using (SqlConnection con = new SqlConnection(Common.ConnectionString))
            {
                Repository<City> cityRepo = new Repository<City>(con, Common.Session);
                List<City> cities = cityRepo.ReadAll("Name", "State=@State", new { State = "GU" }).ToList();
                WriteLine($"Found {cities.Count} Records", ConsoleColor.Green);
                foreach (City city in cities)
                {
                    WriteLine($"Name={city.Name}");
                }
            }

            WriteLine("Exists", ConsoleColor.Green);
            using (SqlConnection con = new SqlConnection(Common.ConnectionString))
            {
                Repository<City> cityRepo = new Repository<City>(con, Common.Session);
                bool isExists = cityRepo.Exists("Name=@Name", new { Name = "Ahmedabad" });
                WriteLine($"Ahmedabad exists {isExists}");

                isExists = cityRepo.Exists("Name=@Name", new { Name = "Dubai" });
                WriteLine($"Dubai exists {isExists}");
            }

            WriteLine("Read one value", ConsoleColor.Green);
            using (SqlConnection con = new SqlConnection(Common.ConnectionString))
            {
                Repository<City> cityRepo = new Repository<City>(con, Common.Session);
                decimal latitude = cityRepo.Query<decimal>("SELECT latitude FROM city WHERE Name=@Name", new { Name = "Ahmedabad" });

                WriteLine($"Latitude of Ahmedabad is {latitude}");
            }

            WriteLine("Count", ConsoleColor.Green);
            using (SqlConnection con = new SqlConnection(Common.ConnectionString))
            {
                Repository<City> cityRepo = new Repository<City>(con, Common.Session);
                long count = cityRepo.Count();// "SELECT latitude FROM city WHERE Name=@Name", new { Name = "Ahmedabad" });

                WriteLine($"Record count is {count}");

                count = cityRepo.Count("State=@State", new { State = "GU" });
                WriteLine($"Record count with State=GU is {count}");
            }

            WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        static long InsertSample()
        {
            WriteLine("Insert Sample", ConsoleColor.Green);
            City city = new City()
            {
                Name = Common.Random(10),
                State = "GU",
                CountryId = 1,
                CityType = EnumCityType.Metro,
                Longitude = 100.23m,
                Latitude = 123.23m
            };

            using (SqlConnection con = new SqlConnection(Common.ConnectionString))
            {
                Repository<City> cityRepo = new Repository<City>(con, Common.Session);
                city.Id = (long)cityRepo.Add(city);
            }
            WriteLine($"City {city.Name} added with Id {city.Id}");

            WriteLine("Press any key to continue...");
            Console.ReadKey();

            return city.Id;
        }

        static void UpdateSample(long id)
        {
            WriteLine("Update Sample", ConsoleColor.Green);

            using (SqlConnection con = new SqlConnection(Common.ConnectionString))
            {
                Repository<City> cityRepo = new Repository<City>(con, Common.Session);

                //Read First
                City city = cityRepo.ReadOne(id);

                city.CityType = EnumCityType.NonMetro;
                cityRepo.Update(city);

                WriteLine($"City {city.Name} updated");
            }
        }

        static void ReadById(long id)
        {
            WriteLine("ReadById Sample", ConsoleColor.Green);

            using (SqlConnection con = new SqlConnection(Common.ConnectionString))
            {
                Repository<City> cityRepo = new Repository<City>(con, Common.Session);
                City city = cityRepo.ReadOne(id);

                WriteLine($"City Id={city.Id},Name={city.Name},Type={city.CityType}");
            }
            WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        static void DeleteSample(long id)
        {
            WriteLine("Delete Sample", ConsoleColor.Green);

            using (SqlConnection con = new SqlConnection(Common.ConnectionString))
            {
                Repository<City> cityRepo = new Repository<City>(con, Common.Session);
                cityRepo.Delete(id);

                WriteLine($"City Id={id} deleted");
            }
            WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        static void ClearDatabase()
        {
            string dbFileName = Environment.CurrentDirectory + "\\" + Common.DBName + ".mdf";
            if (System.IO.File.Exists(dbFileName))
            {
                try
                {
                    using (var con = new SqlConnection(Common.MasterConnectionString))
                    {
                        Repository<User> master = new Repository<User>(con, Common.Session);

                        //close existing connections
                        master.ExecuteNonQuery($"ALTER DATABASE [{Common.DBName}] set single_user with rollback immediate");

                        //drop database
                        master.ExecuteNonQuery($"DROP DATABASE [{Common.DBName}]");
                    }
                }
                catch
                {
                    throw;
                }
            }
        }

        static void CreateDBWithSampleData()
        {
            string dbFileName =  Environment.CurrentDirectory + "\\" + Common.DBName + ".mdf";

            if (!System.IO.File.Exists(dbFileName))
            {
                WriteLine("Creating Database");
                //create database
                try
                {
                    using (var masterCon = new SqlConnection(Common.MasterConnectionString))
                    {
                        Repository<User> master = new Repository<User>(masterCon, Common.Session);
                        master.ExecuteNonQuery($"CREATE DATABASE {Common.DBName} ON (NAME = N'{Common.DBName}', FILENAME = '{dbFileName}')");
                    }
                }
                catch
                {
                    throw;
                }
            }

            WriteLine("Creating missing tables..", ConsoleColor.Green);

            SqlConnection con = new SqlConnection(Common.ConnectionString);

            //Create Tables
            Repository<User> userRepo = new Repository<User>(con, Common.Session);
            if(!userRepo.IsTableExists())
                userRepo.CreateTable();

            Repository<Country> countryRepo = new Repository<Country>(con, Common.Session);
            if (!countryRepo.IsTableExists())
                countryRepo.CreateTable();

            //Create City
            Repository<City> cityRepo = new Repository<City>(con, Common.Session);
            if (!cityRepo.IsTableExists())
                cityRepo.CreateTable();

            //add sample data
            WriteLine("Adding sample data..", ConsoleColor.Green);

            if (!userRepo.Exists("Username=@Username", new { Username="admin"}))
            {
                WriteLine("Adding User...");
                //Create User Entity
                User usr = new User()
                {
                    Username = "admin",
                };
                usr.Id = (short)userRepo.Add(usr);
                WriteLine($"User {usr.Username} added with Id {usr.Id}");
            }

            Country country = countryRepo.ReadOne("Name=@Name", new { Name = "India" });
            if (country == null)
            {
                //Create Country Entity
                country = new Country()
                {
                    Name = "India",
                    Continent = EnumContinent.Asia,
                    Independence = new DateTime(1947, 8, 15)
                };
                country.Id = (int)countryRepo.Add(country);
                WriteLine($"Country {country.Name} added with Id {country.Id}");
            }

            List<City> cities = new List<City>();
            //Create Cities 
            cities.Add(new City()
            {
                Name = "Delhi",
                CityType = EnumCityType.Metro,
                CountryId = country.Id,
                State = "DL",
                Longitude = 19.0760m,
                Latitude = 72.8777m
            });
            cities.Add(new City()
            {
                Name = "Mumbai",
                CityType = EnumCityType.Metro,
                CountryId = country.Id,
                State = "MH",
                Longitude = 28.7041m,
                Latitude = 77.1025m
            });
            cities.Add(new City()
            {
                Name = "Ahmedabad",
                CityType = EnumCityType.NonMetro,
                CountryId = country.Id,
                State = "GJ",
                Longitude = 23.0225m,
                Latitude = 72.5714m
            });

            try
            {
                cityRepo.BeginTransaction();
                foreach (City city in cities)
                {
                    if (!cityRepo.Exists("Name=@Name", new { Name = city.Name }))
                    {
                        city.Id = (long)cityRepo.Add(city);
                        WriteLine($"City {city.Name} added with Id {city.Id}");
                    }
                }
                cityRepo.Commit();
            }
            catch
            {
                cityRepo.Rollback();
            }
        }

        static void WriteLine(string text)
        {
            Console.ForegroundColor = defaultColor;
            Console.WriteLine(text);
        }

        static void WriteLine(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
        }

    }
}
