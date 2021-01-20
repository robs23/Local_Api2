using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Local_Api2.Models
{
    public class StockPallet
    {
        public string LOADUNIT_NR { get; set; }
        public string SP_NR { get; set; }
        public long PRODUCT_ID { get; set; }
        public string PRODUCT_NR { get; set; }
        public string NAME { get; set; }
        public string SERIAL_NR { get; set; }
        public DateTime DATE_EXPIRE { get; set; }
        public long BU_QUANTITY { get; set; }
        public int STATUS_QUALITY { get; set; }
        public DateTime C_DATE { get; set; }
    }
}