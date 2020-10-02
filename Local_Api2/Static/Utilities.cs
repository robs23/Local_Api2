﻿using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Local_Api2.Static
{
    public static class Utilities
    {
        public static OracleDataReader GetRecentProductData(int MachineId, OracleConnection Con)
        {
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
                    new OracleParameter("StartDate", DateTime.Now.AddDays(-1)),
            };
            Command.Parameters.AddRange(parameters);
            var reader = Command.ExecuteReader();
            return reader;
        }
    }
}