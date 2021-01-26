using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations.Model;
using System.Linq;
using System.Web;

namespace Local_Api2.Models
{
    public class ProductMachineEfficiency
    {
        public int MACHINE_ID { get; set; }
        public int PRODUCT_ID { get; set; }
        public int EFFICIENCY { get; set; }
        public int MAX_EFFICIENCY { get; set; }
    }
}