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
            log.LogInformation("Inserting rating ...");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Rating data = JsonConvert.DeserializeObject<Rating>(requestBody);
            //data.id = "";
            //data.timestamp = "";

            try
            {
                /*
                CosmosClient cosmo = getConnection(log);
                Database database = await cosmo.CreateDatabaseIfNotExistsAsync(databaseId);
                Container container = await database.CreateContainerIfNotExistsAsync(containerId, "/userId");
                ItemResponse<Rating> resp = await container.CreateItemAsync<Rating>(data);
                */
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
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Insert");
                return new BadRequestObjectResult(ex.Message);
            }
            return new OkObjectResult(data);
        }

        // https://petsastep3test.azurewebsites.net/api/Getratings?userId=1234
        // http://localhost:7071/api/GetRatings?userId=123
        [FunctionName("GetRatings")]
        public static async Task<IActionResult> getRatings(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("get Ratings for user");

            // get the all Users from database 
            List<RatingDb> data = new List<RatingDb>();

            try
            {
                data.Add(new RatingDb());
                data.Add(new RatingDb());
                // get the rating with key
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Insert");
                return new BadRequestObjectResult(ex.Message);
            }
            return new OkObjectResult(data);
        }


    }

}


