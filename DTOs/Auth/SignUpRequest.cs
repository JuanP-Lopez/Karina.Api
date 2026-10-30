using System.ComponentModel.DataAnnotations;

public class SignUpRequest
{

    [Required(ErrorMessage = "El codigo de registro es obligatorio.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número telefónico es obligatorio.")]
    [Phone(ErrorMessage = "El número telefónico no es válido.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string Password { get; set; } = string.Empty;
}