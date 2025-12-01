namespace TenderAI.Domain.Entities;

/// <summary>
/// Kullanıcının envanter/ürün kataloğu - Teknik şartname ile eşleştirme için
/// </summary>
public class UserProduct
{
    public Guid Id { get; set; }

    /// <summary>
    /// Kullanıcı ID (Identity kullanıcısı)
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Ürün adı
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Ürün kategorisi
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Marka
    /// </summary>
    public string? Brand { get; set; }

    /// <summary>
    /// Model
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Teknik özellikler (JSON formatında)
    /// </summary>
    public string? TechnicalSpecifications { get; set; }

    /// <summary>
    /// Ürün açıklaması
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Birim maliyet (kullanıcının maliyet fiyatı)
    /// </summary>
    public decimal UnitCost { get; set; }

    /// <summary>
    /// Birim
    /// </summary>
    public string Unit { get; set; } = "Adet";

    /// <summary>
    /// Stokta var mı?
    /// </summary>
    public bool IsInStock { get; set; } = true;

    /// <summary>
    /// Temin süresi (gün)
    /// </summary>
    public int? LeadTimeDays { get; set; }

    /// <summary>
    /// Ürün eklenme tarihi
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Son güncellenme tarihi
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
