namespace URL_Shortener.Models
{
    public class UrlModel
    {
        public int Id { get; set; }
        public string LongUrl { get; set; }
        public string ShortCode { get; set; }
        public string CustomAlias { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string Password { get; set; }
        public int ClickCount { get; set; }

    }
}
