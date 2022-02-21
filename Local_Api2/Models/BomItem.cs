using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Local_Api2.Static;

namespace Local_Api2.Models
{
    public class BomItem
    {
        public int ZfinIndex { get; set; }
        public int Material { get; set; }
        public double? Scrap { get; set; }
        public double? Amount { get; set; }
        public string Unit { get; set; }

        public BomItem()
        {
            
        }
    }
}