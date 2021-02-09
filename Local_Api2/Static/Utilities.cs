using Local_Api2.Models;
using Microsoft.Ajax.Utilities;
using NLog;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Web;

namespace Local_Api2.Static
{
    public static class Utilities
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public static OracleDataReader GetRecentProductData(int MachineId, OracleConnection Con)
        {
            Logger.Debug("GetRecentProductData started for Machine={MachineId}", MachineId);

            if (Con.State == System.Data.ConnectionState.Closed)
            {
                Con.Open();
            }

            string sql = $@"SELECT SUM(op_qty.QUANTITY) AS QUANTITY, SUM(uom.WEIGHT_NETTO * op_qty.QUANTITY) AS QUANTITY_KG, prod.PRODUCT_ID, prod.PRODUCT_NR, MIN(operation.STARTED_DATE) AS STARTED_DATE, 
                         MAX(operation.FINISHED_DATE) AS FINISHED_DATE
                         FROM QMES_FO_MACHINE mach LEFT OUTER JOIN
                         QMES_WIP_OPERATION operation ON mach.MACHINE_ID = operation.MACHINE_ID LEFT OUTER JOIN
                         QMES_WIP_ORDER ord ON ord.ORDER_ID = operation.ORDER_ID LEFT OUTER JOIN
                         QMES_WIP_OPERATION_QTY op_qty ON op_qty.OPERATION_ID = operation.OPERATION_ID LEFT OUTER JOIN
                         QCM_PRODUCTS prod ON prod.PRODUCT_ID = op_qty.PRODUCT_ID LEFT OUTER JOIN
                         QCM_PACKAGE_HEADERS pack ON pack.PRODUCT_ID = prod.PRODUCT_ID LEFT OUTER JOIN
                         QCM_PACKAGE_LEVELS uom ON uom.PACKAGE_ID = pack.PACKAGE_ID
                         WHERE        (operation.OPERATION_TYPE_ID = 11) AND (uom.LEVEL_NR = 0) AND (operation.STARTED_DATE > :StartDate) AND (mach.MACHINE_ID = {MachineId})
                         GROUP BY prod.PRODUCT_ID, prod.PRODUCT_NR";

            var Command = new Oracle.ManagedDataAccess.Client.OracleCommand(sql, Con);
            OracleParameter[] parameters = new OracleParameter[]
            {
                    new OracleParameter("StartDate", Utilities.GetStartDate()),
            };
            Command.Parameters.AddRange(parameters);
            var reader = Command.ExecuteReader();
            Logger.Debug("GetRecentProductData finished");

            return reader;
        }

        public static SqlDataReader GetRecentXrayData(string MachineName, SqlConnection Con)
        {
            string sql = @"SELECT ArticleName, DeviceName, ProductionStart, ProductionEnd, TimeStamp, Throughput, CounterError, CounterTrade, CounterTotal, CounterBad, CounterContaminated
                                    FROM StatisticView WHERE ProductionEnd >= @StartDate";
            if (!string.IsNullOrEmpty(MachineName))
            {
                sql += $" AND DeviceName LIKE '%{int.Parse(MachineName.Substring(MachineName.Length - 2, 2))}'";
            }
            sql += " ORDER BY TimeStamp DESC";
            SqlCommand command = new SqlCommand(sql, Con);
            command.Parameters.Add("@StartDate", SqlDbType.DateTime);
            command.Parameters["@StartDate"].Value = Utilities.GetStartDate();
            if(Con.State == ConnectionState.Closed || Con.State == ConnectionState.Broken)
            {
                Con.Open();
            }
            SqlDataReader reader = command.ExecuteReader();
            return reader;
        }

