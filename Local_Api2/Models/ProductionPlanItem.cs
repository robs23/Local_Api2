using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Local_Api2.Models
{
    public class ProductionPlanItem
    {
        public int SCHEDULING_ID { get; set; }
        public DateTime BEGIN_DATE { get; set; }
        public DateTime END_DATE { get; set; }
        public int WEEK { get; set; }
        public int YEAR { get; set; }
        public DateTime START_DATE { get; set; }
        public DateTime STOP_DATE { get; set; }
        public int MACHINE_ID { get; set; }
        public string MACHINE_NAME { get; set; }
        public string ORDER_NR { get; set; }
        public string OPERATION_NR { get; set; }
        public long PRODUCT_ID { get; set; }
        public string PRODUCT_NR { get; set; }
        public string NAME { get; set; }
        public long QUANTITY { get; set; }
        public double WEIGHT { get; set; }
        public double PAL { get; set; }
        public string PalText { get; set; }

    }
}