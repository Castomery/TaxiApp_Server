using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Routing;
using MyServer.Models;
using System;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Reflection;
using System.Xml.Linq;

namespace MyServer.Sevices
{
    public class DistributionCalculation : IDistributionCalculation
    {
        private double priceForCar;
        private double pricePerKm;
        private HttpClient _client = new HttpClient();
        private string _addressUrl = "https://api.mapbox.com/optimized-trips/v1/mapbox/driving/";
        private string _accessToken = "pk.eyJ1IjoiY3VzdG9tZXJ5IiwiYSI6ImNsdTV0ZjdzcDFyYWIycnQ0c3A3YjR4bGMifQ.MhMF_oqJ6eVB6udAestl8g";
        private async Task<OptimizationResponse> GetDistance(string route)
        {
            var response = await _client.GetAsync($"{_addressUrl}{route}?source=first&destination=last&roundtrip=false&access_token={_accessToken}");

            var body = await response.Content.ReadFromJsonAsync<OptimizationResponse>();

            return body;
        }

        private List<List<string>> GetPermutations(string origin, List<string> coordinates)
        {
            List<List<string>> permutations = new List<List<string>>();
            for (int i = 0; i < coordinates.Count(); i++)
            {
                List<string> points = new List<string>() { origin};
                for (int j = 0; j < coordinates.Count(); j++)
                {
                    int index = (i + j) % coordinates.Count;
                    points.Add(coordinates[index]);
                }
                permutations.Add(points);
            }
            return permutations;
        }

        public async Task<List<string>> GetRoute(string origin, List<string> coordinates) 
        {
            List<string> route = new List<string>();

            List<List<string>> permutations = GetPermutations(origin, coordinates);
            double currDistance = double.MaxValue;

            List<Task> tasks = new List<Task>();
            foreach (var permutation in permutations)
            {
                tasks.Add(Task.Run(async () =>
                {
                    OptimizationResponse response = await GetDistance(string.Join(";", permutation.ToArray()));
                    if (currDistance > response.trips[0].distance)
                    {
                        currDistance = response.trips[0].distance;
                        route = permutation;
                        List<string> pointsNames = new List<string>();
                    }
                }));
            }

            await Task.WhenAll(tasks);

            return route;
        }
        private async Task<ShortestRoute> GetShortestDistanceRoute(string origin, List<string> coordinates)
        {
            
            double currDistance = double.MaxValue;
            ShortestRoute shortestRoute = new ShortestRoute();

            List<List<string>> permutations = GetPermutations(origin, coordinates);

            List<Task> tasks = new List<Task>();
            foreach (var permutation in permutations)
            {
                tasks.Add(Task.Run(async () =>
                {
                    
                    OptimizationResponse response = await GetDistance(string.Join(";", permutation.ToArray()));
                    if (currDistance > response.trips[0].distance)
                    {
                        currDistance = response.trips[0].distance;
                        shortestRoute.route = permutation;
                        shortestRoute.pointsNames = response.waypoints.Select(point => point.name).ToList();
                        shortestRoute.distance = currDistance;
                        shortestRoute.duration = response.trips[0].duration/60 < 1? 1: response.trips[0].duration / 60;
                        CalculatePriceForRoute(shortestRoute);
                    }
                }));
            }

            await Task.WhenAll(tasks);

            return shortestRoute;
        }

