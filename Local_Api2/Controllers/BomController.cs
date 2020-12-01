using Local_Api2.Models;
using Local_Api2.Static;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Description;

namespace Local_Api2.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class BomController : ApiController
    {
        [HttpGet]
        [Route("GetRecentMaterialScraps")]
        [ResponseType(typeof(List<BomItem>))]
        public IHttpActionResult GetRecentMaterialcraps(int? MaterialType = null)
        {
            try
            {
                List<BomItem> Items = new List<BomItem>();

                using (SqlConnection npdConnection = new SqlConnection(Static.Secrets.NpdConnectionString))
                {
                    using (SqlDataReader reader = Utilities.GetRecentComponentScrap(npdConnection, MaterialType))
                    {
                        while (reader.Read())
                        {
                            BomItem b = new BomItem();
                            b.ZfinIndex = reader.GetInt32(reader.GetOrdinal("zfinIndex"));
                            b.Material = reader.GetInt32(reader.GetOrdinal("material"));
                            b.Amount = reader.IsDBNull(reader.GetOrdinal("amount")) ? new double?() : reader.GetDouble(reader.GetOrdinal("amount"));
                            b.Unit = reader.GetString(reader.GetOrdinal("unit"));
                            b.Scrap = reader.IsDBNull(reader.GetOrdinal("scrap")) ? new double?() : reader.GetDouble(reader.GetOrdinal("scrap"));
                            Items.Add(b);
                        }
                        return Ok(Items);
                    }
                }
            }catch(Exception ex)
            {
                return InternalServerError(ex);
            }
            
        }
    }
}
