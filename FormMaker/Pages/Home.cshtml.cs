using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Npgsql;
using svod_admin;
using System;
using System.Text;
using GemBox.Spreadsheet;
using System.Diagnostics.Metrics;
using System.Reflection.Metadata;
using static Npgsql.Replication.PgOutput.Messages.RelationMessage;
using static Org.BouncyCastle.Bcpg.Attr.ImageAttrib;

namespace FormMaker.Pages
{
	[Authorize]
	public class HomeModel : PageModel
    {
		[BindProperty] public string TypeForm { get; set; } = "";
		[BindProperty] public string Form { get; set; } = "";
		[BindProperty] public string? Message { get; set; } = "";
        [BindProperty] public bool Success { get; set; }
		[BindProperty] public IFormFile? formFile { get; set; }

		public ICollection<SelectListItem> LastWeekForm = new List<SelectListItem>();

		string? connectionString;
		readonly IConfiguration? configuration;
		public HomeModel(IConfiguration? _configuration)
		{
			configuration = _configuration;
			connectionString = configuration == null ? string.Empty : configuration.GetConnectionString("DefaultConnection");
		}
		public void OnGet()
        {
            Pg.formRows.Clear();
            Pg.formColumns.Clear();
            Pg.Inserts.Clear();
            Pg.form = new Form();

            Message = Convert.ToString(RouteData.Values["Message"]);
			Form form = Pg.form;
			switch (Message)
			{
				case "success":
					Message = "Успешно! Форма добавлена.";
					Success = true;
					break;
				case "error":
					Message = "Ошибка! Форма не была добавлена.";
                    Success = false;
                    break;
				case "seccessEdit":
                    Message = "Успешно! Форма была изменена.";
                    Success = true;
                    break;
				case "errorEdit":
                    Message = "Ошибка! Форма не была изменена."; 
					Success = false;
                    break;
				case "successDelete":
					Message = "Успешно! Форма была удалена.";
					Success = true;
					break;
				case "successClear":
					Message = "Успешно! Данные формы были удалены.";
					Success = true;
					break;
				case "nofile":
					Message = "Ошибка! Загрузите, пожалуйста, файл.";
					Success = false;
					break;
			}

			using Npgsql.NpgsqlConnection conn = new(connectionString);
			conn.Open();
			Npgsql.NpgsqlCommand cmd = conn.CreateCommand();
			cmd.CommandText = "SELECT form, name " +
				"FROM svod2.form WHERE since >= CURRENT_DATE - INTERVAL '60 days' order by form;";
			Npgsql.NpgsqlDataReader reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				SelectListItem item = new();
				item.Value = reader.GetInt32(0).ToString();
				if (!reader.IsDBNull(1))
					item.Text = reader.GetString(1) + " - " + reader.GetInt32(0).ToString();
				LastWeekForm.Add(item);
			}
			conn.Close();
		}

		public IActionResult OnPostCreate()
		{
			Pg.IsEditForm = false;
			string type = TypeForm;
			if (type == "") 
			{
				Message = "Ошибка! Выберите тип формы.";
				return new RedirectToPageResult("/Home", new { Message });
			}
			return new RedirectToPageResult("/PageForm", new { type});
		}
		public IActionResult OnPostClear()
		{
			string form = Form;

			string del = $"delete from svod2.formdata where form = {form}";
			using NpgsqlConnection deleteCol = new(connectionString);
			deleteCol.Open();
			using (NpgsqlCommand command = new(del, deleteCol))
			{
				int res = command.ExecuteNonQuery();
			}
			deleteCol.Close();

			Message = "successClear";
			return new RedirectToPageResult("/Home", new { Message });
		}