        public async Task<ShortestRoute> GetPriceDistributionForOneCar(string origin, double priceForCar, double pricePerKm, List<string> coordinates)
        {
            this.priceForCar = priceForCar;
            this.pricePerKm = pricePerKm;
            int totalRoutes = 1 << coordinates.Count;
            ShortestRoute[] result = new ShortestRoute[totalRoutes];
            Task<ShortestRoute>[] tasks = new Task<ShortestRoute>[totalRoutes];

            for (int i = 0; i < result.Length; i++)
            {
                List<string> points = new List<string>();
                for (int j = 0; j < coordinates.Count; j++)
                {
                    if ((i & (1 << j)) > 0)
                    {
                        points.Add(coordinates[j]);
                    }
                }
                if (i != 0)
                {
                    tasks[i] = GetShortestDistanceRoute(origin, points);
                }
            }

            for (int i = 0; i < tasks.Length; i++)
            {
                if (tasks[i] != null)
                {
                    result[i] = await tasks[i];
                }
            }

            double[] pricesForOneCar = new double[result.Length];

            for (int i = 1; i < totalRoutes; i++)
            {
                if (tasks[i] != null)
                {
                    result[i] = await tasks[i];
                    pricesForOneCar[i] = result[i].totalPrice;
                }
            }

            int indexOfRoute = (1 << coordinates.Count) - 1;
            Dictionary<int, ShortestRoute> answer = new Dictionary<int, ShortestRoute>();
            answer.Add(indexOfRoute, result[indexOfRoute]);

            Dictionary<int, List<string>> routeCoords = new Dictionary<int, List<string>>();
            routeCoords.Add(indexOfRoute, coordinates);
            
            List<ShortestRoute> best = CalculatePriceDistribution(answer, pricesForOneCar, pricesForOneCar, routeCoords);
            ShortestRoute routeToReturn = best.FirstOrDefault();
            return routeToReturn;
        }

        private void CalculatePriceForRoute(ShortestRoute route)
        {
            double distanceInKm = route.distance / 1000;
            route.totalPrice = priceForCar + (distanceInKm - 1) * (pricePerKm + route.route.Count - 1);
        }

        public async Task<List<ShortestRoute>> GetDistribution(string origin,double priceForCar,double pricePerKm, List<string> coordinates)
        {
            this.priceForCar = priceForCar;
            this.pricePerKm = pricePerKm;
            int totalRoutes = 1 << coordinates.Count;
            ShortestRoute[] result = new ShortestRoute[totalRoutes];
            Task<ShortestRoute>[] tasks = new Task<ShortestRoute>[totalRoutes];

            for (int i = 0; i < result.Length; i++)
            {
                List<string> points = new List<string>();
                for (int j = 0; j < coordinates.Count; j++)
                {
                    if ((i & (1 << j)) > 0)
                    {
                        points.Add(coordinates[j]);
                    }
                }
                if (i != 0)
                {
                    tasks[i] = GetShortestDistanceRoute(origin, points);
                }
            }

            for (int i = 0; i < tasks.Length; i++)
            {
                if (tasks[i] != null)
                {
                    result[i] = await tasks[i];
                }
            }

            double[] pricesForOneCar = new double[result.Length];

            double[] lowestPrice = new double[result.Length];
            Array.Fill(lowestPrice, double.MaxValue);

            for (int i = 1; i < totalRoutes; i++)
            {
                if (tasks[i] != null)
                {
                    result[i] = await tasks[i];
                    pricesForOneCar[i] = result[i].totalPrice;
                }
            }

            Dictionary<int, List<int>> distribution = new Dictionary<int, List<int>>();
            for (int i = 0; i < result.Length; i++)
            {
                distribution.Add(i, new List<int>());
            }

            GetAnswer(totalRoutes - 1, result, ref lowestPrice, ref pricesForOneCar, ref distribution);

            Dictionary<int, ShortestRoute> answer = new Dictionary<int, ShortestRoute>();

            FillAnswerList(totalRoutes - 1, distribution, result, ref answer);

            Dictionary<int, List<string>> coords = InitiDictionaryReccomendation(answer, coordinates);

            return CalculatePriceDistribution(answer, lowestPrice, pricesForOneCar, coords);
        }

        private Dictionary<int, List<string>> InitiDictionaryReccomendation(Dictionary<int, ShortestRoute> answer, List<string> coordinates)
        {
            Dictionary<int, List<string>> coords = new Dictionary<int, List<string>>();
            int[] keys = answer.Keys.ToArray<int>();

            for (int i = 0; i < answer.Count; i++)
            {
                coords.Add(keys[i], InitListOfCoords(answer[keys[i]], coordinates));
            }

            return coords;
        }

        private List<string> InitListOfCoords(ShortestRoute shortestRoute, List<string> coordinates)
        {
            List<string> coords = new List<string>();
            for (int i = 0; i < coordinates.Count; i++)
            {
                for (int j = 0; j < shortestRoute.route.Count; j++)
                {
                    if (shortestRoute.route[j].Equals(coordinates[i]))
                    {
                        coords.Add(coordinates[i]);
                        break;
                    }
                }
            }
            return coords;
        }

