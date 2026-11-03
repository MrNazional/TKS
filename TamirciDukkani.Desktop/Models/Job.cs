using System;

namespace TamirciDukkani.Desktop.Models
{
    public class Job
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public DateTime GelisTarihi { get; set; }
        public DateTime? CikisTarihi { get; set; }
        public string Durum { get; set; } = "Atölyede";
        public string Aciklama { get; set; } = string.Empty;
        public decimal? IscilikTutar { get; set; }
        public decimal? ParcaTutar { get; set; }
        public decimal? GenelTutar { get; set; }
        public string? OdemeDurumu { get; set; }
    }
}
