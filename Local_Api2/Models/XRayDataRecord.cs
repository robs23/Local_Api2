using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Local_Api2.Models
{
    public class XRayDataRecord
    {
        public int ZfinIndex { get; set; }
        public string DeviceName { get; set; }
        public DateTime? ProductionStart { get; set; }
        public DateTime? ProductionEnd { get; set; }
        public DateTime TimeStamp { get; set; }
        public int Throughput { get; set; }
        public int CounterError { get; set; }
        public int CounterTrade { get; set; }
        public int CounterTotal { get; set; }
        public int CounterBad { get; set; }
        public int CounterContaminated { get; set; }


    }
}