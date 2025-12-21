using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace AtelierFarida.Pages
{
    public class IndexModel : PageModel
    {
        public List<Branch> Branches { get; set; }

        public void OnGet()
        {
            // بيانات الفروع الحقيقية للأتيليه
            Branches = new List<Branch>
            {
                new Branch {
                    Name = "فرع الخصوص",
                    Phone = "0228852248",
                    WhatsApp = "201144642661",
                    Address = "بجوار قسم شرطة الخصوص الجديد",
                    LocationUrl = "https://www.google.com/maps/place/30%C2%B009'30.1%22N+31%C2%B018'58.4%22E/@30.15835,31.3184099,17z/data=!3m1!4b1!4m4!3m3!8m2!3d30.15835!4d31.3162212?q=30.158349990844727,31.316221237182617&z=17&entry=tts&g_ep=EgoyMDI0MTAwOS4wIPu8ASoASAFQAw%3D%3D"
                },
                new Branch {
                    Name = "فرع أحمد عرابى",
                    Phone = "0220990774",
                    WhatsApp = "201144642661",
                    Address = "137 شارع احمد عرابى - عين شمس الشرقيه بجوار مسجد حمزة",
                    LocationUrl = "https://www.google.com/maps/place/30%C2%B007'56.6%22N+31%C2%B020'53.7%22E/@30.1323948,31.3504458,17z/data=!3m1!4b1!4m4!3m3!8m2!3d30.1323948!4d31.3482571?q=30.132394790649414,31.348257064819336&z=17&entry=tts&g_ep=EgoyMDI0MTAwOS4wIPu8ASoASAFQAw%3D%3D"
                },
                new Branch {
                    Name = "فرع مصطفى حافظ",
                    Phone = "0221865252",
                    WhatsApp = "201144642661",
                    Address = "69 برج السعاده بجوار صيدلية بشري",
                    LocationUrl = "https://www.google.com/maps/place/30%C2%B007'43.2%22N+31%C2%B020'50.4%22E/@30.1286716,31.3495245,17z/data=!3m1!4b1!4m4!3m3!8m2!3d30.1286716!4d31.3473358?hl=ar&entry=ttu&g_ep=EgoyMDI1MTIwOS4wIKXMDSoASAFQAw%3D%3D"
                }
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