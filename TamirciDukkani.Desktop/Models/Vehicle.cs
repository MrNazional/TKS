using System;

namespace TamirciDukkani.Desktop.Models
{
    public class Vehicle
    {
        public int Id { get; set; }
        public string Plaka { get; set; } = string.Empty;
        public string Marka { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int? ModelYili { get; set; }
        public string? SasiNo { get; set; }
        public string MusteriAdi { get; set; } = string.Empty;
        public string? MusteriTel { get; set; }
        public string? Notlar { get; set; }
    }
}
