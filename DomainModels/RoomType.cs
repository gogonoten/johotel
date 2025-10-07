using System.ComponentModel.DataAnnotations;

namespace DomainModels
{
    public enum RoomType
    {
        [Display(Name = "Standardværelse")]
        Standard = 1,

        [Display(Name = "Familieværelse")]
        Family = 2,

        [Display(Name = "Suite")]
        Suite = 3
    }
}
