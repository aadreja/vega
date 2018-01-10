using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;
using Vega;

namespace Demo
{
    class Program
    {

        static Npgsql.NpgsqlConnection con = new Npgsql.NpgsqlConnection(ConfigurationManager.ConnectionStrings["test"].ToString());

        static void Main(string[] args)
        {
            Session.CurrentUserId = 1;

            string datetime = "2018-01-1a 23:07:10";
            DateTime dt = datetime.FromSQLDateTime();

            datetime = "2018-01-10";
            dt = datetime.FromSQLDate();

            StringSplit();

            //KeyIdPerfTestReflectionVsEmit();
            //LoadTestInsert();

            Vega.Repository<City.City> cityRepo = new Vega.Repository<City.City>(con);

            bool result = cityRepo.Delete(29192);

            City.City city = cityRepo.ReadOne(29192);
            city.CityName = "Vega Update 7";
            cityRepo.Update(city);
          
        }

        public static void StringSplit()
        {
            string historyString = "isactive=1,Name=\"So far, =we've been writing regular expressions that partially match pieces across all the text. Sometimes this isn't desirable, imagine for example we wanted to match the word &quot;success&quot;in a log file.We certainly don't want that pattern to match a line that says &quot;Error: unsuccessful operation&quot;! That is why it is often best practice to write as specific regular expressions as possible to ensure that we don't get false positives when matching against real world text.One way to tighten our patterns is to define a pattern that describes both thestart and the end of the line using the special ^ (hat)and $ (dollar sign) metacharacters.In the example above, we can use the pattern ^ success to match only a line that begins with the word &quot;success&quot;, but not the line Error: unsuccessful operation&quot;. And if you combine both the hat and the dollar sign, you create a pattern that matches the whole line completely at the beginning and end.\",State=\"GU\",Longitude=11.50,Latitude=10.65,CountryId=0";

            string regEx = ",(?=(?:(?:[^\"]*\"){2})*[^\"]*$)";
            string regEx1 = "=(?=(?:(?:[^\"]*\"){2})*[^\"]*$)";


            string[] cols = Regex.Split(historyString, regEx);

            foreach (string s in cols)
            {
                string[] val = Regex.Split(s, regEx1);

                Console.WriteLine("{0}={1}", val[0], val[1]);
            }
            Console.ReadLine();
        }

