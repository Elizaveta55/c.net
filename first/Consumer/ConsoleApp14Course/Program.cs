using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Configuration;
using System.Text;

namespace ConsoleApp14Course
{

    class Program
    {
        public static Random random = new Random();
        static public Requests[] requests;

        static public string GetRandomString(int x)
        {
            string pass = "";
            while (pass.Length < x)
            {
                Char c = (char)random.Next(65, 91);
                if (Char.IsLetterOrDigit(c))
                    pass += c;
            }
            return pass;
        }

        static public Requests[] Create_requests()
        {
            string i_itarable_string = ConfigurationManager.AppSettings.Get("i_itarable");
            int i_itarable = Convert.ToInt32(i_itarable_string);
            requests = new Requests[i_itarable];

            for (int i = 0; i < i_itarable; i++)
            {
                Requests re1 = new Requests {Name = GetRandomString(20), Lastname = GetRandomString(20), Midname = GetRandomString(20), Gender = random.Next(1, 3), RequestNumber = random.Next(500), RequestDate = new DateTime(2019, 02, 02), Xml = null };
                requests[i] = re1;
            }
            return requests;
        }



        static void Main(string[] args)
        {
            string host_name = ConfigurationManager.AppSettings.Get("host_name");
            string queue_name = ConfigurationManager.AppSettings.Get("queue_name");

            var logger = NLog.LogManager.GetCurrentClassLogger();

            requests = Create_requests();
            foreach (Requests re in requests)
            {
                var factory = new ConnectionFactory() { HostName = host_name };

                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: queue_name,
                                         durable: false,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);
                    try
                    {
                        string request_json = JsonConvert.SerializeObject(re);
                        logger.Info("Object was serialized to JSON successfully");
                        var body = Encoding.UTF8.GetBytes(request_json);

                        channel.BasicPublish(exchange: "",
                                             routingKey: queue_name,
                                             basicProperties: null,
                                             body: body);
                        logger.Info(" [x] Sent {0}", request_json);
                    }
                    catch (Exception ex)
                    {
                        logger.Info("Object wasn't serialized to JSON successfully. Exception - " + ex.Message);
                    }

                }
            }

            Console.ReadLine();

        }

    }
}
