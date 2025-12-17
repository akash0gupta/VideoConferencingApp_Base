using System.ComponentModel.DataAnnotations;

namespace VideoConferencingApp.Application.DTOs.UserDto
{
    public class DeleteAccountDto
    {
        [Required]
        public string Password { get; set; }

        [StringLength(500)]
        public string Reason { get; set; }

        [Required]
        public bool ConfirmDeletion { get; set; }
    }


}

