using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Data;
using System.Linq;
using DocumentFormat.OpenXml.Spreadsheet;
using static AngelDB.StatisticsAnalisis;
using DocumentFormat.OpenXml.Drawing.Charts;
using Formatting = Newtonsoft.Json.Formatting;
using DataTable = System.Data.DataTable;
using MathNet.Numerics;
using MathNet.Numerics.Distributions;
using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using AngelDBTools;

namespace AngelDB
{

    public static class DBStatistics
    {

        public static class StatisticsCommands
        {
            public static Dictionary<string, string> Commands()
            {
                Dictionary<string, string> commands = new Dictionary<string, string>
            {
                { @"ANALYSIS", @"ANALYSIS#free;FROM#free;VARIABLE#freeoptional" },
                { @"ANALYSIS LIST", @"ANALYSIS LIST#free;SEPARATOR#freeoptional;VALUES#free" },

                { @"SHOW ANALYSIS", @"SHOW ANALYSIS#free" },
                { @"GET Z", @"GET Z#free" },
                { @"GET T", @"GET T#free;CONFIDENCE LEVEL#free;DEGREES OF FREEDOM#free" },
                { @"FREQUENCY TABLE OF", @"FREQUENCY TABLE OF#free;COLUMN#free;AS HTML#optional" },
                { @"CONFIDENCE INTERVAL CASE 1", @"CONFIDENCE INTERVAL CASE 1#free;SAMPLE MEAN#free;POPULATION STANDARD DEVIATION#free;SAMPLE SIZE#free;CONFIDENCE LEVEL#free" },
                { @"CONFIDENCE INTERVAL CASE 2", @"CONFIDENCE INTERVAL CASE 2#free;MEAN#free;STANDARD DEVIATION#free;SIZE#free;CONFIDENCE LEVEL#free" },
                { @"CONFIDENCE INTERVAL CASE 4", @"CONFIDENCE INTERVAL CASE 4#free;MEAN#free;STANDARD DEVIATION#free;SIZE#free;CONFIDENCE LEVEL#free" },
            };

                return commands;

            }

        }


        public static string StatisticsCommand(AngelDB.DB db, string command)
        {

            DbLanguage language = new DbLanguage(db);

            Dictionary<string, string> d = new Dictionary<string, string>();
            language.SetCommands(StatisticsCommands.Commands());
            d = language.Interpreter(command);

            if (d == null)
            {
                return language.errorString;
            }

            if (d.Count == 0)
            {
                return "Error: not command found " + command; ;
            }
            string commandkey = d.First().Key;

            try
            {
                switch (commandkey)
                {
                    case "analysis":
                        return Analysis(db, d);
                    case "analysis_list":
                        return AnalysisList(db, d);
                    case "show_analysis":
                        return ShowAnalysis(db, d);
                    case "get_z":
                        return ConfidenceInterval.GetZValueNormalDistribution(d["get_normal_distribution_z"]).ToString();
                    case "get_t":
                        return ConfidenceInterval.GetTStudentNormalDistribution(d["confidence_level"], d["degrees_of_freedom"]).ToString();
                    case "frequency_table_of":
                        return CreateFrequencyTable(d);
                    default:
                        return "Error: not command found " + command;
                }

            }
            catch (Exception e)
            {
                return "Error: " + e.Message;
            }
        }

        public static string Analysis(AngelDB.DB db, Dictionary<string, string> d)
        {

            try
            {
                StatisticsAnalisis st = new StatisticsAnalisis(db);

                if (d["from"].StartsWith("Error:"))
                {
                    return d["from"];
                }

                string result = st.Data(d["from"], d["variable"]);

                if (!result.StartsWith("Error:"))
                {

                    result = st.Analysis(d["variable"]);

                    if (db.statistics.ContainsKey(d["analysis"]))
                    {
                        db.statistics[d["analysis"]] = st;
                    }
                    else
                    {
                        db.statistics.Add(d["analysis"], st);
                    }
                }

                return result;

            }
            catch (Exception e)
            {
                return "Error: analysis: " + e.Message;
            }

        }

