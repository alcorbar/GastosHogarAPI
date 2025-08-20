using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace GastosHogarAPI.Models
{
    public class Categoria
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(10)]
        public string Emoji { get; set; } = "📝";

        // NUEVO: Color para identificación visual
        [StringLength(7)]
        public string Color { get; set; } = "#757575";

        public bool EsPredeterminada { get; set; } = false;
        public bool Activa { get; set; } = true;

        // NUEVO: Para categorías personalizadas de grupos
        public int? GrupoId { get; set; }

        // NUEVO: Estadísticas
        public int VecesUsada { get; set; } = 0;
        public decimal TotalGastado { get; set; } = 0;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // NUEVO: Navigation
        public Grupo? Grupo { get; set; }
    }
}