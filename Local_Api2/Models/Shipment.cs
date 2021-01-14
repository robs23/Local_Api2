using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.PeerResolvers;
using System.Web;

namespace Local_Api2.Models
{
    public class Shipment
    {
        public long DOC_ID { get; set; }
        public DateTime DATE_EMITTED { get; set; }
        public string DOC_TYPE_NR { get; set; }
        public long FIRM_ID { get; set; }
        public string ADR_STREET { get; set; }
        public string ADR_ZIPCODE { get; set; }
        public string ADR_CITY { get; set; }
        public string C_ORDER_NR { get; set; }
    }
}