using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AtelierFarida.Pages
{
    public class IndexModel : PageModel
    {
        public List<Branch> Branches { get; set; }

        public void OnGet()
        {
            // بيانات الفروع الثلاثة
            Branches = new List<Branch>
            {
                new Branch { Name = "فرع القاهرة (مصر الجديدة)", Phone = "0123456789", WhatsApp = "20123456789", Address = "شارع الثورة، مصر الجديدة", LocationUrl = "#" },
                new Branch { Name = "فرع التجمع الخامس", Phone = "0111222333", WhatsApp = "20111222333", Address = "مول سيفنتي، التسعين الشمالي", LocationUrl = "#" },
                new Branch { Name = "فرع الإسكندرية", Phone = "0100999888", WhatsApp = "20100999888", Address = "طريق الجيش، ستانلي", LocationUrl = "#" }
            };
        }
    }

    public class Branch
    {
        public string Name { get; set; }
        public string Phone { get; set; }
        public string WhatsApp { get; set; }
        public string Address { get; set; }
        public string LocationUrl { get; set; }
    }
}