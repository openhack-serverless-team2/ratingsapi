using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.Azure.Cosmos;
using System.Net.Http;
using System.Net.Http.Headers;

// massimo change

namespace FunctionApp2
{

public class Rating
    {
        public string userId { get; set; }
        public string productId { get; set; }
        public string locationName { get; set; }
        public int rating { get; set; }
        public string userNotes { get; set; }

        public Rating (string userId,string productId,string locationName,int rating,string userNotes)
        {
            this.userId = userId;
            this.productId = productId;
            this.locationName = locationName;
            this.rating = rating;
            this.userNotes = userNotes;
        }
        public Rating ()
        {

        }
    }

 public class RatingDb:Rating
    {
        public string id { get; set; }
        public string timestamp { get; set; }

        public RatingDb(string id, string userId, string productId, string timestamp,string locationName, int rating, string userNotes): base(userId, productId, locationName, rating, userNotes)
        {
            this.id = id;
            this.timestamp = timestamp;
        }
        public RatingDb():base()
        {

        }

    }



    /*
    {
      "userId": "cc20a6fb-a91f-4192-874d-132493685376",
      "productId": "4c25613a-a3c2-4ef3-8e02-9c335eb23204",
      "locationName": "Sample ice cream shop",
      "rating": 5,
      "userNotes": "I love the subtle notes of orange in this ice cream!"
    }
     */



    public static class Function1
    {
        public static string EndpointUri = "https://step3-cosmosdb.documents.azure.com:443/";
        public static string PrimaryKey = "N2TbALiTMNFEjzJtPtzXgQVFTWTLyw1pAs9tisEwM6P4FSktqhY4oqqSaHqmkAkeU4gdUEyvGhAF9TsGVC2TTQ==";
        public static string databaseId = "hackbase";
        public static string containerId = "ratings";
        private static readonly HttpClient client = new HttpClient();

        public static CosmosClient getConnection(ILogger log)
        {
            int retries = 5;
            CosmosClient cosmo = null;
            do
            {
                try
                {
                    cosmo = new CosmosClient(EndpointUri, PrimaryKey, new CosmosClientOptions() { ApplicationName = "Serverless-OpenHack" });
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Ouch");
                    Task.Delay(500).Wait();
                }
            } while (retries-- > 0);

            return cosmo;
        }

        [FunctionName("CreateRating")]
        public static async Task<IActionResult> CreateRating(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Inserting rating ...1 ");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            log.LogInformation($"Inserting rating ...2  {requestBody}");
            RatingDb data = JsonConvert.DeserializeObject<RatingDb>(requestBody);
            log.LogInformation("Inserting rating ...3 ");
            data.id =   Guid.NewGuid().ToString();
            log.LogInformation("Inserting rating ...4 ");
            data.timestamp = DateTime.Now.ToUniversalTime().ToString();
            log.LogInformation("Inserting rating ...5 ");

            try
            {
                if (checkUserId(data.userId) == false) throw new Exception("User not found");
                if (checkProductId(data.productId) == false) throw new Exception("Product not found");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Not found");
                return new NotFoundObjectResult(ex.Message);
            }

            try
            {
                if (data.rating < 0 || data.rating > 5) throw new Exception("Illegal rating");

                log.LogInformation("Inserting rating ...6 ");
                CosmosClient cosmo = getConnection(log);
                Database database = await cosmo.CreateDatabaseIfNotExistsAsync(databaseId);
                Container container = await database.CreateContainerIfNotExistsAsync(containerId, "/userId");
                ItemResponse<RatingDb> resp = await container.CreateItemAsync<RatingDb>(data);
                
                // validate and check the data
                // insert

            }
            catch (Exception ex)
            {
                log.LogError(ex, "Insert");
                return new BadRequestObjectResult(ex.Message);
            }

            return new OkObjectResult(data);
        }

        public static bool checkUserId(string userId)
        {
            // https://serverlessohapi.azurewebsites.net/api/GetUser?userId=cc20a6fb-a91f-4192-874d-132493685376
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("User-Agent", "openhack client");

            var ret = client.GetStringAsync($"https://serverlessohapi.azurewebsites.net/api/GetUser?userId={userId}").Result;
            return ret.Contains("userName");
        }
        public static bool checkProductId(string productId)
        {
            // https://serverlessohapi.azurewebsites.net/api/GetProduct?productId=75542e38-563f-436f-adeb-f426f1dabb5c
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("User-Agent", "openhack client");

            var ret = client.GetStringAsync($"https://serverlessohapi.azurewebsites.net/api/GetProduct?productId={productId}").Result;
            return ret.Contains("productName");
        }

        // http://localhost:7071/api/GetRating?ratingId=c3b9b8a8-e51f-45ae-b8f4-2c154a94e80e
        // https://petsastep3test.azurewebsites.net/api/Getrating?ratingId=1234
        // http://localhost:7071/api/GetRating?ratingId=123
        [FunctionName("GetRating")]
        public static async Task<IActionResult> GetRating(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Get Rating.");

            string ratingId = req.Query["ratingId"];
            RatingDb data = new RatingDb();

            try
            {
                // get the rating with key
                CosmosClient cosmo = getConnection(log);
                Database database = await cosmo.CreateDatabaseIfNotExistsAsync(databaseId);
                Container container = await database.CreateContainerIfNotExistsAsync(containerId, "/userId");

                var sqlQueryText = $"SELECT * FROM c WHERE c.id = '{ratingId}'";

                QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
                FeedIterator<RatingDb> queryResultSetIterator = container.GetItemQueryIterator<RatingDb>(queryDefinition);

                List<RatingDb> ratings = new List<RatingDb>();

                while (queryResultSetIterator.HasMoreResults)
                {
                    FeedResponse<RatingDb> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                    foreach (RatingDb f in currentResultSet)
                    {
                        ratings.Add(f);
                    }
                }
                data =  ratings[0];
            }            
            catch (Exception ex)
            {
                log.LogError(ex, "Search with key");
                return new NotFoundObjectResult(ex.Message);
            }
            return new OkObjectResult(data);
        }

        // http://localhost:7071/api/GetRatings?userId=cc20a6fb-a91f-4192-874d-132493685376
        // https://petsastep3test.azurewebsites.net/api/Getratings?userId=1234
        // http://localhost:7071/api/GetRatings?userId=123
        [FunctionName("GetRatings")]
        public static async Task<IActionResult> getRatings(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("get Ratings for user");

            // get the all Users ratings from database 
            string userId = req.Query["userId"];
            List<RatingDb> data = new List<RatingDb>();

            try
            {
                CosmosClient cosmo = getConnection(log);
                Database database = await cosmo.CreateDatabaseIfNotExistsAsync(databaseId);
                Container container = await database.CreateContainerIfNotExistsAsync(containerId, "/userId");

                var sqlQueryText = $"SELECT * FROM c WHERE c.userId = '{userId}'";

                QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
                FeedIterator<RatingDb> queryResultSetIterator = container.GetItemQueryIterator<RatingDb>(queryDefinition);

                while (queryResultSetIterator.HasMoreResults)
                {
                    FeedResponse<RatingDb> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                    foreach (RatingDb f in currentResultSet)
                    {
                        data.Add(f);
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Insert");
                return new NotFoundObjectResult(ex.Message);
            }
            return new OkObjectResult(data);
        }


    }

}