        private List<ShortestRoute> CalculatePriceDistribution(Dictionary<int, ShortestRoute> answer, double[] lowestPrice, double[] priceForOneCar, Dictionary<int,List<string>> coords)
        {
            List<ShortestRoute> routes = new List<ShortestRoute>();
            int[] keys = answer.Keys.ToArray<int>();

            double[] v = new double[priceForOneCar.Length];

            double[] w = new double[priceForOneCar.Length];

            for (int i = 0; i < keys.Length; i++)
            {
                int key = keys[i];

                ShortestRoute route = answer[key];
                if (HasOnlyOneBitSet(key))
                {
                    routes.Add(route);
                    continue;
                }

                List<int> indexesOfAddresses = new List<int>();
                for (int j = 0; j < coords[key].Count; j++)
                {
                    for (int x = 0; x < route.route.Count; x++)
                    {
                        if (route.route[x].Equals(coords[key][j]))
                        {
                            indexesOfAddresses.Add(x-1);
                        }
                    }
                }

                int count = answer[key].route.Count - 1;

                List<int> subsets = GetBitsFromBitmask(key);

                CalculateValuesForPriceDistributionCalculatiions(key,ref w, ref v, priceForOneCar, subsets);

                int factorial = 1;

                for (int k = 1; k <= count; k++)
                {
                    factorial *= k;
                }

                double[,] array = BuildArrayForCalculations(factorial, count, key, w);

                double[] shaplyVector = CalculateShaply(count, array);

                double[] valueToPay = GetValueToPay(count,shaplyVector,subsets,v);

                route.priceDistribution = new double[valueToPay.Length];

                for (int j = 0; j < route.priceDistribution.Length; j++)
                {
                    route.priceDistribution[indexesOfAddresses[j]] = valueToPay[j];
                }

                routes.Add(route);
            }
            return routes;
        }

        private double[,] BuildArrayForCalculations(int factorial, int count, int key, double[] w)
        {
            double[,] array = new double[factorial, count];

            int[] arrayOfIndexes = GetBitsFromBitmask(key).ToArray();

            List<int[]> result = new List<int[]>();

            PermuteHelper(arrayOfIndexes, 0, arrayOfIndexes.Length - 1, result);

            int[] arrayOfhelpIndexes = new int[arrayOfIndexes.Length];
            for (int x = 0; x < arrayOfhelpIndexes.Length; x++)
            {
                arrayOfhelpIndexes[x] = x;
            }

            List<int[]> helpList = new List<int[]>();

            PermuteHelper(arrayOfhelpIndexes, 0, arrayOfhelpIndexes.Length - 1, helpList);

            for (int k = 0; k < array.GetLength(0); k++)
            {
                int[] indexesToCheck = result[k];
                int[] indexesToFill = helpList[k];
                int indexToCheck = 0;
                double valueToRemove = 0;
                for (int j = 0; j < array.GetLength(1); j++)
                {
                    indexToCheck += 1 << indexesToCheck[j];
                    array[k, indexesToFill[j]] = w[indexToCheck] - valueToRemove;
                    valueToRemove += array[k, indexesToFill[j]];
                }
            }

            return array;
        }

        private double[] GetValueToPay(int count, double[] shaplyVector, List<int> subsets, double[] v)
        {
            double[] valueToPay = new double[count];
            for (int k = 0; k < shaplyVector.Length; k++)
            {
                valueToPay[k] = v[1 << subsets[k]] + shaplyVector[k];
            }
            return valueToPay;
        }

        private double[] CalculateShaply(int count,double[,] array)
        {
            double[] shaplyVector = new double[count];

            for (int k = 0; k < array.GetLength(1); k++)
            {
                double sum = 0;
                for (int j = 0; j < array.GetLength(0); j++)
                {
                    sum += array[j, k];
                }
                shaplyVector[k] = sum / array.GetLength(0);
            }
            return shaplyVector;
        }

        private void CalculateValuesForPriceDistributionCalculatiions(int key, ref double[] w, ref double[] v, double[] priceForOneCar, List<int> subsets)
        {
            
            List<List<int>> combinations = GetAllCombinations(subsets);

            for (int x = 0; x < combinations.Count; x++)
            {
                double sum = 0;
                int indexInArray = 0;
                foreach (var item in combinations[x])
                {
                    indexInArray += 1 << item;
                    sum += priceForOneCar[1 << item];
                }
                v[indexInArray] = priceForOneCar[indexInArray];
                w[indexInArray] = v[indexInArray] - sum;
            }
        }

