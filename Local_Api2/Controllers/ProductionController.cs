using Local_Api2.Models;
using Local_Api2.Static;
using NLog;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Description;

namespace Local_Api2.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class ProductionController : ApiController
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [HttpGet]
        [Route("GetProductionPlan")]
        [ResponseType(typeof(List<ProductionPlanItem>))]
        public IHttpActionResult GetProductionPlan(string query = null)
        {

            try
            {

                List<ProductionPlanItem> Items = _GetProductionPlan(query);
                if (Items.Any())
                {
                    return Ok(Items);
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("GetProductionPlan: Błąd. Szczegóły: {Message}", ex.ToString());
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("GetProductionPlanByDestinations")]
        [ResponseType(typeof(List<Location>))]
        public IHttpActionResult GetProductionPlanByDestinations(string query = null)
        {
            try
            {
                List<ProductionPlanItem> Items = _GetProductionPlan(query);
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
                    List<Location> Locations = new List<Location>();
                    List<Session> Sessions = new List<Session>();

                    foreach(ProductionPlanItem i in Items)
                    {
                        //for each operation
                        //get how to allocate it
                        //What week is this?
                        //info from session dates rather than from operation dates
                        if(!Sessions.Any(s=>s.SCHEDULING_ID == i.SCHEDULING_ID))
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
                        if(!Dividers.Any(d=>d.Week == i.WEEK && d.Year == i.YEAR))
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

                        if (currDiv.Items.Any(x=>x.ZfinIndex == i.PRODUCT_NR))
                        {
                            foreach(LocationAmount la in currDiv.Items.FirstOrDefault(x => x.ZfinIndex == i.PRODUCT_NR).Locations)
                            {
                                if(i.QUANTITY > 0)
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
                                            if(minutesTaken != null)
                                            {
                                                //we have the efficiency set in MES
                                                p.STOP_DATE = p.START_DATE.AddMinutes((double)minutesTaken);
                                                i.START_DATE = p.STOP_DATE; //stop date of this part is beginning of next part
                                            }

                                        }
                                        currLoc.Parts.Add(p); // add this part to operations for this location
                                    }
                                }
                                
                            }
                        }


                    }
                    return Ok(Locations);
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {

                Logger.Error("GetProductionPlanByDestinations: Błąd. Szczegóły: {Message}", ex.ToString());
                return InternalServerError(ex);
            }
            
        }

        private List<ProductionPlanItem> _GetProductionPlan(string query = null)
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
                            query += $" AND (sp.START_DATE >= '{DateTime.Now.StartOfWeek().ToString("yyyy-MM-dd HH:mm:ss")}') ";
                        }
                        query = $"(op.OPERATION_TYPE_ID = 11) AND (uom.LEVEL_NR = 0) AND (uomPal.LEVEL_NR = 3) AND (o2p.ACTION = 'TO_DO') AND {query}";
                    }
                    else
                    {
                        query = $"(op.OPERATION_TYPE_ID = 11) AND (uom.LEVEL_NR = 0) AND (uomPal.LEVEL_NR = 3) AND (o2p.ACTION = 'TO_DO') AND (sp.START_DATE >= '{DateTime.Now.StartOfWeek().ToString("yyyy-MM-dd HH:mm:ss")}')";
                    }

                    string str = $@"SELECT sp.SCHEDULING_ID, ss.BEGIN_DATE, ss.END_DATE, sp.START_DATE, sp.STOP_DATE, sp.MACHINE_ID, m.MACHINE_NAME, ord.ORDER_NR, op.OPERATION_NR, pr.PRODUCT_ID, pr.PRODUCT_NR, pr.NAME, o2p.QUANTITY, (uom.WEIGHT_NETTO * o2p.QUANTITY) AS WEIGHT, (o2p.QUANTITY / uomPal.BU_QUANTITY) AS PAL
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
                            try
                            {
                                if (double.TryParse(reader["PAL"].ToString(), out pal))
                                {
                                    p.PAL = Convert.ToDouble(reader["PAL"].ToString());
                                }
                                else
                                {
                                    p.PAL = 0;
                                    p.PalText = reader["PAL"].ToString();
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
    }
}
