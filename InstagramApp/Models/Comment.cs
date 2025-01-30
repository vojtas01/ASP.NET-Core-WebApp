namespace InstagramApp.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public string Text { get; set; }

        public int PostId { get; set; }
        public virtual Post? Post { get; set; }

        public string? UserId { get; set; }
        public virtual User? User { get; set; }

        public virtual ICollection<User> CommentLikedByUsers { get; set; } = new List<User>();
    }
}