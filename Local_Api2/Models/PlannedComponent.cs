using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Local_Api2.Controllers
{
    public class PlannedComponent
    {
        public DateTime OPERATION_DATE {get; set;} 
        public DateTime OPERATION_DAY {get; set;} 
        public int OPERATION_WEEK {get; set;} 
        public int OPERATION_YEAR {get; set;} 
        public int SHIFT_ID {get; set;} 
        public string SHIFT_NAME {get; set;}
        public string MACHINE_NR {get; set;} 
        public string OPERATION_NR {get; set;} 
        public string OPERATION_TYPE_NAME {get; set;} 
	    public string ORDER_NR {get; set;} 
        public string PRODUCT_NR {get; set;} 
        public string PRODUCT_NAME {get; set;} 
        public string PROD_TYPE {get; set;} 
        public string SUB_PROD_TYPE {get; set;} 
        public string ORDER_TYPE_CODE {get; set;} 
        public string ORDER_TYPE_NAME {get; set;} 
        public string BOM_NR {get; set;} 
        public long PRODUCT_QUANTITY {get; set;} 
        public long PRODUCT_QUANTITY_ALL {get; set;}
        public string PARENT_NR { get; set; }
    }
}