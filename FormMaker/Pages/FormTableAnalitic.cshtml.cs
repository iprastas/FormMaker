using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using svod_admin;
using Npgsql;
using NpgsqlTypes;
using System.Diagnostics.Metrics;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Primitives;
using System.IO;

namespace FormMaker.Pages
{
    [Authorize]
    public class FormTableAnaliticModel : PageModel
    {
        [BindProperty] public string? Message { get; set; } = "";
        public int FormID { get; set; }
        public string Name { get; set; } = "";
        public List<FormColumn> formColumns = [];

        readonly string? connectionString;
        readonly IConfiguration? configuration;
        public FormTableAnaliticModel(IConfiguration? _configuration)
        {
            configuration = _configuration;
            connectionString = configuration == null ? string.Empty : configuration.GetConnectionString("DefaultConnection");
        }

        public void OnGet()
        {
            FormID = Pg.form.formId;
            Name = Pg.form.name;

            foreach (var col in Pg.formColumns)
            {
                formColumns.Add(col);
            }

            Message = Convert.ToString(RouteData.Values["Message"]);
        }

        public IActionResult OnPostDeleteColumn(int ind)
        {
            Pg.formColumns.RemoveAt(ind);
            foreach (var col in Pg.formColumns)
            {
                if (col.formcolumn > ind)
                {
                    col.formcolumn = ind;
                    ind++;
                }
            }
            return Redirect("/FormTableAnalitic");
        }
        public IActionResult OnPostEditColumn(int ind)
        {
            return new RedirectToPageResult("/formColumn", new { ind });
        }
        public IActionResult OnPostColumn()
        {
            int ind = Pg.formColumns.Count == 0 ? -1 : Pg.formColumns.Count;
            return new RedirectToPageResult("/formColumn", new { ind });
        }
        public IActionResult OnPostSave()
        {
            if (Pg.formColumns.Count > 0)
            {
                foreach (var col in Pg.formColumns)
                {
                    StringBuilder ins = new("INSERT INTO svod2.formcolumn(form,formcolumn,");
                    StringBuilder val = new($"VALUES({Pg.form.formId},{col.formcolumn + 1},");
                    ins.Append((!string.IsNullOrEmpty(col.name) ? "name," : "")); val.Append((!string.IsNullOrEmpty(col.name) ? "\'" + col.name + "\'," : ""));
                    ins.Append((!string.IsNullOrEmpty(col.note) ? "note," : "")); val.Append((!string.IsNullOrEmpty(col.note) ? "\'" + col.note + "\'," : ""));
                    ins.Append((col.type != 0 ? "type," : "")); val.Append((col.type != 0 ? col.type + "," : ""));
                    ins.Append((!string.IsNullOrEmpty(col.compute) ? "compute," : "")); val.Append((!string.IsNullOrEmpty(col.compute) ? "\'" + col.compute + "\'," : ""));
                    ins.Append((col.precompute != "0" && !string.IsNullOrEmpty(col.precompute) ? "precompute," : ""));
                    val.Append((col.precompute != "0" && !string.IsNullOrEmpty(col.precompute) ? $"\'[{col.formcolumn + 1}]/*" + col.precompute + "*/\'," : ""));
                    ins.Append((!string.IsNullOrEmpty(col.outercompute) ? "outercompute," : ""));
                    val.Append((!string.IsNullOrEmpty(col.outercompute) ? "\'" + col.outercompute + "\'," : ""));
                    ins.Append("summation,"); val.Append(col.summation + ",");
                    ins.Append("mandatory,"); val.Append(col.mandatory + ",");
                    ins.Append("copying,"); val.Append(col.copying + ",");
                    ins.Append("changedate) "); val.Append("\'" + DateTime.Today + "\')");

                    string text = ins.ToString() + val.ToString();
                    Pg.Inserts.Add(text);
                }
            }
            else
            {
                Message = "Ошибка! Необходимо добавить столбцы, чтобы сохранить форму.";
                return new RedirectToPageResult("/FormTableAnalitic", new { Message });
            }

            string path = $"f-g-{Pg.form.formId}.sql";
            using (NpgsqlConnection connection = new(connectionString))
            {
                connection.Open();

                using NpgsqlTransaction transaction = connection.BeginTransaction();
                try
                {
                    if (Pg.IsEditForm)
                    {
                        string del = $"delete from svod2.formcolumn where form = {Pg.form.formId}";
                        using NpgsqlCommand DelCol = new(del, connection);
                        DelCol.Transaction = transaction;
                        DelCol.ExecuteNonQuery();
                    }

                    string delform = $"delete from svod2.form where form = {Pg.form.formId}";
                    using NpgsqlCommand DelForm = new(delform, connection);
                    DelForm.Transaction = transaction;
                    DelForm.ExecuteNonQuery();

                    foreach (string insertQuery in Pg.Inserts)
                    {
                        using NpgsqlCommand command = new(insertQuery, connection);
                        command.Transaction = transaction;
                        command.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    using StreamWriter writer = new(path, false);
                    foreach (string insertQuery in Pg.Inserts)
                    {
                        writer.WriteLine(insertQuery);
                    }

                    Message = "success";
                    if (Pg.IsEditForm)
                        Message = "seccessEdit";
                }
                catch (NpgsqlException)
                {
                    transaction.Rollback();
                    
                    Message = "error";// e.Message + " errcode=" + e.ErrorCode;
                    if (Pg.IsEditForm)
                        Message = "errorEdit";
                    return new RedirectToPageResult("/Home", new { Message });
                }
                finally
                {
                    connection?.Close();

                    Pg.formRows.Clear();
                    Pg.formColumns.Clear();
                    Pg.Inserts.Clear();
                    Pg.form = new Form();
                }
            }

            return new RedirectToPageResult("/Home", new { Message });
        }
    }
}
