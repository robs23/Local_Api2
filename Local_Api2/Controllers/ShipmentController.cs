using Local_Api2.Models;
using Local_Api2.Static;
using Microsoft.ApplicationInsights.Web;
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
    public class ShipmentController : ApiController
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [HttpGet]
        [Route("GetShipments")]
        [ResponseType(typeof(List<Shipment>))]
        public IHttpActionResult GetShipments(string DeliveryNote = null, string query = null)
        {
            try
            {
                using (OracleConnection Con = new Oracle.ManagedDataAccess.Client.OracleConnection(Static.Secrets.OracleConnectionString))
                {
                    if (Con.State == System.Data.ConnectionState.Closed)
                    {
                        Con.Open();
                    }

                    if (query == null && DeliveryNote == null)
                    {
                        query = $"(d.DATE_EMITTED >= '{DateTime.Now.StartOfWeek().ToString("yyyy-MM-dd HH:mm:ss")}')";
                    }
                    else if(query != null)
                    {
                        if (!query.Contains("DATE_EMITTED"))
                        {
                            query += $" AND (d.DATE_EMITTED >= '{DateTime.Now.StartOfWeek().ToString("yyyy-MM-dd HH:mm:ss")}')";
                        }
                    }

                    if(DeliveryNote != null)
                    {
                        if (string.IsNullOrEmpty(query))
                        {
                            query = $"d.C_ORDER_NR = '{DeliveryNote}'";
                        }
                        else
                        {
                            query += $" AND d.C_ORDER_NR = '{DeliveryNote}'";
                        }
                    }

                    string str = $@"SELECT d.DOC_ID, d.DOC_TYPE_NR, d.DATE_EMITTED, d.FIRM_ID, d.ADR_STREET, d.ADR_ZIPCODE, d.ADR_CITY, d.C_ORDER_NR, di.DOC_ITEM_ID, di.PRODUCT_ID, pr.PRODUCT_NR, pr.NAME, di.PROD_SERIAL_ID, s.SERIAL_NR, di.QUANTITY, di.WEIGHT, di.WEIGHT_NETTO 
                                FROM DOCUMENTS d LEFT JOIN DOCUMENT_ITEMS di ON d.DOC_ID = di.DOC_ID 
	                                LEFT JOIN QCM_PRODUCTS pr ON pr.PRODUCT_ID = di.PRODUCT_ID 
	                                LEFT JOIN QCM_PROD_SERIALS s ON s.PROD_SERIAL_ID = di.PROD_SERIAL_ID 
                                WHERE DOC_TYPE_NR='WHD_WZ' AND {query}";

                    var Command = new Oracle.ManagedDataAccess.Client.OracleCommand(str, Con);

                    var reader = Command.ExecuteReader();

                    List<Shipment> Shipments = new List<Shipment>();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            if(!Shipments.Any(ship=>ship.DOC_ID == Convert.ToInt64(reader["DOC_ID"].ToString())))
                            {
                                //new shipment
                                Shipment sh = new Shipment();
                                sh.DOC_ID = Convert.ToInt64(reader["DOC_ID"].ToString());
                                sh.DOC_TYPE_NR = reader["DOC_TYPE_NR"].ToString();
                                sh.C_ORDER_NR = reader["C_ORDER_NR"].ToString();
                                sh.DATE_EMITTED = Convert.ToDateTime(reader["DATE_EMITTED"].ToString());
                                sh.FIRM_ID = Convert.ToInt64(reader["FIRM_ID"].ToString());
                                sh.ADR_STREET = reader["ADR_STREET"].ToString();
                                sh.ADR_ZIPCODE = reader["ADR_ZIPCODE"].ToString();
                                sh.ADR_CITY = reader["ADR_CITY"].ToString();
                                sh.WEIGHT = 0;
                                sh.WEIGHT_NETTO = 0;
                                sh.Items = new List<ShipmentItem>();
                                Shipments.Add(sh);
                            }
                            ShipmentItem si = new ShipmentItem();
                            si.DOC_ID = Convert.ToInt64(reader["DOC_ID"].ToString());
                            si.DOC_ITEM_ID = Convert.ToInt64(reader["DOC_ITEM_ID"].ToString());
                            si.PRODUCT_ID = Convert.ToInt64(reader["PRODUCT_ID"].ToString());
                            si.PRODUCT_NR = reader["PRODUCT_NR"].ToString();
                            si.NAME = reader["NAME"].ToString();
                            si.PROD_SERIAL_ID = Convert.ToInt64(reader["PROD_SERIAL_ID"].ToString());
                            si.SERIAL_NR = reader["SERIAL_NR"].ToString();
                            si.QUANTITY = Convert.ToInt64(reader["QUANTITY"].ToString());
                            si.WEIGHT = Convert.ToDouble(reader["WEIGHT"].ToString());
                            si.WEIGHT_NETTO = Convert.ToDouble(reader["WEIGHT_NETTO"].ToString());
                            Shipment s = Shipments.Where(sh => sh.DOC_ID == Convert.ToInt64(reader["DOC_ID"].ToString())).FirstOrDefault();
                            s.WEIGHT += si.WEIGHT;
                            s.WEIGHT_NETTO += si.WEIGHT_NETTO;
                            s.Items.Add(si);
                            
                        }
                        return Ok(Shipments);
                    }
                    else
                    {
                        Logger.Info("GetShipments: Porażka, nie znaleziono nic..");
                        return NotFound();
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Error("GetShipments: Błąd. Szczegóły: {Message}", ex.ToString());
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("GetShipmentGroups")]
        [ResponseType(typeof(List<ShipmentGroup>))]
        public IHttpActionResult GetShipmentGroups()
        {
            try
            {
                List<ShipmentGroup> Items = Utilities.GetShipmentGroups();
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
                return InternalServerError(ex);
            }

        }
    }
}
