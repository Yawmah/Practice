using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace plantville
{
    class Game
    {
        protected static string jsonFile = @"saveData.json";
        protected static List<Seed> seeds;
        protected static int money;
        protected static int plots;
        protected static List<Plant> plants;
        protected static List<Plot> offeredPlots = new List<Plot>(); //0 = small, 1 = medium, 2 = large
        protected static StringBuilder sb = new StringBuilder();
        
         [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private delegate bool EventHandler(CtrlType sig);
        static EventHandler handler;

        enum CtrlType
        {
          CTRL_C_EVENT = 0,
          CTRL_BREAK_EVENT = 1,
          CTRL_CLOSE_EVENT = 2,
          CTRL_LOGOFF_EVENT = 5,
          CTRL_SHUTDOWN_EVENT = 6
        }

        private static bool Handler(CtrlType sig)
        {
          switch (sig)
          {
              case CtrlType.CTRL_C_EVENT:
              case CtrlType.CTRL_LOGOFF_EVENT:
              case CtrlType.CTRL_SHUTDOWN_EVENT:
              case CtrlType.CTRL_CLOSE_EVENT:
                  File.WriteAllText(jsonFile, sb.ToString());
                  return false;
              default:
                  return false;
          }
        }

        static void Main(string[] args)
        {
            handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(handler, true);

            // List of seeds we can purchase. 
            seeds = new List<Seed>() {
                new Seed("strawberry", 2, 3, new TimeSpan(0, 2, 0)),
                new Seed("spinach", 5, 10, new TimeSpan(0, 3, 0)),
                new Seed("pears", 3, 6, new TimeSpan(0, 3, 0))
            };

            // How much money player has
            money = 32;
            // Player starts with 7 plots
            plots = 7;
            // List of plants growing in our farm
            plants = new List<Plant>() {
                new Plant(seeds[0]),
                new Plant(seeds[1]),
            };


            Console.WriteLine("Welcome to plantville!");

            //if statement HERE to check if Json file exists then to extract
            if(File.Exists(jsonFile))
            {
                //read json strings into a list
                List < string > readFile = new List<string>();

                using (var reader = new StreamReader(jsonFile))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        readFile.Add(line);
                    }
                }

                //deserialize the json strings into the correct place (0 - Plant, 1 - money, 2 - plots, 3 - offered plots)                           
                List<Plant> deserializedPlantList = JsonConvert.DeserializeObject<List<Plant>>(readFile[0]);
                plants.Clear();
                foreach(Plant p in deserializedPlantList)
                {
                    plants.Add(p);
                }
                
                money = JsonConvert.DeserializeObject<int>(readFile[1]);
                plots = JsonConvert.DeserializeObject<int>(readFile[2]);

                List<Plot> deserializedPlotList = JsonConvert.DeserializeObject<List<Plot>>(readFile[3]);
                offeredPlots.Clear();
                foreach(Plot p in deserializedPlotList)
                {
                    offeredPlots.Add(p);
                }
            }
            

            SaveState();

            // game loop
            while (true) {
                try
                {
                    Console.WriteLine("\n1. Buy seeds");
                    Console.WriteLine("2. Check garden");
                    Console.WriteLine("3. Buy real estate");

                    Console.WriteLine("What do you want to do?");

                    string input = Console.ReadLine();

                    // Visit Seeds Depot to buy more seeds
                    if (input == "1")
                    {
                        Console.WriteLine("\nWelcome to Seeds Depot!");
                        BuySeeds();
                    }
                    // Check out our garden
                    else if (input == "2")
                    {
                        ViewGarden();
                    }
                    else if (input == "3")
                    {
                        BuyRealEstate();
                    }
                    else
                    {
                        Console.WriteLine("{0}Input not recognised, please try again...", Environment.NewLine);
                    }
                    SaveState();
                }
                catch(FormatException e)
                {
                    Console.WriteLine("{0}Error! Please enter a number. Returning to main menu...", Environment.NewLine);
                }
                catch(Exception e)
                {
                    Console.WriteLine("{0}Not sure what went wrong there..", Environment.NewLine);
                    Console.WriteLine("{0}", e.StackTrace);
                }
            }
        }

        public static void SaveState()
        {
            string plantsSerialized = JsonConvert.SerializeObject(plants);
            string moneySerialized = JsonConvert.SerializeObject(money);
            string plotsSerialized = JsonConvert.SerializeObject(plots);
            string offeredPlotsSerialized = JsonConvert.SerializeObject(offeredPlots);

            sb.Clear();
            sb.AppendLine(plantsSerialized);
            sb.AppendLine(moneySerialized);
            sb.AppendLine(plotsSerialized);
            sb.AppendLine(offeredPlotsSerialized);
        }

        /// <summary>
        /// Check out our garden
        /// </summary>
        private static void ViewGarden()
        {

            Console.WriteLine("\nWelcome to your garden");
            Console.WriteLine(string.Format("You have ${0}.", money));
            Console.WriteLine("{0} plots total - {1} plots available", plots, plots - plants.Count);

            // Print list of plants growing and their current harvesting state
            for (int i = 0; i < plants.Count; i++)
            {
                Console.WriteLine(string.Format("{0}. {1} ({2})", i + 1, 
                                                plants[i].Name,
                                                HarvestTimeLeftMessage(plants[i])
                                               ));
            }
            Console.WriteLine("{0}. Harvest all", plants.Count + 1);

            Console.WriteLine(string.Format("{0}. Leave garden", plants.Count + 2));
            Console.WriteLine("What do you want to harvest?");

            int input = Convert.ToInt32(Console.ReadLine())-1;

            if (input < plants.Count)
            {
                // if plant is ready to harvest...
                if (HarvestTimeLeft(plants[input]) <= 0) {

                    // sell it to make the cheddar
                    money += plants[input].HarvestPrice;

                    // blast to the world we made money
                    Console.WriteLine(string.Format("Successfully harvested {0}. Made ${1}.", plants[input].Name, 
                                                    plants[input].HarvestPrice));

                    // remove from our garden
                    plants.RemoveAt(input);
                    SaveState();
                }
                else {
                    // You fool! Plant not ready to harvest
                    Console.WriteLine(string.Format("Cannot harvest {0} yet. {1} minutes left.", plants[input].Name, 
                                                    HarvestTimeLeft(plants[input])));
                }
                // Loop back to the garden
                ViewGarden();             
            }
            else if (input.Equals(plants.Count))
            {
                Console.WriteLine("{0} plants in the farm.", plants.Count);

                foreach (Plant p in (new List<Plant>(plants)))
                {
                    if (HarvestTimeLeft(p) <= 0)
                    {
                        // sell it to make the cheddar
                        money += p.HarvestPrice;

                        // blast to the world we made money
                        Console.WriteLine(string.Format("Successfully harvested {0}. Made ${1}.", p.Name,
                                                        p.HarvestPrice));

                        // remove from our garden
                        plants.Remove(p);
                    }
                    else
                    {
                        Console.WriteLine("{0} not ready for harvesting.", p.Name);
                    }
                }
                SaveState();
                // Loop back to the garden
                ViewGarden();
            }
            else if (input > plants.Count + 1 || input > 2 && plants.Count == 0)
            {
                Console.WriteLine("Unrecognised input entered. Returning to main menu...");
            }
        }

        /// <summary>
        /// Prints message of how much time is left until harvest
        /// </summary>
        /// <returns>Message of how much time left.</returns>
        /// <param name="plant">Plant.</param>
        private static string HarvestTimeLeftMessage(Plant plant)
        {
            // get number of minutes to harvest
            int minutes_to_harvest = HarvestTimeLeft(plant);

            // if harvest ready...
            if (minutes_to_harvest <= 0)
            {
                return "harvest";
            }
            // minutes until harvest if not harvest ready
            else
            {
                return string.Format("{0} minutes left", minutes_to_harvest);
            }
        }

        /// <summary>
        /// Calculates how much time until ready to harvest
        /// </summary>
        /// <returns>The time left.</returns>
        /// <param name="plant">Plant.</param>
        private static int HarvestTimeLeft(Plant plant)
        {
            // What time will the plant be ready to harvest?
            DateTime harvest_ready_time = plant.HarvestTime.Add(plant.HarvestDuration);

            // How far from now until harvest. Negative time means it's ready for harvest. 
            TimeSpan harvest_time_left = harvest_ready_time.Subtract(DateTime.Now);

            return harvest_time_left.Minutes;
        }

        /// <summary>
        /// Buy seeds at Seed Depot
        /// </summary>
        private static void BuySeeds()
        {

            Console.WriteLine(string.Format("{0}You have {1:C}", Environment.NewLine, money));

            // print seed selection
            for (int i = 0; i < seeds.Count; i++) {
                Console.WriteLine(string.Format("{0}. {1} (${2})", i+1, seeds[i].Name, seeds[i].SeedPrice));
            }

            Console.WriteLine(string.Format("{0}. Leave Seeds Depot", seeds.Count+1));
            Console.WriteLine("{0}What do you want to buy?", Environment.NewLine);


            int input = Convert.ToInt32(Console.ReadLine())-1;

            if (input < seeds.Count)
            {
                Console.WriteLine("How many {0} seeds would you like to buy?", seeds[input].Name);
                int inputAmount = int.Parse(Console.ReadLine());
                int fullCost = inputAmount * seeds[input].SeedPrice;

                //check there are enough free plots
                if ((plants.Count + inputAmount) <= plots)
                {
                    // check if you have enough money. 
                    if (money - fullCost < 0)
                    {
                        Console.WriteLine("{0}Not enough money, you greedy farmer!", Environment.NewLine);
                    }
                    else
                    {
                        // if you have enough money, add it to your garden and pay the lady

                        for (int i = 0; i < inputAmount; i++)
                        {
                            var seed = seeds[input];
                            plants.Add(new Plant(seed));
                        }

                        money -= fullCost;
                        SaveState();

                        Console.WriteLine(string.Format("Bought and planted {0} in garden\n", seeds[input].Name));
                    }
                }
                else
                {
                    Console.WriteLine("{0}Not enough room on the farm!{0}You have {1} free plots.", Environment.NewLine, plots - plants.Count);
                }
                BuySeeds();
            }
            else if (input > seeds.Count)
            {
                Console.WriteLine("{0}Unrecognised input entered. Returning to main menu...", Environment.NewLine);
            }
        }

        private static void BuyRealEstate()
        {
            Console.WriteLine("{0}Welcome to the Real Estate Fair{0}Buy plots of land to plant crops in", Environment.NewLine);
            Console.WriteLine("You have {0:C}.", money);
            Console.WriteLine("{0}Current land on offer:", Environment.NewLine);
            Random rand = new Random();

            //instantiate initial land offers
            if (offeredPlots.Count.Equals(0))
            {
                offeredPlots.Add(new Plot("Small", rand)); 
                offeredPlots.Add(new Plot("Medium", rand));
                offeredPlots.Add(new Plot("Large", rand));
            }

            //monitor land offer times and instantiate new offers every 5 minutes
            for (int i = 0; i < offeredPlots.Count; i++)
            {
                DateTime offerExpiration = offeredPlots[i].OfferStart.Add(offeredPlots[i].OfferDuration);
                TimeSpan offerTimeRemaining = offerExpiration.Subtract(DateTime.Now);

                switch(i)
                {
                    case 0:
                    if (offerTimeRemaining.Seconds <= 0)
                    {
                        offeredPlots[i] = new Plot("Small", rand); 
                    }
                    break;
                    case 1:
                    if (offerTimeRemaining.Seconds <= 0)
                    {
                        offeredPlots[i] = new Plot("Medium", rand); 
                    }
                    break;
                    case 2:
                    if (offerTimeRemaining.Seconds <= 0)
                    {
                        offeredPlots[i] = new Plot("Large", rand); 
                    }
                    break;
                }
            }
            for (int i = 0; i < offeredPlots.Count; i++) {
                Console.WriteLine("{0}. {1} (${2})", i+1, offeredPlots[i].SizeName, offeredPlots[i].Price);
            }
            Console.WriteLine("{0}. Leave Real Estate Fair", offeredPlots.Count+1);

            int input = int.Parse(Console.ReadLine()) - 1;

            if (input < offeredPlots.Count)
            {
                if (money >= offeredPlots[input].Price)
                {
                    plots += offeredPlots[input].Size;
                    money -= offeredPlots[input].Price;
                    Console.WriteLine("{0}Congratulations! you are the proud owner of {1} more plots of land, total plots now owned: {2}", Environment.NewLine, offeredPlots[input].Size, plots);
                    SaveState();
                }
                else
                {
                    Console.WriteLine("{0}Not enough funds! Perhaps you would like something a little smaller?", Environment.NewLine);
                }
                BuyRealEstate();
            }
            else if (input > offeredPlots.Count)
            {
                Console.WriteLine("{0}Unrecognised input entered. Returning to main menu", Environment.NewLine);
            }
        }
    }

    class Plot
    {
        public int Size { get; set; }
        public int Price { get; set; }
        public string SizeName { get; set; }
        public TimeSpan OfferDuration { get; set; }
        public DateTime OfferStart { get; set; }
        public Random Rand { get; set; }

        public Plot(string sizeName, Random rand)
        {
            Rand = rand;
            SizeName = sizeName;
            switch(sizeName)
            {
                case "Small":
                    Size = rand.Next(10, 30);
                    Price = rand.Next(22, 33) * Size;
                break;
                case "Medium":
                    Size = rand.Next(30, 70);
                    Price = rand.Next(15, 22) * Size;
                break;
                case "Large":
                    Size = rand.Next(70, 200);
                    Price = rand.Next(8, 15) * Size;
                break;
            }           
            OfferDuration = new TimeSpan(0, 5, 0);
            OfferStart = DateTime.Now;
        }
    }

    class Seed
    {

        public string Name { get; set; }        // name of plant
        public int SeedPrice { get; set; }      // how much
        public int HarvestPrice { get; set; }           // how much we make if we harvest it
        public TimeSpan HarvestDuration { get; set; }       // how long it takes to harvest

        /// <summary>
        /// Initializes a new instance of the <see cref="T:plantville.Seed"/> class.
        /// </summary>
        /// <param name="name">Name of plant</param>
        /// <param name="seedPrice">How much seed costs</param>
        /// <param name="harvestPrice">How much we make if we harvest it</param>
        /// <param name="harvestDuration">How long it takes to harvest. Timespan takes in parameters(hours, minutes, seconds)</param>
        public Seed(string name, int seedPrice, int harvestPrice, TimeSpan harvestDuration)
        {
            Name = name;
            SeedPrice = seedPrice;
            HarvestPrice = harvestPrice;
            HarvestDuration = harvestDuration;
        }
    }


    class Plant
    {
        public string Name { get; set; }        // name of plant
        public int HarvestPrice { get; set; }       // how much sweet cheddar (money) we'll make when it harvests
        public DateTime HarvestTime { get; set; }       // what time we harvested it (must mean planted it)
        public TimeSpan HarvestDuration { get; set; }       // how long (duration) until harvest


        public Plant(Seed seed)
        {
            Name = seed.Name;
            HarvestPrice = seed.HarvestPrice;
            HarvestTime = DateTime.Now;
            HarvestDuration = seed.HarvestDuration;
        }

        /// <summary>
        /// Took me forever to figure out that I needed this to deserialize. *cries*
        /// </summary>
        public Plant() {

        }
    }
}
