using System.Security.Cryptography;

namespace SASI.Helpers
{
    public static class PasswordGenerator
    {
        private const string Minusculas = "abcdefghijklmnopqrstuvwxyz";
        private const string Mayusculas = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string Digitos = "0123456789";
        private const string Especiales = "!@#$%^&*()-_=+";

        public static string GenerarContrasenaTemporal(int longitud = 14)
        {
            if (longitud < 8) longitud = 8;

            var todos = Minusculas + Mayusculas + Digitos + Especiales;
            var bytes = RandomNumberGenerator.GetBytes(longitud);
            var caracteres = new char[longitud];

            caracteres[0] = Mayusculas[bytes[0] % Mayusculas.Length];
            caracteres[1] = Minusculas[bytes[1] % Minusculas.Length];
            caracteres[2] = Digitos[bytes[2] % Digitos.Length];
            caracteres[3] = Especiales[bytes[3] % Especiales.Length];

            for (int i = 4; i < longitud; i++)
            {
                caracteres[i] = todos[bytes[i] % todos.Length];
            }

            var mezcla = RandomNumberGenerator.GetBytes(longitud);
            for (int i = longitud - 1; i > 0; i--)
            {
                int j = mezcla[i] % (i + 1);
                (caracteres[i], caracteres[j]) = (caracteres[j], caracteres[i]);
            }

            return new string(caracteres);
        }
    }
}
