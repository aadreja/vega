using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Vega;

namespace Demo.Account
{
    //https://stackoverflow.com/questions/24260673/populating-nullable-type-from-sqldatareader-using-reflection-emit
    //https://msdn.microsoft.com/en-us/library/system.reflection.emit.opcodes(v=vs.110).aspx

    public class AccountRepo : Repository<Account>
    {

        public IEnumerable<Account> GetAll(int count)
        {
            return base.ReadAll(count);
        }
       

    }
}
