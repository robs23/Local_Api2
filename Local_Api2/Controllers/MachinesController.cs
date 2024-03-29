﻿using Local_Api2.Models;
using Local_Api2.Static;
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
    public class MachinesController : ApiController
    {
        
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [HttpGet]
        [Route("GetMachines")]
        [ResponseType(typeof(List<Machine>))]
        public IHttpActionResult GetMachines(int? Type = null, bool? VisibleInAPS=null)
        {

            try
            {
                if (RuntimeSettings.MockServer)
                {
                    List<Machine> Machines = new List<Machine>();
                    for (int i = 1; i < 11; i++)
                    {
                        string status = i % 2 == 0 ? "PR" : "ST";
                        Machines.Add(new Machine { Id = i, Name = $"Linia {i}", State = status, Type = 3, VisibleInAPS = true });
                    }
                    Logger.Info("GetMachines: Sukces, zwracam {count} maszyn", Machines.Count);
                    return Ok(Machines);
                }
                else
                {
                    using (OracleConnection Con = new Oracle.ManagedDataAccess.Client.OracleConnection(Static.Secrets.ApiConnectionString))
                    {
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
                                m.Id = Convert.ToInt32(reader[reader.GetOrdinal("MACHINE_ID")].ToString());
                                m.Name = reader[reader.GetOrdinal("MACHINE_NR")].ToString();
                                m.State = reader[reader.GetOrdinal("STATE")].ToString();
                                m.Type = Convert.ToInt32(reader[reader.GetOrdinal("MACHINE_TYPE_ID")].ToString());
                                m.VisibleInAPS = reader[reader.GetOrdinal("IS_VISIBLE_APS")].ToString() == "T" ? true : false;
                                Machines.Add(m);
                            }
                            if (Type != null)
                                Machines = Machines.Where(m => m.Type == (int)Type).ToList();
                            if (VisibleInAPS != null)
                                Machines = Machines.Where(m => m.VisibleInAPS == (bool)VisibleInAPS).ToList();
                            Logger.Info("GetMachines: Sukces, zwracam {count} maszyn", Machines.Count);
                            return Ok(Machines);
                        }
                        else
                        {
                            Logger.Info("GetMachines: Porażka, nie znaleziono maszyn.");
                            return NotFound();
                        }
                    }
                    
                }
            }
            catch (Exception ex)
            {
                Logger.Error("GetMachines: Błąd. Szczegóły: {Message}",ex.ToString());
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("GetMachine")]
        [ResponseType(typeof(Machine))]
        public IHttpActionResult GetMachine(int Id)
        {
            try
            {
                if (RuntimeSettings.MockServer)
                {
                    string status = Id % 2 == 0 ? "PR" : "ST";
                    Machine machine = new Machine { Id = Id, Name = $"Linia {Id}", State = status, Type = 3, VisibleInAPS = true };
                    return Ok(machine);
                }
                else
                {
                    using (OracleConnection Con = new Oracle.ManagedDataAccess.Client.OracleConnection(Static.Secrets.ApiConnectionString))
                    {
                        if (Con.State == System.Data.ConnectionState.Closed)
                        {
                            Con.Open();
                        }

                        string str = $@"SELECT MACHINE_ID, MACHINE_NR, STATE, MACHINE_TYPE_ID, IS_VISIBLE_APS 
                            FROM QMES_FO_MACHINE 
                            WHERE MACHINE_ID = {Id}";

                        var Command = new Oracle.ManagedDataAccess.Client.OracleCommand(str, Con);

                        var reader = Command.ExecuteReader();

                        if (reader.HasRows)
                        {
                            Machine m = new Machine();
                            while (reader.Read())
                            {
                                m.Id = Convert.ToInt32(reader[reader.GetOrdinal("MACHINE_ID")].ToString());
                                m.Name = reader[reader.GetOrdinal("MACHINE_NR")].ToString();
                                m.State = reader[reader.GetOrdinal("STATE")].ToString();
                                m.Type = Convert.ToInt32(reader[reader.GetOrdinal("MACHINE_TYPE_ID")].ToString());
                                m.VisibleInAPS = reader[reader.GetOrdinal("IS_VISIBLE_APS")].ToString() == "T" ? true : false;
                            }
                            Logger.Info("GetMachines: Sukces, zwracam maszynę {Id}", m.Id);
                            return Ok(m);
                        }
                        else
                        {
                            Logger.Info("GetMachines: Porażka, nie znaleziono maszyny {Id}", Id);
                            return NotFound();
                        }
                    }
                    
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Błąd w GetMachine. ID:{Id}, Szczegóły: {Message}",Id, ex.ToString());
                return InternalServerError(ex);
            }
        }

    }
}
