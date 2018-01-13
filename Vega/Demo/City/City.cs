using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vega;

namespace Demo.City
{
    public enum EnumContinent
    {
        af=1,
        ae=2,
        ad=3
    }

    [Table("prompt","citynew", NeedsHistory = false)]
    public class City : EntityBase
    {

        [PrimaryKey(true)]
        public long CityId { get; set; }

        public string CityName { get; set; }

        public string Country { get; set; }

        public string AccentCity { get; set; }

        public string Region { get; set; }

        public decimal Latitude { get; set; }

        public decimal Longitude { get; set; }

        public EnumContinent Continent { get; set; }

    }

    [Table("prompt", "citynew", NeedsHistory = true)]
    public class CityNew : EntityBase
    {

        [PrimaryKey]
        public Guid CityId { get; set; }

        public string CityName { get; set; }

        public string Country { get; set; }

        public string AccentCity { get; set; }

        public string Region { get; set; }

        public decimal Latitude { get; set; }

        public decimal Longitude { get; set; }

        public EnumContinent Continent { get; set; }

    }
}