        public static string AnalysisList(AngelDB.DB db, Dictionary<string, string> d)
        {

            try
            {
                StatisticsAnalisis st = new StatisticsAnalisis(db);
                string result = "";
                result = st.DataList(d["values"], d["separator"]); ;



                if (!result.StartsWith("Error:"))
                {

                    result = st.Analysis();

                    if (db.statistics.ContainsKey(d["analysis_list"]))
                    {
                        db.statistics[d["analysis_list"]] = st;
                    }
                    else
                    {
                        db.statistics.Add(d["analysis_list"], st);
                    }
                }

                return result;

            }
            catch (Exception e)
            {
                return "Error: analysis: " + e.Message;
            }

        }

        public static string ShowAnalysis(AngelDB.DB db, Dictionary<string, string> d)
        {
            if (!db.statistics.ContainsKey(d["show_analysis"]))
            {
                return $"Error: Analysis does not exist {d["show_analisis"]}";
            }

            return JsonConvert.SerializeObject(db.statistics[d["show_analysis"]].description.Result, Formatting.Indented);

        }


        public static string CreateFrequencyTable(Dictionary<string, string> d)
        {

            DataTable dt = JsonConvert.DeserializeObject<DataTable>(d["frequency_table_of"]);

            if (d["column"] == "null") return $"Error: Column name not specified {d["column"]}";

            if (!dt.Columns.Contains(d["column"])) return $"Error: Column name not specified {d["column"]}";

            // Agrupa los datos por el valor de la columna y cuenta la frecuencia de cada valor
            var query = dt.AsEnumerable()
                .GroupBy(r => r.Field<object>(d["column"]))
                .Select(g => new { Value = g.Key, Frequency = g.Count() })
                .OrderByDescending(g => g.Frequency);

            // Crea un nuevo DataTable para la tabla de frecuencias
            DataTable freqTable = new DataTable("FrequencyTable");
            freqTable.Columns.Add(d["column"], typeof(object));
            freqTable.Columns.Add("Frequency", typeof(int));

            // Agrega los datos a la tabla de frecuencias
            foreach (var item in query)
            {
                freqTable.Rows.Add(item.Value, item.Frequency);
            }

            if (d["as_html"] == "true") 
            {
                return StringFunctions.ConvertJsonToHtmlTable(JsonConvert.SerializeObject(freqTable, Formatting.Indented));
            }

            return JsonConvert.SerializeObject(freqTable, Formatting.Indented);
        }


    }



    public static class ConfidenceInterval
    {
        // Get the critical Z value from the standard normal distribution for the given confidence level.
        public static double GetZValue(double confidenceLevel)
        {
            // Use the CumulativeDistribution function to get the critical Z value
            double zValue = Normal.InvCDF(0, 1, (1 + confidenceLevel) / 2);

            return zValue;
        }

        public static double GetTValue(double confidenceLevel, int degreesOfFreedom)
        {
            // Use the CumulativeDistribution function to get the critical Z value
            double tValue = StudentT.InvCDF(0, 1, degreesOfFreedom, (confidenceLevel + 1) / 2);

            return tValue;
        }

        public static double GetZValueNormalDistribution(string nivelConfianza)
        {
            double nivel = 0;
            double.TryParse(nivelConfianza, out nivel);
            nivel = nivel / 100;
            // Utilizar MathNet.Numerics para obtener el valor crítico Z
            double valorZ = Normal.InvCDF(0, 1, (1 + nivel) / 2); ;
            return valorZ;
        }

        public static double GetTStudentNormalDistribution(string confidenceLevel, string degreesOfFreedom)
        {
            double level = 0;
            double.TryParse(confidenceLevel, out level);
            level = level / 100;

            int degrees = 0;
            int.TryParse(degreesOfFreedom, out degrees);

            // Use MathNet.Numerics to get the critical Z value
            double tValue = StudentT.InvCDF(0, 1, degrees, (level + 1) / 2);
            return tValue;
        }

    }


    public class StatisticsAnalisis
    {

        public Descriptive description = null;
        private List<double> numeric_data = new List<double>();
        public AngelDB.DB db;
        public string JSonData = "";

        public StatisticsAnalisis(AngelDB.DB db)
        {
            this.db = db;
        }

