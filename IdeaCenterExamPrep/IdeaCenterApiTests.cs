using System;
using System.Net;
using System.Text.Json;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using IdeaCenterExamPrep.Models;

namespace IdeaCenterExamPrep
{
    [TestFixture]
    public class Tests
    {
        private RestClient client;
        private static string? lastCreatedIdeaId;

        private const string baseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:84";

        private static string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiJkYzY1ODQ1Mi1hODEyLTRiYjAtYjE2ZC1mYzIwZTE1NDkxZGUiLCJpYXQiOiIwOC8xMy8yMDI1IDEzOjIxOjQ5IiwiVXNlcklkIjoiZWVkMzc4NzktMzE2OS00MzlmLTZkNTUtMDhkZGQ1YTQxM2E3IiwiRW1haWwiOiJ0ZXN0ZXJAbWFpbC5jb20iLCJVc2VyTmFtZSI6IlRlc3RlcjIiLCJleHAiOjE3NTUxMTI5MDksImlzcyI6IklkZWFDZW50ZXJfQXBwX1NvZnRVbmkiLCJhdWQiOiJJZGVhQ2VudGVyX1dlYkFQSV9Tb2Z0VW5pIn0.4QAOo_uSRVh_RBgvl3cWtHlOReOplQu1Kiy85S0MfuA";

        private static string LoginEmail = "tester@mail.com";
        private static string LoginPassword = "tester1";

        [SetUp]
        public void Setup()
        {
            string JWTtoken;

            if (!string.IsNullOrEmpty(StaticToken))
            {
                JWTtoken = StaticToken;
            }
            else
            {
                JWTtoken = GetToken(LoginEmail, LoginPassword);
            }

            var options = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(JWTtoken),
            };
            this.client = new RestClient(options);
        }

        private string GetToken(string email, string password)
        {
            using var authClient = new RestClient(baseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });
            var response = authClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK && !string.IsNullOrEmpty(response.Content))
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accesstoken").GetString();

                if (string.IsNullOrEmpty(token))
                {
                    throw new InvalidOperationException("Token is null or empty.");
                }
                return token;
            }
            else
            {
                throw new Exception($"Failed to get token: {response.StatusCode} - {response.Content}");
            }
        }

        [Order(1)]
        [Test]
        public void CreateIdea()
        {
            if (client == null)
                throw new InvalidOperationException("REST client not initialized");

            var ideaRequest = new IdeaDTO
            {
                Title = "Test Idea",
                Description = "This is a test idea for the API.",
                Url = string.Empty
            };

            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaRequest);
            var response = client.Execute(request);

            if (response.Content == null)
                throw new InvalidOperationException("Response content is null");

            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(createResponse?.Msg, Is.EqualTo("Successfully created!"));
        }
        [Order(2)]
        [Test]
        public void GetAllIdeas()
        {
            if (client == null)
            {
                throw new InvalidOperationException("REST client not initialized");
            }
            
            var request = new RestRequest("/api/Idea/All", Method.Get);
            var response = client.Execute(request);
            var ResponseItems = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(ResponseItems,Is.Not.Null, "Response should not be null.");
            Assert.That(ResponseItems, Is.Not.Empty);

            lastCreatedIdeaId = ResponseItems.LastOrDefault()?.Id;
        }

        [Order(3)]
        [Test]
        public void EditLastCreatedIdea()
        {
            var EditRequest = new IdeaDTO
            {
                Title = "Updated Test Idea",
                Description = "This is an updated test idea for the API.",
                Url = ""
            };

            var request = new RestRequest($"/api/Idea/Edit", Method.Put);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            request.AddJsonBody(EditRequest);
            var response = client.Execute(request);
            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(editResponse.Msg, Is.EqualTo("Edited successfully"));
        }

        [Order(4)]
        [Test]
        public void DeleteLastCreatedIdea()
        {
            var request = new RestRequest($"/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(response.Content, Does.Contain("The idea is deleted!"));
        }

        [Order(5)]
        [Test]
        public void CreateIdeaWithoutRequiredFields()
        {
            var ideaRequest = new IdeaDTO
            {
                Title = null,
                Description = null,
            };
            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaRequest);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
        }

        [Order(6)]
        [Test]
        public void EditNonExistentIdea()
        {
            var NonExistentIdeaId = "123";
            var EditRequest = new IdeaDTO
            {
                Title = "Non-existent Idea",
                Description = "This idea does not exist.",
                Url = ""
            };
            var request = new RestRequest("/api/Idea/Edit", Method.Put);
            request.AddQueryParameter("ideaId", NonExistentIdeaId);
            request.AddJsonBody(EditRequest);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Not Found.");
            Assert.That(response.Content, Does.Contain("There is no such idea!"));
        }

        [Order(7)]
        [Test]
        public void DeleteNonExistentIdea()
        {
            var NonExistingIdeadID = "123";
            var request = new RestRequest("/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", NonExistingIdeadID);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
            Assert.That(response.Content, Does.Contain("There is no such idea!"));
        }

        [TearDown]
        public void DisposeClient()
        {
            client?.Dispose();
            client = null;
        }
    }
}