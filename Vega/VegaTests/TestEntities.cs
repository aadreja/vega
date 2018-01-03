using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vega;

namespace VegaTests
{
    [Table(NeedsHistory = false)]
    public class Country : EntityBase
    {
        [PrimaryKey(true)]
        public long Id { get; set; }
        public string Name { get; set; }
        public string ShortCode { get; set; }
        public DateTime? Independence { get; set; }
    }

    [Table]
    public class City : EntityBase
    {
        [PrimaryKey(true)]
        public long Id { get; set; }
        public string Name { get; set; }
        public string State { get; set; }
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }
        public Int32 CountryId { get; set; }
    }

    [Table(Name = "Users")]
    public class User : EntityBase
    {
        [PrimaryKey]
        public Int16 Id { get; set; }
        public string Username { get; set; }
    }

}
