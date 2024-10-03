using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using svod_admin;
using Npgsql;
using System.Text;
using NpgsqlTypes;
using static System.Net.Mime.MediaTypeNames;
using System.IO;
using Microsoft.Extensions.Primitives;
using System.Diagnostics.Metrics;

namespace FormMaker.Pages
{
    [Authorize]
    public class PageFormModel : PageModel
    {
        public Form Form {  get; set; } = new Form();   
        public string TypeForm { get; set; } = "";
        public bool IsMaster { get; set; } = false;
        public bool IsDaughter { get; set; } = false;
        public bool IsSimple { get; set; } = false;
        public bool IsAnalitic { get; set; } = false;

        public ICollection<SelectListItem> Items = [];
        [BindProperty] public string FormMaster { get; set; } = "";
        [BindProperty] public int FormID { get; set; }
        [BindProperty] public int FormKind { get; set; }
        [BindProperty] public int Periodic { get; set; }
        [BindProperty] public int Regulations { get; set; }
        [BindProperty] public List<SelectListItem> ListOfRegulations { get; set; } = [];
        [BindProperty] public int ModeVertical { get; set; } = 1;
        [BindProperty] public bool HasSubject { get; set; }
        [BindProperty] public bool HasFacility { get; set; }
        [BindProperty] public bool HasTerritory { get; set; }
        [BindProperty] public string ShortName { get; set; } = "";
        [BindProperty] public string Name { get; set; } = "";
        [BindProperty] public string Note { get; set; } = "";
        [BindProperty] public string Department { get; set; } = "";
        [BindProperty] public DateTime Since { get; set; }
        [BindProperty] public string Message { get; set; } = "";
        public int Modeenter { get; set; } = 2;
        DateTime ChangeDate = DateTime.Today;

        readonly string? connectionString;
        readonly IConfiguration? configuration;
        public PageFormModel(IConfiguration? _configuration)
        {
            configuration = _configuration;
            connectionString = configuration == null ? string.Empty : configuration.GetConnectionString("DefaultConnection");
        }
        public void OnGet()
        {
#pragma warning disable CS8601 // Возможно, назначение-ссылка, допускающее значение NULL.
            TypeForm = Convert.ToString(RouteData.Values["type"]);
#pragma warning restore CS8601 // Возможно, назначение-ссылка, допускающее значение NULL.
			switch (RouteData.Values["type"])
            {
                case "Master":
                    Pg.form.TypeForm = "master";
                    IsMaster = true;
                    break;
                case "Daughter":
                    Pg.form.TypeForm = "daughter";
                    IsDaughter = true;
                    break;
                case "Simple": 
                    Pg.form.TypeForm = "simple";
                    IsSimple = true;
                    break;
                case "Analitic":
                    Pg.form.TypeForm = "analitic";
                    IsAnalitic = true;
                    break;
            }
#pragma warning disable CS8601 // Возможно, назначение-ссылка, допускающее значение NULL.
            Message = Convert.ToString(RouteData.Values["Message"]);
#pragma warning restore CS8601 // Возможно, назначение-ссылка, допускающее значение NULL.
            if (Message != null)
            {
                Form = Pg.form;
                Periodic = Form.periodic;
                FormMaster = Convert.ToString(Form.master);
                FormKind = Pg.form.kind;
                ModeVertical = Pg.form.modevertical;
                Regulations = Pg.form.regulations;
            }

            if (Pg.IsEditForm)
            {
                SetRegulationList(Periodic);
            }
            else
            {
                ListOfRegulations = Pg.ListOfRegulations;
            }

            if (Pg.form.formId == 0)
            {
                using Npgsql.NpgsqlConnection conn = new(connectionString);
                conn.Open();
                Npgsql.NpgsqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = "select max(form) from svod2.form";
                Npgsql.NpgsqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    FormID = reader.GetInt32(0) + 1;
                    Pg.form.formId = FormID;
                }
            }
            else
                FormID = Pg.form.formId;

