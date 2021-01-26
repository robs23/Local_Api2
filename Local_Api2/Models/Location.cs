using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Local_Api2.Models
{
    public class Location
    {
        public string L { get; set; }
        public List<ProductionPlanItem> Parts { get; set; } // parts of productionPlanItem = parts of production operations
    }
}