		public IActionResult OnPostOpen(IFormFile formFile)
		{
			int FormID;
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

			if (formFile != null && formFile.Length > 0)
			{
				Pg.IsFileForm = true;

				SpreadsheetInfo.SetLicense("FREE-LIMITED-KEY");
				string filename = formFile.FileName;
				Pg.form.name = filename.Substring(0, filename.LastIndexOf('.'));

				using (var stream = formFile.OpenReadStream())
				{
					var workbook = ExcelFile.Load(stream);

					foreach (var worksheet in workbook.Worksheets)
					{
						bool isFirstRow = true;
						int rowNum = 0;
						foreach (var row in worksheet.Rows)
						{
							if (isFirstRow)
							{
								int colNum = 0;
								foreach (var cell in row.AllocatedCells)
								{
									FormColumn col = new()
									{
										formId = Pg.form.formId,
										formcolumn = colNum,
										name = (string)cell.Value,
										precompute = "0",
										type = 1
									};
									Pg.formColumns.Add(col);
									colNum++;
								}

								isFirstRow = false; // Сбрасываем флаг после обработки первой строки
							}
							else
							{
								FormRow formrow = new()
								{
									formId = Pg.form.formId,
									formrow = rowNum
								};
								int cellNum = 0;
								foreach (var cell in row.AllocatedCells)
								{
									if (cellNum == 0)
									{
										formrow.name = (string)cell.Value;
									}
									else if (cellNum == 1)
									{
										formrow.code = Convert.ToString(cell.Value);
									}
									else break;
									formrow.type = 1;
									cellNum++;
								}
								formrow.formrow = rowNum;
								Pg.formRows.Add(formrow);
								rowNum++;
							}
						}
					}
				}
				string type = "Simple";
				return new RedirectToPageResult("/PageForm", new { type }); // Перенаправление на другую страницу после обработки
			}

			Message = "nofile";
			return new RedirectToPageResult("/Home", new { Message });
		}

		public IActionResult OnPostContinue()
		{
            Pg.IsEditForm = true;
            string form = Form;
			if (string.IsNullOrEmpty(form))
			{
				Message = "Ошибка! Выберите форму для редактирования.";
				return new RedirectToPageResult("/Home", new { Message });
			}

			using Npgsql.NpgsqlConnection conn = new(connectionString);
			conn.Open();
			Npgsql.NpgsqlCommand cmd = conn.CreateCommand();
			cmd.CommandText = "select form,master,periodic,kind,modeenter,modevertical," +
				"hassubject,hasfacility,hasterritory,short,name,since,note,department" +
				$" from svod2.form where form={form}";
			Npgsql.NpgsqlDataReader reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				if (!reader.IsDBNull(0))
					Pg.form.formId = reader.GetInt32(0);
				if (!reader.IsDBNull(1))
					Pg.form.master = reader.GetInt32(1);
				if (!reader.IsDBNull(2))
					Pg.form.periodic = reader.GetInt32(2);
				if (!reader.IsDBNull(3))
					Pg.form.kind = reader.GetInt32(3);
				if (!reader.IsDBNull(4))
					Pg.form.modeenter = reader.GetInt32(4);
				if (!reader.IsDBNull(5))
					Pg.form.modevertical = reader.GetInt32(5);
				if (!reader.IsDBNull(6))
					Pg.form.hassubject = Convert.ToBoolean(reader.GetInt32(6));
				if (!reader.IsDBNull(7))
					Pg.form.hasfacility = Convert.ToBoolean(reader.GetInt32(7));
				if (!reader.IsDBNull(8))
					Pg.form.hasterritory = Convert.ToBoolean(reader.GetInt32(8));
				if (!reader.IsDBNull(9))
					Pg.form.shortName = reader.GetString(9);
				if (!reader.IsDBNull(10))
					Pg.form.name = reader.GetString(10);
				if (!reader.IsDBNull(11))
					Pg.form.since = reader.GetDateTime(11);
				if (!reader.IsDBNull(12))
					Pg.form.note = reader.GetString(12);
				if (!reader.IsDBNull(13))
					Pg.form.department = reader.GetString(13);
			}
			conn.Close();

