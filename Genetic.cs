using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngelDB
{
    using System;
    using System.Linq;

    public class Genetic
    {
        private const int PopulationSize = 100;
        private const double MutationRate = 0.01;
        private const int MaxGenerations = 1000;
        private static Random rand = new Random();

        public static string PredictString(string target)
        {
            var population = Enumerable.Range(0, PopulationSize)
                                       .Select(_ => RandomString(target.Length))
                                       .ToList();

            int generation = 0;

            while (generation < MaxGenerations)
            {
                var fitnessScores = population.Select(individual => Fitness(individual, target)).ToList();
                if (fitnessScores.Max() == target.Length)
                {
                    return population[fitnessScores.IndexOf(fitnessScores.Max())];
                }

                var newPopulation = new string[PopulationSize];

                for (int i = 0; i < PopulationSize; i++)
                {
                    var parent1 = SelectParent(population, fitnessScores);
                    var parent2 = SelectParent(population, fitnessScores);
                    var child = Crossover(parent1, parent2);
                    child = Mutate(child);
                    newPopulation[i] = child;
                }

                population = newPopulation.ToList();
                generation++;

                Console.WriteLine($"Generation {generation}, Best: {population[fitnessScores.IndexOf(fitnessScores.Max())]}");
            }

            return population.First();
        }

        private static int Fitness(string individual, string target)
        {
            return individual.Zip(target, (c1, c2) => c1 == c2 ? 1 : 0).Sum();
        }

        private static string RandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 ";
            return new string(Enumerable.Repeat(chars, length)
                                        .Select(s => s[rand.Next(s.Length)])
                                        .ToArray());
        }

        private static string SelectParent(List<string> population, List<int> fitnessScores)
        {
            int totalFitness = fitnessScores.Sum();
            int randomFitness = rand.Next(totalFitness);
            int accumulatedFitness = 0;

            for (int i = 0; i < PopulationSize; i++)
            {
                accumulatedFitness += fitnessScores[i];
                if (accumulatedFitness > randomFitness)
                {
                    return population[i];
                }
            }

            return population.Last();
        }

        private static string Crossover(string parent1, string parent2)
        {
            int crossPoint = rand.Next(parent1.Length);
            return parent1.Substring(0, crossPoint) + parent2.Substring(crossPoint);
        }

        private static string Mutate(string individual)
        {
            return new string(individual.Select(c => rand.NextDouble() < MutationRate ? (char)rand.Next(32, 127) : c).ToArray());
        }

    }
}
