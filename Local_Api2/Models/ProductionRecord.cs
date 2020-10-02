using Microsoft.ApplicationInsights.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Local_Api2.Models
{
    public class ProductionRecord
    {
        public int ProductId { get; set; }
        public string ProductNumber { get; set; }
        public double Quantity { get; set; }
        public double QuantityKg { get; set; }
        public DateTime StartedOn { get; set; }
        public DateTime? FinishedOn { get; set; }

    }
}