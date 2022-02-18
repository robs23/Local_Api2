using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Local_Api2.Models
{
    public class Operation2Product
    {
        public int OPERATION_ID { get; set; }
        public string OPERATION_NR { get; set; }
        public int PRODUCT_ID { get; set; }
        public string PRODUCT_NR { get; set; }
        public string SUB_PROD_TYPE { get; set; }
    }
}