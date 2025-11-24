namespace POS.Api.DTOs
{
    public class ShopConfigurationDto
    {
        public int Id { get; set; }
        public string ShopName { get; set; } = string.Empty;
        public string? ShopAddress { get; set; }
        public string? ShopPhone { get; set; }
        public string? ShopEmail { get; set; }
        public string? ShopWebsite { get; set; }
        public string? TaxId { get; set; }
        public string? FooterMessage { get; set; }
        public string? HeaderMessage { get; set; }
        public string? LogoBase64 { get; set; }
    }

    public class UpdateShopConfigurationDto
    {
        public string ShopName { get; set; } = string.Empty;
        public string? ShopAddress { get; set; }
        public string? ShopPhone { get; set; }
        public string? ShopEmail { get; set; }
        public string? ShopWebsite { get; set; }
        public string? TaxId { get; set; }
        public string? FooterMessage { get; set; }
        public string? HeaderMessage { get; set; }
        public string? LogoBase64 { get; set; }
    }
}

