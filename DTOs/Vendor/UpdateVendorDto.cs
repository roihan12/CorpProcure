using CorpProcure.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace CorpProcure.DTOs.Vendor;

/// <summary>
/// DTO untuk mengupdate vendor
/// </summary>
public class UpdateVendorDto
{
    [Required]
    public Guid Id { get; set; }

    #region Basic Information

    [Required(ErrorMessage = "Nama vendor wajib diisi")]
    [MaxLength(200, ErrorMessage = "Nama maksimal 200 karakter")]
    [Display(Name = "Nama Vendor")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kategori wajib dipilih")]
    [Display(Name = "Kategori")]
    public VendorCategory Category { get; set; }

    [MaxLength(1000, ErrorMessage = "Deskripsi maksimal 1000 karakter")]
    [Display(Name = "Deskripsi")]
    public string? Description { get; set; }

    #endregion

    #region Contact Information

    [MaxLength(500, ErrorMessage = "Alamat maksimal 500 karakter")]
    [Display(Name = "Alamat")]
    public string? Address { get; set; }

    [MaxLength(100, ErrorMessage = "Kota maksimal 100 karakter")]
    [Display(Name = "Kota")]
    public string? City { get; set; }

    [MaxLength(100, ErrorMessage = "Provinsi maksimal 100 karakter")]
    [Display(Name = "Provinsi")]
    public string? Province { get; set; }

    [MaxLength(10, ErrorMessage = "Kode pos maksimal 10 karakter")]
    [Display(Name = "Kode Pos")]
    public string? PostalCode { get; set; }

    [MaxLength(100, ErrorMessage = "Nama kontak maksimal 100 karakter")]
    [Display(Name = "Nama Kontak")]
    public string? ContactPerson { get; set; }

    [MaxLength(100, ErrorMessage = "Jabatan kontak maksimal 100 karakter")]
    [Display(Name = "Jabatan Kontak")]
    public string? ContactTitle { get; set; }

    [MaxLength(20, ErrorMessage = "Nomor telepon maksimal 20 karakter")]
    [Display(Name = "Telepon")]
    public string? Phone { get; set; }

    [MaxLength(20, ErrorMessage = "Nomor HP maksimal 20 karakter")]
    [Display(Name = "HP/WhatsApp")]
    public string? Mobile { get; set; }

    [MaxLength(100, ErrorMessage = "Email maksimal 100 karakter")]
    [EmailAddress(ErrorMessage = "Format email tidak valid")]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [MaxLength(200, ErrorMessage = "Website maksimal 200 karakter")]
    [Display(Name = "Website")]
    public string? Website { get; set; }

    #endregion

    #region Legal & Tax Information

    [MaxLength(20, ErrorMessage = "NPWP maksimal 20 karakter")]
    [Display(Name = "NPWP")]
    public string? TaxId { get; set; }

    [MaxLength(50, ErrorMessage = "SIUP/NIB maksimal 50 karakter")]
    [Display(Name = "SIUP/NIB")]
    public string? BusinessLicense { get; set; }

    [Display(Name = "Masa Berlaku Izin")]
    [DataType(DataType.Date)]
    public DateTime? LicenseExpiryDate { get; set; }

    #endregion

    #region Banking Information

    [MaxLength(100, ErrorMessage = "Nama bank maksimal 100 karakter")]
    [Display(Name = "Nama Bank")]
    public string? BankName { get; set; }

    [MaxLength(100, ErrorMessage = "Cabang bank maksimal 100 karakter")]
    [Display(Name = "Cabang")]
    public string? BankBranch { get; set; }

    [MaxLength(30, ErrorMessage = "Nomor rekening maksimal 30 karakter")]
    [Display(Name = "Nomor Rekening")]
    public string? AccountNumber { get; set; }

    [MaxLength(150, ErrorMessage = "Nama rekening maksimal 150 karakter")]
    [Display(Name = "Atas Nama")]
    public string? AccountHolderName { get; set; }

    #endregion

    #region Payment Terms

    [Required(ErrorMessage = "Syarat pembayaran wajib dipilih")]
    [Display(Name = "Syarat Pembayaran")]
    public PaymentTermType PaymentTerms { get; set; }

    [Display(Name = "Batas Kredit")]
    [Range(0, double.MaxValue, ErrorMessage = "Batas kredit harus positif")]
    public decimal? CreditLimit { get; set; }

    #endregion

    #region Status

    [Range(1, 5, ErrorMessage = "Rating harus 1-5")]
    [Display(Name = "Rating")]
    public int Rating { get; set; }

    [Display(Name = "Status")]
    public VendorStatus Status { get; set; }

    #endregion

    #region Contract Information

    [Display(Name = "Tanggal Mulai Kontrak")]
    [DataType(DataType.Date)]
    public DateTime? ContractStartDate { get; set; }

    [Display(Name = "Tanggal Akhir Kontrak")]
    [DataType(DataType.Date)]
    public DateTime? ContractEndDate { get; set; }

    [MaxLength(2000, ErrorMessage = "Catatan maksimal 2000 karakter")]
    [Display(Name = "Catatan")]
    public string? Notes { get; set; }

    #endregion
}
