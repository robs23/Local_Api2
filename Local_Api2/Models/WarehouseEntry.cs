using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Local_Api2.Models
{
    public class WarehouseEntry
    {
        public long LOADUNIT_ID { get; set; }
        public long PRODUCTION_ID { get; set; } //time between production closures
        public long PRODUCT_ID { get; set; }
        public string PRODUCT_NR { get; set; }
        public string LOADUNIT_NR { get; set; } //handling unit
        public string SERIAL_NR { get; set; }
        public DateTime C_DATE { get; set; }
        public DateTime LM_DATE { get; set; }
        public int QUANTITY { get; set; }
        public double WEIGHT { get; set; }
        public int LENGTH { get; set; }
        public int WIDTH { get; set; }
        public int HEIGHT { get; set; }
        public string STATUS { get; set; }
    }
}