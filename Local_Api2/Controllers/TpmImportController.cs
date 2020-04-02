using Local_Api2.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;

namespace Local_Api2.Controllers
{
    public class TpmImportController : ApiController
    {
        [HttpPost]
        [Route("CreateTpmEntry")]
        [ResponseType(typeof(Process))]
        public IHttpActionResult CreateTpmEntry(Process p)
        {
            string ConStr = Static.Secrets.ApiConnectionString;
            var Con = new Oracle.ManagedDataAccess.Client.OracleConnection(ConStr);

            if (Con.State == System.Data.ConnectionState.Closed)
            {
                Con.Open();
            }


            string iStr = @"INSERT INTO ifc.qmes_tpm_repairs_imp (order_nr, manager_nr, start_date, closemean_nr, end_date, initial_diagnosis, repair_actions, STATUS, IS_ADJUSTMENT, REASONCODE2, REASONCODE3) 
                            VALUES (:TheNumber, :Manager, :StartDate, :FinishedBy, :EndDate, :InitialDiagnosis, :RepairActions, :Status, :IS_ADJUSTMENT, :REASONCODE2, :REASONCODE3)";

            var Command = new Oracle.ManagedDataAccess.Client.OracleCommand(iStr, Con);

            OracleParameter[] parameters = new OracleParameter[]
            {
                new OracleParameter("TheNumber", p.Number),
                new OracleParameter("Manager", p.Manager),
                new OracleParameter("StartDate", p.StartDate),
                new OracleParameter("FinishedBy", p.FinishedBy),
                new OracleParameter("EndDate", p.EndDate),
                new OracleParameter("InitialDiagnosis", p.InitialDiagnosis),
                new OracleParameter("RepairActions", p.RepairActions),
                new OracleParameter("Status", p.Status),
                new OracleParameter("IS_ADJUSTMENT", p.IsAdjustment),
                new OracleParameter("REASONCODE2", p.ReasonCode2),
                new OracleParameter("REASONCODE3", p.ReasonCode3)
            };
            Command.Parameters.AddRange(parameters);
            Command.ExecuteNonQuery();

            return Ok();

        }

        [HttpPost]
        [Route("CreateTestEntry")]
        [ResponseType(typeof(Process))]
        public IHttpActionResult CreateTestEntry(Process p)
        {
            string ConStr = Static.Secrets.ApiConnectionString;
            var Con = new Oracle.ManagedDataAccess.Client.OracleConnection(ConStr);

            if (Con.State == System.Data.ConnectionState.Closed)
            {
                Con.Open();
            }


            string iStr = @"INSERT INTO ifc.qmes_tpm_repairs_imp (order_nr, manager_nr, closemean_nr, initial_diagnosis, repair_actions, STATUS, IS_ADJUSTMENT) 
                            VALUES ('{0}','{1}','{2}','{3}', '{4}', '{5}','{6}')";

            iStr = string.Format(iStr, p.Number, p.Manager, p.FinishedBy, p.InitialDiagnosis, p.RepairActions, p.Status,p.IsAdjustment);

            var Command = new Oracle.ManagedDataAccess.Client.OracleCommand(iStr, Con);

            Command.ExecuteNonQuery();

            return Ok();

        }

        [HttpGet]
        [Route("GetEntry")]
        [ResponseType(typeof(Process))]
        public IHttpActionResult GetEntry(string id)
        {
            string ConStr = Static.Secrets.ApiConnectionString;
            var Con = new Oracle.ManagedDataAccess.Client.OracleConnection(ConStr);

            if (Con.State == System.Data.ConnectionState.Closed)
            {
                Con.Open();
            }

            string str = string.Format("SELECT * FROM ifc.qmes_tpm_repairs_imp WHERE order_nr = '{0}'",id);

            var Command = new Oracle.ManagedDataAccess.Client.OracleCommand(str, Con);

            var reader = Command.ExecuteReader();

            Process p = new Process();

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    p = new Process();
                    p.Number = reader[reader.GetOrdinal("order_nr")].ToString();
                    p.Manager = reader[reader.GetOrdinal("manager_nr")].ToString();
                    p.StartDate = null;
                    p.EndDate = null;
                    p.FinishedBy = reader[reader.GetOrdinal("closemean_nr")].ToString();
                    p.InitialDiagnosis = reader[reader.GetOrdinal("initial_diagnosis")].ToString();
                    p.RepairActions = reader[reader.GetOrdinal("repair_actions")].ToString();
                    p.Status = reader[reader.GetOrdinal("STATUS")].ToString();
                    p.IsAdjustment = reader[reader.GetOrdinal("IS_ADJUSTMENT")].ToString();
                    p.ReasonCode2 = reader[reader.GetOrdinal("REASONCODE2")].ToString();
                    p.ReasonCode3 = reader[reader.GetOrdinal("REASONCODE3")].ToString();
                }
                return Ok(p);
            }
            else
            {
                return NotFound();
            }
            
        }

    }
}
