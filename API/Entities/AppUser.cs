using Microsoft.AspNetCore.Identity;
namespace API.Entities
{
    public class AppUser: IdentityUser<int>
    {
        public DateTime DateOfBirth { get; set; }
        public string KnownAs { get; set; }
        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime LastActive { get; set; } = DateTime.Now;
        public string Gender { get; set; }
        public string Introduction { get; set; }
        public string LookingFor { get; set; }
        public string Interests { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public ICollection<Photo> Photos { get; set; }

        // Users that like this user
        public ICollection<UserLike> LikedByUsers { get; set; }

        // Users that this user likes
        public ICollection<UserLike> LikedUsers { get; set; }

        public ICollection<Message> MessagesSent { get; set; }
        public ICollection<Message> MessagesReceived { get; set; }

        // Property defining the many-to-many relationship between AppUser and AppRole
        public ICollection<AppUserRole> UserRoles { get; set; }
    }
}