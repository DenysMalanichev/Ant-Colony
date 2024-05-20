public class AntColonyOptimizator
{
    private readonly int numLocations;
    private readonly int numUnits;
    private readonly double minDist;
    private readonly double[,] costs;
    private readonly double[,] powers;
    private readonly double[,] locations;
    private readonly double evaporationRate;
    private readonly double alpha;  // Вплив феромону
    private readonly double budget;
    private readonly Random rand = new();

    private readonly double[,] precountedDists;

    public AntColonyOptimizator(
        double[,] locations,
        double[,] costs,
        double[,] powers,
        double totalBudget,
        double minDist,
        double evaporationRate = 0.05)
    {
        if(costs.GetLength(0) != powers.GetLength(0) || costs.GetLength(1) != powers.GetLength(1))
        {
            throw new ArgumentException("costs and powers arrays must be of the same size.");
        }

        numLocations = locations.GetLength(0);
        numUnits = costs.GetLength(1);
        this.costs = costs;
        this.powers = powers;
        this.locations = locations;
        this.minDist = minDist;
        this.evaporationRate = evaporationRate;
        budget = totalBudget;
        
        alpha = 1;

        precountedDists = new double[numLocations, numLocations];

        PrecountDists();
    }

    public (double power, double price) Optimize(int iterations, int ants)
    {
        var currentBestSolution = new int[numLocations, numUnits];
        var currentBestSolutionPower = double.MinValue;

        for(int i = 0; i < iterations; i++)
        {
            var pheromones = InitializePheromones();
            for(int a = 0; a < ants; a++)
            {                
                var aTemp = CreateSolution(pheromones);
                var currentPower = CalculateSolutionPower(aTemp);
                if(currentBestSolutionPower < currentPower)
                {
                    currentBestSolution = aTemp;
                    currentBestSolutionPower = currentPower;
                }

                UpdatePheromones(pheromones, aTemp);
            }
        }

        PrintBestSoluion(currentBestSolution, currentBestSolutionPower);

        var price = 0.0;
        for (int i = 0; i < numLocations; i++)
        {
            for (int j = 0; j < numUnits; j++)
            {
                if(currentBestSolution[i, j] == 1)
                    price += costs[i, j]; 
            }
        }

        return (currentBestSolutionPower, price);
    }

    private int[,] CreateSolution(double[,] pheromones)
    {
        var positions = new int[numLocations, numUnits];
        var currentPosibilities = CountInitialCumulativePosibility(pheromones);

        var currentPrice = 0.0;

        var badUnits = new HashSet<int>();
        var locIndexes = Enumerable.Range(0, numLocations).ToArray();
        Shuffle(locIndexes);
        foreach(var loc in locIndexes)
        {  
            double randNum;  
            GenerateRandomNumber: {
                randNum = rand.NextDouble();
            }
              
            if(badUnits.Count == currentPosibilities[loc].Count)
            {
                continue;
            }

            for(int unit = 0; unit < currentPosibilities[loc].Count; unit++)
            {                           
                if(unit == 0)
                {
                    if(0 <= randNum && randNum < currentPosibilities[loc][unit])
                    {
                        if(currentPrice + costs[loc, unit] <= budget && IsGoodDist(positions, loc) && IsUnitFree(positions, unit))
                        {
                            positions[loc, unit] = 1;
                            badUnits = new HashSet<int>();
                            currentPrice += costs[loc, unit];
                            break;
                        }

                        badUnits.Add(unit);
                        UpdatePosibilities(currentPosibilities, pheromones, loc, badUnits);
                        goto GenerateRandomNumber;  
                    }
                    else
                    {
                        continue;
                    }
                }
                

                if(unit == currentPosibilities[loc].Count - 1)
                {
                    if(randNum > currentPosibilities[loc][unit - 1] && randNum <= 1)
                    {
                        if(currentPrice + costs[loc, unit] <= budget && IsGoodDist(positions, loc) && IsUnitFree(positions, unit))
                        {
                            positions[loc, unit] = 1;
                            badUnits = new HashSet<int>();
                            currentPrice += costs[loc, unit];
                            break;
                        }

                        badUnits.Add(unit);
                        UpdatePosibilities(currentPosibilities, pheromones, loc, badUnits);
                        goto GenerateRandomNumber;  
                    }
                    else{
                        continue;
                    }
                } 

                if(currentPosibilities[loc][unit - 1] < randNum && randNum <= currentPosibilities[loc][unit])
                {
                    if(currentPrice + costs[loc, unit] <= budget && IsGoodDist(positions, loc) && IsUnitFree(positions, unit))
                    {
                        positions[loc, unit] = 1;
                        badUnits = new HashSet<int>();
                        currentPrice += costs[loc, unit];
                        break;
                    }

                    badUnits.Add(unit);
                    UpdatePosibilities(currentPosibilities, pheromones, loc, badUnits);
                    goto GenerateRandomNumber;                   
                }
            }
        }

        return positions;
    }

    private double CalculateSolutionPower(int[,] solution)
    {
        double power = 0.0;
        for(int loc = 0; loc < numLocations; loc++)
        {
            for(int unit = 0; unit < numUnits; unit++)
            {
                if(solution[loc, unit] == 1)
                {
                    power += powers[loc, unit];
                }
            }
        }

        return power;
    }

    private bool IsUnitFree(int[,] positions, int unit)
    {
        for(int loc = 0; loc < numLocations; loc++)
        {
            if(positions[loc, unit] == 1)
            {
                return false;
            }
        }

        return true;
    }

    private bool IsGoodDist(int[,] positions, int currentLocation)
    {
        for(int loc = 0; loc < numLocations; loc++)
        {
            for(int unit = 0; unit < numUnits; unit++)
            {
                if(positions[loc, unit] == 1 && precountedDists[loc, currentLocation] < minDist)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private List<double>[] CountInitialCumulativePosibility(double[,] pheromones)
    {
        var cumPher = new List<double>[numLocations];

        for(int loc = 0; loc < numLocations; loc++)
        {
            cumPher[loc] = new List<double>();
            var totalRatio = CountLocationTotalRatio(loc, pheromones);
            for(int unit = 0; unit < numUnits; unit++)
            {
                if(unit == 0)
                {
                    cumPher[loc].Add((powers[loc, unit] / costs[loc, unit] + pheromones[loc, unit]) / totalRatio);
                }
                else
                {
                    cumPher[loc].Add(cumPher[loc][unit-1] + ((powers[loc, unit] / costs[loc, unit] + pheromones[loc, unit]) / totalRatio));
                }
            }  
        }

        return cumPher;
    }

    private void UpdatePosibilities(List<double>[] currentPosibilities, double[,] pheromones, int loc, HashSet<int> badUnits)
    {
        currentPosibilities[loc] = new List<double>();

        double totalRatio = 0.0;
        for (int unit = 0; unit < numUnits; unit++)
        {
            if (!badUnits.Contains(unit))
            {
                totalRatio += powers[loc, unit] / costs[loc, unit] + pheromones[loc, unit];
            }
        }

        for (int unit = 0; unit < numUnits; unit++)
        {
            if (badUnits.Contains(0) && unit == 0)
            {
                currentPosibilities[loc].Add(0.0);
            }
            else if(unit == 0)
            {
                currentPosibilities[loc].Add((powers[loc, unit] / costs[loc, unit] + pheromones[loc, unit]) / totalRatio);
            }
            else if(badUnits.Contains(unit))
            {
                currentPosibilities[loc].Add(currentPosibilities[loc][unit-1]);
            }
            else
            {
                currentPosibilities[loc].Add(currentPosibilities[loc][unit - 1] + ((powers[loc, unit] / costs[loc, unit] + pheromones[loc, unit]) / totalRatio));
            }
        }
    }

    private double CountLocationTotalRatio(int location, double[,] pheromones)
    {
        var ration = 0.0;
        for(int unit = 0; unit < numUnits; unit++)
        {
            ration += powers[location, unit] / costs[location, unit] + pheromones[location, unit];
        }  

        return ration;
    }

    private double[,] InitializePheromones()
    {
        var pheromones = new double[locations.GetLength(0), costs.GetLength(1)]; 

        for (int i = 0; i < numLocations; i++)
        {
            for (int j = 0; j < numUnits; j++)
            {
                pheromones[i, j] = 0.1;  // Initial pheromone level
            }
        }

        return pheromones;
    }

    private void UpdatePheromones(double[,] pheromones, int[,] solution)
    {
        for (int loc = 0; loc < numLocations; loc++)
        {
            for (int unit = 0; unit < numUnits; unit++)
            {
                pheromones[loc, unit] -= evaporationRate; // Evaporate

                if(solution[loc, unit] == 1)
                {
                    pheromones[loc, unit] += alpha; // Ant added pheromone
                }
            }
        }
    }

    private void PrecountDists()
    {        
        for (int i = 0; i < numLocations; i++)
        {
            for (int j = 0; j < numLocations; j++)
            {
                if(i == j)
                {
                    precountedDists[i, j] = int.MinValue;
                }
                else
                {
                    precountedDists[i, j] =
                        Math.Sqrt(Math.Pow(locations[i, 0] - locations[j, 0], 2) + Math.Pow(locations[i, 1] - locations[j, 1], 2));
                }
            }
        }
    }

    private static void Shuffle(int[] array)
    {
        var random = new Random();
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);

            (array[j], array[i]) = (array[i], array[j]);
        }
    }

    private void PrintBestSoluion(int[,] solution, double power)
    {
        Console.WriteLine($"Founded solution is {power} units of power. Placement:");

        Console.Write(" ");
        for (int unit = 0; unit < numUnits; unit++)
        {
            Console.Write($"  U{unit + 1}");
        }

        for (int loc = 0; loc < numLocations; loc++)
        {
            
            Console.WriteLine();

            Console.Write($"L{loc + 1}  ");
            for (int unit = 0; unit < numUnits; unit++)
            {
                if (solution[loc, unit] == 1)
                {
                    Console.Write("✓   ");
                }
                else
                {
                    Console.Write("-   ");
                }
            }
            Console.WriteLine();
        }
    }
}