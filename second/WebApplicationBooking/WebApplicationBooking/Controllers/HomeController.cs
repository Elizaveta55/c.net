using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using WebApplicationBooking.Models;

namespace WebApplicationBooking.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        
        [HttpPost]
        public IActionResult About(Request model)
        {
            String queue_name = "head";
            ConnectionFactory factory = new ConnectionFactory() { HostName = "localhost"};
            using (IConnection connection = factory.CreateConnection())
            using (IModel channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: queue_name,
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);
                
                try
                {
                    byte[] body_plane = Encoding.UTF8.GetBytes(model.ToString());
                    channel.BasicPublish(exchange: queue_name,
                                         routingKey: queue_name,
                                         basicProperties: null,
                                         body: body_plane);
                    //logger.Info(" [x] Sent {0}", "PLANE");
                }
                catch (Exception ex)
                {
                    //logger.Info("Bad bad bad - " + ex.Message);
                }

            }
            return View("Index");
        }
        

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
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
