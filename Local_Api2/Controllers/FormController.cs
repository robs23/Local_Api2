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
    public class FormController : ApiController
    {
        [HttpGet]
        [Route("GetForms")]
        [ResponseType(typeof(List<Form>))]
        public IHttpActionResult GetForms()
        {
            try
            {
                List<Form> Items = new List<Form>();

                using (SqlConnection npdConnection = new SqlConnection(Static.Secrets.NpdConnectionString))
                {
                        string sql = @"SELECT * FROM LT_Forms ORDER BY CreatedOn DESC";

                        SqlCommand command = new SqlCommand(sql, npdConnection);
                        if (npdConnection.State == ConnectionState.Closed || npdConnection.State == ConnectionState.Broken)
                        {
                            npdConnection.Open();
                        }
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            Form f = new Form();
                            f.FormId = reader.GetInt32(reader.GetOrdinal("FormId"));
                            f.Name = reader["Name"].ToString();
                            f.Description = reader["Description"].ToString();
                            f.Link = reader["Link"].ToString();
                            f.Photo = reader["Photo"].ToString();
                            f.CreatedOn = reader.IsDBNull(reader.GetOrdinal("CreatedOn")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("CreatedOn"));
                            Items.Add(f);
                        }
                        return Ok(Items);
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }

        }

        [HttpGet]
        [Route("GetForm")]
        [ResponseType(typeof(Form))]
        public IHttpActionResult GetForm(int id)
        {
            try
            {
                using (SqlConnection npdConnection = new SqlConnection(Static.Secrets.NpdConnectionString))
                {
                    string sql = $@"SELECT * FROM LT_Forms WHERE FormId={id})";

                    SqlCommand command = new SqlCommand(sql, npdConnection);
                    if (npdConnection.State == ConnectionState.Closed || npdConnection.State == ConnectionState.Broken)
                    {
                        npdConnection.Open();
                    }
                    SqlDataReader reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        Form f = new Form();
                        while (reader.Read())
                        {
                            f.FormId = reader.GetInt32(reader.GetOrdinal("FormId"));
                            f.Name = reader["Name"].ToString();
                            f.Description = reader["Description"].ToString();
                            f.Link = reader["Link"].ToString();
                            f.Photo = reader["Photo"].ToString();
                            f.CreatedOn = reader.IsDBNull(reader.GetOrdinal("CreatedOn")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("CreatedOn"));
                        }
                        return Ok(f);
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

        [HttpPost]
        [Route("CreateForm")]
        [ResponseType(typeof(Form))]
        public IHttpActionResult CreateForm(Form form)
        {
            try
            {
                Form ret = new Form();
                using (SqlConnection npdConnection = new SqlConnection(Static.Secrets.NpdConnectionString))
                {
                    string sql = $@"INSERT INTO LT_Forms (Name, Description, Link, CreatedOn) 
                                    VALUES ('{form.Name}', '{form.Description}', '{form.Link}', GETDATE());SELECT SCOPE_IDENTITY()";

                    SqlCommand command = new SqlCommand(sql, npdConnection);
                    if (npdConnection.State == ConnectionState.Closed || npdConnection.State == ConnectionState.Broken)
                    {
                        npdConnection.Open();
                    }
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        form.FormId = reader.GetInt32(reader.GetOrdinal("FormId"));
                    }
                    return Ok(ret);
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }

        }

        [HttpPut]
        [Route("EditForm")]
        [ResponseType(typeof(Form))]
        public IHttpActionResult EditForm(Form form)
        {
            try
            {
                Form ret = new Form();
                using (SqlConnection npdConnection = new SqlConnection(Static.Secrets.NpdConnectionString))
                {
                    string sql = $@"UPDATE LT_Forms
                                    SET Name = '{form.Name}', 
                                        Description = '{form.Description}', 
                                        Link = '{form.Link}'
                                    WHERE FormId={form.FormId}";

                    SqlCommand command = new SqlCommand(sql, npdConnection);
                    if (npdConnection.State == ConnectionState.Closed || npdConnection.State == ConnectionState.Broken)
                    {
                        npdConnection.Open();
                    }
                    var edited = command.ExecuteNonQuery();
                    if(edited > 0)
                    {
                        return Ok(ret);
                    }
                    else
                    {
                        return InternalServerError();
                    }
                    
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }

        }

        [HttpDelete]
        [Route("DeleteForm")]
        public IHttpActionResult DeleteForm(int FormId)
        {
            try
            {
                using (SqlConnection npdConnection = new SqlConnection(Static.Secrets.NpdConnectionString))
                {
                    string sql = $@"DELETE FROM LT_Forms
                                    WHERE FormId={FormId}";

                    SqlCommand command = new SqlCommand(sql, npdConnection);
                    if (npdConnection.State == ConnectionState.Closed || npdConnection.State == ConnectionState.Broken)
                    {
                        npdConnection.Open();
                    }
                    var edited = command.ExecuteNonQuery();
                    if (edited > 0)
                    {
                        return Ok();
                    }
                    else
                    {
                        return InternalServerError();
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
