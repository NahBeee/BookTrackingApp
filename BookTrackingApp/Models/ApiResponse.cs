namespace BookTrackingApp.Models
{
    public class ApiResponse
    {
        public List<Book> Books { get; set; }
    }
    public class Book
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public List<string> Authors { get; set; }

        public ImageLinks ImageLinks { get; set; }
    }

    public class ImageLinks
    {
        public string SmallThumbnail { get; set; }
        public string Thumbnail { get; set; }
    }
    public class User
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

}
