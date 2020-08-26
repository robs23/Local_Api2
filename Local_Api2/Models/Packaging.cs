using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Local_Api2.Models
{
    public class Packaging
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public string LevelName { get; set; }
        public float NetWeight { get; set; }
        public float GrossWeight { get; set; }
        public int Length { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int PcCount { get; set; }
        public string EAN { get; set; }
    }
}