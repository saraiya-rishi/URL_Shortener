using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using URL_Shortener.Models;


//namespace URL_Shortener.Controllers
//{
//    public class UrlController : Controller
//    {
//        private readonly string connectionString = "Data Source=SSMOZ9;Initial Catalog=URL_Demo;User ID=sa;Password=SSM@123;Encrypt=False";
//        public IActionResult Index()
//        {
//            return View();
//        }

//        [HttpPost]
//        public ActionResult ShortenUrl(UrlModel model)
//        {
//            if (string.IsNullOrEmpty(model.LongUrl))
//            {
//                ViewBag.Error = "Please provide a valid URL.";
//                return View("Index");
//            }

//            string shortCode = GenerateShortCode();
//            using (SqlConnection conn = new SqlConnection(connectionString))
//            {
//                string query = "INSERT INTO UrlMappings (ShortCode, LongUrl) VALUES (@ShortCode, @LongUrl)";
//                SqlCommand cmd = new SqlCommand(query, conn);
//                cmd.Parameters.AddWithValue("@ShortCode", shortCode);
//                cmd.Parameters.AddWithValue("@LongUrl", model.LongUrl);
//                conn.Open();
//                cmd.ExecuteNonQuery();
//            }

//            string baseUrl = $"{Request.Scheme}://{Request.Host}";
//            string shortPath = Url.Action("Go", "Url", new { id = shortCode });
//            ViewBag.ShortUrl = baseUrl + shortPath;
//            return View("Index");
//        }

//        public ActionResult Go(string id)
//        {
//            string longUrl = null;

//            using (SqlConnection conn = new SqlConnection(connectionString))
//            {
//                string query = "SELECT LongUrl FROM UrlMappings WHERE ShortCode = @ShortCode";
//                SqlCommand cmd = new SqlCommand(query, conn);
//                cmd.Parameters.AddWithValue("@ShortCode", id);
//                conn.Open();

//                SqlDataReader reader = cmd.ExecuteReader();
//                if (reader.Read())
//                {
//                    longUrl = reader["LongUrl"].ToString();
//                }
//            }

//            if (!string.IsNullOrEmpty(longUrl))
//            {
//                return Redirect(longUrl);
//            }

//            return Content("Invalid or expired URL.");
//        }

//        private string GenerateShortCode()
//        {
//            return Guid.NewGuid().ToString("n").Substring(0, 6);
//        }
//    }

//}


namespace URL_Shortener.Controllers
{
    public class UrlController : Controller
    {
        private readonly string connectionString = "Data Source=RISHI\\SSMSERVER;Initial Catalog=URL_Demo;User ID=sa;Password=rishi8102;Encrypt=False";

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Index(UrlModel model)
        {
            if (string.IsNullOrEmpty(model.LongUrl))
            {
                ViewBag.Error = "URL cannot be empty";
                return View("Index");
            }

            string shortCode = string.IsNullOrWhiteSpace(model.CustomAlias) ? GenerateShortCode() : model.CustomAlias;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string checkQuery = "SELECT COUNT(*) FROM UrlMappings WHERE ShortCode = @ShortCode";
                SqlCommand checkCmd = new SqlCommand(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@ShortCode", shortCode);

                conn.Open();
                int count = (int)checkCmd.ExecuteScalar();
                if (count > 0)
                {
                    ViewBag.Error = "Short code already exists. Try another alias.";
                    return View("Index");
                }

                string insertQuery = "INSERT INTO UrlMappings (LongUrl, ShortCode, ExpiryDate, Password, ClickCount, CreatedAt) VALUES (@LongUrl, @ShortCode, @ExpiryDate, @Password, 0, @CreatedAt)";
                SqlCommand cmd = new SqlCommand(insertQuery, conn);
                cmd.Parameters.AddWithValue("@LongUrl", model.LongUrl);
                cmd.Parameters.AddWithValue("@ShortCode", shortCode);
                cmd.Parameters.AddWithValue("@ExpiryDate", (object?)model.ExpiryDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Password", (object?)model.Password ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                cmd.ExecuteNonQuery();
            }
        
            string baseUrl = $"{Request.Scheme}://{Request.Host}";
            string shortPath = Url.Action("Go", "Url", new { id = shortCode });
            ViewBag.ShortUrl = baseUrl + shortPath;
            return View("Index", model);
        }

        public ActionResult Go(string id)
        {
            string longUrl = null;
            string password = null;
            DateTime? expiry = null;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT LongUrl, Password, ExpiryDate FROM UrlMappings WHERE ShortCode = @ShortCode";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ShortCode", id);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    longUrl = reader["LongUrl"].ToString();
                    password = reader["Password"] == DBNull.Value ? null : reader["Password"].ToString();
                    expiry = reader["ExpiryDate"] == DBNull.Value ? null : (DateTime?)reader["ExpiryDate"];
                }
            }

