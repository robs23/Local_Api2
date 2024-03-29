﻿using Local_Api2.Models;
using Local_Api2.Static;
using Newtonsoft.Json;
using NLog;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing.Imaging;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Description;

namespace Local_Api2.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class ComponentController : ApiController
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [HttpGet]
        [Route("GetPlannedComponents")]
        [ResponseType(typeof(List<PlannedComponent>))]
        public async Task<IHttpActionResult> GetPlannedComponents(string query = null, bool withParents = false)
        {
            try
            {
                List<PlannedComponent> Plan = new List<PlannedComponent>();
                Plan = await _GetPlannedComponents(query);

                if (withParents)
                {
                    //if withParents, got to fetch appropriate Operation2Products containing parent info
                    //and merge it with Plan
                    List<Operation2Product> Operations = new List<Operation2Product>();
                    //var cursed = Plan.Where(p => p.OPERATION_NR.Contains("#"));
                    //if (cursed.Any())
                    //{
                    //    Plan.RemoveAll(o => o.OPERATION_NR.Contains("#"));
                    //    Plan = Plan.Take(200).ToList();
                    //}
                    string operationNumbers = string.Join(",", Plan.Select(g => g.OPERATION_NR).Distinct().Select(p => p.Insert(0, "'").Insert(p.Length + 1, "'")));
                    Operations = await _GetOperation2Product(operationNumbers);
                    if (Operations.Any())
                    {
                        MergePlanAndOperation2Products(Plan, Operations);
                    }
                }
                

                if (Plan.Any())
                {
                    return (Ok(Plan));
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("GetPlannedComponents: Błąd. Szczegóły: {Message}", ex.ToString());
                return InternalServerError(ex);
            }
        }

        private static void MergePlanAndOperation2Products(List<PlannedComponent> Plan, List<Operation2Product> Operations)
        {
            foreach (Operation2Product op in Operations)
            {
                var MatchingPlanItems = Plan.Where(p => p.OPERATION_NR == op.OPERATION_NR);
                if (MatchingPlanItems.Any())
                {
                    foreach (PlannedComponent item in MatchingPlanItems)
                    {
                        if(item.PARENT_NR == null)
                        {
                            item.PARENT_NR = "";
                        }

                        item.PARENT_NR += op.PRODUCT_NR + " ";

                    }
                }
            }
        }

        [HttpGet]
        [Route("GetPlannedComponentsSchedule")]

        public async Task<IHttpActionResult> GetPlannedComponentsSchedule(string query = null)
        {
            try
            {
                List<PlannedComponent> Plan = new List<PlannedComponent>();

                Plan = await _GetPlannedComponents(query);

                //Plan = await _GetPlannedComponentsRemote(query);
                if (Plan.Any())
                {
                    var schedule = Plan.GroupBy(x => new { x.OPERATION_DAY, x.SHIFT_ID, x.SHIFT_NAME, x.PRODUCT_NR, x.PRODUCT_NAME, x.SUB_PROD_TYPE }).Select(s => new
                    {
                        OPEATION_DAY = s.Key.OPERATION_DAY,
                        SHIFT_ID = s.Key.SHIFT_ID,
                        SHIFT_NAME = s.Key.SHIFT_NAME,
                        PRODUCT_NR = s.Key.PRODUCT_NR,
                        PRODUCT_NAME = s.Key.PRODUCT_NAME,
                        SUB_PROD_TYPE = s.Key.SUB_PROD_TYPE,
                        PRODUCT_QUANTITY = s.Sum(y=>y.PRODUCT_QUANTITY)
                    });
                    var columnHeaders = schedule.GroupBy(d => d.OPEATION_DAY).Select(g => g.Key);
                    var rowHeaders = schedule.GroupBy(d => new { d.PRODUCT_NR, d.PRODUCT_NAME, d.SUB_PROD_TYPE }).Select(g => new
                    {
                        PRODUCT_NR = g.Key.PRODUCT_NR,
                        PRODUCT_NAME = g.Key.PRODUCT_NAME,
                        SUB_PROD_TYPE = g.Key.SUB_PROD_TYPE
                    });


                    var table = new DataTable();
                    //add columns  for product
                    table.Columns.Add("Produkt");
                    table.Columns.Add("Nazwa");
                    table.Columns.Add("Typ");

                    foreach (var c in columnHeaders)
                    {
                        //for all 3 shifts each day
                        table.Columns.Add(c.ToString("yyyy-MM-dd") + "__1");
                        table.Columns.Add(c.ToString("yyyy-MM-dd") + "__2");
                        table.Columns.Add(c.ToString("yyyy-MM-dd") + "__3");
                    }

                    foreach(var r in rowHeaders)
                    {
                        string productNr = r.PRODUCT_NR;
                        bool colSet = false;
                 
                        var row = table.NewRow();
                        foreach(DataColumn col in table.Columns)
                        {
                            if(col.Ordinal <= 2 && colSet==false)
                            {
                                row[0] = productNr;
                                row[1] = r.PRODUCT_NAME;
                                row[2] = r.SUB_PROD_TYPE;
                                colSet = true;
                            }
                            else
                            {
                                var splits = Regex.Split(col.ColumnName, "__");
                                bool isParsable = DateTime.TryParse(splits[0], out DateTime date);
                                if (isParsable)
                                {
                                    var shifts = schedule.Where(s => s.OPEATION_DAY == date && s.PRODUCT_NR == productNr).GroupBy(g => g.SHIFT_ID).Select(x => new
                                    {
                                        SHIFT_ID = x.Key,
                                        PRODUCT_QUANTITY = x.Sum(y => y.PRODUCT_QUANTITY)
                                    });
                                    var shift = schedule.Where(s => s.OPEATION_DAY == date && s.PRODUCT_NR == productNr && s.SHIFT_ID == Convert.ToInt32(splits[1])).Sum(g => g.PRODUCT_QUANTITY);
                                    row[col.Ordinal] = shift;
                                }
                            }
                            
                        }
                        table.Rows.Add(row);
                    }
                    if(table.Rows.Count == 0)
                    {
                        return NotFound();
                    }

                    return (Ok(table));
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("GetPlannedComponentsSchedule: Błąd. Szczegóły: {Message}", ex.ToString());
                return InternalServerError(ex);
            }
        }


        private async Task<List<PlannedComponent>> _GetPlannedComponents(string query = null)
        {
            try
            {
                List<PlannedComponent> Plan = new List<PlannedComponent>();

                using (OracleConnection Con = new Oracle.ManagedDataAccess.Client.OracleConnection(Static.Secrets.ApiConnectionString))
                {
                    if (Con.State == System.Data.ConnectionState.Closed)
                    {
                        Con.Open();
                    }

                    //make sure operation date is always indicated
                    //otherwise we can crash the db
                    if (query != null)
                    {
                        if (!query.Contains("OPERATION_DATE") && !query.Contains("OPERATION_WEEK") && !query.Contains("OPERATION_YEAR"))
                        {
                            query += $" AND (OPERATION_WEEK = {DateTime.Now.IsoWeekOfYear()}) AND (OPERATION_YEAR = {DateTime.Now.Year})";
                        }
                    }
                    else
                    {
                        query = $"(OPERATION_WEEK = {DateTime.Now.IsoWeekOfYear()}) AND (OPERATION_YEAR = {DateTime.Now.Year})";
                    }

                    string str = $@"SELECT OPERATION_DATE, OPERATION_DAY, OPERATION_WEEK, OPERATION_YEAR, SHIFT_ID, SHIFT_NAME, MACHINE_NR, OPERATION_NR, OPERATION_TYPE_NAME, 
	                            ORDER_NR, PRODUCT_NR, PRODUCT_NAME, PROD_TYPE, SUB_PROD_TYPE, ORDER_TYPE_CODE, ORDER_TYPE_NAME, BOM_NR, PRODUCT_QUANTITY, PRODUCT_QUANTITY_ALL
                                FROM QMESV_GRID_WIP_PROD_SUPPLY
                                WHERE {query} 
                                ORDER BY OPERATION_DATE, SHIFT_ID, ORDER_NR";


                    var Command = new Oracle.ManagedDataAccess.Client.OracleCommand(str, Con);

                    var reader = Command.ExecuteReader();

                    //List<PlannedComponent> Plan = new List<PlannedComponent>();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            PlannedComponent c = new PlannedComponent();
                            c.OPERATION_DATE = Convert.ToDateTime(reader["OPERATION_DATE"].ToString());
                            c.OPERATION_DAY = Convert.ToDateTime(reader["OPERATION_DAY"].ToString());
                            c.OPERATION_WEEK = Convert.ToInt32(reader["OPERATION_WEEK"].ToString());
                            c.OPERATION_YEAR = Convert.ToInt32(reader["OPERATION_YEAR"].ToString());
                            c.SHIFT_ID = Convert.ToInt32(reader["SHIFT_ID"].ToString());
                            c.SHIFT_NAME = reader["SHIFT_NAME"].ToString();
                            c.MACHINE_NR = reader["MACHINE_NR"].ToString();
                            c.OPERATION_NR = reader["OPERATION_NR"].ToString();
                            c.OPERATION_TYPE_NAME = reader["OPERATION_TYPE_NAME"].ToString();
                            c.ORDER_NR = reader["ORDER_NR"].ToString();
                            c.PRODUCT_NR = reader["PRODUCT_NR"].ToString();
                            c.PRODUCT_NAME = reader["PRODUCT_NAME"].ToString();
                            c.PROD_TYPE = reader["PROD_TYPE"].ToString();
                            c.SUB_PROD_TYPE = reader["SUB_PROD_TYPE"].ToString();
                            c.ORDER_TYPE_CODE = reader["ORDER_TYPE_CODE"].ToString();
                            c.BOM_NR = reader["BOM_NR"].ToString();
                            c.PRODUCT_QUANTITY = Convert.ToInt64(reader["PRODUCT_QUANTITY"].ToString());
                            c.PRODUCT_QUANTITY_ALL = Convert.ToInt64(reader["PRODUCT_QUANTITY_ALL"].ToString());
                            Plan.Add(c);
                        }
                    }

                    return Plan;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("_GetPlannedComponents: Błąd. Szczegóły: {Message}", ex.ToString());
                throw;
            }
        }

        [HttpGet]
        [Route("GetOperation2Product")]
        [ResponseType(typeof(List<Operation2Product>))]
        public async Task<IHttpActionResult> GetOperation2Product(string operationNumbers)
        {
            try
            {
                List<Operation2Product> Items = new List<Operation2Product>();
                Items = await _GetOperation2Product(operationNumbers);

                if (Items.Any())
                {
                    return (Ok(Items));
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("GetOperation2Product: Błąd. Szczegóły: {Message}", ex.ToString());
                return InternalServerError(ex);
            }
        }


        private async Task<List<Operation2Product>> _GetOperation2Product(string operationNumbers)
        {
            try
            {
                using (OracleConnection Con = new Oracle.ManagedDataAccess.Client.OracleConnection(Static.Secrets.ApiConnectionString))
                {
                    if (Con.State == System.Data.ConnectionState.Closed)
                    {
                        Con.Open();
                    }

                    string str = $@"SELECT DISTINCT op.OPERATION_ID, op.OPERATION_NR, pr.PRODUCT_ID, pr.PRODUCT_NR, pr.SUB_PROD_TYPE
                                    FROM QMES_WIP_OPERATION op LEFT OUTER JOIN
                                         QMES_WIP_ORDER2PRODUCT o2p ON o2p.OPERATION_ID = op.OPERATION_ID LEFT OUTER JOIN
                                         QCM_PRODUCTS pr ON pr.PRODUCT_ID = o2p.PRODUCT_ID
                                    WHERE (op.OPERATION_NR IN ({operationNumbers})) AND (pr.SUB_PROD_TYPE IN ('WR', 'PP'))
                                    ORDER BY op.OPERATION_ID, pr.SUB_PROD_TYPE DESC";


                    var Command = new Oracle.ManagedDataAccess.Client.OracleCommand(str, Con);

                    var reader = Command.ExecuteReader();

                    List<Operation2Product> Items = new List<Operation2Product>();


                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            Operation2Product o = new Operation2Product();
                            o.OPERATION_ID = Convert.ToInt32(reader["OPERATION_ID"].ToString());
                            o.OPERATION_NR = reader["OPERATION_NR"].ToString();
                            o.PRODUCT_ID = Convert.ToInt32(reader["PRODUCT_ID"].ToString());
                            o.PRODUCT_NR = reader["PRODUCT_NR"].ToString();
                            o.SUB_PROD_TYPE = reader["SUB_PROD_TYPE"].ToString();

                            Items.Add(o);
                        }
                    }
                    else
                    {

                    }
                    return Items;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task<List<Operation2Product>> _GetOperation2ProductRemote(string operationNumbers)
        {
            try
            {
                List<Operation2Product> vItems = new List<Operation2Product>();

                using (var client = new HttpClient())
                {
                    string url = $"{Secrets.MesApi}/GetOperation2Product?operationNumbers={operationNumbers}";
                    using (var response = await client.GetAsync(new Uri(url)))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            var userJsonString = await response.Content.ReadAsStringAsync();
                            vItems = JsonConvert.DeserializeObject<Operation2Product[]>(userJsonString).ToList();
                        }
                        return vItems;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("_GetOperation2ProductRemote: Błąd. Szczegóły: {Message}", ex.ToString());
                throw;
            }
        }

        private async Task<List<PlannedComponent>> _GetPlannedComponentsRemote(string query = null)
        {
            try
            {
                List<PlannedComponent> vItems = new List<PlannedComponent>();

                using (var client = new HttpClient())
                {
                    string url = $"{Secrets.MesApi}/GetPlannedComponents";
                    if(query != null)
                    {
                        url += $"?query={query}";
                    }

                    using (var response = await client.GetAsync(new Uri(url)))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            var userJsonString = await response.Content.ReadAsStringAsync();
                            vItems = JsonConvert.DeserializeObject<PlannedComponent[]>(userJsonString).ToList();
                        }
                        return vItems;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("_GetPlannedComponentsRemote: Błąd. Szczegóły: {Message}", ex.ToString());
                throw;
            }
        }

        [HttpGet]
        [Route("GetInventorySnapshots")]
        [ResponseType(typeof(List<InventorySnapshot>))]
        public async Task<IHttpActionResult> GetInventorySnapshots(string query = null)
        {
            try
            {
                List<InventorySnapshot> Items = new List<InventorySnapshot>();
                List<InventorySnapshot> ZcomItems = new List<InventorySnapshot>();

                Task<List<InventorySnapshot>> zpkgTask = Task.Run(() => _GetInventorySnapshots(query));
                Task<List<InventorySnapshot>> zcomTask = Task.Run(() => _GetZComInventorySnapshots());

                await Task.WhenAll(zpkgTask, zcomTask);
                
                Items = await zpkgTask;
                ZcomItems = await zcomTask;

                if (ZcomItems.Any())
                {
                    Items.AddRange(ZcomItems);
                }

                if (Items.Any())
                {
                    return (Ok(Items));
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("GetInventorySnapshots: Błąd. Szczegóły: {Message}", ex.ToString());
                return InternalServerError(ex);
            }
        }

        private async Task<List<InventorySnapshot>> _GetInventorySnapshots(string query = null)
        {
            try
            {
                List<InventorySnapshot> Inventories = new List<InventorySnapshot>();

                using (SqlConnection Con = new SqlConnection(Static.Secrets.NpdConnectionString))
                {
                    if (Con.State == System.Data.ConnectionState.Closed)
                    {
                        Con.Open();
                    }

                    //make sure takenOn date is always indicated
                    //it should default to last inventory snapshot taken on date
                    if (query != null)
                    {
                        if (!query.Contains("TakenOn"))
                        {
                            query += $" AND (TakenOn = (SELECT TOP(1) TakenOn FROM tbInventorySnapshots ORDER BY TakenOn DESC))";
                        }
                    }
                    else
                    {
                        query = $"(TakenOn = (SELECT TOP(1) TakenOn FROM tbInventorySnapshots ORDER BY TakenOn DESC))";
                    }

                    string str = $@"SELECT z.zfinIndex, z.zfinName, i.*
                                FROM tbInventorySnapshots i LEFT JOIN tbZfin z ON z.zfinId=i.ProductId
                                WHERE {query}";


                    var Command = new SqlCommand(str, Con);

                    var reader = Command.ExecuteReader();


                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            InventorySnapshot i = new InventorySnapshot();
                            i.InventorySnapshotId = Convert.ToInt32(reader["InventorySnapshotId"].ToString());
                            i.ProductId = Convert.ToInt32(reader["ProductId"].ToString());
                            i.ProductIndex = reader["ZfinIndex"].ToString();
                            i.ProductName = reader["ZfinName"].ToString();
                            i.Size = Convert.ToDouble(reader["Size"].ToString());
                            i.Unit = reader["Unit"].ToString();
                            i.Status = reader["Status"].ToString();
                            i.TakenOn = Convert.ToDateTime(reader["TakenOn"].ToString());
                            Inventories.Add(i);
                        }
                    }

                    return Inventories;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("_GetInventorySnapshots: Błąd. Szczegóły: {Message}", ex.ToString());
                throw;
            }
        }
        private async Task<List<InventorySnapshot>> _GetZComInventorySnapshots()
        {
            try
            {
                List<InventorySnapshot> Inventories = new List<InventorySnapshot>();

                using (SqlConnection Con = new SqlConnection(Static.Secrets.ScadaConnectionString))
                {
                    if (Con.State == System.Data.ConnectionState.Closed)
                    {
                        Con.Open();
                    }


                    string str = $@"SELECT s.IDSKLADNIK as Id, s.MaterialNumber as Number, s.NAZWASKLADNIKA as Name, SUM(z.ILOSC_W_ZBIORNIKU) as Size
                                    FROM ZBIORNIKI z 
	                                    LEFT JOIN SKLADNIKI s ON s.IDSKLADNIK=z.IDSKLADNIK
                                    WHERE s.MaterialNumber IS NOT NULL 
                                    GROUP BY s.IDSKLADNIK, s.MaterialNumber, s.NAZWASKLADNIKA";


                    var Command = new SqlCommand(str, Con);

                    var reader = Command.ExecuteReader();


                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            InventorySnapshot i = new InventorySnapshot();
                            i.InventorySnapshotId = 0;
                            i.ProductId = Convert.ToInt32(reader["Id"].ToString());
                            i.ProductIndex = reader["Number"].ToString();
                            i.ProductName = reader["Name"].ToString();
                            i.Size = Math.Round(Convert.ToDouble(reader["Size"].ToString()));
                            i.Unit = "KG";
                            i.Status = "U";
                            i.TakenOn = DateTime.Now;
                            Inventories.Add(i);
                        }
                    }

                    return Inventories;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("_GetZComInventorySnapshots: Błąd. Szczegóły: {Message}", ex.ToString());
                throw;
            }
        }


        [HttpGet]
        [Route("GetDeliveryItems")]
        [ResponseType(typeof(List<InventorySnapshot>))]
        public async Task<IHttpActionResult> GetDeliveryItems(string query = null)
        {
            try
            {
                List<DeliveryItem> Items = new List<DeliveryItem>();
                Items = await _GetDeliveryItems(query);

                if (Items.Any())
                {
                    return (Ok(Items));
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("GetDeliveryItems: Błąd. Szczegóły: {Message}", ex.ToString());
                return InternalServerError(ex);
            }
        }

        private async Task<List<DeliveryItem>> _GetDeliveryItems(string query = null)
        {
            try
            {
                List<DeliveryItem> Items = new List<DeliveryItem>();

                using (SqlConnection Con = new SqlConnection(Static.Secrets.NpdConnectionString))
                {
                    if (Con.State == System.Data.ConnectionState.Closed)
                    {
                        Con.Open();
                    }

                    //make sure CreatedOn date is always indicated
                    //it should default to last deliveryItem CreatedOn date
                    if (query != null)
                    {
                        if (!query.Contains("CreatedOn"))
                        {
                            query += $" AND (CreatedOn = (SELECT TOP(1) CreatedOn FROM tbDeliveryItems ORDER BY CreatedOn DESC))";
                        }
                    }
                    else
                    {
                        query = $"(CreatedOn = (SELECT TOP(1) CreatedOn FROM tbDeliveryItems ORDER BY CreatedOn DESC))";
                    }

                    string str = $@"SELECT z.zfinIndex, z.zfinName, d.*
                                FROM tbDeliveryItems d LEFT JOIN tbZfin z ON z.zfinId=d.ProductId
                                WHERE {query}";


                    var Command = new SqlCommand(str, Con);

                    var reader = Command.ExecuteReader();


                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            DeliveryItem i = new DeliveryItem();
                            i.DeliveryItemId = Convert.ToInt32(reader["DeliveryItemId"].ToString());
                            i.ProductId = Convert.ToInt32(reader["ProductId"].ToString());
                            i.ProductIndex = reader["ZfinIndex"].ToString();
                            i.ProductName = reader["ZfinName"].ToString();
                            i.DocumentDate = Convert.ToDateTime(reader["DocumentDate"].ToString());
                            i.PurchaseOrder = reader["PurchaseOrder"].ToString();
                            i.OrderQuantity = Convert.ToDouble(reader["OrderQuantity"].ToString());
                            i.OpenQuantity = Convert.ToDouble(reader["OpenQuantity"].ToString());
                            i.ReceivedQuantity = Convert.ToDouble(reader["ReceivedQuantity"].ToString());
                            i.NetPrice = Convert.ToDouble(reader["NetPrice"].ToString());
                            i.DeliveryDate = Convert.ToDateTime(reader["DeliveryDate"].ToString());
                            i.Vendor = reader["Vendor"].ToString();
                            i.CreatedOn = Convert.ToDateTime(reader["CreatedOn"].ToString());
                            Items.Add(i);
                        }
                    }

                    return Items;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("_GetDeliveryItems: Błąd. Szczegóły: {Message}", ex.ToString());
                throw;
            }
        }

    }

}