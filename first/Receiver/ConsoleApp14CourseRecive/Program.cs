using Newtonsoft.Json;
using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ConsoleApp14CourseRecive
{
    internal static class Program
    {
        public static Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public static DBContext db = new DBContext();
        public static LinkedList<string> list_hash = new LinkedList<string>();

        public static string GetHash(string input)
        {
            MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));

            return Convert.ToBase64String(hash);
        }

        public static string Serialize<T>(this T toSerialize)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            using (StringWriter textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, toSerialize);
                return textWriter.ToString();
            }
        }

        public static requests DeserializationFromJSON(string re_string)
        {
            try
            {
                requests re = JsonConvert.DeserializeObject<requests>(re_string);
                re.Xml = Serialize(re);
                logger.Info("Object " + re.RequestNumber + " was deserialized from JSON successfully");
                return re;
            }
            catch (Exception ex)
            {
                logger.Info("Object wasn't deserialized from JSON.. Exception: {0}", ex.Message);
                return null;
            }
        }

        public static async void AddToDB(requests re)
        {
            await Task.Delay(3000);
            LinkedList<string> list_hash = new LinkedList<string>();
            DBContext temp_db = new DBContext();
            if (re.RequestNumber % 2 == 0)
            {
                try
                {

                    temp_db.Requests.Add(re);
                    logger.Info("Even object {0} was added to DB successfully", re.RequestNumber);
                }
                catch (Exception ex)
                {
                    logger.Info("Even object {0} wasn't added to DB. Exception: {1}", re.RequestNumber, ex.Message);
                }
            }
            else
            {
                if (list_hash.Count == 0)
                {
                    try
                    {
                        list_hash.AddLast(GetHash(re.RequestNumber.ToString()));

                        temp_db.Requests.Add(re);
                        logger.Info("Unique odd object {0} was added to DB successfully.", re.RequestNumber);
                    }
                    catch (Exception ex)
                    {
                        logger.Info("Unique odd object {0} wasn't added to DB. Exception: {1}", re.RequestNumber, ex.Message);
                    }

                }
                else
                {
                    if (!list_hash.Contains(GetHash(re.RequestNumber.ToString())))
                    {
                        list_hash.AddLast(GetHash(re.RequestNumber.ToString()));
                        temp_db.Requests.Add(re);
                        logger.Info("Unique odd object {0} was added to DB", re.RequestNumber);
                    }
                    else
                    {
                        logger.Info("Not unique odd object {0} wasn't added to DB", re.RequestNumber);
                    }
                }
            }
            await temp_db.SaveChangesAsync();
        }


        public static void Deser(string re_string)
        {
            requests re = DeserializationFromJSON(re_string);
            AddToDB(re);
        }

        public static void InitializeDB()
        {
            try
            {
                db.Database.Initialize(force: false);
                logger.Info("Initialization was done successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                logger.Info("Initialization wasn't done successfully. Exception: {0}", ex.Message);
                Console.ReadLine();
            }
        }

        private static void Main(string[] args)
        {
            logger.Info("Initialization of DB");
            InitializeDB();

            string host_name = ConfigurationManager.AppSettings.Get("host_name");
            string queue_name = ConfigurationManager.AppSettings.Get("queue_name");

            try
            {
                ConnectionFactory factory = new ConnectionFactory() { HostName = host_name };
                logger.Info("Connection factory was created successfully");
                string re_string = null;
                List<string> res = new List<string>();

                try
                {
                    using (IConnection connection = factory.CreateConnection())
                    using (IModel channel = connection.CreateModel())
                    {
                        logger.Info("Connection and channel was created successfully");
                        channel.QueueDeclare(queue: queue_name,
                                             durable: false,
                                             exclusive: false,
                                             autoDelete: false,
                                             arguments: null);

                        EventingBasicConsumer consumer = new EventingBasicConsumer(channel);
                        logger.Info("Consumer was created successfully");
                        consumer.Received += (model, ea) =>
                        {
                            logger.Info("Consumer is recieving message");
                            byte[] body = ea.Body;
                            string message = Encoding.UTF8.GetString(body);
                            re_string = message;
                            res.Add(re_string);
                            logger.Info(" [x] Received {0}", message);
                        };
                        channel.BasicConsume(queue: queue_name,
                                             autoAck: true,
                                             consumer: consumer);
                        Thread.Sleep(10000);
                    }
                }
                catch (Exception ex)
                {
                    logger.Info("Objects wasn't received. Exception: {0}", ex.Message);
                }


                foreach (string res_string in res)
                    Task.Run(() => Deser(res_string));
            }
            catch (Exception ex)
            {
                logger.Info("Connection factory wasn't initaliaze. Exception: {0}", ex.Message);
            }

            Console.WriteLine("Нажми Enter для выхода");
            Console.ReadLine();

        }
    }
}
