using System.ComponentModel.DataAnnotations;

namespace Auth01.Models
{
    public class ProductViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Product Name")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; } // decrypted before passing

        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal Price { get; set; }

        [Display(Name = "Stock Quantity")]
        public int StockQuantity { get; set; }

        [Display(Name = "Created At")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Updated At")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateTime? UpdatedAt { get; set; }

        [Display(Name = "Created By")]
        public string CreatedByName { get; set; } = string.Empty;
    }
}
