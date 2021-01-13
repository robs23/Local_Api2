using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Local_Api2.Models
{
    public class DividerItem
    {
        public long ZfinIndex { get; set; }
        //public List<Tuple<string, int>> Locations { get; set; }
        public List<LocationAmount> Locations { get; set; }

    }
}