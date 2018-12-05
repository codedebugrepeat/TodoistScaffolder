using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TodoistScaffolder
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // Wait for async task so main method doesn't finish
            new Scaffolder().Scaffold().GetAwaiter().GetResult();
            Console.ReadKey();
        }
    }

    internal class Scaffolder
    {
        private const string todoistUrl = "https://todoist.com/API";

        private const string configFilePath = "config.json";

        public async Task Scaffold()
        {
            var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configFilePath));

            if (config.DeleteProjectsAtStart)
                await DeleteProjects(config);

            var projectsCommands = File.ReadAllText(config.PathToProjects);

            var commandsWithUids = GetCommandsWithUids(projectsCommands);

            var uri = GetSyncUri(config);

            Console.WriteLine($"Uri: {uri}");
            Console.WriteLine();
            Console.WriteLine(projectsCommands);
            Console.WriteLine();

            await SendCommands(uri, commandsWithUids);
        }

        private string GetSyncUri(Config config) => $"{todoistUrl}/v{config.ApiVersion}/sync?token={config.SyncToken}";

        private async Task DeleteProjects(Config config)
        {
            var syncUri = $"{GetSyncUri(config)}&sync_token=*&resource_types=[\"projects\"]";
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(syncUri);
                var responseString = await response.Content.ReadAsStringAsync();

                var syncResponse = JsonConvert.DeserializeObject<ProjectsSyncResponse>(responseString);
                var idsToDelete = syncResponse.Projects.Where(x => x.Name != "Inbox").Select(x => x.Id);
                var idsToDeleteCommaSeparatedList = string.Join(",", idsToDelete);

                var deleteCommand = $"[{{\"type\": \"project_delete\", \"uuid\": \"{Guid.NewGuid()}\", \"args\": {{\"ids\": [{idsToDeleteCommaSeparatedList}]}}}}]";
                await SendCommands(GetSyncUri(config), deleteCommand);
            }
        }

        private string GetCommandsWithUids(string commands)
        {
            dynamic commandsArray = JsonConvert.DeserializeObject(commands) as JArray;
            foreach (var command in commandsArray)
            {
                command.uuid = Guid.NewGuid();
            }
            return JsonConvert.SerializeObject(commandsArray);
        }

        private async Task SendCommands(string uri, string commands)
        {
            var commandJson = $"{{\"resource_types\":[\"all\"],\"commands\":{commands}}}";

            using (var client = new HttpClient())
            {
                var response = await client.PostAsync(uri, new StringContent(commandJson, Encoding.UTF8, "application/json"));
                Console.WriteLine("Commands sent");
                Console.WriteLine("Response:");
                Console.WriteLine();

                var responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseString);
            }
        }
    }

    internal class Config
    {
        public string SyncToken { get; set; }
        public string ApiVersion { get; set; }
        public string PathToProjects { get; set; }
        public bool DeleteProjectsAtStart { get; set; }
    }

    internal class ProjectsSyncResponse
    {
        [JsonProperty("projects")]
        public ProjectRemote[] Projects { get; set; }
    }

    internal class ProjectRemote
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
