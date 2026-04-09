using System;
using System.Net;
using System.Text.Json;
using RestSharp;
using RestSharp.Authenticators;




namespace ExamPrepIdeaCenter

{
    [TestFixture]
    public class Tests
    {
        private RestClient client;
        private static string createdFoodId;

        private const string BaseUrl = "http://144.91.123.158:81";
        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiIzOTE4YzhiYy05ZjRjLTQ4MTQtYWVjNC05Njc3OTZlMzk3NmIiLCJpYXQiOiIwNC8wOS8yMDI2IDE1OjU3OjQ3IiwiVXNlcklkIjoiYTU3ZTkwOGItY2JlYi00NjQwLTc0NDMtMDhkZTc2OGU4Zjk3IiwiRW1haWwiOiJzb2Z0dW5pQHNvZnR1bmkuY29tIiwiVXNlck5hbWUiOiJzb2Z0dW5pIiwiZXhwIjoxNzc1NzcxODY3LCJpc3MiOiJGb29keV9BcHBfU29mdFVuaSIsImF1ZCI6IkZvb2R5X1dlYkFQSV9Tb2Z0VW5pIn0.uRbTSgrwPSY-2R6-8i536sEWPA4QN3W038N719TSC4w";
        private const string LoginUserName = "softuni";
        private const string LoginPassword = "123456";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;

            if (!string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
            }
            else
            {
                jwtToken = GetJwtToken(LoginUserName, LoginPassword);
            }

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            this.client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            var tempClient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { username, password });

            var response = tempClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Token not found in the response.");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Response: {response.Content}");
            }
        }

        [Test, Order(1)]
        public void CreateFood_ShouldReturnSuccess()
        {

            var food = new
            {
                name = "Test Food",
                description = "This is a test food item.",
                url = ""
            };
            var request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(food);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response.Content);
            createdFoodId = jsonResponse.GetProperty("foodId").GetString();
        }

        [Test, Order(2)]
        public void EditFoodTitle_ShouldReturnOk()
        {
            var changes = new[]
            {
                new {path = "/name", op = "replace", value = "Updated Food Name"}
            };

            var request = new RestRequest($"/api/Food/Edit/{createdFoodId}", Method.Patch);

            request.AddJsonBody(changes);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("Successfully edited"));
        }

        [Test, Order(3)]
        public void GetAllFoods_ShouldReturnOk()
        {
            var request = new RestRequest("/api/Food/All", Method.Get);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var foods = JsonSerializer.Deserialize<List<object>>(response.Content);
            Assert.That(foods, Is.Not.Empty);
        }

        [Test, Order(4)]
        public void DeleteFood_ShouldReturnOk()
        {
            var request = new RestRequest($"/api/Food/Delete/{createdFoodId}", Method.Delete);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        }

        [Test, Order(5)]
        public void CreateFoodWithoutRequiredFields_ShouldReturnBadRequest()
        {
            var food = new
            {
                description = "Missing name field",
                url = ""
            };
            var request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(food);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test, Order(6)]
        public void EditNonExistingFood_ShouldReturnNotFound()
        {
            string fakeId = "9999";
            var changes = new[]
            {
                new {path = "/name", op = "replace", value = "Non-existing Food"}
            };
            var request = new RestRequest("/api/Food/Edit/{fakeId}", Method.Patch);
            request.AddJsonBody(changes);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(response.Content, Does.Contain("No food revues..."));
        }

        [Test, Order(7)]
        public void DeleteNonExistingFood_ShouldReturnNotFound()
        {
            string fakeId = "9999";
            var request = new RestRequest($"/api/Food/Delete/{fakeId}", Method.Delete);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(response.Content, Does.Contain("No food revues..."));
        }






        [OneTimeTearDown]
        public void TearDown()
        {
            this.client?.Dispose();
        }
    }
}