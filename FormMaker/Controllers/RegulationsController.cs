using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Npgsql;
using svod_admin;
using NpgsqlTypes;
using System.Runtime.InteropServices;
using System.Text.Json;
namespace FormMaker.Controllers
{
    public class RegulationsController : Controller
    {
        IConfiguration config;
        public static string? connStr;
        [BindProperty] public static List<SelectListItem> ListOfRegulations { get; set; } = new List<SelectListItem>();
        public RegulationsController(IConfiguration configuration)
        {
            config = configuration;
            connStr = configuration.GetConnectionString("DefaultConnection"); 
        }
        [HttpGet("/regulations/index")]
        public JsonResult Index(int periodic)
        {
            if (Pg.form.TypeForm == "daughter")
            {
                using Npgsql.NpgsqlConnection con = new(connStr);
                con.Open();
                Npgsql.NpgsqlCommand get = con.CreateCommand();
                get.CommandText = $"select periodic from svod2.form t where form={periodic}";
                Npgsql.NpgsqlDataReader read = get.ExecuteReader();
                while (read.Read())
                {
                    if (!read.IsDBNull(0))
                        periodic = read.GetInt32(0);
                }
                con.Close();
            }

            ListOfRegulations.Clear();
            Pg.ListOfRegulations.Clear();
            SelectListItem item0 = new("Отсутствует", "0");
            ListOfRegulations.Add(item0);
            using (Npgsql.NpgsqlConnection conn = new NpgsqlConnection(connStr))
            {
                conn.Open();
                using (NpgsqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "select regulation,note from svod2.formregulations where periodic=:p and period is null";
                    cmd.Parameters.Add(":p", NpgsqlDbType.Integer).Value = periodic;

                    NpgsqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        SelectListItem item = new();
                        if (!reader.IsDBNull(0))
                        {
                            item.Value = reader[0].ToString();
                            item.Text = reader[1].ToString();
                            ListOfRegulations.Add((SelectListItem)item);
                        }
                    }
                    reader.Close();
                }
            }

            Pg.ListOfRegulations = ListOfRegulations;
            return Json(ListOfRegulations);
        }
        class Item
        {
            public int Id { get;set;} = 0;
            public string Name { get;set;} = string.Empty;
        }
    }
}
