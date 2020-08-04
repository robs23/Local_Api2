using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Local_Api2.Models
{
    public class Machine
    {
        public int MachineId { get; set; }
        public string Name { get; set; }
        public int Type { get; set; }
        public bool VisibleInAPS { get; set; }
        public string State { get; set; }
    }
}