using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Local_Api2.Models
{
    public class InventorySnapshot
    {
        public int InventorySnapshotId { get; set; }
        public int ProductId { get; set; }
        public string ProductIndex { get; set; }
        public string ProductName { get; set; }
        public double Size { get; set; }
        public string Unit { get; set; }
        public string Status { get; set; }
        public DateTime TakenOn { get; set; }
    }
}