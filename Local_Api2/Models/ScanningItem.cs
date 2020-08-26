using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Local_Api2.Models
{
    public class ScanningItem
    {
        public int Id { get; set; }
        public int ScanningHour { get; set; }
        public DateTime Date { get; set; }
        public int Quantity { get; set; }
        public float QuantityKg { get; set; }
        public int Speed { get; set; }
        public int EanType { get; set; }
    }
}