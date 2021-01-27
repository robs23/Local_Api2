using Local_Api2.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;

namespace Local_Api2.Models
{
    public class Session
    {
        public int SCHEDULING_ID { get; set; }
        public DateTime BEGIN_DATE { get; set; }
        public DateTime END_DATE { get; set; }
        public int Week { get; set; }
        public int Year { get; set; }

        public void CalcualatePeriod()
        {
            List<Period> Weeks = new List<Period>();
            int currWeek;
            int currYear;

            //get the most common week number in range
            DateTime d = BEGIN_DATE;
            while (d <= END_DATE)
            {
                currWeek = d.IsoWeekOfYear();
                currYear = d.Year;
                if (Weeks.Any(w => w.Week == currWeek && w.Year == currYear))
                {
                    //we have this week alrady, bump the 3rd item
                    Weeks.Where(w => w.Week == currWeek && w.Year == currYear).First().Counter++;
                }
                else
                {
                    Weeks.Add(new Period { Week = currWeek, Year = currYear, Counter = 1 });
                }
                d = d.AddDays(1);
            }

            Period winner = Weeks.OrderByDescending(w => w.Counter).FirstOrDefault();
            Week = winner.Week;
            Year = winner.Year;
        }

        class Period
        {
            public int Week { get; set; }
            public int Year { get; set; }
            public int Counter { get; set; } = 0;
        }


    }
}