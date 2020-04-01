using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Local_Api2.Models
{
    public class Process
    {
        public string Number { get; set; }
        public string Manager { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string FinishedBy { get; set; }
        public string InitialDiagnosis { get; set; }
        public string RepairActions { get; set; }
        public string Status { get; set; }
        public string IsAdjustment { get; set; }
        public string ReasonCode2 { get; set; }
        public string ReasonCode3 { get; set; }
    }
}