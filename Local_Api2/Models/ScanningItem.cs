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
        public double QuantityKg { get; set; }
        public int QuantityFromBoxes { get; set; }
        public double FoilLossPercentage
        {
            get
            {
                double perc = 0;
                if(this.Quantity>0 && this.QuantityFromBoxes > 0)
                {
                    perc = 100*(((double)this.Quantity - (double)this.QuantityFromBoxes) / (double)this.QuantityFromBoxes);
                    if(perc < 0) { perc = 0; }
                }
                return perc;
            }
        }
        public int Speed { get; set; }
        public int AssumedSpeed { get; set; }
        public int EanType { get; set; }
        public int ChangeOvers { get; set; }
        public int SpeedDiff
        {
            get
            {
                int d = 0;
                if(AssumedSpeed > 0)
                {
                    d = Speed - AssumedSpeed;
                }
                return d;
            }
        }
        public int Zfin { get; set; }
        public double ConfirmedKg { get; set; }
    }
}