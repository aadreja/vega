using Npgsql;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using Vega;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

//https://xunit.github.io/docs/comparisons.html

namespace Vega.Tests
{

    public class DbConnectionFixuture : IDisposable
    {
        public DbConnectionFixuture()
        {

#if PGSQL
            Connection = new NpgsqlConnection("server=localhost;port=5432;Database=postgres;User Id=postgres;Password=postgres;timeout=1024;");
#elif SQLITE
            Connection = new System.Data.SQLite.SQLiteConnection("Data Source=.\\test.db;Version=3;");
#else
            if (IsAppVeyor)
                Connection = new SqlConnection("Server=(local)\\SQL2016;Database=master;User ID=sa;Password=Password12!");
            else
                Connection = new SqlConnection("Data Source=.;Initial Catalog=tempdb;Integrated Security=True");
#endif
            //Configure Vega
            Configuration configuration = new Configuration
            {
                CreatedUpdatedByColumnType = DbType.Int32
            };
            Config.Configure(configuration);

            //Create Required Tables
            Repository<Country> countryRepo = new Repository<Country>(Connection);
            countryRepo.CreateTable();

            Repository<City> cityRepo = new Repository<City>(Connection);
            cityRepo.CreateTable();

            Repository<User> userRepo = new Repository<User>(Connection);
            userRepo.CreateTable();

            Repository<Employee> empRepo = new Repository<Employee>(Connection);
            empRepo.CreateTable();

            Repository<Department> deptRepo = new Repository<Department>(Connection);
            deptRepo.CreateTable();

            Repository<Job> jobRepo = new Repository<Job>(Connection);
            jobRepo.CreateTable();

            Repository<Society> socRepo = new Repository<Society>(Connection);
            socRepo.CreateTable();
        }

        public IDbConnection Connection { get; set; }

        public void Dispose()
        {
            //Drop created tables
            Repository<Country> countryRepo = new Repository<Country>(Connection);
            countryRepo.DropTable();

            Repository<City> cityRepo = new Repository<City>(Connection);
            cityRepo.DropTable();

            Repository<User> userRepo = new Repository<User>(Connection);
            userRepo.DropTable();

            Repository<Employee> empRepo = new Repository<Employee>(Connection);
            empRepo.DropTable();

            Repository<Department> deptRepo = new Repository<Department>(Connection);
            deptRepo.DropTable();

            Repository<Job> jobRepo = new Repository<Job>(Connection);
            jobRepo.DropTable();

            Repository<Society> socRepo = new Repository<Society>(Connection);
            socRepo.DropTable();

            Connection?.Dispose();
        }

        public static bool IsAppVeyor
        {
            get
            {
                if (Environment.GetEnvironmentVariable("isappveyor") != null && Environment.GetEnvironmentVariable("isappveyor") == "1")
                    return true;
                else
                    return false;
            }
        }

        public int CurrentUserId
        {
            get
            {
                return 1;
            }
        }
    }

    public class TestPriorityAttribute : Attribute
    {
        public int Priority { get; set; }
        public TestPriorityAttribute(int Priority)
        {
            this.Priority = Priority;
        }
    }

    public class TestCollectionOrderer : ITestCaseOrderer
    {
        public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases) where TTestCase : ITestCase
        {
            var sortedMethods = new SortedDictionary<int, TTestCase>();

            foreach (TTestCase testCase in testCases)
            {
                IAttributeInfo attribute = testCase.TestMethod.Method.
                GetCustomAttributes((typeof(TestPriorityAttribute)
                .AssemblyQualifiedName)).FirstOrDefault();

                var priority = attribute.GetNamedArgument<int>("Priority");
                sortedMethods.Add(priority, testCase);
            }

            return sortedMethods.Values;
        }
    }

}
