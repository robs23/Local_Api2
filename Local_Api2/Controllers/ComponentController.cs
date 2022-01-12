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
    public class ComponentController : ApiController
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [HttpGet]
        [Route("GetPlannedComponents")]
        [ResponseType(typeof(List<PlannedComponent>))]
        public IHttpActionResult GetPlannedComponents(string query = null)
        {
            try
            {
                using (OracleConnection Con = new Oracle.ManagedDataAccess.Client.OracleConnection(Static.Secrets.ApiConnectionString))
                {
                    if (Con.State == System.Data.ConnectionState.Closed)
                    {
                        Con.Open();
                    }

                    //make sure operation date is always indicated
                    //otherwise we can crash the db
                    if (query != null)
                    {
                        if (!query.Contains("OPERATION_DATE") && !query.Contains("OPERATION_WEEK") && !query.Contains("OPERATION_YEAR"))
                        {
                            query += $" AND (OPERATION_WEEK = {DateTime.Now.IsoWeekOfYear()}) AND (OPERATION_YEAR = {DateTime.Now.Year})";
                        }
                    }
                    else
                    {
                        query = $"(OPERATION_WEEK = {DateTime.Now.IsoWeekOfYear()}) AND (OPERATION_YEAR = {DateTime.Now.Year})";
                    }

                    string str = $@"SELECT OPERATION_DATE, OPERATION_DAY, OPERATION_WEEK, OPERATION_YEAR, SHIFT_ID, SHIFT_NAME, MACHINE_NR, OPERATION_NR, OPERATION_TYPE_NAME, 
	                            ORDER_NR, PRODUCT_NR, PRODUCT_NAME, PROD_TYPE, SUB_PROD_TYPE, ORDER_TYPE_CODE, ORDER_TYPE_NAME, BOM_NR, PRODUCT_QUANTITY, PRODUCT_QUANTITY_ALL
                                FROM QMESV_GRID_WIP_PROD_SUPPLY
                                WHERE {query} 
                                ORDER BY OPERATION_DATE, SHIFT_ID, ORDER_NR";


                    var Command = new Oracle.ManagedDataAccess.Client.OracleCommand(str, Con);

                    var reader = Command.ExecuteReader();

                    List<PlannedComponent> Plan = new List<PlannedComponent>();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            PlannedComponent c = new PlannedComponent();
                            c.OPERATION_DATE = Convert.ToDateTime(reader["OPERATION_DATE"].ToString());
                            c.OPERATION_DAY = Convert.ToDateTime(reader["OPERATION_DAY"].ToString());
                            c.OPERATION_WEEK = Convert.ToInt32(reader["OPERATION_WEEK"].ToString());
                            c.OPERATION_YEAR = Convert.ToInt32(reader["OPERATION_YEAR"].ToString());
                            c.SHIFT_ID = Convert.ToInt32(reader["SHIFT_ID"].ToString());
                            c.SHIFT_NAME = reader["SHIFT_NAME"].ToString();
                            c.MACHINE_NR = reader["MACHINE_NR"].ToString();
                            c.OPERATION_NR = reader["OPERATION_NR"].ToString();
                            c.OPERATION_TYPE_NAME = reader["OPERATION_TYPE_NAME"].ToString();
                            c.ORDER_NR = reader["ORDER_NR"].ToString();
                            c.PRODUCT_NR = reader["PRODUCT_NR"].ToString();
                            c.PRODUCT_NAME = reader["PRODUCT_NAME"].ToString();
                            c.PROD_TYPE = reader["PROD_TYPE"].ToString();
                            c.SUB_PROD_TYPE = reader["SUB_PROD_TYPE"].ToString();
                            c.ORDER_TYPE_CODE = reader["ORDER_TYPE_CODE"].ToString();
                            c.BOM_NR = reader["BOM_NR"].ToString();
                            c.PRODUCT_QUANTITY = Convert.ToInt64(reader["PRODUCT_QUANTITY"].ToString());
                            c.PRODUCT_QUANTITY_ALL = Convert.ToInt64(reader["PRODUCT_QUANTITY_ALL"].ToString());
                            Plan.Add(c);
                        }
                    }
                    else
                    {
                        return NotFound();
                    }
                    return Ok(Plan);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("GetPlannedComponents: Błąd. Szczegóły: {Message}", ex.ToString());
                return InternalServerError(ex);
            }
        }
    }
}