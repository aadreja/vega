using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Vega;

namespace Demo.City
{

    public class CityRepo : Repository<City>
    {
        public CityRepo(IDbConnection con) : base(con) { }
        public CityRepo(IDbTransaction tran) : base(tran) { }

    }
}