        private List<List<int>> GetAllCombinations(List<int> subsets)
        {
            List<List<int>> result = new List<List<int>>();

            for (int i = 1; i <= subsets.Count; i++)
            {
                GetAllCombinationsHelper(subsets, 0, new List<int>(), i, result);
            }

            return result;
        }

        private static void GetAllCombinationsHelper(List<int> indexes, int start, List<int> current, int r, List<List<int>> result)
        {
            if (r == 0)
            {
                result.Add(new List<int>(current));
                return;
            }

            for (int i = start; i < indexes.Count; i++)
            {
                current.Add(indexes[i]);
                GetAllCombinationsHelper(indexes, i + 1, current, r - 1, result);
                current.RemoveAt(current.Count - 1);
            }
        }

        private List<int> GetBitsFromBitmask(int bitmask)
        {
            List<int> indexes = new List<int>();
            for (int i = 0; i < sizeof(int) * 8; i++)
            {
                if ((bitmask & (1 << i)) != 0)
                {
                    indexes.Add(i);
                }
            }

            return indexes;
        }

        private bool HasOnlyOneBitSet(int bitmask)
        {
            return (bitmask & (bitmask - 1)) == 0 && bitmask != 0;
        }

        private void PermuteHelper(int[] arr, int startIndex, int endIndex, List<int[]> result)
        {
            if (startIndex == endIndex)
            {
                result.Add((int[])arr.Clone());
            }
            else
            {
                for (int i = startIndex; i <= endIndex; i++)
                {
                    Swap(ref arr[startIndex], ref arr[i]);
                    PermuteHelper(arr, startIndex + 1, endIndex, result);
                    Swap(ref arr[startIndex], ref arr[i]);
                }
            }
        }

        private void Swap(ref int a, ref int b)
        {
            int temp = a;
            a = b;
            b = temp;
        }

        private void FillAnswerList(int n, Dictionary<int, List<int>> distribution, ShortestRoute[] shortestRoutes, ref Dictionary<int, ShortestRoute> answer)
        {
            if (distribution[n].Count != 0)
            {
                for (int i = 0; i < distribution[n].Count; i++)
                {
                    FillAnswerList(distribution[n][i], distribution, shortestRoutes, ref answer);
                }
            }
            else
            {
                answer.Add(n, shortestRoutes[n]);
            }
        }

        private List<int> GetSubsets(int n)
        {
            int count = n;
            List<int> arrsubsets = new List<int>();
            while (n > 0)
            {
                n = (n - 1) & count;
                if (n == 0)
                {
                    break;
                }
                arrsubsets.Add(n);
            }
            return arrsubsets;
        }

        private void GetAnswer(int count, ShortestRoute[] result, ref double[] lowestPrice, ref double[] priceForOneCar, ref Dictionary<int, List<int>> distribution)
        {

            int c = count;
            List<int> arrsubsets = GetSubsets(c);
            int length = arrsubsets.Count / 2;
            int x = 0, y = arrsubsets.Count - 1;

            if (arrsubsets.Count == 0)
            {
                lowestPrice[count] = priceForOneCar[count];
                return;
            }
            for (int i = 0; i < length; i++, x++, y--)
            {
                if (lowestPrice[arrsubsets[x]] == double.MaxValue)
                {
                    GetAnswer(arrsubsets[x], result, ref lowestPrice, ref priceForOneCar, ref distribution);

                }
                if (lowestPrice[arrsubsets[y]] == double.MaxValue)
                {
                    GetAnswer(arrsubsets[y], result, ref lowestPrice, ref priceForOneCar, ref distribution);

                }

                double temp = lowestPrice[arrsubsets[x]] + lowestPrice[arrsubsets[y]];
                if (temp < priceForOneCar[count])
                {
                    distribution[count] = new List<int>() { arrsubsets[x], arrsubsets[y] };
                }
                else
                {
                    temp = priceForOneCar[count];

                }
                if (lowestPrice[count] > temp)
                {
                    lowestPrice[count] = temp;

                }
            }
        }
    }
}