			conn.Open();
			cmd = conn.CreateCommand();
			cmd.CommandText = "select form,formcolumn,name,note,compute,precompute," +
				"outercompute,summation,mandatory,copying,type" +
				$" from svod2.formcolumn where form={form}";
			reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				FormColumn col = new (); 
				if (!reader.IsDBNull(0))
					col.formId = reader.GetInt32(0);
				if (!reader.IsDBNull(1))
					col.formcolumn = reader.GetInt32(1) - 1;
				if (!reader.IsDBNull(2))
					col.name = reader.GetString(2);
				if (!reader.IsDBNull(3))
					col.note = reader.GetString(3);
				if (!reader.IsDBNull(4))
					col.compute = reader.GetString(4);
				if (!reader.IsDBNull(5))
					col.precompute = reader.GetString(5).Split('*')[1];
				if (!reader.IsDBNull(6))
					col.outercompute = reader.GetString(6);
				if (!reader.IsDBNull(7))
					col.summation = reader.GetInt32(7);
				if (!reader.IsDBNull(8))
					col.mandatory = reader.GetInt32(8);
				if (!reader.IsDBNull(9))
					col.copying = reader.GetInt32(9);
				if (!reader.IsDBNull(10))
					col.type = reader.GetInt32(10);
				Pg.formColumns.Add(col);
			}
			conn.Close();

			conn.Open();
			cmd = conn.CreateCommand();
			cmd.CommandText = "select form,formrow,code,name,note,unit,balancesign," +
				"okp,measure,compute,summation,mandatory,copying,type" +
				$" from svod2.formrow where form={form}";
			reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				FormRow row = new FormRow();
				if (!reader.IsDBNull(0))
					row.formId = reader.GetInt32(0);
				if (!reader.IsDBNull(1))
					row.formrow = reader.GetInt32(1) - 1;
				if (!reader.IsDBNull(2))
					row.code = reader.GetString(2);
				if (!reader.IsDBNull(3))
					row.name = reader.GetString(3);
				if (!reader.IsDBNull(4))
					row.note = reader.GetString(4);
				if (!reader.IsDBNull(5))
					row.unit = reader.GetString(5);
				if (!reader.IsDBNull(6))
					row.balancesign = reader.GetString(6);
				if (!reader.IsDBNull(7))
					row.okp = reader.GetString(7);
				if (!reader.IsDBNull(8))
					row.measure = reader.GetString(8);
				if (!reader.IsDBNull(9))
					row.compute = reader.GetString(9);
				if (!reader.IsDBNull(10))
					row.summation = reader.GetInt32(10);
				if (!reader.IsDBNull(11))
					row.mandatory = reader.GetInt32(11);
				if (!reader.IsDBNull(12))
					row.copying = reader.GetInt32(12);
				if (!reader.IsDBNull(13))
					row.type = reader.GetInt32(13);
				Pg.formRows.Add(row);
			}
			conn.Close();
			string type = "";

			if (Pg.form.modeenter == 10)
				type = "Analitic";
			if (Pg.form.master != 0)
			{
				type = "Daughter";
			}
			else
			{
				type = "Simple";
			}
			return new RedirectToPageResult("/PageForm", new { type });
		}

		public IActionResult OnPostDelete()
		{
			string form = Form;

			if (string.IsNullOrEmpty(form))
			{
                Message = "Ошибка! Выберите форму для удаления.";
                return new RedirectToPageResult("/Home", new { Message });
            }
			string del = $"delete from svod2.formcolumn where form = {form}";
			using NpgsqlConnection deleteCol = new(connectionString);
			deleteCol.Open();
			using (NpgsqlCommand command = new(del, deleteCol))
			{
				int res = command.ExecuteNonQuery();
			}
			deleteCol.Close();
			del = $"delete from svod2.formrow where form = {form}";
			using NpgsqlConnection deleteRow = new(connectionString);
			deleteRow.Open();
			using (NpgsqlCommand command = new(del, deleteRow))
			{
				int res = command.ExecuteNonQuery();
			}
			deleteRow.Close();
			del = $"delete from svod2.form where form = {form}";
			using NpgsqlConnection deleteForm = new(connectionString);
			deleteForm.Open();
			using (NpgsqlCommand command = new(del, deleteForm))
			{
				int res = command.ExecuteNonQuery();
			}
			deleteForm.Close();
			Message = "successDelete";
			return new RedirectToPageResult("/Home", new { Message });
		}
	}
}
