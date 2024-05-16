Console.WriteLine("Choose mdoe (1 - data entered in program 2 - generated data):");
var answer = Console.ReadLine();

double evaporationRate = 0.05;  // Швидкість випаровування феромону

if(answer == "1")
{
    // Визначення параметрів тестового сценарію
    double minDist = 10; // мінімальна відстань між локаціями ВДЕ
    double budget = 10; // загальний бюджет

    // Визначення координат локацій
    double[,] locations = new double[,]
    {
                { 18, 71 },  // Локація 1
                { 28, 71 },  // Локація 2
                { 25, 64.11508 },  // Локація 3
                { 28, 71.3588 },  // Локація 4
    };

    // Визначення вартості установки для кожного ВДЕ в кожній локації
    double[,] costs = new double[,]
    {
                { 114, 117, 117, 112, 100 },
                { 5, 4, 6, 7, 7 },
                { 41, 84, 77, 53, 100 },
                { 41, 84, 77, 53, 100 }
    };

    // Визначення потужності кожного ВДЕ в кожній локації
    double[,] powers = new double[,]
    {
                { 112, 107, 118, 118, 100 },
                { 70, 32, 16, 85, 22 },
                { 48, 34, 94, 27, 100 },
                { 112, 107, 118, 118, 100 }
    };

    
    
    // Створення екземпляра алгоритму
    var aco = new AntColonyOptimization(locations, costs, powers, budget, minDist, evaporationRate);
    aco.Optimize(20, 100);  // Запуск оптимізації з 20 мурашками та 100 ітераціями    
}
else
{
    Console.WriteLine("Enter number of tasks: ");

    int tasks = 0;

    try
    {
        tasks = int.Parse(Console.ReadLine()!);
    }
    catch(Exception)
    {
        Console.WriteLine("Wrong input");
    }

    Console.WriteLine("Enter number of locations:");
    int locations = 0;
    try
    {
        locations = int.Parse(Console.ReadLine()!);
    }
    catch(Exception)
    {
        Console.WriteLine("Wrong input");
    }

    Console.WriteLine("Enter number of units:");
    int units = 0;
    try
    {
        units = int.Parse(Console.ReadLine()!);
    }
    catch(Exception)
    {
        Console.WriteLine("Wrong input");
    }

    Console.WriteLine("Enter budget:");
    int budget = 0;
    try
    {
        budget = int.Parse(Console.ReadLine()!);
    }
    catch(Exception)
    {
        Console.WriteLine("Wrong input");
    }

    Console.WriteLine("Enter minimal distance between VDEs:");
    int minDist = 0;
    try
    {
        minDist = int.Parse(Console.ReadLine()!);
    }
    catch(Exception)
    {
        Console.WriteLine("Wrong input");
    }

    double avgError = 0;

    for(int i = 0; i < tasks; i++)
    {
        var generator = new TaskGenerator(locations, units, budget, minDist);
        var task = generator.Generate();

        var aco1 = new AntColonyOptimization(task.Locations, task.Costs, task.Powers, task.Budget, task.MinDist, evaporationRate);
        (double power, double price) foundSolution = aco1.Optimize(30, 100); // Запуск оптимізації з 20 мурашками та 100 ітераціями

        var error = Math.Abs(task.ExpectedTotalPower / foundSolution.power * 100 - 100);
        avgError = (avgError + error) / 2; 

        Console.WriteLine($"Expected result: {task.ExpectedTotalPower}. Returned: {foundSolution.power}.");
        Console.WriteLine($"Error is {error}");
        Console.WriteLine("Price: " + foundSolution.price);
    }

    System.Console.WriteLine(tasks + " tasks finished with avarage error " + avgError);
    
    // var generator = new TaskGenerator(locations: 10, units: 11, budget: 1000, minDist: 10);
    // var task = generator.Generate();
}

