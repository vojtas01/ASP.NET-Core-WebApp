using System.ComponentModel.DataAnnotations.Schema;

namespace InstagramApp.Models
{
    public class Post
    {
        public int Id { get; set; }
        public string? Text { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string? UserId { get; set; }
        public virtual User? User { get; set; }

        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<User> PostLikedByUsers { get; set; } = new List<User>();

        [NotMapped]
        public IFormFile? ImageFile { get; set; }
        public string ImagePath { get; set; }
	}
}