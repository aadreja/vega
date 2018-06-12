using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using Dapper;
//using BenchmarkDotNet.Attributes;

namespace Vega.SampleApp
{
    public interface IPerfTest
    {
        long InsertTest(int count);
        long UpdateTest(int count);
        long SelectTest(int count);
        long SelectListTest(int count);
    }




    public class PerformanceTest
    {
        ConsoleColor defaultColor = Console.ForegroundColor;

        public static string ConString = "Server=.;Initial Catalog=tempdb;Integrated Security=true;";

        public void Run()
        {

            CreateTable();

            int iteration = 5;
            int count = 1000;

            Dictionary<string, Dictionary<int, long>> insertTimings = new Dictionary<string, Dictionary<int, long>>();
            Dictionary<string, Dictionary<int, long>> updateTimings = new Dictionary<string, Dictionary<int, long>>();
            Dictionary<string, Dictionary<int, long>> selectTimings = new Dictionary<string, Dictionary<int, long>>();
            Dictionary<string, Dictionary<int, long>> selectListTimings = new Dictionary<string, Dictionary<int, long>>();

            insertTimings.Add("ADO", new Dictionary<int, long>());
            insertTimings.Add("Vega", new Dictionary<int, long>());
            insertTimings.Add("Dapper", new Dictionary<int, long>());

            updateTimings.Add("ADO", new Dictionary<int, long>());
            updateTimings.Add("Vega", new Dictionary<int, long>());
            updateTimings.Add("Dapper", new Dictionary<int, long>());

            selectTimings.Add("ADO", new Dictionary<int, long>());
            selectTimings.Add("Vega", new Dictionary<int, long>());
            selectTimings.Add("Dapper", new Dictionary<int, long>());

            selectListTimings.Add("ADO", new Dictionary<int, long>());
            selectListTimings.Add("Vega", new Dictionary<int, long>());
            selectListTimings.Add("Dapper", new Dictionary<int, long>());

            for (int i = 1; i <= iteration; i++)
            {
                WriteLine("Iteration " + i, ConsoleColor.Yellow);

                ADOTest adoTest = new ADOTest();
                VegaTest vegaTest = new VegaTest();
                DapperTest dapperTest = new DapperTest();

                insertTimings["ADO"].Add(i, adoTest.InsertTest(count));
                insertTimings["Vega"].Add(i, vegaTest.InsertTest(count));
                insertTimings["Dapper"].Add(i, dapperTest.InsertTest(count));

                updateTimings["ADO"].Add(i, adoTest.UpdateTest(count));
                updateTimings["Vega"].Add(i, vegaTest.UpdateTest(count));
                updateTimings["Dapper"].Add(i, dapperTest.UpdateTest(count));

                selectTimings["ADO"].Add(i, adoTest.SelectTest(count));
                selectTimings["Vega"].Add(i, vegaTest.SelectTest(count));
                selectTimings["Dapper"].Add(i, dapperTest.SelectTest(count));

                selectListTimings["ADO"].Add(i, adoTest.SelectListTest(count));
                selectListTimings["Vega"].Add(i, vegaTest.SelectListTest(count));
                selectListTimings["Dapper"].Add(i, dapperTest.SelectListTest(count));
            }

            //show results
            Console.Clear();
            WriteLine($"Results for {count} records {iteration} iteration", ConsoleColor.Yellow);
            WriteLine("--------" + new string('-',10 * iteration), ConsoleColor.Green);
            Write("Iteration\t", ConsoleColor.Green);
            for (int i = 1; i <= iteration; i++)
            {
                Write($"{i}\t", ConsoleColor.Green);
            }
            Write("Mean\t", ConsoleColor.Green);
            Write("stDEV\t", ConsoleColor.Green);
            WriteLine("");
            WriteLine("--------" + new string('-', 10 * iteration), ConsoleColor.Green);

            WriteStatus("Insert Tests", insertTimings);
            WriteStatus("Update Tests", updateTimings);
            WriteStatus("Select Tests", selectTimings);
            WriteStatus("Select List Tests", selectListTimings);
            
            Console.ReadLine();
        }

