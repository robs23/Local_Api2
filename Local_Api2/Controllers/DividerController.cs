using Local_Api2.Models;
using Local_Api2.Static;
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
                List<DividerItem> Items = Utilities.GetDivider(week, year);
                if (Items.Any())
                {
                    return Ok(Items);
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }

        }

        [HttpGet]
        [Route("GetDefaultDestinations")]
        [ResponseType(typeof(List<DividerItem>))]
        public IHttpActionResult GetDefaultDestinations()
        {
            try
            {
                List<DividerItem> Items = Utilities.GetDefaultDestinations();
                if (Items.Any())
                {
                    return Ok(Items);
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }

        }
    }
}
