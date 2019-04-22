using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Configuration;
using System.Linq;
using System.Text;

namespace BookingPlane
{
    internal class Program
    {
        public static Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public static DBContext db;
        public static int code;

        public static Plane GetPlane(string message)
        {
            string[] parts = message.Split();
            code = Convert.ToInt32(parts[0]);
            string[] parts_dateend = parts[3].Split();
            DateTime datebegin = new DateTime(year: Convert.ToInt32(parts_dateend[2]), month: Convert.ToInt32(parts_dateend[1]), day: Convert.ToInt32(parts_dateend[0]));
            return new Plane { DestinatiomCountry = parts[2], HomeCountry = parts[1], FlightDay = datebegin, FlightInformation = "какая-то компания", TicketsCount = 1 };

        }

        public static bool ChechPlane(string HomeCountry, string DestinatiomCountry, DateTime FlightDay)
        {
            foreach (Plane plane in db.Planes.Where(c => c.HomeCountry.Equals(HomeCountry) && c.DestinatiomCountry.Equals(DestinatiomCountry) && c.FlightDay.Equals(FlightDay)))
            {
                logger.Info("Нашел подходящий рейс {0}", plane.FlightInformation);
                if (plane.TicketsCount > 0)
                {
                    logger.Info("Есть свободные билеты");
                    return true;
                }
                else
                {
                    logger.Info("Нет свободных билетов");
                }
            }
            return false;
        }

        public Plane FindVacant(string HomeCountry, string DestinatiomCountry, DateTime FlightDay)
        {
            foreach (Plane plane in db.Planes.Where(c => c.HomeCountry.Equals(HomeCountry) && c.DestinatiomCountry.Equals(DestinatiomCountry) && c.FlightDay.Equals(FlightDay)))
            {
                logger.Info("Нашел подходящий рейс");
                if (plane.TicketsCount > 0)
                {
                    logger.Info("Есть свободные билеты");
                    return plane;
                }
                else
                {
                    logger.Info("Нет свободных билетов");
                }
            }
            return null;
        }

        public static bool IncPlane(string HomeCountry, string DestinatiomCountry, DateTime FlightDay)
        {
            try
            {
                foreach (Plane plane in db.Planes.Where(c => c.HomeCountry.Equals(HomeCountry) && c.DestinatiomCountry.Equals(DestinatiomCountry) && c.FlightDay.Equals(FlightDay)))
                {
                    if (plane.TicketsCount > 0)
                    {
                        logger.Info("Число билетов на рейс инкрементировано");
                        plane.TicketsCount++;
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                logger.Info("Ошибка. Рейс не найден.");
                return false;
            }
            return false;
        }


        public static bool DecPlane(string HomeCountry, string DestinatiomCountry, DateTime FlightDay)
        {
            try
            {
                foreach (Plane plane in db.Planes.Where(c => c.HomeCountry.Equals(HomeCountry) && c.DestinatiomCountry.Equals(DestinatiomCountry) && c.FlightDay.Equals(FlightDay)))
                {
                    if (plane.TicketsCount > 0)
                    {
                        logger.Info("Число билетов на рейс декрементировано");
                        plane.TicketsCount--;
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                logger.Info("Ошибка. Рейс не найден");
                return false;
            }
            return false;
        }

        private static void Main(string[] args)
        {
            db = new DBContext();
            string host_name = ConfigurationManager.AppSettings.Get("host_name");
            string queue_name = ConfigurationManager.AppSettings.Get("queue_name");
            Plane plane = null;

            try
            {
                ConnectionFactory factory = new ConnectionFactory() { HostName = host_name };
                logger.Info("Connection factory was created successfully");
                try
                {
                    using (IConnection connection = factory.CreateConnection())
                    using (IModel channel_for_recive = connection.CreateModel())
                    {
                        logger.Info("Connection and channel was created successfully");
                        channel_for_recive.QueueDeclare(queue: queue_name,
                                             durable: false,
                                             exclusive: false,
                                             autoDelete: false,
                                             arguments: null);

                        channel_for_recive.QueueBind(queue: queue_name, exchange: queue_name, routingKey: "plane");

                        EventingBasicConsumer consumer = new EventingBasicConsumer(channel_for_recive);
                        logger.Info("Consumer was created successfully");
                        consumer.Received += (model, ea) =>
                        {
                            logger.Info("Consumer is recieving message");
                            byte[] body = ea.Body;
                            string message = Encoding.UTF8.GetString(body);

                            plane = GetPlane(message);

                            logger.Info(" [x] Received {0}", message);
                        };
                        channel_for_recive.BasicConsume(queue: queue_name,
                                             autoAck: true,
                                             consumer: consumer);

                        string queue_name_consume = "result";
                        using (IModel channel_to_send = connection.CreateModel())
                        {
                            logger.Info("Connection and channel was created successfully");
                            channel_to_send.QueueDeclare(queue: queue_name_consume,
                                                 durable: false,
                                                 exclusive: false,
                                                 autoDelete: false,
                                                 arguments: null);
                            if (code == 0)
                            {
                                bool result = ChechPlane(plane.HomeCountry, plane.DestinatiomCountry, plane.FlightDay);

                                try
                                {

                                    byte[] body_plane = Encoding.UTF8.GetBytes("plane " + result);
                                    channel_to_send.BasicPublish(exchange: queue_name_consume,
                                                         routingKey: "plane",
                                                         basicProperties: null,
                                                         body: body_plane);
                                    logger.Info(" [x] Sent {0}", "PLANE");

                                }
                                catch (Exception ex1)
                                {
                                    logger.Info("Не удалось отправить результат проверки на наличие. Ошибка - " + ex1.Message);
                                }

                            }

                            if (code == 1)
                            {
                                bool result = DecPlane(plane.HomeCountry, plane.DestinatiomCountry, plane.FlightDay);
                                try
                                {
                                    byte[] body_plane;
                                    if (result)
                                    {
                                        body_plane = Encoding.UTF8.GetBytes("plane booked");
                                    }
                                    else
                                    {
                                        body_plane = Encoding.UTF8.GetBytes("plane not booked");
                                    }
                                    channel_to_send.BasicPublish(exchange: queue_name_consume,
                                                         routingKey: "plane",
                                                         basicProperties: null,
                                                         body: body_plane);
                                    logger.Info(" [x] Sent {0}", "PLANE");

                                }
                                catch (Exception ex1)
                                {
                                    logger.Info("Не удалось забронировать билет. Ошибка - " + ex1.Message);
                                }
                            }
                            if (code == 2)
                            {
                                bool result = IncPlane(plane.HomeCountry, plane.DestinatiomCountry, plane.FlightDay);

                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    logger.Info("Objects wasn't received. Exception: {0}", ex.Message);
                }

            }
            catch (Exception ex)
            {
                logger.Info("Connection factory wasn't initaliaze. Exception: {0}", ex.Message);
            }
        }
    }
}
