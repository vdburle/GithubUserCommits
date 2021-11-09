using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Net.Http.Json;
using System.Text;
using System.IO;

namespace GitHubUserCommits
{
    class Program
    {
        private const string baseURL = "https://api.github.com/";
        private const string searchAPIParams = "/search/commits?q=committer:{0}&sort=committer-date&order=desc&per_page={1}&page={2}";
        private const string perPageResults = "60";
        private const string pageNumtoFecth = "1";

        public static string commiter;
        public static string token;

        internal class Committer
        {
            public DateTime date { get; set; }
            public string name { get; set; }
            public string email { get; set; }
            public string login { get; set; }
        }

        internal class Commit
        {
            public string url { get; set; }
            public Committer committer { get; set; }
            public string message { get; set; }
            public int comment_count { get; set; }
        }

        internal class Item
        {
            public Commit commit { get; set; }
        }

        internal class Root
        {
            public int total_count { get; set; }
            public bool incomplete_results { get; set; }
            public List<Item> items { get; set; }
        }

        static void Main(string[] args)
        {


            getUserInput();
            Root responseData = fetchCommitDataAsync().Result;
            writeToCSV(responseData);


        }

        public static void getUserInput()
        {
            while (true)
            {
                Console.Write("Enter the UserName:");
                commiter = Console.ReadLine();
                if (string.IsNullOrEmpty(commiter.Trim()))
                {
                    Console.WriteLine("The Username cannot be empty, please enter a non empty username");
                    continue;
                }

                break;
            }

            while (true)
            {
                Console.Write("Enter the token:");
                token = Console.ReadLine();
                if (string.IsNullOrEmpty(token.Trim()))
                {
                    Console.WriteLine("The Username cannot be empty, please enter a non empty username");
                    continue;
                }
                break;
            }
        }

        public static async Task<Root> fetchCommitDataAsync()
        {
            Root responseData = null;

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(baseURL);
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GHApp_vdburle", "1"));
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.cloak-preview"));
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("token", token);

                using var response = await client.GetAsync(string.Format(searchAPIParams, commiter, perPageResults, pageNumtoFecth));

                try
                {
                    response.EnsureSuccessStatusCode();
                    responseData = response.Content.ReadFromJsonAsync<Root>().Result;

                    if (responseData.items.Count == 0)
                    {
                        throw new Exception(string.Format("The user {0} doesnt have any commits", commiter));
                    }
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine("There is a problem with the API response, please check the username and token");
                    Console.WriteLine(ex.Message);
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Environment.Exit(0);
                }
            }


            return responseData;
        }

        public static void writeToCSV(Root responseData)
        {
            double avgCommitInSec = 0;

            int numCommitResults = responseData.items.Count;
            if (numCommitResults > 0)
            {
                double cumulativeCheckinTS = 0;
                StringBuilder sb = new StringBuilder("Commit Dates");
                DateTime prevDate = responseData.items[0].commit.committer.date;
                foreach (Item item in responseData.items)
                {
                    DateTime currentDate = item.commit.committer.date;
                    sb.AppendLine();
                    sb.Append(currentDate.ToString());
                    cumulativeCheckinTS += (prevDate - currentDate).TotalSeconds;
                    prevDate = currentDate;
                }
                avgCommitInSec = cumulativeCheckinTS / (numCommitResults - 1);
                File.WriteAllText(commiter + "_commits.csv", sb.ToString());
                Console.WriteLine("Average time between commits for the user {0} is : {1}", commiter, avgCommitInSec);
            }
        }

        

    }
}

