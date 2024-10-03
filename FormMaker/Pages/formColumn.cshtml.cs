using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using svod_admin;
using System.Text;
using System.Diagnostics.Metrics;
using System.IO;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FormMaker.Pages
{
    [Authorize]
    public class formColumnModel : PageModel
    {
		[BindProperty] public string? Message { get; set; } = "";
        [BindProperty] public int FormID { get; set; }
        [BindProperty] public string TypeForm { get; set; } = "";
        public int FormColumn { get; set; }
        public int Periodic = Pg.form.periodic;
        [BindProperty] public string ColName { get; set; } = "";
		[BindProperty] public string Note { get; set; } = "";
		[BindProperty] public int Type { get; set; }
		[BindProperty] public string Compute { get; set; } = "";
		[BindProperty] public string Precompute { get; set; } = "";
        [BindProperty] public string Outercompute { get; set; } = "";
		[BindProperty] public int Summation { get; set; }
		[BindProperty] public int Mandatory { get; set; }
		[BindProperty] public int Copying { get; set; }
        public FormColumn col {  get; set; } = new FormColumn();

        public void OnGet()
        {
            int ind = Convert.ToInt32(RouteData.Values["ind"]);
            FormID = Pg.form.formId;
            Pg.col.formcolumn = ind == -1? 0 : ind;
            TypeForm = Pg.form.TypeForm;
            Message = Convert.ToString(RouteData.Values["Message"]);
            if (Message != null )
            {
                col = Pg.col;
                FormColumn = col.formcolumn;
                ColName = col.name;
                Note = col.note;
                Type = col.type;
                Compute = col.compute;
                Precompute = col.precompute;
                Outercompute = col.outercompute;
                Summation = col.summation;
                Mandatory = col.mandatory;
                Copying = col.copying;
            }
            if (ind != -1 && ind < Pg.formColumns.Count())
            {
                FormColumn = Pg.formColumns[ind].formcolumn;
                ColName = Pg.formColumns[ind].name;
                Note = Pg.formColumns[ind].note;
                Type = Pg.formColumns[ind].type;
                Compute = Pg.formColumns[ind].compute;
                Precompute = Pg.formColumns[ind].precompute;
                Outercompute = Pg.formColumns[ind].outercompute;
                Summation = Pg.formColumns[ind].summation;
                Mandatory = Pg.formColumns[ind].mandatory;
                Copying = Pg.formColumns[ind].copying;
            }
        }
        public IActionResult OnPostCancel()
        {
            if (Pg.form.TypeForm == "Analitic")
            {
                return Redirect("/FormTableAnalitic");
            }
            return Redirect("/FormTable");
        }
        public IActionResult OnPostCreate()
        {
            FormColumn = Pg.col.formcolumn;
            Pg.col.formId = Pg.form.formId;
            Pg.col.name = ColName;
            Pg.col.note = Note;
            Pg.col.type = Type;
            Pg.col.compute = Compute;
            Pg.col.precompute = Precompute;
            Pg.col.outercompute = Outercompute;
            Pg.col.summation = Summation;
            Pg.col.mandatory = Mandatory;
            Pg.col.copying = Copying;

            if (string.IsNullOrEmpty(ColName))
            {
                Message = "Ошибка! Введите название столбца.";
                return new RedirectToPageResult("/formColumn", new { Message });
            }
            if (Type == 0)
            {
                Message = "Ошибка! Выберите тип столбца.";
                return new RedirectToPageResult("/formColumn", new { Message });
            }
            if (Pg.form.TypeForm == "Analitic" && Outercompute == null)
            {
                Message = "Ошибка! Заполните поле \"Межформенные расчеты\".";
                return new RedirectToPageResult("/formColumn", new { Message });
            }
            var it = Pg.formColumns.Find(x => x.formcolumn == FormColumn);
            if (it != null)
            {
                Pg.formColumns.Remove(it);
            }
            Pg.formColumns.Insert(FormColumn, Pg.col);
            Pg.col = new FormColumn();

            //StringBuilder ins = new StringBuilder("INSERT INTO svod2.formcolumn(form,formcolumn,");
            //StringBuilder val = new StringBuilder($"VALUES({Pg.form.formId},{FormColumn},");
            //ins.Append((ColName != null ? "name," : "")); val.Append((ColName != null ? "\'" + ColName + "\'," : ""));
            //ins.Append((Note != null ? "note," : "")); val.Append((Note != null ? "\'" + Note + "\'," : ""));
            //ins.Append((Type != 0 ? "type," : "")); val.Append((Type != 0 ? Type + "," : ""));
            //ins.Append((Compute != null ? "compute," : "")); val.Append((Compute != null ? "\'" + Compute + "\'," : ""));
            //ins.Append((Precompute != null ? "precompute," : "")); val.Append((Precompute != null ? $"\'[{FormColumn}]/*" + Precompute + "*/\'," : ""));
            //ins.Append((Outercompute != null ? "outercompute," : "")); val.Append((Outercompute != null ? "\'" + Outercompute + "\'," : ""));
            //ins.Append("summation,"); val.Append(Summation + ",");
            //ins.Append("mandatory,"); val.Append(Mandatory + ",");
            //ins.Append("copying,"); val.Append(Copying + ",");
            //ins.Append("changedate) "); val.Append("\'" + DateTime.Now + "\')");

            //string text = ins.ToString() + val.ToString();
            //Pg.Inserts.Add(text);
            if (Pg.form.TypeForm == "Analitic")
            {
                return Redirect("/FormTableAnalitic");
            }
            return Redirect("/FormTable");
        }
    }
}
