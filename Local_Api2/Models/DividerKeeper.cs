using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Local_Api2.Models
{
    public class DividerKeeper
    {
        public List<DividerItem> Items { get; set; }
        public int Week { get; set; }
        public int Year { get; set; }

        public DividerKeeper()
        {
            Items = new List<DividerItem>();
        }
    }
}