using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;

namespace InstagramApp.Models
{
    public class User : IdentityUser
    {
        public string? Firstname { get; set; }
        public string? Lastname { get; set; }
        public uint Age { get; set; }

        public virtual ICollection<User> Following { get; set; } = new List<User>();
        public virtual ICollection<User> Followed { get; set; } = new List<User>();
        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<Message> SentMessages { get; set; } = new List<Message>();
        public virtual ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
    }
}
