using System.ComponentModel.DataAnnotations;

namespace CorpProcure.Models
{
    public class AuditTrail
    {
        public int Id { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Action { get; set; } = string.Empty;

        [Required]
        public string Module { get; set; } = string.Empty;

        public string? Details { get; set; }

        public string? IpAddress { get; set; }

        // Optional - link to a specific reservation
        public int? ReservationId { get; set; }
        public VehicleReservation? Reservation { get; set; }
    }
}
