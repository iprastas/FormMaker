using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data.Common;

namespace svod_admin
{
    public class Pg
    {
        //readonly IConfiguration config;
        internal static string? connStr;
        public static readonly Dictionary<int, string> forms = [];
        public static List<SelectListItem> ListOfUnits { get; set; } = [];
        public static List<SelectListItem> ListOfRegulations { get; set; } = [];
        internal static Form form = new();
        internal static FormColumn col = new();
        internal static FormRow row = new ();
        internal static List<FormColumn> formColumns = [];
        internal static List<FormRow> formRows = [];
        internal static List<string> Inserts = [];
        internal static Dictionary<int, bool> formsbool = [];
        internal static bool IsEditForm;
        internal static bool IsFileForm;

		public  Pg(IConfiguration configuration)
        {
            //config = configuration;
            connStr = configuration.GetConnectionString("DefaultConnection");

            using (Npgsql.NpgsqlConnection conn = new(Pg.connStr))
            {
                conn.Open();
                Npgsql.NpgsqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT formkind, short FROM svod2.formkind Where formkind > 0 and formkind < 5 ORDER BY formkind ASC";
                Npgsql.NpgsqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int i = reader.GetInt32(0);
                    string s = reader.GetString(1);
                    bool fl = false;

                    forms.Add(i, s);
                    formsbool.Add(i, fl);
                }
                conn.Close();
            }

            SelectListItem item0 = new("Выберите единицу измерения","-1");
            ListOfUnits.Add(item0);

            using (Npgsql.NpgsqlConnection conn = new(Pg.connStr))
            {
                conn.Open();
                Npgsql.NpgsqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = "select unit, name from svod2.unit order by name";
                Npgsql.NpgsqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    SelectListItem item = new()
                    {
                        Value = reader.GetString(0),
                        Text = reader.GetString(1)
                    };
                    ListOfUnits.Add(item);
                }
                conn.Close();
            }
        }

    }
}
