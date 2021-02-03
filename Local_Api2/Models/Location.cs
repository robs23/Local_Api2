using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls.WebParts;

namespace Local_Api2.Models
{
    public class Location
    {
        public string L { get; set; }
        public string ShipmentGroupName { get; set; }
        public DateTime ProductionStart { get; set; }
        public DateTime ProductionEnd { get; set; }
        public double TotalPallets { get; set; }
        public double PalletsBeforeWeekend { get; set; }

        public List<ProductionPlanItem> Parts { get; set; } // parts of productionPlanItem = parts of production operations
        public Location()
        {
            Parts = new List<ProductionPlanItem>();
        }

        public void Compose()
        {
            ProductionStart = Parts.Min(p => p.BEGIN_DATE);
            ProductionEnd = Parts.Max(p => p.END_DATE);
            TotalPallets = Parts.Sum(p => p.PAL);
        }
    }
}