            if (expiry.HasValue && DateTime.Now > expiry.Value)
            {
                return Content("⏳ This link has expired.");
            }

            if (!string.IsNullOrEmpty(password))
            {
                TempData["TargetUrl"] = longUrl;
                return RedirectToAction("Verify", new { code = id });
            }

            UpdateClickCount(id);
            return Redirect(longUrl);
        }

        public ActionResult Verify(string code)
        {
            ViewBag.Code = code;
            return View();
        }

        [HttpPost]
        public ActionResult Verify(string code, string inputPassword)
        {
            string storedPassword = null;
            string longUrl = null;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT LongUrl, Password FROM UrlMappings WHERE ShortCode = @ShortCode";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ShortCode", code);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    storedPassword = reader["Password"].ToString();
                    longUrl = reader["LongUrl"].ToString();
                }
            }

            if (storedPassword == inputPassword)
            {
                UpdateClickCount(code);
                return Redirect(longUrl);
            }
            else
            {
                ViewBag.Code = code;
                ViewBag.Error = "Incorrect password.";
                return View();
            }
        }

        private void UpdateClickCount(string shortCode)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string updateQuery = "UPDATE UrlMappings SET ClickCount = ClickCount + 1 WHERE ShortCode = @ShortCode";
                SqlCommand cmd = new SqlCommand(updateQuery, conn);
                cmd.Parameters.AddWithValue("@ShortCode", shortCode);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public ActionResult Dashboard()
        {
            List<UrlModel> urls = new List<UrlModel>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT * FROM UrlMappings ORDER BY CreatedAt DESC";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    urls.Add(new UrlModel
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        LongUrl = reader["LongUrl"].ToString(),
                        ShortCode = reader["ShortCode"].ToString(),
                        ClickCount = Convert.ToInt32(reader["ClickCount"]),
                        ExpiryDate = reader["ExpiryDate"] != DBNull.Value ? (DateTime?)reader["ExpiryDate"] : null
                    });
                }
            }
            return View(urls);
        }

        public ActionResult Edit(int id)
        {
            UrlModel model = new UrlModel();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT * FROM UrlMappings WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    model.Id = id;
                    model.LongUrl = reader["LongUrl"].ToString();
                    model.ShortCode = reader["ShortCode"].ToString();
                    model.Password = reader["Password"]?.ToString();
                    model.ExpiryDate = reader["ExpiryDate"] != DBNull.Value ? (DateTime?)reader["ExpiryDate"] : null;
                }
            }
            return View(model);
        }

        [HttpPost]
        public ActionResult Edit(UrlModel model)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string update = "UPDATE UrlMappings SET LongUrl=@LongUrl, ShortCode=@ShortCode, Password=@Password, ExpiryDate=@ExpiryDate WHERE Id=@Id";
                SqlCommand cmd = new SqlCommand(update, conn);
                cmd.Parameters.AddWithValue("@Id", model.Id);
                cmd.Parameters.AddWithValue("@LongUrl", model.LongUrl);
                cmd.Parameters.AddWithValue("@ShortCode", model.ShortCode);
                cmd.Parameters.AddWithValue("@Password", (object?)model.Password ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ExpiryDate", (object?)model.ExpiryDate ?? DBNull.Value);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Dashboard");
        }

        public ActionResult Delete(int id)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string delete = "DELETE FROM UrlMappings WHERE Id=@Id";
                SqlCommand cmd = new SqlCommand(delete, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Dashboard");
        }

        private string GenerateShortCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var code = new char[6];
            for (int i = 0; i < code.Length; i++)
            {
                code[i] = chars[random.Next(chars.Length)];
            }
            return new string(code);
        }

       
    }
}