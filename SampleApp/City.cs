using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vega;

public enum EnumCityType
{
    Metro = 1,
    NonMetro = 2,
    Town = 3
}

namespace SampleApp
{
    [Table(NeedsHistory = true)]
    public class City : EntityBase
    {
        [PrimaryKey(true)]
        public long Id { get; set; }
        [Column(ColumnDbType = System.Data.DbType.String, Size = 4000)]
        public string Name { get; set; }
        [Column(ColumnDbType = System.Data.DbType.String, Size = 50)]
        public string State { get; set; }
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }
        [IgnoreColumn(false, true, false)]
        public long CountryId { get; set; }
        [IgnoreColumn(true)]
        public string CountryName { get; set; }
        public EnumCityType CityType { get; set; }
    }
}