            using (Npgsql.NpgsqlConnection conn = new(connectionString))
            {
                conn.Open();
                Npgsql.NpgsqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = "select form, coalesce(t.name, t.short) from svod2.form t where modeenter=2 and " +
                    "coalesce(upto,current_date)>=current_date and master is null order by name;";
                Npgsql.NpgsqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    SelectListItem item = new()
                    {
                        Value = reader.GetInt32(0).ToString()
                    };
                    if (!reader.IsDBNull(1)) 
                        item.Text = reader.GetString(1);
                    Items.Add(item);
                }
                reader.Close();
                cmd.Dispose();
            }
        }
        private void SetRegulationList(int periodic)
        {
            SelectListItem item0 = new("Отсутствует", "0");
            ListOfRegulations.Add(item0);
            using Npgsql.NpgsqlConnection conn = new(connectionString);
            conn.Open();
            using NpgsqlCommand cmd = conn.CreateCommand();
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
        public IActionResult OnPostCancel()
        {
            Pg.form = new Form();
            return Redirect("/Home");
        }
        public IActionResult OnPostCreate(string type)
        {
            switch (type)
            {
                case "Master":
                    IsMaster = true;
                    break;
                case "Daughter":
                    IsDaughter = true;
                    break;
                case "Simple":
                    IsSimple = true;
                    break;
                case "Analitic":
                    IsAnalitic = true;
                    Modeenter = 10;
                    break;
            }
            Pg.form.shortName = ShortName;
            Pg.form.name = Name;

            if (IsDaughter)
            {
                int master;
                try
                {
                    master = int.Parse(FormMaster);
                }
                catch
                {
                    Message = "Ошибка! Выберите мастер-форму.";
                    return new RedirectToPageResult("/PageForm", new { Message });
                }

                using Npgsql.NpgsqlConnection conn = new(connectionString);
                conn.Open();
                Npgsql.NpgsqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = "select periodic,modeenter,hassubject,hasfacility,hasterritory," +
                    $"since,note,department,changedate,kind from svod2.form t where form={master}";
                Npgsql.NpgsqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                        Periodic = reader.GetInt32(0);
                    if (!reader.IsDBNull(1))
                        Modeenter = reader.GetInt32(1);
                    if (!reader.IsDBNull(2))
                        HasSubject = Convert.ToBoolean(reader.GetInt32(2));
                    if (!reader.IsDBNull(3))
                        HasFacility = Convert.ToBoolean(reader.GetInt32(3));
                    if (!reader.IsDBNull(4))
                        HasTerritory = Convert.ToBoolean(reader.GetInt32(4));
                    if (!reader.IsDBNull(5))
                        Since = reader.GetDateTime(5);
                    if (!reader.IsDBNull(6))
                        Note = reader.GetString(6);
                    if (!reader.IsDBNull(7))
                        Department = reader.GetString(7);
                    if (!reader.IsDBNull(8))
                        ChangeDate = reader.GetDateTime(8);
                    if (!reader.IsDBNull(9))
                        FormKind = reader.GetInt32(9);
                }
                conn.Close();
            }
            if (FormKind == 1)
            {
                HasSubject = true;
            }
            if (FormKind == 3)
            {
                HasSubject = true;
                HasFacility = true;
            }
            if (FormKind == 4)
            {
                HasTerritory = true;
            }


            FormID = Pg.form.formId;
            Pg.form.TypeForm = type;
            Pg.form.kind = FormKind;
            if (IsDaughter)
                Pg.form.master = Convert.ToInt32(FormMaster);
            Pg.form.periodic = Periodic;
            Pg.form.regulations = Regulations;
            Pg.form.modeenter = Modeenter;
            Pg.form.modevertical = ModeVertical;
            Pg.form.hassubject = HasSubject;
            Pg.form.hasfacility = HasFacility;
            Pg.form.hasterritory = HasTerritory;
            Pg.form.since = Since;
            Pg.form.note = Note;
            Pg.form.department = Department;
            Pg.form.formdate = DateTime.Today;

            if (string.IsNullOrEmpty(Name))
            {
                Message = "Ошибка! Введите название формы.";
                return new RedirectToPageResult("/PageForm", new { Message });
            }
            if (FormKind == 0)
            {
                Message = "Ошибка! Выберите вид формы.";
                return new RedirectToPageResult("/PageForm", new { Message });
            }
            if (!HasSubject && !HasTerritory && !HasFacility)
            {
                Message = "Ошибка! Выберите флаг.";
                return new RedirectToPageResult("/PageForm", new { Message });
            }

            if (IsMaster)
            {           
                StringBuilder ins = new("INSERT INTO svod2.form(form,");
                StringBuilder val = new($"VALUES({Pg.form.formId},");
                ins.Append(Periodic != -1 ? "periodic," : ""); val.Append(Periodic != -1 ? Periodic + "," : "");
                ins.Append(FormKind != 0 ? "kind," : ""); val.Append(FormKind != 0 ? FormKind + "," : "");
                ins.Append("modeenter,"); val.Append($"{Modeenter},");
                ins.Append("modevertical,"); val.Append($"{ModeVertical},");
                ins.Append("hassubject,"); val.Append($"{Convert.ToInt32(HasSubject)},");
                ins.Append("hasfacility,"); val.Append($"{Convert.ToInt32(HasFacility)},");
                ins.Append("hasterritory,"); val.Append($"{Convert.ToInt32(HasTerritory)},");
                ins.Append(ShortName != null ? "short," : ""); val.Append(ShortName != null ? "\'" + ShortName + "\'," : "");
                ins.Append(Name != null ? "name," : ""); val.Append(Name != null ? "\'" + Name + "\'," : "");
                ins.Append(Note != null ? "note," : ""); val.Append(Note != null ? "\'" + Note + "\'," : "");
                ins.Append(Department != null ? "department," : ""); val.Append(Department != null ? "\'" + Department + "\'," : "");
                if (!Pg.IsEditForm)
                {
                    ins.Append("formdate,"); 
                    val.Append("\'" + DateTime.Today + "\',");
                }
                ins.Append("since,"); val.Append("\'" + Since + "\',");
                ins.Append("changedate) "); val.Append("\'" + ChangeDate + "\');");

                string text = ins.ToString() + val.ToString();
                string path = $"f-g-{Pg.form.formId}.sql";
                using (NpgsqlConnection connection = new(connectionString))
                {
                    connection.Open();
                    try
                    {
                        using NpgsqlCommand command = new(text, connection);
                        command.ExecuteNonQuery();
                        connection.Close();

                        using StreamWriter writer = new(path, false);
                        writer.WriteLine(text);
                    }
                    catch
                    {
                        connection.Close();
                        Message = "error";
                        return new RedirectToPageResult("/Home", new { Message });
                    }
                }
                Message = "success";
                return new RedirectToPageResult("/Home", new { Message });
            }
            else
            {
                int IsEditForm = 0;
                using Npgsql.NpgsqlConnection conn = new(connectionString);
                conn.Open();
                Npgsql.NpgsqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = $"select count(*) from svod2.form where form = {FormID}";
                Npgsql.NpgsqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                        IsEditForm = reader.GetInt32(0);
                }
                
                if (IsEditForm == 0)
                {
                    string insert = $"insert into svod2.form (form,formdate,changedate) values ({FormID},'{DateTime.Today}',\'{DateTime.Today}\')";
                    using NpgsqlConnection connection = new(connectionString);
                    connection.Open();
                    using (NpgsqlCommand command = new(insert, connection))
                    {
                        int res = command.ExecuteNonQuery();
                        if (res != 1)
                        {
                            Message = "Ошибка! Форма не была сохранена.";
                            return new RedirectToPageResult("/PageForm", new { Message });
                        }
                    }
                    connection.Close();
                }                
                StringBuilder ins = new("INSERT INTO svod2.form(form,");
                StringBuilder val = new($"VALUES({Pg.form.formId},");
                if (IsDaughter)
                {
                    ins.Append("master,"); val.Append($"\'" + FormMaster + "\',");
                }
                ins.Append(Periodic != -1 ? "periodic," : ""); val.Append(Periodic != -1 ? Periodic + "," : "");
                ins.Append(FormKind != 0 ? "kind," : ""); val.Append(FormKind != 0 ? FormKind + "," : "");
                ins.Append(Regulations != 0 ? "formregulations,": ""); val.Append(Regulations != 0 ? Regulations + "," : "");
                ins.Append("modeenter,"); val.Append($"{Modeenter},");
                ins.Append("modevertical,"); val.Append($"{ModeVertical},");
                ins.Append("hassubject,"); val.Append($"{Convert.ToInt32(HasSubject)},");
                ins.Append("hasfacility,"); val.Append($"{Convert.ToInt32(HasFacility)},");
                ins.Append("hasterritory,"); val.Append($"{Convert.ToInt32(HasTerritory)},");
                ins.Append(ShortName != null ? "short," : ""); val.Append(ShortName != null ? "\'" + ShortName + "\'," : "");
                ins.Append(Name != null ? "name," : ""); val.Append(Name != null ? "\'" + Name + "\'," : "");
                ins.Append(Note != null ? "note," : ""); val.Append(Note != null ? "\'" + Note + "\'," : "");
                ins.Append(Department != null ? "department," : ""); val.Append(Department != null ? "\'" + Department + "\'," : "");
                if (!Pg.IsEditForm)
                {
                    ins.Append("formdate,"); 
                    val.Append("\'" + DateTime.Today + "\',");
                }
                ins.Append("since,"); val.Append("\'" + Since + "\',");
                ins.Append("changedate) "); val.Append("\'" + ChangeDate + "\');");


                string text = ins.ToString() + val.ToString();
                Pg.Inserts.Add(text);
            }

            if (IsAnalitic)
            {
                return Redirect("/FormTableAnalitic");
            }
            if (Pg.IsFileForm)
            {
				return Redirect("/FormTable");
			}

            if (Pg.formColumns.Count == 0)
            {
                FormColumn col1 = new()
                {
                    formId = Pg.form.formId,
                    formcolumn = 0,
                    name = "%name",
                    precompute = "0"
                };
                FormColumn col2 = new()
                {
                    formId = Pg.form.formId,
                    formcolumn = 1,
                    name = "%code",
                    precompute = "0"
                };
                Pg.formColumns.Add(col1);
                Pg.formColumns.Add(col2);
            }

            return Redirect("/FormTable");
        }
    }
}
