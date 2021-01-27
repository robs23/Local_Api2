using Local_Api2.Models;
using NLog;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Web;

namespace Local_Api2.Static
{
    public static class Utilities
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public static OracleDataReader GetRecentProductData(int MachineId, OracleConnection Con)
        {
            Logger.Debug("GetRecentProductData started for Machine={MachineId}", MachineId);

            if (Con.State == System.Data.ConnectionState.Closed)
            {
                Con.Open();
            }

            string sql = $@"SELECT SUM(op_qty.QUANTITY) AS QUANTITY, SUM(uom.WEIGHT_NETTO * op_qty.QUANTITY) AS QUANTITY_KG, prod.PRODUCT_ID, prod.PRODUCT_NR, MIN(operation.STARTED_DATE) AS STARTED_DATE, 
                         MAX(operation.FINISHED_DATE) AS FINISHED_DATE
                         FROM QMES_FO_MACHINE mach LEFT OUTER JOIN
                         QMES_WIP_OPERATION operation ON mach.MACHINE_ID = operation.MACHINE_ID LEFT OUTER JOIN
                         QMES_WIP_ORDER ord ON ord.ORDER_ID = operation.ORDER_ID LEFT OUTER JOIN
                         QMES_WIP_OPERATION_QTY op_qty ON op_qty.OPERATION_ID = operation.OPERATION_ID LEFT OUTER JOIN
                         QCM_PRODUCTS prod ON prod.PRODUCT_ID = op_qty.PRODUCT_ID LEFT OUTER JOIN
                         QCM_PACKAGE_HEADERS pack ON pack.PRODUCT_ID = prod.PRODUCT_ID LEFT OUTER JOIN
                         QCM_PACKAGE_LEVELS uom ON uom.PACKAGE_ID = pack.PACKAGE_ID
                         WHERE        (operation.OPERATION_TYPE_ID = 11) AND (uom.LEVEL_NR = 0) AND (operation.STARTED_DATE > :StartDate) AND (mach.MACHINE_ID = {MachineId})
                         GROUP BY prod.PRODUCT_ID, prod.PRODUCT_NR";

            var Command = new Oracle.ManagedDataAccess.Client.OracleCommand(sql, Con);
            OracleParameter[] parameters = new OracleParameter[]
            {
                    new OracleParameter("StartDate", Utilities.GetStartDate()),
            };
            Command.Parameters.AddRange(parameters);
            var reader = Command.ExecuteReader();
            Logger.Debug("GetRecentProductData finished");

            return reader;
        }

        public static SqlDataReader GetRecentXrayData(string MachineName, SqlConnection Con)
        {
            string sql = @"SELECT ArticleName, DeviceName, ProductionStart, ProductionEnd, TimeStamp, Throughput, CounterError, CounterTrade, CounterTotal, CounterBad, CounterContaminated
                                    FROM StatisticView WHERE ProductionEnd >= @StartDate";
            if (!string.IsNullOrEmpty(MachineName))
            {
                sql += $" AND DeviceName LIKE '%{int.Parse(MachineName.Substring(MachineName.Length - 2, 2))}'";
            }
            sql += " ORDER BY TimeStamp DESC";
            SqlCommand command = new SqlCommand(sql, Con);
            command.Parameters.Add("@StartDate", SqlDbType.DateTime);
            command.Parameters["@StartDate"].Value = Utilities.GetStartDate();
            if(Con.State == ConnectionState.Closed || Con.State == ConnectionState.Broken)
            {
                Con.Open();
            }
            SqlDataReader reader = command.ExecuteReader();
            return reader;
        }

        public static SqlDataReader GetRecentComponentScrap(SqlConnection Con, int? MaterialType = null)
        {
            string sql = @"SELECT z.zfinIndex, m.zfinIndex as material, bom.amount, bom.unit, t.scrap
                            FROM tbBom bom LEFT JOIN tbZfin z ON z.zfinId = bom.zfinId
	                            LEFT JOIN tbBomReconciliation br ON br.bomRecId = bom.bomRecId
	                            LEFT JOIN tbZfin m ON m.zfinId = bom.materialId
	                            LEFT JOIN 
                            (SELECT z.zfinIndex,cs.scrap
                            FROM tbComponentScrap cs LEFT JOIN tbZfin z ON z.zfinId = cs.zfinId
	                            LEFT JOIN tbScrapReconciliation sr ON sr.scrapReconciliationId = cs.componentScrapId
                            WHERE cs.scrapReconciliationId = (SELECT TOP(1) scrapReconciliationId FROM tbScrapReconciliation ORDER BY dateAdded DESC)) t ON t.zfinIndex = m.zfinIndex
                            WHERE bom.bomRecId = (SELECT TOP(1) bomRecId FROM tbBomReconciliation ORDER BY dateAdded DESC)";
            if (MaterialType != null)
            {
                sql += $" AND m.materialType={MaterialType}";
            }
            SqlCommand command = new SqlCommand(sql, Con);
            if (Con.State == ConnectionState.Closed || Con.State == ConnectionState.Broken)
            {
                Con.Open();
            }
            SqlDataReader reader = command.ExecuteReader();
            return reader;
        }

