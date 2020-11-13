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
        public int Quantity
        {
            get
            {
                if (QuantityFromBoxes > 0)
                {
                    return QuantityFromBoxes;
                }
                else
                {
                    return QuantityFromFoil;
                }
            }
        }
        public double QuantityKg
        {
            get
            {
                return Quantity * NetWeight;
            }
        }
        public int QuantityFromFoil { get; set; }
        public int QuantityFromBoxes { get; set; }
        public double FoilLossPercentage
        {
            get
            {
                double perc = 0;
                if(this.QuantityFromFoil>0 && this.QuantityFromBoxes > 0)
                {
                    perc = 100*(((double)this.QuantityFromFoil - (double)this.QuantityFromBoxes) / (double)this.QuantityFromBoxes);
                    if(perc < 0) { perc = 0; }
                }
                return perc;
            }
        }
        public int Speed
        {
            get
            {
                int currentMinutes = 60;
                if (Date == DateTime.Now.Date && ScanningHour == DateTime.Now.Hour)
                {
                    currentMinutes = DateTime.Now.Minute;
                    if (currentMinutes == 0) { currentMinutes = 1; }
                }
                return Quantity / currentMinutes;
            }
        }
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
        public int Contaminated { get; set; } //Xray 
        public double GE { get; set; }
        public double NetWeight { get; set; }
        public int BoxCount { get; set; }
    }
}