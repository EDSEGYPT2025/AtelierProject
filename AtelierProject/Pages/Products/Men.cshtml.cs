using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace AtelierProject.Pages.Products
{
    public class MenModel : PageModel
    {
        public List<ProductViewModel> Products { get; set; } = new List<ProductViewModel>();

        public void OnGet()
        {
            // تعبئة بيانات وهمية للعرض
            Products = new List<ProductViewModel>
            {
                new ProductViewModel { Id = 1, Name = "بدلة سوداء سليم فيت", Code = "M-001", Category = "بدل كلاسيك", Price = 500, Stock = 10 },
                new ProductViewModel { Id = 2, Name = "بدلة عريس تركي", Code = "M-002", Category = "بدل زفاف", Price = 1200, Stock = 3 },
                new ProductViewModel { Id = 3, Name = "بيبيون ستان أحمر", Code = "A-101", Category = "اكسسوارات", Price = 50, Stock = 25 },
                new ProductViewModel { Id = 4, Name = "بدلة كحلي دبل برست", Code = "M-003", Category = "بدل كلاسيك", Price = 600, Stock = 1 },
                new ProductViewModel { Id = 5, Name = "قميص أبيض قطن", Code = "S-202", Category = "قمصان", Price = 150, Stock = 15 },
            };
        }
    }

    // كلاس بسيط لتمثيل البيانات في الجدول
    public class ProductViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
    }
}