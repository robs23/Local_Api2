using Local_Api2.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;

namespace Local_Api2.Controllers
{
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
                    
                    string sql = @"SELECT ArticleName, DeviceName, ProductionStart, ProductionEnd, TimeStamp, Throughput, CounterError, CounterTrade, CounterTotal, CounterBad, CounterContaminated
                                    FROM StatisticView WHERE ProductionStart >= @StartDate";
                    if (!string.IsNullOrEmpty(MachineName))
                    {
                        sql += $" AND DeviceName LIKE '%{int.Parse(MachineName.Substring(MachineName.Length - 2, 2))}'";
                    }
                    sql += " ORDER BY TimeStamp DESC";
                    SqlCommand command = new SqlCommand(sql, XrayConn);
                    command.Parameters.Add("@StartDate", SqlDbType.DateTime);
                    command.Parameters["@StartDate"].Value = DateTime.Now.AddDays(-1);
                    XrayConn.Open();
                    using(SqlDataReader reader = command.ExecuteReader())
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
