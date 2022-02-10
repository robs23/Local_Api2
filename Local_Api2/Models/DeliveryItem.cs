using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Local_Api2.Models
{
    public class DeliveryItem
    {
        public int DeliveryItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductIndex { get; set; }
        public string ProductName { get; set; }
        public DateTime DocumentDate { get; set; }
        public string PurchaseOrder { get; set; }
        public double OrderQuantity { get; set; }
        public double OpenQuantity { get; set; }
        public double ReceivedQuantity { get; set; }
        public double NetPrice { get; set; }
        public DateTime DeliveryDate { get; set; }
        public string Vendor { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}