using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace GastosHogarAPI.Models
{
    public class Gasto
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }

        // NUEVO: Relación con grupo
        public int GrupoId { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        [Required]
        [Range(0.01, 999999.99)]
        public decimal Importe { get; set; }

        public int CategoriaId { get; set; }

        [Required]
        [StringLength(500)]
        public string Descripcion { get; set; } = string.Empty;

        // CAMBIO: EsRegalo → EsDetalle (nueva nomenclatura)
        public bool EsDetalle { get; set; } = false;

        public int Mes { get; set; }
        public int Año { get; set; }

        // NUEVO: Información de la foto del ticket
        public byte[]? FotoTicket { get; set; }
        public string? NombreFotoTicket { get; set; }
        public DateTime? FechaFoto { get; set; }
        public long? TamañoFoto { get; set; }

        // NUEVO: Información adicional
        [StringLength(200)]
        public string? Tienda { get; set; }

        [StringLength(500)]
        public string? Notas { get; set; }

        // NUEVO: Geolocalización opcional
        public double? Latitud { get; set; }
        public double? Longitud { get; set; }

        // NUEVO: Control de modificaciones
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaModificacion { get; set; }

        // Navigation properties (actualizadas)
        public Usuario? Usuario { get; set; }
        public Grupo? Grupo { get; set; }
        public Categoria? Categoria { get; set; }
    }
}