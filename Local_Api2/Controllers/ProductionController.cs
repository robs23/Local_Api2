using Local_Api2.Models;
using Local_Api2.Static;
using NLog;
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

                    string str = $@"SELECT sp.SCHEDULING_ID, sp.START_DATE, sp.STOP_DATE, sp.MACHINE_ID, m.MACHINE_NAME, ord.ORDER_NR, op.OPERATION_NR, pr.PRODUCT_ID, pr.PRODUCT_NR, pr.NAME, o2p.QUANTITY, (uom.WEIGHT_NETTO * o2p.QUANTITY) AS WEIGHT, (o2p.QUANTITY / uomPal.BU_QUANTITY) AS PAL
                                    FROM QMES_EJS_SCHEDULING_POSITION sp 
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

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            ProductionPlanItem p = new ProductionPlanItem();
                            p.SCHEDULING_ID = Convert.ToInt32(reader["SCHEDULING_ID"].ToString());
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
                            p.PAL = Convert.ToDouble(reader["PAL"].ToString());
                            Plan.Add(p);
                        }
                        return Ok(Plan);
                    }
                    else
                    {
                        Logger.Info($"GetProductionPlan: brak danych dla tych kryteriów: {query}");
                        return NotFound();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("GetProductionPlan: Błąd. Szczegóły: {Message}", ex.ToString());
                return InternalServerError(ex);
            }
        }
    }
}
