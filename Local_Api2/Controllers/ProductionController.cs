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
    }
}
