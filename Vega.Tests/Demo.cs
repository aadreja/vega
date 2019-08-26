using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Vega.Tests
{
    /*
    public class AccountBalance
    {
        public string AccountName { get; set; }

        public decimal LastYearBalance { get; set;}
        public decimal CurrentYearBalance { get; set; }
    }

    public class Demo
    {
        //show previous year records in balance sheet

        public List<AccountBalance> GetPreviousYearRecords()
        {
            return null;
        }  
    }

    public class TestDemo
    {

        [Fact]
        public void TestBalanceSheetWithPreviousYearData()
        {
            //add account entries in last year with different account type

            //add account entries in current year with different account type

            //balance transfer


            List<AccountBalance> shouldReturn = new List<AccountBalance>()
            {
                new AccountBalance
                {
                    AccountName ="Cash",
                    LastYearBalance = 2000,
                    CurrentYearBalance = 1000
                },
                new AccountBalance
                {
                    AccountName ="Bank",
                    LastYearBalance = 5000,
                    CurrentYearBalance = 5000
                },
            };


            //check
            Demo demo = new Demo();
            List<AccountBalance> haveReturned = demo.GetPreviousYearRecords();


            Assert.Equal(shouldReturn.Count, haveReturned.Count);
            Assert.Equal(shouldReturn.Sum(s => s.CurrentYearBalance), haveReturned.Sum(s => s.CurrentYearBalance));
            Assert.Equal(shouldReturn.Sum(s => s.LastYearBalance), haveReturned.Sum(s => s.LastYearBalance));

        }



    }
    */
}
