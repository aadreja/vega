using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vega;

namespace Demo.Account
{
    public class Account : EntityBase
    {
        [PrimaryKey]
        [Column(Name = "accountid", Title = "Id")]
        public PrimaryKey AccountId { get; set; }

        private string _AccountCode;
        [Column(Name = "accountcode", Title = "Code", IsAllowSearch = true, IsAllowSorting = true, SearchOperator = DbSearchOperator.Like)]
        public string AccountCode { get { return _AccountCode; } set { _AccountCode = value.ToUpper(); } }

        private string _AccoutName;
        [Column(Name = "accountname", Title = "Account Name", IsAllowSearch = true, IsAllowSorting = true, SearchOperator = DbSearchOperator.Like)]
        public string AccountName { get { return _AccoutName; } set { _AccoutName = value.ToUpper(); } }

        [Column(Name = "accountnamelocal", Title = "Account Name", IsAllowSearch = true, IsAllowSorting = true, SearchOperator = DbSearchOperator.Like)]
        public string AccountNameLocal { get; set; }

        public override PrimaryKey KeyId
        {
            get { return AccountId; }
            set { AccountId = value; }
        }
    }
}
