using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace risk.control.system.Models
{
    public class PermissionModule : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string PermissionModuleId { get; set; } = Guid.NewGuid().ToString();

        public string ModuleName { get; set; }

        public List<PermissionType> PermissionTypes { get; set; }

        public List<PermissionSubModule>? Sections { get; set; }
    }

    public class PermissionSubModule : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string PermissionSubModuleId { get; set; } = Guid.NewGuid().ToString();

        public string SubModuleName { get; set; }

        public List<PermissionType> PermissionTypes { get; set; }
    }

    public class PermissionType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string PermissionTypeId { get; set; } = Guid.NewGuid().ToString();

        public string Name { get; set; }
    }
}