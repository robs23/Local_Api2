using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Local_Api2.Models
{
    public class StockProduct
    {
        public int LOADUNIT_NR { get; set; }
        public long PRODUCT_ID { get; set; }
        public string PRODUCT_NR { get; set; }
        public string NAME { get; set; }
        public long BU_QUANTITY { get; set; }
        public int STATUS_QUALITY { get; set; }
    }
}