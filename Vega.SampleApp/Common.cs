using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vega.SampleApp
{
    public static class Common
    {
        public static Session Session;

        public static string DBName = "vegasample";
        public static string ConnectionString = $"Server=.;Initial Catalog={DBName};Integrated Security=true;";
        public static string MasterConnectionString = $"Server=.;Initial Catalog=master;Integrated Security=true;";

        public static string Random(int length = 8)
        {
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

            var randomString = new StringBuilder();
            var random = new Random();

            for (int i = 0; i < length; i++)
                randomString.Append(chars[random.Next(chars.Length)]);

            return randomString.ToString();
        }
    }
}
