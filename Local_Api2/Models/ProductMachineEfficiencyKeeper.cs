using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Local_Api2.Models
{
    public class ProductMachineEfficiencyKeeper
    {
        public List<ProductMachineEfficiency> Items { get; set; }
        public ProductMachineEfficiencyKeeper()
        {
            Items = new List<ProductMachineEfficiency>();
        }

        public int? Amount2Minutes(int MachineId, int ProductId, int Amount)
        {
            int? res = null;

            ProductMachineEfficiency ef = Items.FirstOrDefault(i => i.MACHINE_ID == MachineId && i.PRODUCT_ID == ProductId);
            if (ef != null)
            {
                int AmountByMin = ef.EFFICIENCY / 60;
                res = Amount / AmountByMin;
            }
            return res;
        }
    }
}