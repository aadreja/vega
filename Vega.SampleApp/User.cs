using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vega.SampleApp
{

    [Table(Name = "Users", NoIsActive = true)]
    public class User : EntityBase
    {
        [PrimaryKey(true)]
        [ForeignKey("city", "createdby", true)]
        [ForeignKey("city", "updatedby", true)]
        [ForeignKey("country", "createdby", true)]
        [ForeignKey("country", "updatedby", true)]
        public Int16 Id { get; set; }
        public string Username { get; set; }
    }
}