        public static void JsonTest()
        {
            string jsonData = "{ \"CityName\":\"Ahmedabad\",\"Country\":\"India\" }";

            DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(City.City));
            MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonData));

            stream.Position = 0;
            City.City city = (City.City)jsonSerializer.ReadObject(stream);

            Console.WriteLine(string.Concat("Hi ", city.CityName, " " + city.Country));

            Console.ReadLine();
        }

        public static void KeyIdPerfTestReflectionVsEmit()
        {
            int counter = 1000000;

            City.CityNew city = new City.CityNew();
            city.CityId = Guid.NewGuid();

            //Reflection
            //Get
            Stopwatch w = new Stopwatch();
            w.Start();
            for (int i=0; i < counter; i++)
            {
                //var x = city.KeyIdRef;
            }
            Console.WriteLine("Reflection Get: {0} ", w.Elapsed.TotalMilliseconds);
            w.Reset();

            //Set
            w.Start();
            for (int i = 0; i < counter; i++)
            {
                //city.KeyIdRef = Guid.NewGuid();
            }
            Console.WriteLine("Reflection Set: {0}", w.Elapsed.TotalMilliseconds);
            w.Reset();

            //Emit
            //Get
            w.Start();
            for (int i = 0; i < counter; i++)
            {
                var x = city.CityId;
            }
            Console.WriteLine("Emit Get:{0} ", w.Elapsed.TotalMilliseconds);
            w.Reset();

            //Set
            w.Start();
            for (int i = 0; i < counter; i++)
            {
                city.CityId = Guid.NewGuid();
            }
            Console.WriteLine("Emit Set:{0} ", w.Elapsed.TotalMilliseconds);
            w.Reset();

            Console.ReadLine();
        }

        public static void LoadTestInsert()
        {
            City.City city = new City.City
            {
                CityName = "Ahmedabad",
                Country = "IN",
                Region = "AS",
                Latitude = 10.30m,
                Longitude = 10.30m,
                Continent = City.EnumContinent.ad
            };
            city.AccentCity = city.CityName;

            int iteration = 1000;
            List<double> adoTiming = new List<double>();
            List<double> vegaTiming = new List<double>();



            City.CityADO cityAdo = new City.CityADO();
            City.CityRepo cityRepo = new City.CityRepo(con);

            Stopwatch w = new Stopwatch();

            Console.WriteLine("Vega...");
            w = new Stopwatch();
            for (int i = 0; i < iteration; i++)
            {
                city.CityId = 0;
                city.CityName = "Vega" + i;
                city.AccentCity = city.CityName;

                Console.WriteLine("Iteration " + i);
                w.Start();

                cityRepo.Add(city);

                vegaTiming.Add(w.Elapsed.TotalMilliseconds);
                w.Reset();
            }

            Console.WriteLine("ADO...");
            w = new Stopwatch();
            for (int i = 0; i < iteration; i++)
            {
                city.CityId = 0;
                city.CityName = "ADO" + i;
                city.AccentCity = city.CityName;

                Console.WriteLine("Iteration " + i);
                w.Start();
                cityAdo.Add(city);
                adoTiming.Add(w.Elapsed.TotalMilliseconds);
                w.Reset();
            }

            Console.Clear();
            Console.WriteLine("------------------------------");
            Console.WriteLine("#\t Ado\t Vega\t Dapper");
            Console.WriteLine("------------------------------");
            for (int i = 0; i < iteration; i++)
            {
                Console.WriteLine("{0:0}\t {1:00}\t {2:00}", i, adoTiming[i], vegaTiming[i]);
            }
            Console.WriteLine("------------------------------");
            Console.WriteLine("T\t {0:00}\t {1:00}", adoTiming.Skip(1).Sum(), vegaTiming.Skip(1).Sum());
            Console.WriteLine("A\t {0:00}\t {1:00}", adoTiming.Skip(1).Average(), vegaTiming.Skip(1).Average());
            Console.WriteLine("------------------------------");

            Console.Read();
        }

        public static void LoadTestGet()
        {
            int recCount = 100000;
            int iteration = 10;

            //init 1st time
            City.CityADO cityAdo = new City.CityADO();
            //cityAdo.ReadAll(recCount);
            //City.CityDapper cityDapper = new City.CityDapper();
            //cityDapper.ReadAll(recCount);
            City.CityRepo cityRepo = new City.CityRepo(con);
            List<City.City> cities = new List<City.City>();// cityRepo.ReadAll(recCount).ToList();

            City.CityRepo cityRepo1 = new City.CityRepo(con);

            List<double> adoTiming = new List<double>();
            List<double> vegaTiming = new List<double>();
            List<double> dapperTiming = new List<double>();

            Stopwatch w = new Stopwatch();

            Console.WriteLine("ADO...");
            w = new Stopwatch();
            for (int i = 0; i < iteration; i++)
            {
                Console.WriteLine("Iteration " + i);
                w.Start();
                cityAdo.ReadAll(recCount);
                adoTiming.Add(w.Elapsed.TotalMilliseconds);
                w.Reset();
            }

            Console.WriteLine("Vega...");
            w = new Stopwatch();
            for (int i = 0; i < iteration; i++)
            {
                Console.WriteLine("Iteration " + i);
                w.Start();
                List<City.City> city1 = new List<City.City>(); //cityRepo.ReadAll(recCount).ToList();
                vegaTiming.Add(w.Elapsed.TotalMilliseconds);
                w.Reset();
            }

            Console.WriteLine("Dapper...");
            w = new Stopwatch();
            for (int i = 0; i < iteration; i++)
            {
                Console.WriteLine("Iteration " + i);
                w.Start();
                //List<City.City> city1 = cityDapper.ReadAll(recCount);
                dapperTiming.Add(w.Elapsed.TotalMilliseconds);
                w.Reset();
            }

            Console.Clear();
            Console.WriteLine("------------------------------");
            Console.WriteLine("#\t Ado\t Vega\t Dapper");
            Console.WriteLine("------------------------------");
            for (int i = 0; i < iteration; i++)
            {
                Console.WriteLine("{0:0}\t {1:00}\t {2:00}\t {3:00}", i, adoTiming[i], vegaTiming[i], dapperTiming[i]);
            }
            Console.WriteLine("------------------------------");
            Console.WriteLine("T\t {0:00}\t {1:00}\t {2:00}", adoTiming.Skip(1).Sum(), vegaTiming.Skip(1).Sum(), dapperTiming.Skip(1).Sum());
            Console.WriteLine("A\t {0:00}\t {1:00}\t {2:00}", adoTiming.Skip(1).Average(), vegaTiming.Skip(1).Average(), dapperTiming.Skip(1).Average());
            Console.WriteLine("------------------------------");

            Console.Read();
        }

        public static void TestEmit()
        {
            Type adderType = BuildAdderType();

            object addIns = Activator.CreateInstance(adderType);

            object[] addParams = new object[2];

            Console.Write("Enter an integer value: ");
            addParams[0] = (object)Convert.ToInt32(Console.ReadLine());

            Console.Write("Enter another integer value: ");
            addParams[1] = (object)Convert.ToInt32(Console.ReadLine());

            Console.WriteLine("---");

            int adderResult = (int)adderType.InvokeMember("DoAdd",
                            BindingFlags.InvokeMethod,
                            null,
                            addIns,
                            addParams);

            if (adderResult != -1)
            {

                Console.WriteLine("{0} + {1} = {2}", addParams[0], addParams[1], adderResult);

            }
            else
            {

                Console.WriteLine("One of the integers to add was greater than 100!");

            }

            Console.Read();
        }

        public static Type BuildAdderType()
        {

            AppDomain myDomain = Thread.GetDomain();
            AssemblyName myAsmName = new AssemblyName
            {
                Name = "AdderExceptionAsm"
            };
            AssemblyBuilder myAsmBldr = myDomain.DefineDynamicAssembly(myAsmName,
                                          AssemblyBuilderAccess.Run);

            ModuleBuilder myModBldr = myAsmBldr.DefineDynamicModule("AdderExceptionMod");

            TypeBuilder myTypeBldr = myModBldr.DefineType("Adder");

            Type[] adderParams = new Type[] { typeof(int), typeof(int) };

            // This method will add two numbers which are 100 or less. If either of the
            // passed integer vales are greater than 100, it will return the value of -1.

            MethodBuilder adderBldr = myTypeBldr.DefineMethod("DoAdd",
                                    MethodAttributes.Public |
                                       MethodAttributes.Static,
                                       typeof(int),
                                       adderParams);
            ILGenerator adderIL = adderBldr.GetILGenerator();

            // In order to successfully branch, we need to create labels
            // representing the offset IL instruction block to branch to.
            // These labels, when the MarkLabel(Label) method is invoked,
            // will specify the IL instruction to branch to.

            Label failed = adderIL.DefineLabel();
            Label endOfMthd = adderIL.DefineLabel();

            // First, load argument 0 and the integer value of "100" onto the
            // stack. If arg0 > 100, branch to the label "failed", which is marked
            // as the address of the block that loads -1 onto the stack, bypassing
            // the addition.

            adderIL.Emit(OpCodes.Ldarg_0);
            adderIL.Emit(OpCodes.Ldc_I4_S, 100);
            adderIL.Emit(OpCodes.Bgt_S, failed);

            // Now, check to see if argument 1 was greater than 100. If it was,
            // branch to "failed." Otherwise, fall through and perform the addition,
            // branching unconditionally to the instruction at the label "endOfMthd".

            adderIL.Emit(OpCodes.Ldarg_1);
            adderIL.Emit(OpCodes.Ldc_I4_S, 100);
            adderIL.Emit(OpCodes.Bgt_S, failed);

            adderIL.Emit(OpCodes.Ldarg_0);
            adderIL.Emit(OpCodes.Ldarg_1);
            adderIL.Emit(OpCodes.Add_Ovf_Un);
            adderIL.Emit(OpCodes.Br_S, endOfMthd);

            // If this label is branched to (the failure case), load -1 onto the stack
            // and fall through to the return opcode.
            adderIL.MarkLabel(failed);
            adderIL.Emit(OpCodes.Ldc_I4_M1);

            // The end of the method. If both values were less than 100, the
            // correct result will return. If one of the arguments was greater
            // than 100, the result will be -1. 

            adderIL.MarkLabel(endOfMthd);
            adderIL.Emit(OpCodes.Ret);

            return myTypeBldr.CreateType();

        }
    }
}
