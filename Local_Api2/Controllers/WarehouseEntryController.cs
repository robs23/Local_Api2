using Local_Api2.Models;
using NLog;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;

namespace Local_Api2.Controllers
{
    public class WarehouseEntryController : ApiController
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [HttpGet]
        [Route("GetWarehouseEntries")]
        [ResponseType(typeof(List<WarehouseEntry>))]
        public IHttpActionResult GetWarehouseEntries(string query = null)
        {
            try
            {
                using (OracleConnection Con = new Oracle.ManagedDataAccess.Client.OracleConnection(Static.Secrets.OracleConnectionString))
                {
                    if (Con.State == System.Data.ConnectionState.Closed)
                    {
                        Con.Open();
                    }

                    if (query == null)
                    {
                        query = "(pi.C_DATE >= '2021-01-14')";
                    }

                    string str = $@"SELECT pr.PRODUCT_NR, s.SERIAL_NR, pi.PRODUCTION_ID, pi.LOADUNIT_ID, pi.LOADUNIT_NR, pi.PRODUCT_ID, pi.DATE_PROD, pi.QUANTITY, pi.WEIGHT, pi.LENGTH, pi.WIDTH, pi.HEIGHT, pi.STATUS, pi.C_DATE, pi.LM_DATE
                         FROM PRODUCTION_ITEMS pi LEFT OUTER JOIN
                         QCM_PRODUCTS pr ON pr.PRODUCT_ID = pi.PRODUCT_ID LEFT OUTER JOIN
                         QCM_PROD_SERIALS s ON s.PROD_SERIAL_ID = pi.PROD_SERIAL_ID
                         WHERE {query}";

                    var Command = new Oracle.ManagedDataAccess.Client.OracleCommand(str, Con);

                    var reader = Command.ExecuteReader();

                    List<WarehouseEntry> Entries = new List<WarehouseEntry>();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            WarehouseEntry w = new WarehouseEntry();
                            w.LOADUNIT_ID = Convert.ToInt64(reader["LOADUNIT_ID"].ToString());
                            w.LOADUNIT_NR = reader["LOADUNIT_NR"].ToString();
                            w.PRODUCTION_ID = Convert.ToInt64(reader["PRODUCTION_ID"].ToString());
                            w.PRODUCT_ID = Convert.ToInt64(reader["PRODUCT_ID"].ToString());
                            w.PRODUCT_NR = reader["PRODUCT_NR"].ToString();
                            w.SERIAL_NR = reader["SERIAL_NR"].ToString(); 
                            w.QUANTITY = Convert.ToInt32(reader["QUANTITY"].ToString());
                            w.STATUS = reader["STATUS"].ToString();
                            w.WEIGHT = Convert.ToDouble(reader["WEIGHT"].ToString());
                            w.WIDTH = Convert.ToInt32(reader["WIDTH"].ToString());
                            w.LENGTH = Convert.ToInt32(reader["LENGTH"].ToString());
                            w.HEIGHT = Convert.ToInt32(reader["HEIGHT"].ToString());
                            w.C_DATE = Convert.ToDateTime(reader["C_DATE"].ToString());
                            w.LM_DATE = Convert.ToDateTime(reader["LM_DATE"].ToString());
                            Entries.Add(w);
                        }
                        return Ok(Entries);
                    }
                    else
                    {
                        Logger.Info("GetWarehouseEntries: Porażka, nie znaleziono nic..");
                        return NotFound();
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Error("GetWarehouseEntries: Błąd. Szczegóły: {Message}", ex.ToString());
                return InternalServerError(ex);
            }
        }
    }
}
