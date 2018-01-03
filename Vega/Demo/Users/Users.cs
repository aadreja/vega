using System;
using System.Collections.Generic;
using System.Text;
using Vega;

namespace Demo.Users
{
    public class Users : EntityBase
    {

        #region properties

        [PrimaryKey]
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Fullname { get; set; }

        #endregion
    }
}
