using Local_Api2.Models;
using System;
using System.Collections.Generic;
using System.Data;
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
    public class DividerController : ApiController
    {
        [HttpGet]
        [Route("GetDivider")]
        [ResponseType(typeof(List<DividerItem>))]
        public IHttpActionResult GetDivider(int week, int year)
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
                            long zfinIndex = Convert.ToInt64(reader["zfinIndex"].ToString());
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
                                d.ZfinIndex = Convert.ToInt64(reader["zfinIndex"].ToString());
                                d.Locations = new List<LocationAmount>();
                                d.Locations.Add(la);
                                Items.Add(d);
                            }
                            
                        }
                        return Ok(Items);
                    }
                    else
                    {
                        return NotFound();
                    }
                    
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }

        }
    }
}
