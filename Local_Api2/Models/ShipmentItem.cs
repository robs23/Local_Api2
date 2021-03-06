﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Local_Api2.Models
{
    public class ShipmentItem
    {
        public long DOC_ID { get; set; }
        public long DOC_ITEM_ID { get; set; }
        public long PRODUCT_ID { get; set; }
        public string PRODUCT_NR { get; set; }
        public string NAME { get; set; }
        public long PROD_SERIAL_ID { get; set; }
        public string SERIAL_NR { get; set; }
        public long QUANTITY { get; set; }
        public double WEIGHT { get; set; }
        public double WEIGHT_NETTO { get; set; }
    }
}