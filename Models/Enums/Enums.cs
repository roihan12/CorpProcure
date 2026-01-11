namespace CorpProcure.Models.Enums;

/// <summary>
/// Role pengguna dalam sistem
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Staff biasa yang mengajukan permintaan pembelian
    /// </summary>
    Staff = 1,

    /// <summary>
    /// Manager divisi - Approval level 1 (Operasional)
    /// </summary>
    Manager = 2,

    /// <summary>
    /// Head of Finance/Procurement - Approval level 2 (Finansial)
    /// </summary>
    Finance = 3,

    /// <summary>
    /// Admin sistem
    /// </summary>
    Admin = 4,

    /// <summary>
    /// Admin procurement yang mengirim PO ke vendor
    /// </summary>
    Procurement = 5
}

/// <summary>
/// Status purchase request dalam workflow approval
/// </summary>
public enum RequestStatus
{
    /// <summary>
    /// Draft - Belum disubmit oleh requester
    /// </summary>
    Draft = 1,

    /// <summary>
    /// Menunggu approval dari Manager (Level 1)
    /// </summary>
    PendingManager = 2,

    /// <summary>
    /// Manager approved, menunggu approval Finance (Level 2)
    /// </summary>
    PendingFinance = 3,

    /// <summary>
    /// Fully approved - siap menjadi PO
    /// </summary>
    Approved = 4,

    /// <summary>
    /// Ditolak oleh approver (Manager atau Finance)
    /// </summary>
    Rejected = 5,

    /// <summary>
    /// Dibatalkan oleh requester
    /// </summary>
    Cancelled = 6
}

/// <summary>
/// Action yang dilakukan approver
/// </summary>
public enum ApprovalAction
{
    /// <summary>
    /// Menyetujui request
    /// </summary>
    Approved = 1,

    /// <summary>
    /// Menolak request
    /// </summary>
    Rejected = 2,

    /// <summary>
    /// Membatalkan approval yang sudah diberikan
    /// </summary>
    Cancelled = 3
}

/// <summary>
/// Tipe audit log untuk tracking perubahan
/// </summary>
public enum AuditLogType
{
    /// <summary>
    /// Record baru dibuat
    /// </summary>
    Create = 1,

    /// <summary>
    /// Record diupdate
    /// </summary>
    Update = 2,

    /// <summary>
    /// Record dihapus (soft delete)
    /// </summary>
    Delete = 3,

    /// <summary>
    /// Request disubmit
    /// </summary>
    Submit = 4,

    /// <summary>
    /// Request diapprove
    /// </summary>
    Approve = 5,

    /// <summary>
    /// Request direject
    /// </summary>
    Reject = 6,

    /// <summary>
    /// Request dibatalkan
    /// </summary>
    Cancel = 7,

    /// <summary>
    /// Budget direserve/digunakan
    /// </summary>
    BudgetReserve = 8,

    /// <summary>
    /// Budget dirilis kembali
    /// </summary>
    BudgetRelease = 9,

    /// <summary>
    /// User login berhasil
    /// </summary>
    Login = 10,

    /// <summary>
    /// User login gagal
    /// </summary>
    LoginFailed = 11,

    /// <summary>
    /// User logout
    /// </summary>
    Logout = 12
}

/// <summary>
/// Kategori vendor berdasarkan jenis layanan/produk
/// </summary>
public enum VendorCategory
{
    /// <summary>
    /// Vendor barang fisik
    /// </summary>
    Goods = 1,

    /// <summary>
    /// Vendor jasa/layanan
    /// </summary>
    Services = 2,

    /// <summary>
    /// Vendor barang dan jasa
    /// </summary>
    Both = 3
}

/// <summary>
/// Status vendor dalam sistem
/// </summary>
public enum VendorStatus
{
    /// <summary>
    /// Vendor baru, menunggu review/approval
    /// </summary>
    PendingReview = 0,

    /// <summary>
    /// Vendor aktif, dapat menerima PO
    /// </summary>
    Active = 1,

    /// <summary>
    /// Vendor tidak aktif sementara
    /// </summary>
    Inactive = 2,

    /// <summary>
    /// Vendor diblokir, tidak dapat menerima PO
    /// </summary>
    Blacklisted = 3
}

/// <summary>
/// Tipe syarat pembayaran vendor
/// </summary>
public enum PaymentTermType
{
    /// <summary>
    /// Pembayaran segera saat pengiriman
    /// </summary>
    Immediate = 0,

    /// <summary>
    /// Pembayaran dalam 15 hari
    /// </summary>
    Net15 = 15,

    /// <summary>
    /// Pembayaran dalam 30 hari (paling umum)
    /// </summary>
    Net30 = 30,

    /// <summary>
    /// Pembayaran dalam 45 hari
    /// </summary>
    Net45 = 45,

    /// <summary>
    /// Pembayaran dalam 60 hari
    /// </summary>
    Net60 = 60
}