        public static List<DividerItem> GetDivider(int week, int year)
        {
            try
            {
                List<DividerItem> Items = new List<DividerItem>();

                using (SqlConnection npdConnection = new SqlConnection(Static.Secrets.NpdConnectionString))
                {
                    string sql = $@"SELECT z.zfinIndex, d.* FROM tbDivider d
                                    LEFT JOIN tbZfin z ON z.zfinId = d.ProductId 
                                    WHERE Week = {week} AND Year = {year}";

                    SqlCommand command = new SqlCommand(sql, npdConnection);
                    if (npdConnection.State == ConnectionState.Closed || npdConnection.State == ConnectionState.Broken)
                    {
                        npdConnection.Open();
                    }
                    SqlDataReader reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            string zfinIndex = reader["zfinIndex"].ToString();
                            DividerItem d;
                            LocationAmount la = new LocationAmount();
                            la.L = reader["L"].ToString();
                            la.Amount = Convert.ToInt32(reader["Amount"].ToString());
                            if (Items.Any(i => i.ZfinIndex == zfinIndex))
                            {
                                //there's already this product row in collection, append it
                                d = Items.FirstOrDefault(i => i.ZfinIndex == zfinIndex);
                                d.Locations.Add(la);
                            }
                            else
                            {
                                //there is not this product yet, let's add it
                                d = new DividerItem();
                                d.ZfinIndex = reader["zfinIndex"].ToString();
                                d.Locations = new List<LocationAmount>();
                                d.Locations.Add(la);
                                Items.Add(d);
                            }

                        }
                    }
                    return Items;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static List<DividerItem> GetDefaultDestinations()
        {
            try
            {
                List<DividerItem> Items = new List<DividerItem>();

                using (SqlConnection npdConnection = new SqlConnection(Static.Secrets.NpdConnectionString))
                {
                    string sql = $@"SELECT z.zfinIndex, cs.location 
                                  FROM tbZfin z LEFT JOIN tbCustomerString cs ON cs.custStringId = z.custString 
                                  WHERE z.custString IS NOT NULL AND prodStatus = 'PR'";

                    SqlCommand command = new SqlCommand(sql, npdConnection);
                    if (npdConnection.State == ConnectionState.Closed || npdConnection.State == ConnectionState.Broken)
                    {
                        npdConnection.Open();
                    }
                    SqlDataReader reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            DividerItem d = new DividerItem();
                            d.ZfinIndex = reader["zfinIndex"].ToString();
                            d.Locations = new List<LocationAmount>();
                            LocationAmount la = new LocationAmount();
                            la.L = reader["location"].ToString().Trim();
                            la.Amount = 0;
                            d.Locations.Add(la);
                            Items.Add(d);


                        }
                    }
                    return Items;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static List<ProductMachineEfficiency> GetProductMachineEfficiencies(string ProductIds = null)
        {
            List<ProductMachineEfficiency> Items = new List<ProductMachineEfficiency>();

            try
            {
                using (OracleConnection Con = new Oracle.ManagedDataAccess.Client.OracleConnection(Static.Secrets.OracleConnectionString))
                {
                    if (Con.State == System.Data.ConnectionState.Closed)
                    {
                        Con.Open();
                    }
                    string sql = $@"SELECT PRODUCT_ID, MACHINE_ID, CAST(EFFICIENCY AS INT) AS EFFICIENCY, CAST(MAX_EFFICIENCY AS INT) AS MAX_EFFICIENCY FROM QMES_FO_MACHINE_EFFICIENCY";
                    if (!string.IsNullOrEmpty(ProductIds))
                    {
                        sql += $" WHERE PRODUCT_ID IN({ ProductIds})";
                    }

                    var Command = new Oracle.ManagedDataAccess.Client.OracleCommand(sql, Con);

                    var reader = Command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            ProductMachineEfficiency ef = new ProductMachineEfficiency();
                            ef.PRODUCT_ID = Convert.ToInt32(reader["PRODUCT_ID"].ToString());
                            ef.MACHINE_ID = Convert.ToInt32(reader["MACHINE_ID"].ToString());
                            ef.EFFICIENCY = Convert.ToInt32(reader["EFFICIENCY"].ToString());
                            ef.MAX_EFFICIENCY = Convert.ToInt32(reader["MAX_EFFICIENCY"].ToString());
                            Items.Add(ef);
                        }
                    }
                }
                return Items;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static DateTime GetStartDate()
        {
            DateTime yesterday = DateTime.Now.AddDays(-1);
            DateTime rDate;
            bool keepDate = true;
            int h = yesterday.Hour;
            if(h >= 6 && h < 14)
            {
                h = 6;
            }else if(h >= 14 && h < 22)
            {
                h = 14;
            }
            else if(h == 22 || h == 23)
            {
                h = 22;
            }
            else
            {
                //00 - 05:59
                h = 22;
                keepDate = false;
            }
            if (!keepDate)
            {
                //yesterday = yesterday - 1
                yesterday = yesterday.AddDays(-1);
            }

            //Set to yesterday H:00:00
            rDate = new DateTime(yesterday.Year, yesterday.Month, yesterday.Day, h, 0, 0);

            return rDate;
        }
    }
}