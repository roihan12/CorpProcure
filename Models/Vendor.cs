using CorpProcure.Models.Base;
using CorpProcure.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CorpProcure.Models;

/// <summary>
/// Model Vendor - Supplier/rekanan untuk pengadaan barang/jasa
/// </summary>
public class Vendor : BaseEntity
{
    #region Basic Information

    /// <summary>
    /// Kode vendor (auto-generated, e.g., "VND-0001")
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Nama perusahaan/vendor
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Kategori vendor (Goods/Services/Both)
    /// </summary>
    [Required]
    public VendorCategory Category { get; set; } = VendorCategory.Goods;

    /// <summary>
    /// Deskripsi bisnis vendor
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    #endregion

    #region Contact Information

    /// <summary>
    /// Alamat lengkap
    /// </summary>
    [MaxLength(500)]
    public string? Address { get; set; }

    /// <summary>
    /// Kota
    /// </summary>
    [MaxLength(100)]
    public string? City { get; set; }

    /// <summary>
    /// Provinsi
    /// </summary>
    [MaxLength(100)]
    public string? Province { get; set; }

    /// <summary>
    /// Kode pos
    /// </summary>
    [MaxLength(10)]
    public string? PostalCode { get; set; }

    /// <summary>
    /// Nama kontak utama
    /// </summary>
    [MaxLength(100)]
    public string? ContactPerson { get; set; }

    /// <summary>
    /// Jabatan kontak
    /// </summary>
    [MaxLength(100)]
    public string? ContactTitle { get; set; }

    /// <summary>
    /// Nomor telepon kantor
    /// </summary>
    [MaxLength(20)]
    public string? Phone { get; set; }

    /// <summary>
    /// Nomor HP/WhatsApp
    /// </summary>
    [MaxLength(20)]
    public string? Mobile { get; set; }

    /// <summary>
    /// Email utama
    /// </summary>
    [MaxLength(100)]
    [EmailAddress]
    public string? Email { get; set; }

    /// <summary>
    /// Website perusahaan
    /// </summary>
    [MaxLength(200)]
    public string? Website { get; set; }

    #endregion

    #region Legal & Tax Information

    /// <summary>
    /// NPWP (15 digit)
    /// </summary>
    [MaxLength(20)]
    public string? TaxId { get; set; }

    /// <summary>
    /// Nomor SIUP/NIB
    /// </summary>
    [MaxLength(50)]
    public string? BusinessLicense { get; set; }

    /// <summary>
    /// Tanggal berakhir izin usaha
    /// </summary>
    public DateTime? LicenseExpiryDate { get; set; }

    #endregion

    #region Banking Information

    /// <summary>
    /// Nama bank
    /// </summary>
    [MaxLength(100)]
    public string? BankName { get; set; }

    /// <summary>
    /// Cabang bank
    /// </summary>
    [MaxLength(100)]
    public string? BankBranch { get; set; }

    /// <summary>
    /// Nomor rekening
    /// </summary>
    [MaxLength(30)]
    public string? AccountNumber { get; set; }

    /// <summary>
    /// Nama pemilik rekening
    /// </summary>
    [MaxLength(150)]
    public string? AccountHolderName { get; set; }

    #endregion

    #region Payment Terms

    /// <summary>
    /// Syarat pembayaran
    /// </summary>
    [Required]
    public PaymentTermType PaymentTerms { get; set; } = PaymentTermType.Net30;

    /// <summary>
    /// Batas kredit maksimal
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? CreditLimit { get; set; }

    #endregion

    #region Performance & Status

    /// <summary>
    /// Rating vendor (1-5 bintang)
    /// </summary>
    [Range(1, 5)]
    public int Rating { get; set; } = 3;

    /// <summary>
    /// Total jumlah order (auto-calculated)
    /// </summary>
    public int TotalOrders { get; set; } = 0;

    /// <summary>
    /// Total nilai order (auto-calculated)
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalOrderValue { get; set; } = 0;

    /// <summary>
    /// Status vendor
    /// </summary>
    [Required]
    public VendorStatus Status { get; set; } = VendorStatus.Active;

    /// <summary>
    /// Alasan status (terutama untuk blacklist)
    /// </summary>
    [MaxLength(500)]
    public string? StatusReason { get; set; }

    /// <summary>
    /// Tanggal status terakhir diubah
    /// </summary>
    public DateTime? StatusChangedAt { get; set; }

    #endregion

    #region Contract Information

    /// <summary>
    /// Tanggal mulai kontrak
    /// </summary>
    public DateTime? ContractStartDate { get; set; }

    /// <summary>
    /// Tanggal berakhir kontrak
    /// </summary>
    public DateTime? ContractEndDate { get; set; }

    /// <summary>
    /// Catatan internal
    /// </summary>
    [MaxLength(2000)]
    public string? Notes { get; set; }

    #endregion

    #region Navigation Properties

    /// <summary>
    /// Collection purchase request yang terkait dengan vendor ini
    /// </summary>
    public ICollection<PurchaseRequest> PurchaseRequests { get; set; } = new List<PurchaseRequest>();

    #endregion

    #region Helper Methods

    /// <summary>
    /// Cek apakah vendor dapat menerima PO
    /// </summary>
    public bool CanReceivePO()
    {
        return Status == VendorStatus.Active;
    }

    /// <summary>
    /// Update status vendor
    /// </summary>
    public void UpdateStatus(VendorStatus newStatus, string? reason = null)
    {
        Status = newStatus;
        StatusReason = reason;
        StatusChangedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Blacklist vendor
    /// </summary>
    public void Blacklist(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Alasan blacklist harus diisi", nameof(reason));

        UpdateStatus(VendorStatus.Blacklisted, reason);
    }

    /// <summary>
    /// Aktivasi ulang vendor
    /// </summary>
    public void Activate()
    {
        UpdateStatus(VendorStatus.Active, null);
    }

    #endregion
}
