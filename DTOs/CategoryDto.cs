namespace POS.Api.DTOs
{
    public class MainCategoryDto
    {
        public int MainCategoryId { get; set; }
        public string MainCategoryName { get; set; } = string.Empty;
        public string? MainCategoryDescription { get; set; }
    }

    public class SecondCategoryDto
    {
        public int SecondCategoryId { get; set; }
        public int MainCategoryId { get; set; }
        public string SecondCategoryName { get; set; } = string.Empty;
        public string? SecondCategoryDescription { get; set; }
        public string? MainCategoryName { get; set; }
    }

    public class ThirdCategoryDto
    {
        public int ThirdCategoryId { get; set; }
        public int SecondCategoryId { get; set; }
        public string ThirdCategoryName { get; set; } = string.Empty;
        public string? ThirdCategoryDescription { get; set; }
        public string? SecondCategoryName { get; set; }
    }

    public class VendorDto
    {
        public int VendorId { get; set; }
        public string? VendorTag { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public int ThirdCategoryId { get; set; }
        public string? VendorDescription { get; set; }
        public string VendorStatus { get; set; } = string.Empty;
        public DateTime RegisterDate { get; set; }
        public string? ThirdCategoryName { get; set; }
    }

    public class BrandDto
    {
        public int BrandId { get; set; }
        public string? BrandTag { get; set; }
        public string BrandName { get; set; } = string.Empty;
        public int VendorId { get; set; }
        public string? BrandDescription { get; set; }
        public string BrandStatus { get; set; } = string.Empty;
        public string? VendorName { get; set; }
    }

    // Create DTOs
    public class CreateMainCategoryDto
    {
        public string MainCategoryName { get; set; } = string.Empty;
        public string? MainCategoryDescription { get; set; }
    }

    public class CreateSecondCategoryDto
    {
        public int MainCategoryId { get; set; }
        public string SecondCategoryName { get; set; } = string.Empty;
        public string? SecondCategoryDescription { get; set; }
    }

    public class CreateThirdCategoryDto
    {
        public int SecondCategoryId { get; set; }
        public string ThirdCategoryName { get; set; } = string.Empty;
        public string? ThirdCategoryDescription { get; set; }
    }

    public class CreateVendorDto
    {
        public string? VendorTag { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public int ThirdCategoryId { get; set; }
        public string? VendorDescription { get; set; }
        public string VendorStatus { get; set; } = "YES";
    }

    public class CreateBrandDto
    {
        public string? BrandTag { get; set; }
        public string BrandName { get; set; } = string.Empty;
        public int VendorId { get; set; }
        public string? BrandDescription { get; set; }
        public string BrandStatus { get; set; } = "YES";
    }
}

