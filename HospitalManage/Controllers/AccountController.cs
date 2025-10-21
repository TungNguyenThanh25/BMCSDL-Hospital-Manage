// Controllers/AccountController.cs
using HospitalManage.Models;
using HospitalManage.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using System.Security.Claims;

namespace HospitalManage.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _configuration;

        public AccountController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                string connectionString = _configuration.GetConnectionString("OracleDBConnection");
                using (OracleConnection con = new OracleConnection(connectionString))
                {
                    con.Open();
                    string checkUserSql = "SELECT COUNT(*) FROM accounts WHERE username = :username";
                    using (OracleCommand checkCmd = new OracleCommand(checkUserSql, con))
                    {
                        checkCmd.Parameters.Add(new OracleParameter("username", model.Username));
                        int userExists = Convert.ToInt32(checkCmd.ExecuteScalar());
                        if (userExists > 0)
                        {
                            ModelState.AddModelError("", "Tên đăng nhập đã tồn tại.");
                            return View(model);
                        }
                    }

                    // 2. Băm mật khẩu
                    string hashedPassword = PasswordHasher.HashPassword(model.Password);

                    // 3. Thêm người dùng mới vào DB
                    string insertSql = "INSERT INTO accounts (username, password, role, staff_id, created_at) VALUES (:username, :password, :role, :staff_id, SYSTIMESTAMP)";
                    using (OracleCommand cmd = new OracleCommand(insertSql, con))
                    {
                        cmd.Parameters.Add(new OracleParameter("username", model.Username));
                        cmd.Parameters.Add(new OracleParameter("password", hashedPassword));
                        cmd.Parameters.Add(new OracleParameter("role", model.Role));
                        cmd.Parameters.Add(new OracleParameter("staff_id", model.StaffId));

                        cmd.ExecuteNonQuery();
                    }
                }
                return RedirectToAction("Login");
            }
            return View(model);
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                string connectionString = _configuration.GetConnectionString("OracleDBConnection");
                using (OracleConnection con = new OracleConnection(connectionString))
                {
                    con.Open();
                    string sql = "SELECT password, role, staff_id FROM accounts WHERE username = :username";
                    using (OracleCommand cmd = new OracleCommand(sql, con))     
                    {
                        cmd.Parameters.Add(new OracleParameter("username", model.Username));
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string storedHashedPassword = reader["password"].ToString();
                                // Xác thực mật khẩu
                                if (PasswordHasher.VerifyPassword(model.Password, storedHashedPassword))
                                {
                                    // Lấy thông tin role và staff_id
                                    string role = reader["role"].ToString();
                                    string staffId = reader["staff_id"].ToString();

                                    // Tạo claims
                                    var claims = new List<Claim>
                                    {
                                        new Claim(ClaimTypes.Name, model.Username),
                                        new Claim(ClaimTypes.Role, role),
                                        new Claim("StaffId", staffId) // Thêm claim tùy chỉnh
                                    };

                                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                                    var authProperties = new AuthenticationProperties { };

                                    // Đăng nhập người dùng
                                    await HttpContext.SignInAsync(
                                        CookieAuthenticationDefaults.AuthenticationScheme,
                                        new ClaimsPrincipal(claimsIdentity),
                                        authProperties);

                                    return RedirectToAction("Index", "Home");
                                }
                            }
                        }
                    }
                }
            }
            ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không hợp lệ.");
            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}