        public string Data(string jSonData, string AnalyzeColumn)
        {
            try
            {
                DataTable data = JsonConvert.DeserializeObject<DataTable>(jSonData);
                this.JSonData = jSonData;

                foreach (DataRow item in data.Rows)
                {

                    if (AnalyzeColumn != "null")
                    {
                        if (item[AnalyzeColumn] is DBNull)
                        {
                            continue;
                        }

                        numeric_data.Add(Convert.ToDouble(item[AnalyzeColumn]));
                    }
                    else
                    {
                        if (item[0] is DBNull)
                        {
                            continue;
                        }

                        numeric_data.Add(Convert.ToDouble(item[0]));
                    }
                }

                return "Ok.";
            }
            catch (Exception e)
            {
                return $"Error: Data: {e.ToString()}";
            }
        }

        public string DataList(string List, string separator)
        {
            try
            {

                if (separator == "null")
                {
                    separator = ",";
                }

                string[] separadores = new string[] { separator, "\n" };
                string[] data = List.Split(separadores, StringSplitOptions.RemoveEmptyEntries);

                foreach (string item in data)
                {
                    numeric_data.Add(Convert.ToDouble(item));
                }

                return "Ok.";
            }
            catch (Exception e)
            {
                return $"Error: DataList: {e.ToString()}";
            }
        }


        public string Analysis(string variable_name = "")
        {
            try
            {
                description = new Descriptive(this.numeric_data.ToArray());
                description.Result.VariableName = variable_name;
                description.Analyze();

                return JsonConvert.SerializeObject(description.Result, Formatting.Indented);
            }
            catch (Exception e)
            {
                return $"Error: Analisys: {e.ToString()}";
            }
        }


        /// <summary>
        /// The result class the holds the analysis results
        /// </summary>
        public class Statistics
        {
            // sortedData is used to calculate percentiles
            internal double[] sortedData;

            /// <summary>
            /// DescriptiveResult default constructor
            /// </summary>
            public Statistics() { }

            public string VariableName = "";
            public uint Count;
            public double Sum;
            public double Mean;
            public double Mode;
            public double Min;
            public double Max;
            public double Variance;
            public double StdDev;
            public double Range;
            public double GeometricMean;
            public double HarmonicMean;
            public double Skewness;
            public double Kurtosis;
            public double IQR;
            public double Median;
            public double FirstQuartile;
            public double ThirdQuartile;
            internal double SumOfError;
            internal double SumOfErrorSquare;

            public double Percentile(double percent)
            {
                return Descriptive.percentile(sortedData, percent);
            }
        }


        /// <summary>
        /// Descriptive class
        /// </summary>
        public class Descriptive
        {
            private double[] data;
            private double[] sortedData;

            /// <summary>
            /// Descriptive results
            /// </summary>
            public Statistics Result = new Statistics();

            #region Constructors
            /// <summary>
            /// Descriptive analysis default constructor
            /// </summary>
            public Descriptive() { } // default empty constructor

            /// <summary>
            /// Descriptive analysis constructor
            /// </summary>
            /// <param name="dataVariable">Data array</param>
            public Descriptive(double[] dataVariable)
            {
                data = dataVariable;
            }
            #endregion //  Constructors

