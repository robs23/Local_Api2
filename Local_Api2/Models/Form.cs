using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Local_Api2.Models
{
    public class Form
    {
        public int FormId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Link { get; set; }
        public string Photo { get; set; }
        public DateTime? CreatedOn { get; set; }
    }
}