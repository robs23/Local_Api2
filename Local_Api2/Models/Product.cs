using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Web;

namespace Local_Api2.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Index { get; set; }
        public string Name { get; set; }
        public int PackageId { get; set; }
        public Packaging Packaging { get; set; }

    }
}