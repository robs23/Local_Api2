using Local_Api2.Models;
using NLog;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;

namespace Local_Api2.Controllers
{
    public class StockController : ApiController
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [HttpGet]
        [Route("GetStocks")]
        [ResponseType(typeof(List<StockPallet>))]
        public IHttpActionResult GetStocks(string query = null)
        {

            try
            {
                
                using (OracleConnection Con = new Oracle.ManagedDataAccess.Client.OracleConnection(Static.Secrets.OracleConnectionString))
                {
                    if (Con.State == System.Data.ConnectionState.Closed)
                    {
                        Con.Open();
                    }

                    string str = @"SELECT
                                  LU.LOADUNIT_NR,
                                  SPC.IS_BULK,
                                  LU.LOADUNIT_ID,
                                  SP.SP_NR,
                                  SP.SP_ID,
                                  SPC.NAME AS SPC_NAME,
                                  LUC.POSITION_NR,
                                  P.PRODUCT_NR,
                                  P.name,
                                  P.PRODUCT_ID,
                                  LUC.PROD_SERIAL_ID,
                                  PS.SERIAL_NR,
                                  LUC.DATE_EXPIRE,
                                  LUC.BU_QUANTITY,
                                  PKGL_FL.BU_QUANTITY AS MAX_BU_QUANTITY,
                                  PKGL_BU.INFO AS OPAKOWANIE,
                                  PKGH.HAS_FULL_LU,
                                  V_SUM_Q.BU_QUANTITY_LU,
                                  SA.wh_id,
                                  LU.status_quality,
                                  LU.c_Date
                                FROM
                                  LOAD_UNITS_FAST LU JOIN QWHV_FULFILL_AUX V_SUM_Q ON V_SUM_Q.loadunit_id = LU.LOADUNIT_ID
                                  JOIN LU_CONTENTS_FAST LUC ON LU.LOADUNIT_ID = LUC.LOADUNIT_ID 
                                  JOIN QCM_PRODUCTS P ON LUC.PRODUCT_ID = P.PRODUCT_ID 
                                  JOIN QCM_PROD_SERIALS PS ON LUC.PRODUCT_ID = PS.PRODUCT_ID AND LUC.PROD_SERIAL_ID = PS.PROD_SERIAL_ID
                                  JOIN STORAGEPLACES SP ON LU.SP_ID = SP.SP_ID 
                                  JOIN SP_CLASSES SPC ON SP.SP_CLASS_ID = SPC.SP_CLASS_ID 
                                  JOIN QCM_PACKAGE_HEADERS PKGH ON P.PRODUCT_ID = PKGH.PRODUCT_ID 
                                  JOIN QCM_PACKAGE_LEVELS PKGL_BU ON PKGH.PACKAGE_ID = PKGL_BU.PACKAGE_ID
                                  JOIN STORAGEAREAS SA ON SA.SA_ID = SP.SA_ID 
                                  JOIN SA_CLASSES SAC ON SA.SA_CLASS_ID = SAC.SA_CLASS_ID 
                                  LEFT OUTER JOIN QCM_PACKAGE_LEVELS PKGL_FL ON PKGL_FL.PACKAGE_ID = PKGH.PACKAGE_ID 
                                WHERE
                                   PKGL_BU.LEVEL_NR = 0  AND 
                                   SAC.IS_STORAGE = 'T' AND
                                   PKGL_BU.status = 'OK' AND 
                                   PKGL_FL.is_full_lu = 'T' AND 
                                   PKGL_FL.status = 'OK'";
                    if (query != null)
                    {
                        str += $" AND {query}";
                    }

                    var Command = new Oracle.ManagedDataAccess.Client.OracleCommand(str, Con);

                    var reader = Command.ExecuteReader();

                    List<StockPallet> Stocks = new List<StockPallet>();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            StockPallet s = new StockPallet();
                            s.LOADUNIT_NR = reader["LOADUNIT_NR"].ToString();
                            s.SP_NR = reader["SP_NR"].ToString();
                            s.PRODUCT_ID = Convert.ToInt64(reader["PRODUCT_ID"].ToString());
                            s.PRODUCT_NR = reader["PRODUCT_NR"].ToString();
                            s.NAME = reader["NAME"].ToString();
                            s.SERIAL_NR = reader["SERIAL_NR"].ToString();
                            s.DATE_EXPIRE = Convert.ToDateTime(reader["DATE_EXPIRE"].ToString());
                            s.BU_QUANTITY = Convert.ToInt32(reader["BU_QUANTITY"].ToString());
                            s.STATUS_QUALITY = Convert.ToInt32(reader["STATUS_QUALITY"].ToString());
                            s.C_DATE = Convert.ToDateTime(reader["C_DATE"].ToString());
                            Stocks.Add(s);
                        }
                        return Ok(Stocks);
                    }
                    else
                    {
                        Logger.Info("GetStocks: Nie znaleziono zapasów");
                        return NotFound();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("GetStocks: Błąd. Szczegóły: {Message}", ex.ToString());
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("GetStocksByProduct")]
        [ResponseType(typeof(List<StockProduct>))]
        public IHttpActionResult GetStocksByProduct(string query = null)
        {

            try
            {

                using (OracleConnection Con = new Oracle.ManagedDataAccess.Client.OracleConnection(Static.Secrets.OracleConnectionString))
                {
                    if (Con.State == System.Data.ConnectionState.Closed)
                    {
                        Con.Open();
                    }

                    string qString = "";

                    if (query != null)
                    {
                        qString += $" AND {query}";
                    }

                    string str = $@"SELECT        COUNT(LU.LOADUNIT_NR) AS LOADUNIT_NR, P.PRODUCT_NR, P.NAME, P.PRODUCT_ID, SUM(LUC.BU_QUANTITY) AS BU_QUANTITY, LU.STATUS_QUALITY
                                    FROM            LOAD_UNITS_FAST LU INNER JOIN
                                                             QWHV_FULFILL_AUX V_SUM_Q ON V_SUM_Q.LOADUNIT_ID = LU.LOADUNIT_ID INNER JOIN
                                                             LU_CONTENTS_FAST LUC ON LU.LOADUNIT_ID = LUC.LOADUNIT_ID INNER JOIN
                                                             QCM_PRODUCTS P ON LUC.PRODUCT_ID = P.PRODUCT_ID INNER JOIN
                                                             QCM_PROD_SERIALS PS ON LUC.PRODUCT_ID = PS.PRODUCT_ID AND LUC.PROD_SERIAL_ID = PS.PROD_SERIAL_ID INNER JOIN
                                                             STORAGEPLACES SP ON LU.SP_ID = SP.SP_ID INNER JOIN
                                                             SP_CLASSES SPC ON SP.SP_CLASS_ID = SPC.SP_CLASS_ID INNER JOIN
                                                             QCM_PACKAGE_HEADERS PKGH ON P.PRODUCT_ID = PKGH.PRODUCT_ID INNER JOIN
                                                             QCM_PACKAGE_LEVELS PKGL_BU ON PKGH.PACKAGE_ID = PKGL_BU.PACKAGE_ID INNER JOIN
                                                             STORAGEAREAS SA ON SA.SA_ID = SP.SA_ID INNER JOIN
                                                             SA_CLASSES SAC ON SA.SA_CLASS_ID = SAC.SA_CLASS_ID LEFT OUTER JOIN
                                                             QCM_PACKAGE_LEVELS PKGL_FL ON PKGL_FL.PACKAGE_ID = PKGH.PACKAGE_ID
                                    WHERE        (PKGL_BU.LEVEL_NR = 0) AND (SAC.IS_STORAGE = 'T') AND (PKGL_BU.STATUS = 'OK') AND (PKGL_FL.IS_FULL_LU = 'T') AND (PKGL_FL.STATUS = 'OK') {qString}
                                    GROUP BY P.PRODUCT_NR, P.NAME, P.PRODUCT_ID, LU.STATUS_QUALITY";
                    

                    var Command = new Oracle.ManagedDataAccess.Client.OracleCommand(str, Con);

                    var reader = Command.ExecuteReader();

                    List<StockProduct> Stocks = new List<StockProduct>();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            StockProduct s = new StockProduct();
                            s.LOADUNIT_NR = Convert.ToInt32(reader["LOADUNIT_NR"].ToString());
                            s.PRODUCT_ID = Convert.ToInt64(reader["PRODUCT_ID"].ToString());
                            s.PRODUCT_NR = reader["PRODUCT_NR"].ToString();
                            s.NAME = reader["NAME"].ToString();
                            s.BU_QUANTITY = Convert.ToInt32(reader["BU_QUANTITY"].ToString());
                            s.STATUS_QUALITY = Convert.ToInt32(reader["STATUS_QUALITY"].ToString());
                            Stocks.Add(s);
                        }
                        return Ok(Stocks);
                    }
                    else
                    {
                        Logger.Info("GetStocksByProduct: Nie znaleziono zapasów");
                        return NotFound();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("GetStocksByProduct: Błąd. Szczegóły: {Message}", ex.ToString());
                return InternalServerError(ex);
            }
        }
    }
}
