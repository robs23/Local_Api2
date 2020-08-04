using Local_Api2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;

namespace Local_Api2.Controllers
{
    public class MachinesController : ApiController
    {
        [HttpGet]
        [Route("GetMachines")]
        [ResponseType(typeof(List<Machine>))]
        public IHttpActionResult GetMachines(int? Type = null, bool? VisibleInAPS=null)
        {
            string ConStr = Static.Secrets.ApiConnectionString;
            var Con = new Oracle.ManagedDataAccess.Client.OracleConnection(ConStr);

            if (Con.State == System.Data.ConnectionState.Closed)
            {
                Con.Open();
            }

            string str = @"SELECT MACHINE_ID, MACHINE_NR, STATE, MACHINE_TYPE_ID, IS_VISIBLE_APS 
                            FROM QMES_FO_MACHINE 
                            ORDER BY MACHINE_NR";

            var Command = new Oracle.ManagedDataAccess.Client.OracleCommand(str, Con);

            var reader = Command.ExecuteReader();

            List<Machine> Machines = new List<Machine>();

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    Machine m = new Machine();
                    m.MachineId = Convert.ToInt32(reader[reader.GetOrdinal("MACHINE_ID")].ToString());
                    m.Name = reader[reader.GetOrdinal("MACHINE_NR")].ToString();
                    m.State = reader[reader.GetOrdinal("STATE")].ToString();
                    m.Type = Convert.ToInt32(reader[reader.GetOrdinal("MACHINE_TYPE_ID")].ToString());
                    m.VisibleInAPS = reader[reader.GetOrdinal("IS_VISIBLE_APS")].ToString() == "T" ? true : false;
                    Machines.Add(m);
                }
                if(Type!=null)
                    Machines = Machines.Where(m => m.Type == (int)Type).ToList() ;
                if(VisibleInAPS!=null)
                    Machines = Machines.Where(m => m.VisibleInAPS == (bool)VisibleInAPS).ToList();
                return Ok(Machines);
            }
            else
            {
                return NotFound();
            }

        }

    }
}
