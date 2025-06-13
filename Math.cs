using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngelDB
{
    public static class Statistics
    {
        /// <summary>
        /// Fits a line to a collection of (x,y) points.
        /// </summary>
        /// <param name="xVals">The x-axis values.</param>
        /// <param name="yVals">The y-axis values.</param>
        /// <param name="rSquared">The r^2 value of the line.</param>
        /// <param name="yIntercept">The y-intercept value of the line (i.e. y = ax + b, yIntercept is b).</param>
        /// <param name="slope">The slop of the line (i.e. y = ax + b, slope is a).</param>
        public static void LinearRegression(
            double[] xVals,
            double[] yVals,
            out double rSquared,
            out double yIntercept,
            out double slope)
        {
            if (xVals.Length != yVals.Length)
            {
                throw new Exception("Input values should be with the same length.");
            }

            double sumOfX = 0;
            double sumOfY = 0;
            double sumOfXSq = 0;
            double sumOfYSq = 0;
            double sumCodeviates = 0;

            for (var i = 0; i < xVals.Length; i++)
            {
                var x = xVals[i];
                var y = yVals[i];
                sumCodeviates += x * y;
                sumOfX += x;
                sumOfY += y;
                sumOfXSq += x * x;
                sumOfYSq += y * y;
            }

            var count = xVals.Length;
            var ssX = sumOfXSq - ((sumOfX * sumOfX) / count);
            var ssY = sumOfYSq - ((sumOfY * sumOfY) / count);

            var rNumerator = (count * sumCodeviates) - (sumOfX * sumOfY);
            var rDenom = (count * sumOfXSq - (sumOfX * sumOfX)) * (count * sumOfYSq - (sumOfY * sumOfY));
            var sCo = sumCodeviates - ((sumOfX * sumOfY) / count);

            var meanX = sumOfX / count;
            var meanY = sumOfY / count;
            var dblR = rNumerator / Math.Sqrt(rDenom);

            rSquared = dblR * dblR;
            yIntercept = meanY - ((sCo / ssX) * meanX);
            slope = sCo / ssX;
        }
    }


    public class Algebra 
    {
        // Algoritmo para la formula general, para resolver ecuaciones de segundo grado
        // Entrada: a, b, c
        // Salida: x1, x2
        public string FormulaGeneral(double a, double b, double c)
        {
            double x1, x2;
            double discriminante = Math.Pow(b, 2) - 4 * a * c;
            if (discriminante < 0)
            {
                return "No hay soluciones reales";
            }
            else
            {
                x1 = (-b + Math.Sqrt(discriminante)) / (2 * a);
                x2 = (-b - Math.Sqrt(discriminante)) / (2 * a);
                return $"x1 = {x1}, x2 = {x2}";
            }
        }

    }


}
