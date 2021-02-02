using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Local_Api2.Models
{
    public class VirtualTruck
    {
        public string L { get; set; }
        public List<Location> Locations { get; set; }
        public DateTime ProductionStart { get; set; }
        public DateTime ProductionEnd { get; set; }
        public int TotalPallets { get; set; }

        public VirtualTruck()
        {
            Locations = new List<Location>();
        }
    }
}