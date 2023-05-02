using Microsoft.AspNetCore.Mvc.Rendering;

namespace risk.control.system.Models.ViewModel
{
    public class VendorServiceViewModel
    {
        public long[] members { get; set; }

        public string selectedmembers { get; set; } //used to store the selected members, such as: "tom,johnn,david"  
        public SelectList memberList { get; set; } = Load_Members();

        public static SelectList Load_Members()
        {
            List<SelectListItem> list = new List<SelectListItem>()
            {
                new SelectListItem(){ Value="tom", Text="Tom"},
                 new SelectListItem(){ Value="jack", Text="Jack"},
                  new SelectListItem(){ Value="johnn", Text="Johnn"},
                   new SelectListItem(){ Value="vivian", Text="Vivian"},
                    new SelectListItem(){ Value="david", Text="David"},
            };


            return new SelectList(list, "Value", "Text");
        }
    }
}