        void WriteStatus(string title, Dictionary<string, Dictionary<int, long>> timings)
        {
            WriteLine(title, ConsoleColor.Yellow);
            foreach (var item in timings)
            {
                double mean = item.Value.Values.Average();
                double variance = 0;

                Write(item.Key + "\t\t");
                foreach (var it in item.Value)
                {
                    Write(it.Value + "\t");
                    variance += Math.Pow((it.Value - mean), 2);
                }
                variance = variance / item.Value.Values.Count;

                Write(item.Value.Values.Average() + "\t");
                Write(Math.Sqrt(variance) + "\t");

                WriteLine("");
            }
        }

        static void CreateTable()
        {
            using (SqlConnection con = new SqlConnection(ConString))
            {
                con.Open();

                SqlCommand cmd = con.CreateCommand();

                cmd.CommandType = System.Data.CommandType.Text;
                cmd.CommandText = @"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Employee' AND xtype='U')
                                        CREATE TABLE Employee (EmployeeId int PRIMARY KEY, 
                                            EmployeeName nvarchar(100), 
                                            Location nvarchar(100), 
                                            Department nvarchar(100), 
                                            Designation nvarchar(100), 
                                            DOB datetime, 
                                            CTC decimal)";

                cmd.ExecuteNonQuery();
                con.Close();
            }
        }

        public static void TruncateTable()
        {
            using (SqlConnection con = new SqlConnection(ConString))
            {
                con.Open();

                SqlCommand cmd = con.CreateCommand();

                cmd.CommandType = System.Data.CommandType.Text;
                cmd.CommandText = @"TRUNCATE TABLE Employee";

                cmd.ExecuteNonQuery();
                con.Close();
            }
        }

        void Write(string text)
        {
            Console.ForegroundColor = defaultColor;
            Console.Write(text);
        }

        void Write(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(text);
        }

        void WriteLine(string text)
        {
            Console.ForegroundColor = defaultColor;
            Console.WriteLine(text);
        }

        void WriteLine(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
        }
    }

    [Table(NeedsHistory =false, NoCreatedBy =true, NoCreatedOn =true, NoUpdatedBy =true, NoUpdatedOn =true, NoVersionNo =true, NoIsActive =true)]
    public class Employee : EntityBase
    {
        [PrimaryKey]
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string Location { get; set; }
        public string Department { get; set; }
        public string Designation { get; set; }
        public DateTime DOB { get; set; }
        public decimal CTC { get; set; }
    }

    public class ADOTest : IPerfTest
    {
        public long InsertTest(int count)
        {
            PerformanceTest.TruncateTable();

            Stopwatch w = new Stopwatch();

            using (SqlConnection con = new SqlConnection(PerformanceTest.ConString))
            {
                con.Open();
                w.Start();
                for (int i = 1; i <= count; i++)
                {
                    Employee emp = new Employee()
                    {
                        EmployeeId = i,
                        EmployeeName = "Employee " + i,
                        Location = "Location " + i,
                        Department = "Department " + i,
                        Designation = "Designation " + i,
                        DOB = new DateTime(1978, (int)Math.Ceiling(( Convert.ToDecimal(i) / Convert.ToDecimal(count))*12m), (int)Math.Ceiling((Convert.ToDecimal(i) / Convert.ToDecimal(count)) * 25m)),
                        CTC = i * 1000m
                    };

                    SqlCommand cmd = con.CreateCommand();
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.CommandText = @"INSERT INTO Employee (EmployeeId, EmployeeName, Location, Department, Designation, DOB, CTC)
                                        VALUES(@EmployeeId, @EmployeeName, @Location, @Department, @Designation, @DOB, @CTC)";

                    cmd.Parameters.AddWithValue("EmployeeId", emp.EmployeeId);
                    cmd.Parameters.AddWithValue("EmployeeName", emp.EmployeeName);
                    cmd.Parameters.AddWithValue("Location", emp.Location);
                    cmd.Parameters.AddWithValue("Department", emp.Department);
                    cmd.Parameters.AddWithValue("Designation", emp.Designation);
                    cmd.Parameters.AddWithValue("DOB", emp.DOB);
                    cmd.Parameters.AddWithValue("CTC", emp.CTC);

                    cmd.ExecuteNonQuery();
                }
                w.Stop();
                con.Close();
            }
            return w.ElapsedMilliseconds;
        }

