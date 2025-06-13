using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngelDB
{
    public class PasswordGenerator
    {
        public static string GenerateSecurePassword(int length = 16)
        {
            const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZÑ";
            const string lowerCase = "abcdefghijklmnopqrstuvwxyzñ";
            const string numbers = "0123456789";
            const string specialChars = "!@#$%^&*()-_=+[]{};:,.<>?";
            string allChars = upperCase + lowerCase + numbers + specialChars;

            if (length < 16) length = 16; // Asegura una longitud mínima

            StringBuilder password = new StringBuilder();
            Random random = new Random();

            // Añade al menos un carácter de cada tipo
            password.Append(upperCase[random.Next(upperCase.Length)]);
            password.Append(lowerCase[random.Next(lowerCase.Length)]);
            password.Append(numbers[random.Next(numbers.Length)]);
            password.Append(specialChars[random.Next(specialChars.Length)]);

            // Rellena el resto de la contraseña con caracteres aleatorios
            for (int i = 4; i < length; i++)
            {
                password.Append(allChars[random.Next(allChars.Length)]);
            }

            // Mezcla los caracteres para que no siga un patrón
            return ShuffleString(password.ToString());
        }

        private static string ShuffleString(string input)
        {
            char[] array = input.ToCharArray();
            Random random = new Random();
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
            return new string(array);
        }
    }
}
