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

                List<ProductionPlanItem> Items = Utilities.GetProductionPlan(query);
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
                List<Location> Items = Utilities.GetProductionPlanByCountry(query);
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

                Logger.Error("GetProductionPlanByDestinations: Błąd. Szczegóły: {Message}", ex.ToString());
                return InternalServerError(ex);
            }
            
        }

        [HttpGet]
        [Route("GetVirtualTrucks")]
        [ResponseType(typeof(List<VirtualTruck>))]
        public IHttpActionResult GetVirtualTrucks(string query = null)
        {
            try
            {
                List<Location> Items = Utilities.GetProductionPlanByCountry(query);
                List<ShipmentGroup> ShipmentGroups = Utilities.GetShipmentGroups();
                List<VirtualTruck> Trucks = new List<VirtualTruck>();

                if (Items.Any())
                {
                    ProductMachineEfficiencyKeeper EfficiencyKeeper = new ProductMachineEfficiencyKeeper();
                    EfficiencyKeeper.Items = Utilities.GetProductMachineEfficiencies();

                    //let's create first truck
                    VirtualTruck currTruck = null;
                    int counter = 0;

                    foreach(Location currLoc in Items.OrderBy(i => i.TotalPallets))
                    {
                        Logger.Info("Lokacja: {loc}", currLoc.L);
                        //let's plan trucks starting from those locations, that have the fewest pallets

                        if(currTruck != null)
                        {
                            //location has changed. each location to have separate truck for now
                            Logger.Info("Dodaje samochód do lokacji: {L}, palet: {pal}", currTruck.L, currTruck.TotalPallets);
                            currTruck.Compose();
                            Trucks.Add(currTruck);
                            currTruck = null;
                        }
                        foreach(ProductionPlanItem p in currLoc.Parts.OrderBy(p => p.END_DATE))
                        {
                            counter = 0;
                            while(p.PAL > 0.9 && counter < 10)
                            {
                                Logger.Info("Operacja: {op}, przejście: {counter}", p.OPERATION_NR, counter);
                                //while this order hasn't been consumed
                                if (currTruck != null)
                                {
                                    if (currTruck.Pallets2Full == 0)
                                    {
                                        //we can't add any pallet to current truck
                                        Logger.Info("Dodaje samochód do lokacji: {L}, palet: {pal}", currTruck.L, currTruck.TotalPallets);
                                        currTruck.Compose();
                                        Trucks.Add(currTruck);
                                        currTruck = null;
                                    }
                                }

                                if (currTruck == null)
                                {
                                    //we have to create new truck
                                    Logger.Info("Nowe auto do: {L}", p.LOCATION);
                                    currTruck = new VirtualTruck();
                                    currTruck.L = p.LOCATION;
                                }
                                double palletCount = p.QUANTITY / p.PAL;

                                ProductionPlanItem pi = new ProductionPlanItem();
                                pi = p.CloneJson();

                                Logger.Info("Ilość palet w operacji: {op}, pozostało na aucie: {rem}", p.PAL, currTruck.Pallets2Full);
                                if (p.PAL < currTruck.Pallets2Full)
                                {
                                    currTruck.Parts.Add(pi);
                                    currTruck.TotalPallets += p.PAL;
                                    p.PAL = 0;
                                    p.QUANTITY = 0;
                                }
                                else
                                {
                                    //part of this operation will go to current truck and part will go to next truck
                                    pi.PAL = currTruck.Pallets2Full;
                                    pi.QUANTITY = Convert.ToInt64(pi.PAL * palletCount);
                                    currTruck.TotalPallets += pi.PAL;
                                    currTruck.Parts.Add(pi);
                                    //as we don't consume the whole operation,
                                    //we must adjust the REMAINING & CONSUMED parts (stop date, quantity, etc)
                                    p.PAL -= pi.PAL; //subtract pallets you've taken
                                    p.QUANTITY -= pi.QUANTITY;
                                    long? minutesTaken = EfficiencyKeeper.Amount2Minutes(pi.MACHINE_ID, pi.PRODUCT_ID, pi.QUANTITY);
                                    if (minutesTaken != null)
                                    {
                                        //we have the efficiency set in MES
                                        pi.STOP_DATE = pi.START_DATE.AddMinutes((double)minutesTaken);
                                        p.START_DATE = pi.STOP_DATE; //stop date of this part is beginning of next part
                                    }
                                }
                                counter++;
                            }
                            
                        }
                    }

                    return Ok(Trucks);
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

    }
}
