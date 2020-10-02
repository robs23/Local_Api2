using Local_Api2.Models;
using Local_Api2.Static;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Description;

namespace Local_Api2.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class ProductController : ApiController
    {
        private OracleConnection Con = new Oracle.ManagedDataAccess.Client.OracleConnection(Static.Secrets.ApiConnectionString);

        [HttpGet]
        [Route("GetRecentProducts")]
        [ResponseType(typeof(List<ProductionRecord>))]
        public IHttpActionResult GetRecentProducts(int MachineId)
        {

            try
            {
                List<ProductionRecord> Records = new List<ProductionRecord>();
                using (OracleConnection Con = new Oracle.ManagedDataAccess.Client.OracleConnection(Static.Secrets.ApiConnectionString))
                {
                    using (var reader = Utilities.GetRecentProductData(MachineId, Con))
                    {
                        while (reader.Read())
                        {
                            ProductionRecord pr = new ProductionRecord();
                            pr.ProductId = Convert.ToInt32(reader["PRODUCT_ID"].ToString());
                            pr.ProductNumber = reader["PRODUCT_NR"].ToString();
                            pr.StartedOn = Convert.ToDateTime(reader["STARTED_DATE"].ToString());
                            pr.FinishedOn = Convert.ToDateTime(reader["FINISHED_DATE"].ToString());
                            pr.Quantity = Convert.ToDouble(reader["QUANTITY"].ToString());
                            pr.QuantityKg = Convert.ToDouble(reader["QUANTITY_KG"].ToString());
                            Records.Add(pr);
                        }
                    }
                    return Ok(Records);
                }


            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }


            
        }
    } 
}
