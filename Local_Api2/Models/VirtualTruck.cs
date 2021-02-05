using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Local_Api2.Models
{
    public class VirtualTruck
    {
        public string L { get; set; }
        public List<ProductionPlanItem> Parts { get; set; }
        public DateTime ProductionStart { get; set; }
        public DateTime ProductionEnd { get; set; }
        public double TotalPallets { get; set; } = 0;

        public double Pallets2Full
        {
            get
            {
                if(TotalPallets >= 33)
                {
                    return 0;
                }
                else
                {
                    return 33 - TotalPallets;
                }
            }
        }

        public VirtualTruck()
        {
            Parts = new List<ProductionPlanItem>();
        }

        public void Compose()
        {
            ProductionStart = Parts.Min(p => p.START_DATE);
            ProductionEnd = Parts.Max(p => p.STOP_DATE);
        }
    }
}