using Microsoft.AspNetCore.Identity;
namespace API.Entities
{
    public class AppUserRole: IdentityUserRole<int>
    {
        // This class is used to define the many-to-many
        // relationship between AppUser and AppRole.
        public AppUser User { get; set; }
        public AppRole Role { get; set; }
    }
}