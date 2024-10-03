using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Security.Claims;

namespace FormMaker.Pages
{
    [IgnoreAntiforgeryToken]
    public class IndexModel(IConfiguration _configuration) : PageModel
    {
		readonly IConfiguration configuration = _configuration;

        [BindProperty] public string Username { get; set; } = "";
        [BindProperty] public string Password { get; set; } = "";
        public string? Message { get; set; } = "";

        public void OnGet()
        {
            Message = Convert.ToString(RouteData.Values["Message"]);
        }
        public IActionResult OnGetExit()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("/Index");
        }

        public IActionResult OnPostEnter()
        {
            if (ModelState.IsValid && Authenticate(Username, Password))
            {
                var claims = new List<Claim>
                {
                    new(ClaimsIdentity.DefaultNameClaimType, Username)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties { AllowRefresh = true };
                HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);
                return Redirect("/Home");
            }
            Message = "Ошибка! Неверно введен логин или пароль.";
            return new RedirectToPageResult("/Index", new { Message });
        }
        private bool Authenticate(string user, string pwd)
        {
            string? connectionString = configuration.GetConnectionString("DefaultConnection");
            bool ret = false;
            using (NpgsqlConnection conn = new(connectionString))
            {
                conn.Open();

                Npgsql.NpgsqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT count(*) FROM svod2.targetusers WHERE TRIM(login)=TRIM(:u) and TRIM(password)=:pwd and coalesce(passwordupto,current_date)>=current_date";
                cmd.Parameters.Add(":u",NpgsqlTypes.NpgsqlDbType.Text).Value=user;
                cmd.Parameters.Add(":pwd",NpgsqlTypes.NpgsqlDbType.Text).Value=pwd;
                NpgsqlDataReader reader = cmd.ExecuteReader();
                while(reader.Read()) { 
                    if(reader.GetDecimal(0) > 0 ) ret = true;      
                }
                conn.Close();
            }
            return ret;
        }
    }
}