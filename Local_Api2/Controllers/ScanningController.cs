using Local_Api2.Models;
using Local_Api2.Static;
using NLog;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Description;

namespace Local_Api2.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class ScanningController : ApiController
    {
        private OracleConnection Con = new Oracle.ManagedDataAccess.Client.OracleConnection(Static.Secrets.ApiConnectionString);
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [HttpGet]
        [Route("GetRecentScans")]
        [ResponseType(typeof(List<ScanningItem>))]

        public IHttpActionResult GetRecentScans(int MachineId)
        {
            try
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
                    var reader = GetRecentFoilScans(MachineId);

                    List<ScanningItem> Scans = new List<ScanningItem>();
                    int index = 0;
                    int prevHour = -1;
                    int currentHour = 0;

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            index++;
                            currentHour = Convert.ToInt32(reader[reader.GetOrdinal("SCAN_HOUR")].ToString());
                            DateTime currentDate = DateTime.ParseExact(reader[reader.GetOrdinal("SCAN_DAY")].ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
                            int currentQty = Convert.ToInt32(reader[reader.GetOrdinal("QUANTITY")].ToString());
                            double currentQtyKg = Convert.ToDouble(reader[reader.GetOrdinal("WEIGHT_NETTO")].ToString()) * currentQty;
                            int currentMinutes = 60;
                            int efficiency = Convert.ToInt32(reader[reader.GetOrdinal("EFFICIENCY")].ToString());
                            if (currentDate == DateTime.Now.Date && currentHour == DateTime.Now.Hour)
                            {
                                currentMinutes = DateTime.Now.Minute;
                                if (currentMinutes == 0) { currentMinutes = 1; }
                            }
                            if (prevHour == currentHour)
                            {
                                //current hour is the same hour, let's combine them
                                if (Scans.Any(s => s.Date == currentDate && s.ScanningHour == currentHour))
                                {
                                    ScanningItem _s = Scans.FirstOrDefault(s => s.Date == currentDate && s.ScanningHour == currentHour);
                                    _s.Quantity += currentQty;
                                    _s.QuantityKg += currentQtyKg;
                                    _s.Speed = _s.Quantity / currentMinutes;
                                    _s.ChangeOvers++;
                                    _s.AssumedSpeed = (_s.AssumedSpeed + (efficiency / 60)) / (_s.ChangeOvers + 1);
                                }
                            }
                            else
                            {
                                ScanningItem i = new ScanningItem();
                                i.Id = index;
                                i.Date = currentDate;
                                i.ScanningHour = currentHour;
                                i.Quantity = currentQty;
                                i.QuantityKg = currentQtyKg;
                                i.Speed = i.Quantity / currentMinutes;
                                i.EanType = Convert.ToInt32(reader[reader.GetOrdinal("EAN_TYPE")].ToString());
                                i.AssumedSpeed = efficiency / 60;
                                Scans.Add(i);
                            }
                            prevHour = currentHour;

                        }

                        var readerB = GetRecentBoxesScans(MachineId);
                        if (readerB.HasRows)
                        {
                            while (readerB.Read())
                            {
                                currentHour = Convert.ToInt32(readerB[readerB.GetOrdinal("SCAN_HOUR")].ToString());
                                DateTime currentDate = DateTime.ParseExact(readerB[readerB.GetOrdinal("SCAN_DAY")].ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
                                if (Scans.Any(s => s.Date == currentDate && s.ScanningHour == currentHour))
                                {
                                    //update item with data from boxes scanner
                                    Scans.Where(s => s.Date == currentDate && s.ScanningHour == currentHour).FirstOrDefault().QuantityFromBoxes += (Convert.ToInt32(readerB[readerB.GetOrdinal("QUANTITY")].ToString()) * Convert.ToInt32(readerB[readerB.GetOrdinal("BU_QUANTITY")].ToString()));
                                }
                            }
                        }
                        Logger.Info("GetRecentScans: Sukces, zwracam {count} skanów dla maszyny {MachineId}", Scans.Count, MachineId);
                        return Ok(Scans);
                    }
                    else
                    {
                        Logger.Info("GetRecentScans: Sukces, brak skanów dla maszyny {MachineId}", MachineId);
                        return NotFound();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("GetRecentScans: Błąd. Szczegóły: {Message}", ex.ToString());
                return InternalServerError(ex);
            }
        }

        private OracleDataReader GetRecentBoxesScans(int MachineId)
        {
            if (Con.State == System.Data.ConnectionState.Closed)
            {
                Con.Open();
            }

            string str = $@"SELECT        scan.MACHINE_ID, to_char(scan.C_DATE, 'HH24') AS SCAN_HOUR, to_char(scan.C_DATE, 'YYYY-MM-DD') AS SCAN_DAY, SUM(scan.SCAN_COUNT) AS QUANTITY, uom.BU_QUANTITY, SUM(scan.ERROR_COUNT) AS ERROR, 
                            scan.EAN_TYPE, op_qty.PRODUCT_ID, prod.PRODUCT_NR
                            FROM QMES_WIP_SCAN_COUNT scan LEFT OUTER JOIN
                            QMES_WIP_OPERATION_QTY op_qty ON op_qty.OPERATION_ID = scan.OPERATION_ID LEFT OUTER JOIN
                            QCM_PRODUCTS prod ON prod.PRODUCT_ID = op_qty.PRODUCT_ID LEFT OUTER JOIN
                            QCM_PACKAGE_HEADERS pack ON pack.PRODUCT_ID = prod.PRODUCT_ID LEFT OUTER JOIN
                            QCM_PACKAGE_LEVELS uom ON uom.PACKAGE_ID = pack.PACKAGE_ID
                            WHERE (scan.C_DATE >= :StartDate) AND (scan.MACHINE_ID = {MachineId}) AND (scan.EAN_TYPE=1) AND (uom.LEVEL_NR = 1)
                            GROUP BY scan.MACHINE_ID, to_char(scan.C_DATE, 'HH24'), to_char(scan.C_DATE, 'YYYY-MM-DD'), scan.EAN_TYPE, op_qty.PRODUCT_ID, prod.PRODUCT_NR, uom.BU_QUANTITY
                            ORDER BY SCAN_DAY DESC, SCAN_HOUR DESC";

            var Command = new Oracle.ManagedDataAccess.Client.OracleCommand(str, Con);

            OracleParameter[] parameters = new OracleParameter[]
            {
                new OracleParameter("StartDate", DateTime.Now.AddDays(-1)),
            };
            Command.Parameters.AddRange(parameters);

            var reader = Command.ExecuteReader();
            return reader;
        }

        private OracleDataReader GetRecentFoilScans(int MachineId)
        {
            if (Con.State == System.Data.ConnectionState.Closed)
            {
                Con.Open();
            }


            string str = $@"SELECT        scan.MACHINE_ID, to_char(scan.C_DATE, 'HH24') AS SCAN_HOUR, to_char(scan.C_DATE, 'YYYY-MM-DD') AS SCAN_DAY, SUM(scan.SCAN_COUNT) AS QUANTITY, uom.WEIGHT_NETTO, SUM(scan.ERROR_COUNT) AS ERROR, 
                         scan.EAN_TYPE, op_qty.PRODUCT_ID, prod.PRODUCT_NR, CAST(ef.EFFICIENCY AS INT) AS EFFICIENCY 
                         FROM QMES_WIP_SCAN_COUNT scan LEFT OUTER JOIN
                         QMES_WIP_OPERATION_QTY op_qty ON op_qty.OPERATION_ID = scan.OPERATION_ID LEFT OUTER JOIN
                         QCM_PRODUCTS prod ON prod.PRODUCT_ID = op_qty.PRODUCT_ID LEFT OUTER JOIN
                         QCM_PACKAGE_HEADERS pack ON pack.PRODUCT_ID = prod.PRODUCT_ID LEFT OUTER JOIN
                         QCM_PACKAGE_LEVELS uom ON uom.PACKAGE_ID = pack.PACKAGE_ID LEFT OUTER JOIN
                         QMES_FO_MACHINE_EFFICIENCY ef ON ef.MACHINE_ID = scan.MACHINE_ID AND ef.PRODUCT_ID = op_qty.PRODUCT_ID
                         WHERE (scan.C_DATE >= :StartDate) AND (scan.MACHINE_ID = {MachineId}) AND (scan.EAN_TYPE=2) AND (uom.LEVEL_NR = 0)
                         GROUP BY scan.MACHINE_ID, to_char(scan.C_DATE, 'HH24'), to_char(scan.C_DATE, 'YYYY-MM-DD'), scan.EAN_TYPE, op_qty.PRODUCT_ID, prod.PRODUCT_NR, uom.WEIGHT_NETTO, ef.EFFICIENCY
                         ORDER BY SCAN_DAY DESC, SCAN_HOUR DESC";

            var Command = new Oracle.ManagedDataAccess.Client.OracleCommand(str, Con);

            OracleParameter[] parameters = new OracleParameter[]
            {
                new OracleParameter("StartDate", DateTime.Now.AddDays(-1)),
            };
            Command.Parameters.AddRange(parameters);

            var reader = Command.ExecuteReader();
            return reader;
        }
    }
}
