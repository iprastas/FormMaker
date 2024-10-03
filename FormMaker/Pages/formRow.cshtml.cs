using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using svod_admin;
using System.Text;

namespace FormMaker.Pages
{
    [Authorize]
    public class formRowModel : PageModel
    {
        [BindProperty] public string? Message { get; set; } = "";
        [BindProperty] public int FormID { get; set; }
        [BindProperty] public string TypeForm { get; set; } = "";
        [BindProperty] public string Code { get; set; } = "";
        public int FormRow { get; set; }
        [BindProperty] public string Name { get; set; } = "";
        [BindProperty] public string Note { get; set; } = "";
        [BindProperty] public int Type { get; set; }
        [BindProperty] public string Unit { get; set; } = "";
        [BindProperty] public string Balancesign { get; set; } = "";
        [BindProperty] public string Okp { get; set; } = "";
        [BindProperty] public string Measure { get; set; } = "";
        [BindProperty] public string Compute { get; set; } = "";
        [BindProperty] public int Summation { get; set; }
        [BindProperty] public int Mandatory { get; set; }
        [BindProperty] public int Copying { get; set; }
        [BindProperty] public List<SelectListItem> Units { get; set; } = new List<SelectListItem>();

        public FormRow row { get; set; } = new FormRow(); 

        public void OnGet()
        {
            int ind = Convert.ToInt32(RouteData.Values["ind"]);
            FormID = Pg.form.formId;
            Pg.row.formrow = ind == -1 ? 0 : ind;
            TypeForm = Pg.form.TypeForm;
            Message = Convert.ToString(RouteData.Values["Message"]);

            Units = Pg.ListOfUnits;

            if (Message != null )
            {
                row = Pg.row;
                Code = row.code;
                Name = row.name;
                Note = row.note;
                Type = row.type;
                Unit = row.unit;
                Balancesign = row.balancesign;
                Okp = row.okp;
                Measure = row.measure;
                Compute = row.compute;
                Summation = row.summation;
                Mandatory = row.mandatory;
                Copying = row.copying;
            }
            if (ind != -1 && ind < Pg.formRows.Count())
            {
                FormRow = Pg.formRows[ind].formrow;
                Code = Pg.formRows[ind].code;
                Name = Pg.formRows[ind].name;
                Note = Pg.formRows[ind].note;
                Type = Pg.formRows[ind].type;
                Unit = Pg.formRows[ind].unit;
                Balancesign = Pg.formRows[ind].balancesign;
                Okp = Pg.formRows[ind].okp;
                Measure = Pg.formRows[ind].measure;
                Compute = Pg.formRows[ind].compute;
                Summation = Pg.formRows[ind].summation;
                Mandatory = Pg.formRows[ind].mandatory;
                Copying = Pg.formRows[ind].copying;
            }
        }
        public IActionResult OnPostCancel()
        {
            return Redirect("/FormTable");
        }
        public IActionResult OnPostCreate()
        {
            Pg.row.formId = Pg.form.formId;
            FormRow = Pg.row.formrow;
            Pg.row.code = Code;
            Pg.row.name = Name;
            Pg.row.note = Note;
            Pg.row.type = Type;
            Pg.row.unit = Unit;
            Pg.row.balancesign = Balancesign;
            Pg.row.okp = Okp;
            Pg.row.measure = Measure;
            Pg.row.compute = Compute;
            Pg.row.summation = Summation;
            Pg.row.mandatory = Mandatory;
            Pg.row.copying = Copying;

            if (string.IsNullOrEmpty(Name))
            {
                Message = "ќшибка! ¬ведите название строки.";
                return new RedirectToPageResult("/formRow", new { Message });
            }
            var it = Pg.formRows.Find(x => x.formrow == FormRow);
            if (it != null)
            {
                Pg.formRows.Remove(it);
            }
            Pg.formRows.Insert(FormRow, Pg.row);
            Pg.row = new FormRow();

            //StringBuilder ins = new StringBuilder("INSERT INTO svod2.formrow(form,formrow,");
            //StringBuilder val = new StringBuilder($"VALUES({Pg.form.formId},{FormRow},");
            //ins.Append((Code != null ? "code," : "")); val.Append((Code != null ? "\'" + Code + "\'," : ""));
            //ins.Append((Name != null ? "name," : "")); val.Append((Name != null ? "\'" + Name + "\'," : ""));
            //ins.Append((Note != null ? "note," : "")); val.Append((Note != null ? "\'" + Note + "\'," : ""));
            //ins.Append((Type != 0 ? "type," : "")); val.Append((Type != 0 ? Type + "," : ""));
            //ins.Append((Unit != "-1" ? "unit," : "")); val.Append((Unit != "-1" ? "\'" + Unit + "\'," : ""));
            //ins.Append((Balancesign != null ? "balancesign," : "")); val.Append((Balancesign != null ? "\'" + Balancesign + "\'," : ""));
            //ins.Append((Okp != null ? "okp," : "")); val.Append((Okp != null ? "\'" + Okp + "," : ""));
            //ins.Append((Measure != null ? "measure," : "")); val.Append((Measure != null ? "\'" + Measure + "\'," : ""));
            //ins.Append((Compute != null ? "compute," : "")); val.Append((Compute != null ? "\'" + Compute + "\'," : ""));
            //ins.Append("summation,"); val.Append(Summation + ",");
            //ins.Append("mandatory,"); val.Append(Mandatory + ",");
            //ins.Append("copying,"); val.Append(Copying + ",");
            //ins.Append("changedate) "); val.Append("\'" + DateTime.Now + "\')");

            //string text = ins.ToString() + val.ToString();
            //Pg.Inserts.Add(text);

            return Redirect("/FormTable");
        }
    }
}
