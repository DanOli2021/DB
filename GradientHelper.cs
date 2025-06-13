using System;
using System.Drawing;

namespace AngelDB 
{
    public class GradientHelper
    {
        /// <summary>
        /// Genera un color intermedio basado en un porcentaje entre un color inicial y un color final.
        /// </summary>
        /// <param name="startColor">Color inicial (cuando el porcentaje es 100%).</param>
        /// <param name="endColor">Color final (cuando el porcentaje es 0%).</param>
        /// <param name="percentage">Porcentaje (entre 0 y 100).</param>
        /// <returns>El color interpolado.</returns>
        public static Color GetGradientColor(Color startColor, Color endColor, double percentage)
        {
            if (percentage < 0) percentage = 0;
            if (percentage > 100) percentage = 100;

            // Normalizar porcentaje entre 0.0 y 1.0
            double ratio = percentage / 100.0;

            // Interpolar valores de R, G y B
            int r = (int)(startColor.R * ratio + endColor.R * (1 - ratio));
            int g = (int)(startColor.G * ratio + endColor.G * (1 - ratio));
            int b = (int)(startColor.B * ratio + endColor.B * (1 - ratio));

            return Color.FromArgb(r, g, b);
        }
    }
}
