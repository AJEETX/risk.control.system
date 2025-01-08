using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class UserSessionAlive : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long UserSessionAliveId { get; set; }
        public ApplicationUser ActiveUser { get; set; }
        public string CurrentPage { get; set; }
        public bool LoggedOut { get; set; }
    }
}