        public static SqlDataReader GetRecentComponentScrap(SqlConnection Con, int? MaterialType = null)
        {
            string sql = @"SELECT z.zfinIndex, m.zfinIndex as material, bom.amount, bom.unit, t.scrap
                            FROM tbBom bom LEFT JOIN tbZfin z ON z.zfinId = bom.zfinId
	                            LEFT JOIN tbBomReconciliation br ON br.bomRecId = bom.bomRecId
	                            LEFT JOIN tbZfin m ON m.zfinId = bom.materialId
	                            LEFT JOIN 
                            (SELECT z.zfinIndex,cs.scrap
                            FROM tbComponentScrap cs LEFT JOIN tbZfin z ON z.zfinId = cs.zfinId
	                            LEFT JOIN tbScrapReconciliation sr ON sr.scrapReconciliationId = cs.componentScrapId
                            WHERE cs.scrapReconciliationId = (SELECT TOP(1) scrapReconciliationId FROM tbScrapReconciliation ORDER BY dateAdded DESC)) t ON t.zfinIndex = m.zfinIndex
                            WHERE bom.bomRecId = (SELECT TOP(1) bomRecId FROM tbBomReconciliation ORDER BY dateAdded DESC)";
            if (MaterialType != null)
            {
                sql += $" AND m.materialType={MaterialType}";
            }
            SqlCommand command = new SqlCommand(sql, Con);
            if (Con.State == ConnectionState.Closed || Con.State == ConnectionState.Broken)
            {
                Con.Open();
            }
            SqlDataReader reader = command.ExecuteReader();
            return reader;
        }

