using System.ComponentModel.DataAnnotations;

namespace SASI.Models
{
    public class OlvidePasswordViewModel
    {
        [Required(ErrorMessage = "Ingrese su correo electrónico o nombre de usuario.")]
        [Display(Name = "Correo o usuario")]
        public string Identificador { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "El mensaje no debe superar los 500 caracteres.")]
        public string? Mensaje { get; set; }

        public string? ClientId { get; set; }
        public string? ReturnUrl { get; set; }
        public string? NombreSistema { get; set; }
    }
}
