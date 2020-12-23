using Local_Api2.Models;
using Local_Api2.Static;
using NLog;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
                                EanType = 2
                            }
                            );
                    }
                    return Ok(Scans);
                }
                else
                {
                    using(OracleConnection Con = new Oracle.ManagedDataAccess.Client.OracleConnection(Static.Secrets.OracleConnectionString))
                    {
                        Logger.Debug("GetRecentScans has started");
                        var reader = GetRecentFoilScans(MachineId, Con);

                        List<ScanningItem> Scans = new List<ScanningItem>();
                        int index = 0;
                        int currentHour = 0;
                        int prevHour = -1;
                        int prevProduct = -1;
                        int currentProduct = 0;
                        string MachineName = "";
                        string MachineNumber = "";
                        if (reader.HasRows)
                        {
                            try
                            {
                                while (reader.Read())
                                {
                                    if (index == 0)
                                    {
                                        MachineName = reader["MACHINE_NR"].ToString();
                                        MachineNumber = MachineName.Substring(MachineName.Length - 2, 2);
                                    }
                                    index++;
                                    currentHour = Convert.ToInt32(reader[reader.GetOrdinal("SCAN_HOUR")].ToString());
                                    currentProduct = Convert.ToInt32(reader["PRODUCT_NR"].ToString());
                                    DateTime currentDate = DateTime.ParseExact(reader[reader.GetOrdinal("SCAN_DAY")].ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
                                    int currentQty = Convert.ToInt32(reader[reader.GetOrdinal("QUANTITY")].ToString());
                                    int efficiency = Convert.ToInt32(reader[reader.GetOrdinal("EFFICIENCY")].ToString());
                                    int max_efficiency = Convert.ToInt32(reader[reader.GetOrdinal("MAX_EFFICIENCY")].ToString());

                                    ScanningItem i = new ScanningItem();
                                    i.Id = index;
                                    i.Date = currentDate;
                                    i.ScanningHour = currentHour;
                                    i.QuantityFromFoil = currentQty;
                                    i.EanType = Convert.ToInt32(reader[reader.GetOrdinal("EAN_TYPE")].ToString());
                                    i.AssumedSpeed = efficiency / 60;
                                    i.GE = ((double)i.Quantity / (double)max_efficiency)*100;
                                    i.Zfin = currentProduct;
                                    i.NetWeight = Convert.ToDouble(reader[reader.GetOrdinal("WEIGHT_NETTO")].ToString());
                                    i.MachineName = MachineName;
                                    if (currentHour == prevHour)
                                    {
                                        i.ChangeOvers = 1;
                                        Scans.Last().ChangeOvers = 1;
                                        if (Scans.Count >= 2)
                                        {
                                            if (currentProduct == Scans[Scans.Count - 2].Zfin)
                                            {
                                                Scans.Last().Id = index;
                                                Scans.Insert(Scans.Count - 1, i);
                                            }
                                            else
                                            {
                                                Scans.Add(i);
                                            }
                                        }
                                        
                                    }
                                    else
                                    {
                                        Scans.Add(i);
                                    }

                                    prevHour = currentHour;
                                    prevProduct = currentProduct;
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Błąd podczas czytania rekordów ze skanera folii dla maszyny {machine}. Szczegóły: {ex}", reader[reader.GetOrdinal("MACHINE_NR")].ToString(), ex);
                                throw;
                            }

                            var readerB = GetRecentBoxesScans(MachineId, Con);
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
                            var readerC = Utilities.GetRecentProductData(MachineId, Con);
                            if (readerC.HasRows)
                            {
                                int currentZfin;
                                DateTime start;
                                DateTime? end;
                                string[] formats = { "yyyy-MM-dd HH:mm:ss", "dd.MM.yyyy HH:mm:ss" };
                                
                                while (readerC.Read())
                                {
                                    currentZfin = Convert.ToInt32(readerC["PRODUCT_NR"].ToString());
                                    try
                                    {
                                        start = DateTime.ParseExact(readerC[readerC.GetOrdinal("STARTED_DATE")].ToString(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None);
                                    }catch(Exception ex)
                                    {
                                        Logger.Error("Start Date {Date} has invalid format. Szczegóły: {ex}",readerC[readerC.GetOrdinal("STARTED_DATE")].ToString(), ex);
                                        throw;
                                    }
                                    
                                    if (readerC.IsDBNull(readerC.GetOrdinal("FINISHED_DATE")))
                                    {
                                        end = DateTime.Now;
                                    }
                                    else
                                    {
                                        try
                                        {
                                            end = DateTime.ParseExact(readerC[readerC.GetOrdinal("FINISHED_DATE")].ToString(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None);
                                        }catch(Exception ex)
                                        {
                                            Logger.Error("Finish Date {Date} has invalid format. Szczegóły: {ex}", readerC[readerC.GetOrdinal("FINISHED_DATE")].ToString(), ex);
                                            throw;
                                        }
                                        
                                    }
                                    
                                    DateTime rndStart = new DateTime(start.Year, start.Month, start.Day, start.Hour, 0, 0);
                                    DateTime rndEnd = ((DateTime)end).AddHours(1);

                                    foreach (ScanningItem si in Scans.Where(s => s.Zfin == currentZfin))
                                    {

                                        try
                                        {
                                            DateTime siDate = new DateTime(si.Date.Year, si.Date.Month, si.Date.Day, si.ScanningHour, 0, 0);
                                            if (siDate >= rndStart && siDate <= rndEnd)
                                            {
                                                si.ConfirmedKg = readerC.GetDouble(readerC.GetOrdinal("QUANTITY_KG"));
                                            }
                                        }
                                        catch (Exception ex)
                                        {

                                            throw;
                                        }
                                    }
                                }
                            }
                            using(SqlConnection XRayConn = new SqlConnection(Static.Secrets.xRayConnectionString))
                            {
                                using (var readerD = Utilities.GetRecentXrayData(MachineNumber, XRayConn))
                                {
                                    if (readerD.HasRows)
                                    {
                                        int currentZfin;
                                        DateTime start;
                                        DateTime end;

                                        while (readerD.Read())
                                        {
                                            currentZfin = Convert.ToInt32(readerD["ArticleName"].ToString());
                                            try
                                            {
                                                start = readerD.GetDateTime(readerD.GetOrdinal("ProductionStart"));
                                                end = readerD.GetDateTime(readerD.GetOrdinal("ProductionEnd"));
                                                DateTime rndStart = new DateTime(start.Year, start.Month, start.Day, start.Hour, 0, 0);
                                                DateTime rndEnd = ((DateTime)end).AddHours(1);
                                                foreach (ScanningItem si in Scans.Where(s => s.Zfin == currentZfin))
                                                {

                                                    try
                                                    {
                                                        DateTime siDate = new DateTime(si.Date.Year, si.Date.Month, si.Date.Day, si.ScanningHour, 0, 0);
                                                        if (siDate >= rndStart && siDate <= rndEnd)
                                                        {
                                                            si.Contaminated = Convert.ToInt32(readerD["CounterContaminated"].ToString());
                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {

                                                        throw;
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Logger.Error("Błąd podczas czytania rekordów z XRaya dla maszyny {machine}. Szczegóły: {ex}", readerD[readerD.GetOrdinal("DeviceName")].ToString(), ex);
                                                throw;
                                            }
                                        }
                                    }
                                    
                                }
                            }
                            using(SqlConnection NpdConnection = new SqlConnection(Static.Secrets.NpdConnectionString))
                            {
                                using(var readerE = Utilities.GetRecentComponentScrap(NpdConnection, 2))
                                {
                                    //Get foil scrap
                                    int currentZfin; 

                                    while (readerE.Read())
                                    {
                                        currentZfin = Convert.ToInt32(readerE["ZfinIndex"].ToString());
                                        double? scrap = readerE.IsDBNull(readerE.GetOrdinal("scrap")) ? new double?() : readerE.GetDouble(readerE.GetOrdinal("scrap"));
                                        if (scrap != null)
                                        {
                                            foreach(ScanningItem si in Scans.Where(i=> i.Zfin == currentZfin))
                                            {
                                                si.AssumedFoilLossPercentage = scrap;
                                            }
                                        }
                                    }
                                }
                            }
                            using(NpgsqlConnection FenixConnection = new NpgsqlConnection(Static.Secrets.FenixConnectionString))
                            {
                                using(var readerF = GetRecentOverweights(MachineId, FenixConnection))
                                {

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
            }
            catch (Exception ex)
            {
                Logger.Error("GetRecentScans: Błąd. Szczegóły: {Message}", ex.ToString());
                return InternalServerError(ex);
            }
        }

        private OracleDataReader GetRecentBoxesScans(int MachineId, OracleConnection Con)
        {
            Logger.Debug("GetRecentBoxesScans started for Machine={MachineId}", MachineId);

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
                new OracleParameter("StartDate", Utilities.GetStartDate()),
            };
            Command.Parameters.AddRange(parameters);

            var reader = Command.ExecuteReader();
            Logger.Debug("GetRecentBoxesScans ended");

            return reader;
        }

        private OracleDataReader GetRecentFoilScans(int MachineId, OracleConnection Con)
        {
            Logger.Debug("GetRecentFoilsScans started for Machine={MachineId}", MachineId);

            if (Con.State == System.Data.ConnectionState.Closed)
            {
                Con.Open();
            }


            string str = $@"SELECT        scan.MACHINE_ID, mach.MACHINE_NR, to_char(scan.C_DATE, 'HH24') AS SCAN_HOUR, to_char(scan.C_DATE, 'YYYY-MM-DD') AS SCAN_DAY, SUM(scan.SCAN_COUNT) AS QUANTITY, uom.WEIGHT_NETTO, SUM(scan.ERROR_COUNT) AS ERROR, 
                         scan.EAN_TYPE, op_qty.PRODUCT_ID, prod.PRODUCT_NR, CAST(ef.EFFICIENCY AS INT) AS EFFICIENCY, CAST(ef.MAX_EFFICIENCY AS INT) AS MAX_EFFICIENCY 
                         FROM QMES_WIP_SCAN_COUNT scan LEFT OUTER JOIN
                         QMES_WIP_OPERATION_QTY op_qty ON op_qty.OPERATION_ID = scan.OPERATION_ID LEFT OUTER JOIN
                         QCM_PRODUCTS prod ON prod.PRODUCT_ID = op_qty.PRODUCT_ID LEFT OUTER JOIN
                         QCM_PACKAGE_HEADERS pack ON pack.PRODUCT_ID = prod.PRODUCT_ID LEFT OUTER JOIN
                         QCM_PACKAGE_LEVELS uom ON uom.PACKAGE_ID = pack.PACKAGE_ID LEFT OUTER JOIN
                         QMES_FO_MACHINE_EFFICIENCY ef ON ef.MACHINE_ID = scan.MACHINE_ID AND ef.PRODUCT_ID = op_qty.PRODUCT_ID LEFT OUTER JOIN 
                         QMES_FO_MACHINE mach ON mach.MACHINE_ID = scan.MACHINE_ID 
                         WHERE (scan.C_DATE >= :StartDate) AND (scan.MACHINE_ID = {MachineId}) AND (scan.EAN_TYPE=2) AND (uom.LEVEL_NR = 0)
                         GROUP BY scan.MACHINE_ID, mach.MACHINE_NR, to_char(scan.C_DATE, 'HH24'), to_char(scan.C_DATE, 'YYYY-MM-DD'), scan.EAN_TYPE, op_qty.PRODUCT_ID, prod.PRODUCT_NR, uom.WEIGHT_NETTO, ef.EFFICIENCY, ef.MAX_EFFICIENCY
                         ORDER BY SCAN_DAY DESC, SCAN_HOUR DESC";

            var Command = new Oracle.ManagedDataAccess.Client.OracleCommand(str, Con);

            OracleParameter[] parameters = new OracleParameter[]
            {
                new OracleParameter("StartDate", Utilities.GetStartDate()),
            };
            Command.Parameters.AddRange(parameters);

            var reader = Command.ExecuteReader();
            Logger.Debug("GetRecentFoilsScans finished");

            return reader;
        }

        private NpgsqlDataReader GetRecentOverweights(int MachineId, NpgsqlConnection Con)
        {
            if (Con.State == System.Data.ConnectionState.Closed)
            {
                Con.Open();
            }
            string str = $@"";

            var Command = new NpgsqlCommand(str, Con);
            var reader = Command.ExecuteReader();
            return reader;
        }
    }
}
