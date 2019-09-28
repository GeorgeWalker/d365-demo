using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using dynamics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using portal.Models;

namespace portal.Controllers
{
    public class ComplaintModel
    {
        public string test_summary { get; set; }
        public string test_description { get; set; }
    }
    public class HomeController : Controller
    {
        private readonly IConfiguration Configuration;
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;


        public HomeController(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            Configuration = configuration;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger(typeof(HomeController));
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(FeedbackViewModel feedback)
        {
            _logger.LogError("Testing Dynamics");
            
            var client = DynamicsClient.GenerateClient(Configuration);

            string url = "/api/data/v9.0/test_complaints";

            HttpRequestMessage _httpRequest = new HttpRequestMessage(HttpMethod.Post, url);

            ComplaintModel complaint = new ComplaintModel()
            {
                test_description = feedback.description,
                test_summary = feedback.summary
            };

            string jsonString = JsonConvert.SerializeObject(complaint);

            _httpRequest.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            _httpRequest.Headers.Add("PREFER", "return=representation");

            var _httpResponse2 = await client.SendAsync(_httpRequest);

            HttpStatusCode _statusCode = _httpResponse2.StatusCode;
            

            var _responseContent2 = await _httpResponse2.Content.ReadAsStringAsync();



            return Ok("Result:\n" + _responseContent2);
            
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