            /// <summary>
            /// Run the analysis to obtain descriptive information of the data
            /// </summary>
            public void Analyze()
            {

                // initializations
                Result.Count = 0;
                Result.Min = Result.Max = Result.Range = Result.Mean =
                Result.Sum = Result.StdDev = Result.Variance = 0.0d;

                double sumOfSquare = 0.0d;
                double sumOfESquare = 0.0d; // must initialize

                double[] squares = new double[data.Length];
                double cumProduct = 1.0d; // to calculate geometric mean
                double cumReciprocal = 0.0d; // to calculate harmonic mean

                // First iteration
                for (int i = 0; i < data.Length; i++)
                {
                    if (i == 0) // first data point
                    {
                        Result.Min = data[i];
                        Result.Max = data[i];
                        Result.Mean = data[i];
                        Result.Range = 0.0d;
                    }
                    else
                    { // not the first data point
                        if (data[i] < Result.Min) Result.Min = data[i];
                        if (data[i] > Result.Max) Result.Max = data[i];
                    }
                    Result.Sum += data[i];
                    squares[i] = Math.Pow(data[i], 2); //TODO: may not be necessary
                    sumOfSquare += squares[i];

                    cumProduct *= data[i];
                    cumReciprocal += 1.0d / data[i];
                }



                Result.Count = (uint)data.Length;
                double n = (double)Result.Count; // use a shorter variable in double type
                Result.Mean = Result.Sum / n;
                Result.GeometricMean = Math.Pow(cumProduct, 1.0 / n);
                Result.HarmonicMean = 1.0d / (cumReciprocal / n); // see http://mathworld.wolfram.com/HarmonicMean.html
                Result.Range = Result.Max - Result.Min;

                // second loop, calculate Stdev, sum of errors
                //double[] eSquares = new double[data.Length];
                double m1 = 0.0d;
                double m2 = 0.0d;
                double m3 = 0.0d; // for skewness calculation
                double m4 = 0.0d; // for kurtosis calculation
                                  // for skewness
                for (int i = 0; i < data.Length; i++)
                {
                    double m = data[i] - Result.Mean;
                    double mPow2 = m * m;
                    double mPow3 = mPow2 * m;
                    double mPow4 = mPow3 * m;

                    m1 += Math.Abs(m);

                    m2 += mPow2;

                    // calculate skewness
                    m3 += mPow3;

                    // calculate skewness
                    m4 += mPow4;

                }

                Result.SumOfError = m1;
                Result.SumOfErrorSquare = m2; // Added for Excel function DEVSQ
                sumOfESquare = m2;

                // var and standard deviation
                Result.Variance = sumOfESquare / ((double)Result.Count - 1);
                Result.StdDev = Math.Sqrt(Result.Variance);

                // using Excel approach
                double skewCum = 0.0d; // the cum part of SKEW formula
                for (int i = 0; i < data.Length; i++)
                {
                    skewCum += Math.Pow((data[i] - Result.Mean) / Result.StdDev, 3);
                }
                Result.Skewness = n / (n - 1) / (n - 2) * skewCum;

                // kurtosis: see http://en.wikipedia.org/wiki/Kurtosis (heading: Sample Kurtosis)
                double m2_2 = Math.Pow(sumOfESquare, 2);
                Result.Kurtosis = ((n + 1) * n * (n - 1)) / ((n - 2) * (n - 3)) *
                    (m4 / m2_2) -
                    3 * Math.Pow(n - 1, 2) / ((n - 2) * (n - 3)); // second last formula for G2

                // calculate quartiles
                sortedData = new double[data.Length];
                data.CopyTo(sortedData, 0);
                Array.Sort(sortedData);

                // copy the sorted data to result object so that
                // user can calculate percentile easily
                Result.sortedData = new double[data.Length];
                sortedData.CopyTo(Result.sortedData, 0);

                Result.FirstQuartile = percentile(sortedData, 25);
                Result.ThirdQuartile = percentile(sortedData, 75);
                Result.Median = percentile(sortedData, 50);
                Result.IQR = percentile(sortedData, 75) -
                percentile(sortedData, 25);

                // Calcular la moda
                var frequencyTable = new Dictionary<double, int>();
                foreach (var value in data)
                {
                    if (frequencyTable.ContainsKey(value))
                    {
                        frequencyTable[value]++;
                    }
                    else
                    {
                        frequencyTable[value] = 1;
                    }
                }

                double mode = data[0];
                int maxCount = 0;
                foreach (var pair in frequencyTable)
                {
                    if (pair.Value > maxCount)
                    {
                        mode = pair.Key;
                        maxCount = pair.Value;
                    }
                }

                Result.Mode = mode;

            } // end of method Analyze


