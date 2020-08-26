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
    public class ScanningController : ApiController
    {
        private OracleConnection Con = new Oracle.ManagedDataAccess.Client.OracleConnection(Static.Secrets.ApiConnectionString);
        
        [HttpGet]
        [Route("GetRecentScans")]
        [ResponseType(typeof(List<ScanningItem>))]

        public IHttpActionResult GetRecentScans(int MachineId)
        {
            if (RuntimeSettings.MockServer)
            {
                List<ScanningItem> Scans = new List<ScanningItem>();
                for (int i = 1; i < 24; i++)
                {
                    Scans.Add(
                        new ScanningItem
                        {
                            Id = i,
                            ScanningHour = i,
                            Date = DateTime.Today,
                            EanType = 2,
                            Quantity = i * 100,
                            QuantityKg = i * 50,
                            Speed = (i * 100) / 60
                        }
                        );
                }
                return Ok(Scans);
            }
            else
            {
                if (Con.State == System.Data.ConnectionState.Closed)
                {
                    Con.Open();
                }

                string str = @"SELECT        scan.MACHINE_ID, to_char(scan.C_DATE, 'HH24') AS SCAN_HOUR, to_char(scan.C_DATE, 'YYYY-MM-DD') AS SCAN_DAY, SUM(scan.SCAN_COUNT) AS QUANTITY, uom.WEIGHT_NETTO, SUM(scan.ERROR_COUNT) AS ERROR, 
                            scan.EAN_TYPE, op_qty.PRODUCT_ID, prod.PRODUCT_NR
                            FROM QMES_WIP_SCAN_COUNT scan LEFT OUTER JOIN
                            QMES_WIP_OPERATION_QTY op_qty ON op_qty.OPERATION_ID = scan.OPERATION_ID LEFT OUTER JOIN
                            QCM_PRODUCTS prod ON prod.PRODUCT_ID = op_qty.PRODUCT_ID LEFT OUTER JOIN
                            QCM_PACKAGE_HEADERS pack ON pack.PRODUCT_ID = prod.PRODUCT_ID LEFT OUTER JOIN
                            QCM_PACKAGE_LEVELS uom ON uom.PACKAGE_ID = pack.PACKAGE_ID
                            WHERE (scan.C_DATE >= '2020-08-25') AND (scan.MACHINE_ID = 326) AND (scan.EAN_TYPE = 2) AND (uom.LEVEL_NR = 0)
                            GROUP BY scan.MACHINE_ID, to_char(scan.C_DATE, 'HH24'), to_char(scan.C_DATE, 'YYYY-MM-DD'), scan.EAN_TYPE, op_qty.PRODUCT_ID, prod.PRODUCT_NR, uom.WEIGHT_NETTO
                            ORDER BY SCAN_DAY, SCAN_HOUR";

                var Command = new Oracle.ManagedDataAccess.Client.OracleCommand(str, Con);

                var reader = Command.ExecuteReader();

                List<ScanningItem> Scans = new List<ScanningItem>();
                int index = 0;

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        index++;
                        ScanningItem i = new ScanningItem();
                        i.Id = index;
                        i.Date = 
                        m.Name = reader[reader.GetOrdinal("MACHINE_NR")].ToString();
                        m.State = reader[reader.GetOrdinal("STATE")].ToString();
                        m.Type = Convert.ToInt32(reader[reader.GetOrdinal("MACHINE_TYPE_ID")].ToString());
                        m.VisibleInAPS = reader[reader.GetOrdinal("IS_VISIBLE_APS")].ToString() == "T" ? true : false;
                        Scans.Add(m);
                    }
                    return Ok(Scans);
                }
                else
                {
                    return NotFound();
                }
            }
        }

    }
}
