using System.ComponentModel.DataAnnotations;

namespace GastosHogarAPI.Models.DTOs
{
    public class CrearGastoRequest
    {
        public int UsuarioId { get; set; }
        public int GrupoId { get; set; }

        [Required]
        [Range(0.01, 999999.99)]
        public decimal Importe { get; set; }

        public int CategoriaId { get; set; }

        [Required]
        [StringLength(500)]
        public string Descripcion { get; set; } = string.Empty;

        public bool EsDetalle { get; set; } = false; // Cambio: era EsRegalo

        [StringLength(200)]
        public string? Tienda { get; set; }

        [StringLength(500)]
        public string? Notas { get; set; }

        public byte[]? FotoTicket { get; set; }
        public string? NombreFotoTicket { get; set; }

        public double? Latitud { get; set; }
        public double? Longitud { get; set; }
    }

    public class GastoResponse
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string? AliasUsuario { get; set; }
        public string ColorUsuario { get; set; } = string.Empty;
        public string? FotoUsuario { get; set; }

        public DateTime Fecha { get; set; }
        public decimal Importe { get; set; }
        public int CategoriaId { get; set; }
        public string NombreCategoria { get; set; } = string.Empty;
        public string EmojiCategoria { get; set; } = string.Empty;
        public string ColorCategoria { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;
        public bool EsDetalle { get; set; } // Cambio: era EsRegalo
        public string? Tienda { get; set; }
        public string? Notas { get; set; }

        public bool TieneFoto { get; set; }
        public string? UrlFoto { get; set; }

        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }

        public string TipoGastoTexto => EsDetalle ? "💝 Mi detalle" : "🤝 Compartido";
        public string TipoGastoDescripcion => EsDetalle ? "No se divide" : "Se divide entre todos";
    }
}