using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;

namespace Local_Api2.Models
{
    public class ShipmentGroup
    {
        public int ShipmentGroupId { get; set; }
        public string ShortName { get; set; }
        public string Name { get; set; }
        public List<Location> Members { get; set; }
        public ShipmentGroup()
        {
            Members = new List<Location>();
        }
    }
}