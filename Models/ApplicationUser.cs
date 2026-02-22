using Microsoft.AspNetCore.Identity;

namespace VetRandevu.Api.Models;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
}