            /// <summary>
            /// Calculate percentile of a sorted data set
            /// </summary>
            /// <param name="sortedData"></param>
            /// <param name="p"></param>
            /// <returns></returns>
            internal static double percentile(double[] sortedData, double p)
            {
                // algo derived from Aczel pg 15 bottom
                if (p >= 100.0d) return sortedData[sortedData.Length - 1];

                double position = (double)(sortedData.Length + 1) * p / 100.0;
                double leftNumber = 0.0d, rightNumber = 0.0d;

                double n = p / 100.0d * (sortedData.Length - 1) + 1.0d;

                if (position >= 1)
                {
                    leftNumber = sortedData[(int)System.Math.Floor(n) - 1];
                    rightNumber = sortedData[(int)System.Math.Floor(n)];
                }
                else
                {
                    leftNumber = sortedData[0]; // first data
                    rightNumber = sortedData[1]; // first data
                }

                if (leftNumber == rightNumber)
                    return leftNumber;
                else
                {
                    double part = n - System.Math.Floor(n);
                    return leftNumber + part * (rightNumber - leftNumber);
                }
            } // end of internal function percentile

        } // end of class Descriptive        

    }

    public class Bayes
    {

        public Dictionary<string, BayesNode> nodes = new Dictionary<string, BayesNode>();
        public decimal TotalProbability { get; set; }
        public string problem;

        public Bayes(string problem)
        {
            this.problem = problem;
        }

        public string AddBayesNode(string NodeName, double probality, double probability_dependency)
        {

            if (probality >= 1)
            {
                return "Error: The value of probality must always be less than 1.";
            }

            if (probability_dependency >= 1)
            {
                return "Error: The value of probability_dependency must always be less than 1.";
            }

            if (string.IsNullOrEmpty(NodeName))
            {
                return "Error: The NodeNameA parameter must not be an empty string";
            }

            BayesNode node = new BayesNode();

            node.NodeName = NodeName;
            node.Probality = probality;
            node.ProbalityDependency = probability_dependency;

            if (!nodes.ContainsKey(NodeName))
            {
                this.nodes.Add(NodeName, node);
            }
            else
            {
                this.nodes[NodeName] = node;
            }

            return "Ok.";
        }


        public double CalculationTotalProbability()
        {

            double total = 0;

            foreach (string key in nodes.Keys)
            {
                total += nodes[key].Probality * nodes[key].ProbalityDependency;
            }

            return total;
        }


        public string CalculationTotalProbabilityDependency()
        {

            double total = CalculationTotalProbability();
            Dictionary<string, object> data = new Dictionary<string, object>();

            foreach (string key in this.nodes.Keys)
            {
                double dependency = (nodes[key].Probality * nodes[key].ProbalityDependency) / total;
                nodes[key].ProbalityResult = dependency;
            }

            return "Ok.";

        }

    }


    public class BayesNode
    {
        public string NodeName { get; set; }
        public double Probality { get; set; }
        public double ProbalityDependency { get; set; }
        public double ProbalityResult { get; set; }
    }


    public static class ChiSquareDistribution
    {
        // Función para calcular la densidad de probabilidad de la distribución chi cuadrado
        public static double ChiSquareDensity(double x, int degreesOfFreedom)
        {
            // Validamos que los grados de libertad sean al menos 1
            if (degreesOfFreedom <= 0)
            {
                throw new ArgumentException("Los grados de libertad deben ser al menos 1.", nameof(degreesOfFreedom));
            }

            // Calculamos la constante (1 / (2^(k/2) * Γ(k/2)))
            double constant = 1 / (Math.Pow(2, degreesOfFreedom / 2.0) * SpecialGammaFunction(degreesOfFreedom / 2.0));

            // Calculamos la parte de la exponencial (e^(-x/2))
            double expPart = Math.Exp(-x / 2.0);

            // Calculamos la parte del término con x (x^(k/2-1))
            double xPart = Math.Pow(x, degreesOfFreedom / 2.0 - 1);

            // Calculamos la densidad de probabilidad completa
            double density = constant * xPart * expPart;

            return density;
        }

        // Función especial para el cálculo de la función gamma (Γ)
        // En C#, podemos utilizar la función Gamma de la clase Math, pero aquí se muestra cómo sería implementarla desde cero.
        private static double SpecialGammaFunction(double x)
        {
            if (x == 1)
                return 1;

            return (x - 1) * SpecialGammaFunction(x - 1);
        }
    }
}




