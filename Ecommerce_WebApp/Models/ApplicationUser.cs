using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

public class ApplicationUser : IdentityUser
{

    [Required] 
    [StringLength(100)]
    public string FullName { get; set; }

    [DataType(DataType.Date)] 
    public DateTime? DateOfBirth { get; set; }
}