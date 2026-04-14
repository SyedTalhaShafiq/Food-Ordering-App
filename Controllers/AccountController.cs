using FoodOrderingApp.Models;
using System;
using System.Data.Entity.Infrastructure; // For DbUpdateException
using System.Data.SqlClient; // For SqlException
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;
using System.Web.Security; // For FormsAuthentication

namespace FoodOrderingApp.Controllers
{
    public class AccountController : Controller
    {
        private FoodOrderingDBEntities db = new FoodOrderingDBEntities();

        // GET: /Account/Register
        public ActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (db.Users.Any(u => u.Email == model.Email))
                    {
                        ModelState.AddModelError("Email", "Email address is already registered.");
                        return View(model);
                    }

                    string passwordHash = HashPassword(model.Password);

                    var newUser = new User
                    {
                        FullName = model.FullName,
                        Email = model.Email,
                        PasswordHash = passwordHash,
                        Role = "Customer", // Default role for registered users
                        CreatedAt = DateTime.Now
                    };

                    db.Users.Add(newUser);
                    db.SaveChanges();

                    ViewBag.RegistrationSuccess = "Registration successful! You can now log in.";
                    return View("Login"); // Redirect to login page after registration
                }
                catch (DbUpdateException ex)
                {
                    // Handle database-related exceptions (e.g., unique constraint violations, null values)
                    var sqlException = ex.GetBaseException() as SqlException;
                    if (sqlException != null && sqlException.Number == 2627) // Violation of UNIQUE KEY constraint
                    {
                        ModelState.AddModelError("Email", "Email address is already registered.");
                    }
                    else if (sqlException != null && (sqlException.Number == 515 || sqlException.Number == 1048)) // Cannot insert NULL
                    {
                        // Log the detailed error for debugging
                        System.Diagnostics.Debug.WriteLine($"Database Insert Error: {sqlException.Message}");
                        ModelState.AddModelError("", "One or more required fields are missing. Please check your input.");
                    }
                    else
                    {
                        // Log the general error for debugging
                        System.Diagnostics.Debug.WriteLine($"Database Update Exception: {ex.Message}");
                        ModelState.AddModelError("", "An error occurred while saving your registration. Please try again later.");
                    }
                    return View(model);
                }
            }

            return View(model);
        }

        // GET: /Account/Login
        public ActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                
                
                    var user = db.Users.FirstOrDefault(u => u.Email == model.Email);
                    if (user != null)
                    {
                        // Verify the password hash
                        if (VerifyPassword(model.Password, user.PasswordHash))
                        {
                            // Authentication successful
                            FormsAuthentication.SetAuthCookie(user.Email, model.RememberMe); // Use FormsAuthentication
                            return RedirectToAction("Index", "Home"); // Redirect to home page
                        }
                        else
                        {
                            ModelState.AddModelError("", "Invalid login attempt.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", "Invalid login attempt.");
                    }                

                    return View(model);
                
            }

            return View(model);
        }

        // GET: /Account/Logout
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Home");
        }

        // Helper method to hash the password
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        // Helper method to verify the password
        private bool VerifyPassword(string password, string hashedPassword)
        {
            string hashedInputPassword = HashPassword(password);
            return hashedInputPassword.Equals(hashedPassword, StringComparison.OrdinalIgnoreCase);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}