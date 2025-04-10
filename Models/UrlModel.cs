namespace URL_Shortener.Models
{
    public class UrlModel
    {
        public string LongUrl { get; set; }
        public string ShortCode { get; set; }
        public string CustomAlias { get; set; }
        public string Password { get; set; }
        public DateTime? ExpiryDate { get; set; }

    }
}