        public long SelectListTest(int count)
        {
            Stopwatch w = new Stopwatch();

            List<Employee> employeeList = new List<Employee>();

            using (SqlConnection con = new SqlConnection(PerformanceTest.ConString))
            {
                con.Open();
                w.Start();

                SqlCommand cmd = con.CreateCommand();
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.CommandText = $@"SELECT TOP {count} EmployeeId, EmployeeName, Location, Department, Designation, DOB, CTC FROM Employee";
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    while(rdr.Read())
                    {
                        employeeList.Add(new Employee()
                        {
                            EmployeeId = rdr.GetInt32(0),
                            EmployeeName = rdr.GetString(1),
                            Location = rdr.GetString(2),
                            Department = rdr.GetString(3),
                            Designation = rdr.GetString(4),
                            DOB = rdr.GetDateTime(5),
                            CTC = rdr.GetDecimal(6)
                        });
                    }
                }
                w.Stop();
                con.Close();
            }
            return w.ElapsedMilliseconds;
        }

        public long SelectTest(int count)
        {
            Stopwatch w = new Stopwatch();

            using (SqlConnection con = new SqlConnection(PerformanceTest.ConString))
            {
                con.Open();
                w.Start();
                for (int i = 1; i <= count; i++)
                {
                    SqlCommand cmd = con.CreateCommand();
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.CommandText = @"SELECT EmployeeId, EmployeeName, Location, Department, Designation, DOB, CTC FROM Employee WHERE EmployeeId=@EmployeeId";
                    cmd.Parameters.AddWithValue("EmployeeId", i);
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            Employee emp = new Employee()
                            {
                                EmployeeId = rdr.GetInt32(0),
                                EmployeeName = rdr.GetString(1),
                                Location = rdr.GetString(2),
                                Department = rdr.GetString(3),
                                Designation = rdr.GetString(4),
                                DOB = rdr.GetDateTime(5),
                                CTC = rdr.GetDecimal(6)
                            };
                        }
                    }
                }
                w.Stop();
                con.Close();
            }
            return w.ElapsedMilliseconds;
        }

        public long UpdateTest(int count)
        {
            Stopwatch w = new Stopwatch();

            using (SqlConnection con = new SqlConnection(PerformanceTest.ConString))
            {
                con.Open();
                w.Start();
                for (int i = 1; i <= count; i++)
                {
                    Employee emp = new Employee()
                    {
                        EmployeeId = i,
                        EmployeeName = "Update Employee " + i,
                        Location = "Location " + i,
                        Department = "Department " + i,
                        Designation = "Designation " + i,
                        DOB = new DateTime(1978, (int)Math.Ceiling((Convert.ToDecimal(i) / Convert.ToDecimal(count)) * 12m), (int)Math.Ceiling((Convert.ToDecimal(i) / Convert.ToDecimal(count)) * 25m)),
                        CTC = i * 1000m
                    };

                    SqlCommand cmd = con.CreateCommand();
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.CommandText = @"UPDATE Employee SET EmployeeName=@EmployeeName, 
                                            Location=@Location, Department=@Department, 
                                            Designation=@Designation, DOB=@DOB, CTC=@CTC
                                        WHERE EmployeeId=@EmployeeId";

                    cmd.Parameters.AddWithValue("EmployeeId", emp.EmployeeId);
                    cmd.Parameters.AddWithValue("EmployeeName", emp.EmployeeName);
                    cmd.Parameters.AddWithValue("Location", emp.Location);
                    cmd.Parameters.AddWithValue("Department", emp.Department);
                    cmd.Parameters.AddWithValue("Designation", emp.Designation);
                    cmd.Parameters.AddWithValue("DOB", emp.DOB);
                    cmd.Parameters.AddWithValue("CTC", emp.CTC);

                    cmd.ExecuteNonQuery();
                }
                w.Stop();
                con.Close();
            }
            return w.ElapsedMilliseconds;
        }
    }

    public class VegaTest : IPerfTest
    {
        public long InsertTest(int count)
        {
            PerformanceTest.TruncateTable();

            Stopwatch w = new Stopwatch();

            using (SqlConnection con = new SqlConnection(PerformanceTest.ConString))
            {
                con.Open();
                w.Start();
                for (int i = 1; i <= count; i++)
                {
                    Employee emp = new Employee()
                    {
                        EmployeeId = i,
                        EmployeeName = "Employee " + i,
                        Location = "Location " + i,
                        Department = "Department " + i,
                        Designation = "Designation " + i,
                        DOB = new DateTime(1978, (int)Math.Ceiling((Convert.ToDecimal(i) / Convert.ToDecimal(count)) * 12m), (int)Math.Ceiling((Convert.ToDecimal(i) / Convert.ToDecimal(count)) * 25m)),
                        CTC = i * 1000m
                    };

                    Repository<Employee> repository = new Repository<Employee>(con);
                    repository.Add(emp);
                }
                w.Stop();
                con.Close();
            }
            return w.ElapsedMilliseconds;
        }

        public long SelectListTest(int count)
        {
            Stopwatch w = new Stopwatch();

            List<Employee> employeeList = new List<Employee>();

            using (SqlConnection con = new SqlConnection(PerformanceTest.ConString))
            {
                con.Open();
                w.Start();

                Repository<Employee> repository = new Repository<Employee>(con);
                employeeList = repository.ReadAll().ToList();

                w.Stop();
                con.Close();
            }
            return w.ElapsedMilliseconds;
        }

        public long SelectTest(int count)
        {
            Stopwatch w = new Stopwatch();

            using (SqlConnection con = new SqlConnection(PerformanceTest.ConString))
            {
                con.Open();
                w.Start();
                for (int i = 1; i <= count; i++)
                {
                    Repository<Employee> repository = new Repository<Employee>(con);
                    repository.ReadOne(i);
                }
                w.Stop();
                con.Close();
            }
            return w.ElapsedMilliseconds;
        }

        public long UpdateTest(int count)
        {
            Stopwatch w = new Stopwatch();

            using (SqlConnection con = new SqlConnection(PerformanceTest.ConString))
            {
                con.Open();
                w.Start();
                for (int i = 1; i <= count; i++)
                {
                    Employee emp = new Employee()
                    {
                        EmployeeId = i,
                        EmployeeName = "Update Employee " + i,
                        Location = "Location " + i,
                        Department = "Department " + i,
                        Designation = "Designation " + i,
                        DOB = new DateTime(1978, (int)Math.Ceiling((Convert.ToDecimal(i) / Convert.ToDecimal(count)) * 12m), (int)Math.Ceiling((Convert.ToDecimal(i) / Convert.ToDecimal(count)) * 25m)),
                        CTC = i * 1000m
                    };

                    Repository<Employee> repository = new Repository<Employee>(con);
                    repository.Update(emp);
                }
                w.Stop();
                con.Close();
            }
            return w.ElapsedMilliseconds;
        }
    }

    public class DapperTest : IPerfTest
    {
        public long InsertTest(int count)
        {
            PerformanceTest.TruncateTable();

            Stopwatch w = new Stopwatch();

            using (SqlConnection con = new SqlConnection(PerformanceTest.ConString))
            {
                con.Open();
                w.Start();
                for (int i = 1; i <= count; i++)
                {
                    Employee emp = new Employee()
                    {
                        EmployeeId = i,
                        EmployeeName = "Employee " + i,
                        Location = "Location " + i,
                        Department = "Department " + i,
                        Designation = "Designation " + i,
                        DOB = new DateTime(1978, (int)Math.Ceiling((Convert.ToDecimal(i) / Convert.ToDecimal(count)) * 12m), (int)Math.Ceiling((Convert.ToDecimal(i) / Convert.ToDecimal(count)) * 25m)),
                        CTC = i * 1000m
                    };

                    con.Execute(@"INSERT Employee(EmployeeId, EmployeeName, Location, Department, Designation, DOB, CTC)
                                        VALUES(@EmployeeId, @EmployeeName, @Location, @Department, @Designation, @DOB, @CTC)", emp);
                }
                w.Stop();
                con.Close();
            }
            return w.ElapsedMilliseconds;
        }

        public long SelectListTest(int count)
        {
            Stopwatch w = new Stopwatch();

            List<Employee> employeeList = new List<Employee>();

            using (SqlConnection con = new SqlConnection(PerformanceTest.ConString))
            {
                con.Open();
                w.Start();

                employeeList = con.Query<Employee>("SELECT * FROM Employee").ToList();

                w.Stop();
                con.Close();
            }
            return w.ElapsedMilliseconds;
        }

        public long SelectTest(int count)
        {
            Stopwatch w = new Stopwatch();

            using (SqlConnection con = new SqlConnection(PerformanceTest.ConString))
            {
                con.Open();
                w.Start();
                for (int i = 1; i <= count; i++)
                {
                    con.QueryFirst<Employee>("SELECT * FROM Employee WHERE EmployeeId=@EmployeeId", new { EmployeeId=i });
                }
                w.Stop();
                con.Close();
            }
            return w.ElapsedMilliseconds;
        }

        public long UpdateTest(int count)
        {
            Stopwatch w = new Stopwatch();

            using (SqlConnection con = new SqlConnection(PerformanceTest.ConString))
            {
                con.Open();
                w.Start();
                for (int i = 1; i <= count; i++)
                {
                    Employee emp = new Employee()
                    {
                        EmployeeId = i,
                        EmployeeName = "Update Employee " + i,
                        Location = "Location " + i,
                        Department = "Department " + i,
                        Designation = "Designation " + i,
                        DOB = new DateTime(1978, (int)Math.Ceiling((Convert.ToDecimal(i) / Convert.ToDecimal(count)) * 12m), (int)Math.Ceiling((Convert.ToDecimal(i) / Convert.ToDecimal(count)) * 25m)),
                        CTC = i * 1000m
                    };

                    con.Execute(@"UPDATE Employee SET EmployeeName = @EmployeeName,
                                            Location = @Location, Department = @Department,
                                            Designation = @Designation, DOB = @DOB, CTC = @CTC
                                        WHERE EmployeeId = @EmployeeId", emp);
                }
                w.Stop();
                con.Close();
            }
            return w.ElapsedMilliseconds;
        }
    }

    //public class BenchmarkTest
    //{
    //    int count = 1000;
    //    ADOTest adoTest;
    //    VegaTest vegaTest;
    //    DapperTest dapperTest;

    //    public BenchmarkTest()
    //    {
    //        adoTest = new ADOTest();
    //        vegaTest = new VegaTest();
    //        dapperTest = new DapperTest();
    //    }

    //    [Benchmark]
    //    public void InsertADO()
    //    {
    //        adoTest.InsertTest(count);
    //    }

    //    [Benchmark]
    //    public void UpdateADO()
    //    {
    //        adoTest.InsertTest(count);
    //    }

    //    [Benchmark]
    //    public void SelectADO()
    //    {
    //        adoTest.SelectTest(count);
    //    }

    //    [Benchmark]
    //    public void SelectListADO()
    //    {
    //        adoTest.SelectListTest(count);
    //    }

    //    [Benchmark]
    //    public void InsertVega()
    //    {
    //        vegaTest.InsertTest(count);
    //    }

    //    [Benchmark]
    //    public void UpdateVega()
    //    {
    //        vegaTest.InsertTest(count);
    //    }

    //    [Benchmark]
    //    public void SelectVega()
    //    {
    //        vegaTest.SelectTest(count);
    //    }

    //    [Benchmark]
    //    public void SelectListVega()
    //    {
    //        vegaTest.SelectListTest(count);
    //    }

    //    [Benchmark]
    //    public void InsertDapper()
    //    {
    //        dapperTest.InsertTest(count);
    //    }

    //    [Benchmark]
    //    public void UpdateDapper()
    //    {
    //        dapperTest.InsertTest(count);
    //    }

    //    [Benchmark]
    //    public void SelectDapper()
    //    {
    //        dapperTest.SelectTest(count);
    //    }

    //    [Benchmark]
    //    public void SelectListDapper()
    //    {
    //        dapperTest.SelectListTest(count);
    //    }

    //}
}
