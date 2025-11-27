namespace POS.Api.DTOs
{
    public class ProductDto
    {
        public int ProductId { get; set; }
        public string? ProductIdTag { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int? BrandId { get; set; }  // Made nullable
        public string? ProductDescription { get; set; }
        public int ProductQuantityPerUnit { get; set; }
        public decimal ProductPerUnitPrice { get; set; }
        public decimal ProductMSRP { get; set; }
        public string ProductStatus { get; set; } = string.Empty;
        public decimal ProductDiscountRate { get; set; }
        public string? ProductSize { get; set; }
        public string? ProductColor { get; set; }
        public decimal ProductWeight { get; set; }
        public int ProductUnitStock { get; set; }
        public int StockThreshold { get; set; }
        public string? BrandName { get; set; }
        public string? ProductImageBase64 { get; set; }
    }

    public class CreateProductDto
    {
        public string? ProductIdTag { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int? BrandId { get; set; }  // Made nullable
        public string? ProductDescription { get; set; }
        public int ProductQuantityPerUnit { get; set; }
        public decimal ProductPerUnitPrice { get; set; }
        public decimal ProductMSRP { get; set; }
        public string ProductStatus { get; set; } = "YES";
        public decimal ProductDiscountRate { get; set; }
        public string? ProductSize { get; set; }
        public string? ProductColor { get; set; }
        public decimal ProductWeight { get; set; }
        public int ProductUnitStock { get; set; }
        public int StockThreshold { get; set; } = 10;
        public string? ProductImageBase64 { get; set; }
    }

    public class UpdateProductDto
    {
        public string ProductName { get; set; } = string.Empty;
        public int? BrandId { get; set; }  // Made nullable
        public string? ProductDescription { get; set; }
        public int ProductQuantityPerUnit { get; set; }
        public decimal ProductPerUnitPrice { get; set; }
        public decimal ProductMSRP { get; set; }
        public string ProductStatus { get; set; } = string.Empty;
        public decimal ProductDiscountRate { get; set; }
        public string? ProductSize { get; set; }
        public string? ProductColor { get; set; }
        public decimal ProductWeight { get; set; }
        public int ProductUnitStock { get; set; }
        public int StockThreshold { get; set; }
        public string? ProductImageBase64 { get; set; }
    }
}

