using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum EnumContinent
{
    Asia,
    America,
    Africa
}

namespace Vega.SampleApp
{
    //Entity Class
    [Table(NeedsHistory = true)]
    public class Country : EntityBase
    {
        [PrimaryKey(true)]
        public int Id { get; set; }
        public string Name { get; set; }
        public string ShortCode { get; set; }
        public DateTime Independence { get; set; }
        public EnumContinent Continent { get; set; }
    }
}