        public static List<DividerItem> GetDivider(int week, int year)
        {
            try
            {
                List<DividerItem> Items = new List<DividerItem>();

                using (SqlConnection npdConnection = new SqlConnection(Static.Secrets.NpdConnectionString))
                {
                    string sql = $@"SELECT z.zfinIndex, d.* FROM tbDivider d
                                    LEFT JOIN tbZfin z ON z.zfinId = d.ProductId 
                                    WHERE Week = {week} AND Year = {year}";

                    SqlCommand command = new SqlCommand(sql, npdConnection);
                    if (npdConnection.State == ConnectionState.Closed || npdConnection.State == ConnectionState.Broken)
                    {
                        npdConnection.Open();
                    }
                    SqlDataReader reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            string zfinIndex = reader["zfinIndex"].ToString();
                            DividerItem d;
                            LocationAmount la = new LocationAmount();
                            la.L = reader["L"].ToString();
                            la.Amount = Convert.ToInt32(reader["Amount"].ToString());
                            if (Items.Any(i => i.ZfinIndex == zfinIndex))
                            {
                                //there's already this product row in collection, append it
                                d = Items.FirstOrDefault(i => i.ZfinIndex == zfinIndex);
                                d.Locations.Add(la);
                            }
                            else
                            {
                                //there is not this product yet, let's add it
                                d = new DividerItem();
                                d.ZfinIndex = reader["zfinIndex"].ToString();
                                d.Locations = new List<LocationAmount>();
                                d.Locations.Add(la);
                                Items.Add(d);
                            }

                        }
                    }
                    return Items;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static List<DividerItem> GetDefaultDestinations()
        {
            try
            {
                List<DividerItem> Items = new List<DividerItem>();

                using (SqlConnection npdConnection = new SqlConnection(Static.Secrets.NpdConnectionString))
                {
                    string sql = $@"SELECT z.zfinIndex, cs.location 
                                  FROM tbZfin z LEFT JOIN tbCustomerString cs ON cs.custStringId = z.custString 
                                  WHERE z.custString IS NOT NULL AND prodStatus = 'PR'";

                    SqlCommand command = new SqlCommand(sql, npdConnection);
                    if (npdConnection.State == ConnectionState.Closed || npdConnection.State == ConnectionState.Broken)
                    {
                        npdConnection.Open();
                    }
                    SqlDataReader reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            DividerItem d = new DividerItem();
                            d.ZfinIndex = reader["zfinIndex"].ToString();
                            d.Locations = new List<LocationAmount>();
                            LocationAmount la = new LocationAmount();
                            la.L = reader["location"].ToString().Trim();
                            la.Amount = 0;
                            d.Locations.Add(la);
                            Items.Add(d);


                        }
                    }
                    return Items;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static List<ShipmentGroup> GetShipmentGroups()
        {
            try
            {
                List<ShipmentGroup> Items = new List<ShipmentGroup>();

                using (SqlConnection npdConnection = new SqlConnection(Static.Secrets.NpdConnectionString))
                {
                    string sql = $@"SELECT cs.location ,sg.* 
                                    FROM tbCustomerString cs LEFT JOIN tbShipmentGroups sg ON sg.ShipmentGroupId=cs.shipmentGroupId 
                                    WHERE sg.Name IS NOT NULL";

                    SqlCommand command = new SqlCommand(sql, npdConnection);
                    if (npdConnection.State == ConnectionState.Closed || npdConnection.State == ConnectionState.Broken)
                    {
                        npdConnection.Open();
                    }
                    SqlDataReader reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(reader.GetOrdinal("location")))
                            {
                                Location l = new Location();
                                l.L = reader["location"].ToString().Trim();
                                if(!Items.Any(i=>i.ShipmentGroupId == Convert.ToInt32(reader["ShipmentGroupId"].ToString())))
                                {
                                    // We need to setup this shipment group first
                                    ShipmentGroup sg = new ShipmentGroup();
                                    sg.ShipmentGroupId = Convert.ToInt32(reader["ShipmentGroupId"].ToString());
                                    sg.ShortName = reader["ShortName"].ToString();
                                    sg.Name = reader["Name"].ToString();
                                    Items.Add(sg);
                                }
                                Items.Where(i => i.ShipmentGroupId == Convert.ToInt32(reader["ShipmentGroupId"].ToString())).FirstOrDefault().Members.Add(l);
                            }
                        }
                    }
                    return Items;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static List<ProductMachineEfficiency> GetProductMachineEfficiencies(string ProductIds = null)
        {
            List<ProductMachineEfficiency> Items = new List<ProductMachineEfficiency>();

            try
            {
                using (OracleConnection Con = new Oracle.ManagedDataAccess.Client.OracleConnection(Static.Secrets.OracleConnectionString))
                {
                    if (Con.State == System.Data.ConnectionState.Closed)
                    {
                        Con.Open();
                    }
                    string sql = $@"SELECT PRODUCT_ID, MACHINE_ID, CAST(EFFICIENCY AS INT) AS EFFICIENCY, CAST(MAX_EFFICIENCY AS INT) AS MAX_EFFICIENCY FROM QMES_FO_MACHINE_EFFICIENCY";
                    if (!string.IsNullOrEmpty(ProductIds))
                    {
                        sql += $" WHERE PRODUCT_ID IN({ ProductIds})";
                    }

                    var Command = new Oracle.ManagedDataAccess.Client.OracleCommand(sql, Con);

                    var reader = Command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            ProductMachineEfficiency ef = new ProductMachineEfficiency();
                            ef.PRODUCT_ID = Convert.ToInt32(reader["PRODUCT_ID"].ToString());
                            ef.MACHINE_ID = Convert.ToInt32(reader["MACHINE_ID"].ToString());
                            ef.EFFICIENCY = Convert.ToInt32(reader["EFFICIENCY"].ToString());
                            ef.MAX_EFFICIENCY = Convert.ToInt32(reader["MAX_EFFICIENCY"].ToString());
                            Items.Add(ef);
                        }
                    }
                }
                return Items;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static List<Location> GetProductionPlanByCountry(string query = null)
        {
            try
            {
                List<Location> Locations = new List<Location>();
                List<ProductionPlanItem> Items = GetProductionPlan(query);
                if (Items.Any())
                {
                    List<DividerKeeper> Dividers = new List<DividerKeeper>();
                    List<DividerItem> DefaultDestinations = Utilities.GetDefaultDestinations();

                    DateTime start = Items.Min(i => i.START_DATE);
                    DateTime stop = Items.Max(i => i.STOP_DATE);
                    TimeSpan span = stop - start;
                    string ProductIds = string.Join(",", Items.Select(i => i.PRODUCT_ID).ToList().Distinct());
                    ProductMachineEfficiencyKeeper EfficiencyKeeper = new ProductMachineEfficiencyKeeper();
                    EfficiencyKeeper.Items = Utilities.GetProductMachineEfficiencies(ProductIds);
                    List<Session> Sessions = new List<Session>();
                    List<ShipmentGroup> shipmentGroups = Utilities.GetShipmentGroups();

                    foreach (ProductionPlanItem i in Items)
                    {
                        //for each operation
                        //get how to allocate it
                        //What week is this?
                        //info from session dates rather than from operation dates
                        if (!Sessions.Any(s => s.SCHEDULING_ID == i.SCHEDULING_ID))
                        {
                            //we need to create the session and calculate its week/year
                            Session s = new Session();
                            s.SCHEDULING_ID = i.SCHEDULING_ID;
                            s.BEGIN_DATE = i.BEGIN_DATE;
                            s.END_DATE = i.END_DATE;
                            s.CalcualatePeriod();
                            Sessions.Add(s);
                        }
                        i.WEEK = Sessions.FirstOrDefault(s => s.SCHEDULING_ID == i.SCHEDULING_ID).Week;
                        i.YEAR = Sessions.FirstOrDefault(s => s.SCHEDULING_ID == i.SCHEDULING_ID).Year;
                        if (!Dividers.Any(d => d.Week == i.WEEK && d.Year == i.YEAR))
                        {
                            //create it
                            DividerKeeper div = new DividerKeeper();
                            div.Week = i.WEEK;
                            div.Year = i.YEAR;
                            div.Items = Utilities.GetDivider(i.WEEK, i.YEAR);
                            Dividers.Add(div);
                        }
                        DividerKeeper currDiv = Dividers.FirstOrDefault(d => d.Week == i.WEEK && d.Year == i.YEAR);
                        double palletCount = i.QUANTITY / i.PAL; //number of pieces on pallet

                        if (currDiv.Items.Any(x => x.ZfinIndex == i.PRODUCT_NR))
                        {

                        }
                        else
                        {
                            //this product hasn't been found in divider for week X
                            //check futher weeks, maybe it's in week X+1
                            currDiv = null;
                            foreach (DividerKeeper dk in Dividers.Where(d => (d.Week > i.WEEK && d.Year == i.YEAR) || (d.Week < i.WEEK && d.Year > i.YEAR)))
                            {
                                if (dk.Items.Any(y => y.ZfinIndex == i.PRODUCT_NR))
                                {
                                    //it's in divider for next week
                                    currDiv = dk;
                                }
                            }
                        }
                        if (currDiv != null)
                        {
                            foreach (LocationAmount la in currDiv.Items.FirstOrDefault(x => x.ZfinIndex == i.PRODUCT_NR).Locations)
                            {
                                if (i.QUANTITY > 0)
                                {
                                    //if there's nothing left to allocate in this operation, go to next operation
                                    if (la.Amount > 0)
                                    {
                                        //there's still quantity to allocate
                                        if (!Locations.Any(l => l.L.Trim() == la.L.Trim()))
                                        {
                                            //we don't have this location started yet
                                            Location loc = new Location();
                                            loc.L = la.L.Trim();
                                            if (shipmentGroups.Any(s => s.Members.Any(m => m.L.Trim() == loc.L)))
                                            {
                                                loc.ShipmentGroupName = shipmentGroups.FirstOrDefault(s => s.Members.Any(m => m.L.Trim() == loc.L)).Name;
                                            }
                                            Locations.Add(loc);
                                        }
                                        Location currLoc = Locations.FirstOrDefault(l => l.L.Trim() == la.L.Trim());
                                        ProductionPlanItem p = new ProductionPlanItem();
                                        p = i.CloneJson();
                                        if (la.Amount >= i.QUANTITY)
                                        {
                                            //this L needs more than operation quantity or all of it
                                            //take only operation quantity then
                                            la.Amount -= i.QUANTITY; //decrease the amount of allocation that remains to this L
                                            i.QUANTITY = 0;
                                            i.PAL = 0;
                                        }
                                        else
                                        {
                                            //there will remain some quantity for other L
                                            p.QUANTITY -= la.Amount;
                                            p.PAL = la.Amount / palletCount;
                                            la.Amount = 0;
                                            i.QUANTITY -= p.QUANTITY;
                                            i.PAL -= p.QUANTITY / palletCount;
                                            //as we don't consume the whole operation,
                                            //we must adjust the REMAINING & CONSUMED parts (stop date, quantity, etc)
                                            long? minutesTaken = EfficiencyKeeper.Amount2Minutes(i.MACHINE_ID, i.PRODUCT_ID, p.QUANTITY);
                                            if (minutesTaken != null)
                                            {
                                                //we have the efficiency set in MES
                                                p.STOP_DATE = p.START_DATE.AddMinutes((double)minutesTaken);
                                                i.START_DATE = p.STOP_DATE; //stop date of this part is beginning of next part
                                            }

                                        }
                                        p.LOCATION = la.L;
                                        p.DIVIDER_WEEK = currDiv.Week;
                                        p.DIVIDER_YEAR = currDiv.Year;
                                        currLoc.Parts.Add(p); // add this part to operations for this location
                                    }
                                }

                            }
                        }
                        else
                        {
                            //It's not divider-based product
                            //take default allocation
                            Location currLoc;

                            if (DefaultDestinations.Any(d => d.ZfinIndex == i.PRODUCT_NR))
                            {
                                LocationAmount la = DefaultDestinations.Where(d => d.ZfinIndex == i.PRODUCT_NR).FirstOrDefault().Locations.FirstOrDefault();
                                //default destination found
                                if (!Locations.Any(l => l.L.Trim() == la.L.Trim()))
                                {
                                    //we don't have this location started yet
                                    Location loc = new Location();
                                    loc.L = la.L.Trim();
                                    if (shipmentGroups.Any(s => s.Members.Any(m => m.L.Trim() == loc.L)))
                                    {
                                        loc.ShipmentGroupName = shipmentGroups.FirstOrDefault(s => s.Members.Any(m => m.L.Trim() == loc.L)).Name;
                                    }
                                    Locations.Add(loc);
                                }
                                currLoc = Locations.FirstOrDefault(l => l.L.Trim() == la.L.Trim());
                            }
                            else
                            {
                                //default destination doesn't exist
                                //add it to unknow collection
                                if (!Locations.Any(l => l.L.Trim() == "LXXX"))
                                {
                                    //we don't have this location started yet
                                    Location loc = new Location();
                                    loc.L = "LXXX";
                                    Locations.Add(loc);
                                }
                                currLoc = Locations.FirstOrDefault(l => l.L.Trim() == "LXXX");
                            }

                            ProductionPlanItem p = new ProductionPlanItem();
                            p = i.CloneJson();
                            i.QUANTITY = 0;
                            i.PAL = 0;
                            p.LOCATION = currLoc.L;
                            p.DIVIDER_WEEK = 0; // not divider-based
                            p.DIVIDER_YEAR = 0; // not divider-based
                            currLoc.Parts.Add(p); // add this part to operations for this location
                        }


                    }
                }
                foreach(Location l in Locations)
                {
                    l.Compose();
                }
                return Locations;
            }
            catch (Exception ex)
            {

                Logger.Error("GetProductionPlanByDestinations: Błąd. Szczegóły: {Message}", ex.ToString());
                return null;
            }
        }

        public static List<ProductionPlanItem> GetProductionPlan(string query = null)
        {
            try
            {
                using (OracleConnection Con = new Oracle.ManagedDataAccess.Client.OracleConnection(Static.Secrets.OracleConnectionString))
                {
                    if (Con.State == System.Data.ConnectionState.Closed)
                    {
                        Con.Open();
                    }

                    if (query != null)
                    {
                        if (!query.Contains("START_DATE"))
                        {
                            //make sure start date is always indicated
                            //otherwise we can crash the db
                            query += $" AND (sp.START_DATE >= '{DateTime.Now.StartOfWeek().ToString("yyyy-MM-dd HH:mm:ss")}') AND (ss.STATUS = 'CO') AND (op.STATUS <> 'RG')";
                        }
                        query = $"(op.OPERATION_TYPE_ID = 11) AND (uom.LEVEL_NR = 0) AND (uomPal.LEVEL_NR = 3) AND (o2p.ACTION = 'TO_DO') AND (ss.STATUS = 'CO') AND (op.STATUS <> 'RG') AND {query}";
                    }
                    else
                    {
                        query = $"(op.OPERATION_TYPE_ID = 11) AND (uom.LEVEL_NR = 0) AND (uomPal.LEVEL_NR = 3) AND (o2p.ACTION = 'TO_DO') AND (ss.STATUS = 'CO') AND (op.STATUS <> 'RG') AND (sp.START_DATE >= '{DateTime.Now.StartOfWeek().ToString("yyyy-MM-dd HH:mm:ss")}')";
                    }

                    string str = $@"SELECT sp.SCHEDULING_ID, ss.BEGIN_DATE, ss.END_DATE, sp.START_DATE, sp.STOP_DATE, sp.MACHINE_ID, m.MACHINE_NAME, ord.ORDER_NR, op.OPERATION_NR, op.STATUS, pr.PRODUCT_ID, pr.PRODUCT_NR, pr.NAME, o2p.QUANTITY, (uom.WEIGHT_NETTO * o2p.QUANTITY) AS WEIGHT, (o2p.QUANTITY / uomPal.BU_QUANTITY) AS PAL
                                    FROM QMES_EJS_SCHEDULING_POSITION sp 
                                    LEFT JOIN QMES_EJS_SCHEDULING_SESSION ss ON ss.SCHEDULING_ID = sp.SCHEDULING_ID 
                                    LEFT JOIN QMES_FO_MACHINE m ON m.MACHINE_ID = sp.MACHINE_ID LEFT OUTER JOIN 
                                    QMES_WIP_OPERATION op ON op.OPERATION_ID = sp.OPERATION_ID LEFT OUTER JOIN
                                    QMES_WIP_ORDER ord ON ord.ORDER_ID = op.ORDER_ID LEFT OUTER JOIN
                                    QMES_WIP_ORDER2PRODUCT o2p ON o2p.OPERATION_ID = sp.OPERATION_ID LEFT OUTER JOIN 
                                    QCM_PRODUCTS pr ON pr.PRODUCT_ID = o2p.PRODUCT_ID LEFT OUTER JOIN
                                    QCM_PACKAGE_HEADERS pack ON pack.PRODUCT_ID = pr.PRODUCT_ID LEFT OUTER JOIN
                                    QMES_WIP_ORDER2PRODUCT o2p ON o2p.OPERATION_ID = sp.OPERATION_ID AND o2p.PRODUCT_ID = pr.PRODUCT_ID LEFT OUTER JOIN 
                                    QCM_PACKAGE_LEVELS uom ON uom.PACKAGE_ID = pack.PACKAGE_ID LEFT OUTER JOIN 
                                    QCM_PACKAGE_LEVELS uomPal ON uomPal.PACKAGE_ID = pack.PACKAGE_ID 
                                    WHERE {query} 
                                    ORDER BY sp.START_DATE";


                    var Command = new Oracle.ManagedDataAccess.Client.OracleCommand(str, Con);

                    var reader = Command.ExecuteReader();

                    List<ProductionPlanItem> Plan = new List<ProductionPlanItem>();

                    double pal;

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            ProductionPlanItem p = new ProductionPlanItem();
                            p.SCHEDULING_ID = Convert.ToInt32(reader["SCHEDULING_ID"].ToString());
                            p.BEGIN_DATE = Convert.ToDateTime(reader["BEGIN_DATE"].ToString());
                            p.END_DATE = Convert.ToDateTime(reader["END_DATE"].ToString());
                            p.START_DATE = Convert.ToDateTime(reader["START_DATE"].ToString());
                            p.STOP_DATE = Convert.ToDateTime(reader["STOP_DATE"].ToString());
                            p.MACHINE_ID = Convert.ToInt32(reader["MACHINE_ID"].ToString());
                            p.MACHINE_NAME = reader["MACHINE_NAME"].ToString();
                            p.ORDER_NR = reader["ORDER_NR"].ToString();
                            p.OPERATION_NR = reader["OPERATION_NR"].ToString();
                            p.PRODUCT_ID = Convert.ToInt64(reader["PRODUCT_ID"].ToString());
                            p.PRODUCT_NR = reader["PRODUCT_NR"].ToString();
                            p.NAME = reader["NAME"].ToString();
                            p.QUANTITY = Convert.ToInt64(reader["QUANTITY"].ToString());
                            p.WEIGHT = Convert.ToDouble(reader["WEIGHT"].ToString());
                            p.STATUS = reader["STATUS"].ToString();
                            try
                            {
                                if (double.TryParse(reader["PAL"].ToString(), out pal))
                                {
                                    p.PAL = Convert.ToDouble(reader["PAL"].ToString());
                                }
                                else
                                {
                                    p.PAL = 0;
                                }
                            }
                            catch (Exception ex)
                            {
                                p.PAL = 0;
                            }

                            Plan.Add(p);
                        }
                    }
                    else
                    {

                    }
                    return Plan;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static DateTime GetStartDate()
        {
            DateTime yesterday = DateTime.Now.AddDays(-1);
            DateTime rDate;
            bool keepDate = true;
            int h = yesterday.Hour;
            if(h >= 6 && h < 14)
            {
                h = 6;
            }else if(h >= 14 && h < 22)
            {
                h = 14;
            }
            else if(h == 22 || h == 23)
            {
                h = 22;
            }
            else
            {
                //00 - 05:59
                h = 22;
                keepDate = false;
            }
            if (!keepDate)
            {
                //yesterday = yesterday - 1
                yesterday = yesterday.AddDays(-1);
            }

            //Set to yesterday H:00:00
            rDate = new DateTime(yesterday.Year, yesterday.Month, yesterday.Day, h, 0, 0);

            return rDate;
        }
    }
}