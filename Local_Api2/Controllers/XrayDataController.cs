using Local_Api2.Models;
using Local_Api2.Static;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Description;

namespace Local_Api2.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class XrayDataController : ApiController
    {
        [HttpGet]
        [Route("GetRecentXrayRecords")]
        [ResponseType(typeof(List<XRayDataRecord>))]

        public IHttpActionResult GetRecentXrayRecords(string MachineName=null)
        {
            try
            {
                List<XRayDataRecord> Records = new List<XRayDataRecord>();
                using(SqlConnection XrayConn = new SqlConnection(Static.Secrets.xRayConnectionString))
                {

                    using (SqlDataReader reader = Utilities.GetRecentXrayData(MachineName, XrayConn))
                    {
                        while (reader.Read())
                        {
                            XRayDataRecord x = new XRayDataRecord();
                            x.ZfinIndex = Convert.ToInt32(reader["ArticleName"].ToString());
                            x.DeviceName = reader["DeviceName"].ToString();
                            x.ProductionStart = reader.GetDateTime(reader.GetOrdinal("ProductionStart"));
                            x.ProductionEnd = reader.GetDateTime(reader.GetOrdinal("ProductionEnd"));
                            x.TimeStamp = reader.GetDateTime(reader.GetOrdinal("TimeStamp"));
                            x.Throughput = Convert.ToInt32(reader["Throughput"].ToString());
                            x.CounterTrade = Convert.ToInt32(reader["CounterError"].ToString());
                            x.CounterTotal = Convert.ToInt32(reader["CounterTrade"].ToString());
                            x.CounterError = Convert.ToInt32(reader["CounterTotal"].ToString());
                            x.CounterBad = Convert.ToInt32(reader["CounterBad"].ToString());
                            x.CounterContaminated = Convert.ToInt32(reader["CounterContaminated"].ToString());
                            Records.Add(x);
                        }
                    }
                    return Ok(Records);
                }

                
            }catch(Exception ex)
            {
                return InternalServerError(ex);
            }
            
        }
    }
}
