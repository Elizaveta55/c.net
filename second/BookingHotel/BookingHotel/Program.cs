using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Configuration;
using System.Linq;
using System.Text;

namespace BookingHotel
{
    internal class Program
    {
        public static Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public static DBContext db;

        public static int code;

        public static Hotel GetHotel(string message)
        {
            string[] parts = message.Split();
            code = Convert.ToInt32(parts[0]);
            string[] parts_datebegin = parts[1].Split();
            DateTime datebegin = new DateTime(year: Convert.ToInt32(parts_datebegin[2]), month: Convert.ToInt32(parts_datebegin[1]), day: Convert.ToInt32(parts_datebegin[0]));
            string[] parts_dateend = parts[2].Split();
            DateTime dateend = new DateTime(year: Convert.ToInt32(parts_dateend[2]), month: Convert.ToInt32(parts_dateend[1]), day: Convert.ToInt32(parts_dateend[0]));
            return new Hotel { DayComing = datebegin, DayClosing = dateend, HotelCountry = parts[4], PersonsCount = Convert.ToInt32(parts[3]), RoomsCount = 1 };
        }

        public static bool CheckHotel(DateTime DayComing, DateTime DayClosing, int PersonsCount, string CountryHotel)
        {
            foreach (Hotel hotel in db.Hotels.Where(c => c.DayComing.Equals(DayComing) && c.DayClosing.Equals(DayClosing) && c.PersonsCount.Equals(PersonsCount) && c.HotelCountry.Equals(CountryHotel)))
            {
                logger.Info("Нашел подходящий номер {0}", hotel.HotelInformation);
                if (hotel.RoomsCount > 0)
                {
                    logger.Info("Есть свободные номера");
                    return true;
                }
                else
                {
                    logger.Info("Нет свободных номеров");
                }
            }
            return false;
        }

        public static Hotel FindVacant(DateTime DayComing, DateTime DayClosing, int PersonsCount, string CountryHotel)
        {
            foreach (Hotel hotel in db.Hotels.Where(c => c.DayComing.Equals(DayComing) && c.DayClosing.Equals(DayClosing) && c.PersonsCount.Equals(PersonsCount) && c.HotelCountry.Equals(CountryHotel)))
            {
                logger.Info("Нашел подходящий отель");
                if (hotel.RoomsCount > 0)
                {
                    logger.Info("Есть свободные номеры");
                    return hotel;
                }
                else
                {
                    logger.Info("Нет свободных номеров");
                }
            }
            return null;
        }

        public static bool IncHotel(DateTime DayComing, DateTime DayClosing, int PersonsCount, string CountryHotel)
        {
            try
            {
                foreach (Hotel hotel in db.Hotels.Where(c => c.DayComing.Equals(DayComing) && c.DayClosing.Equals(DayClosing) && c.PersonsCount.Equals(PersonsCount) && c.HotelCountry.Equals(CountryHotel)))
                {
                    if (hotel.RoomsCount > 0)
                    {
                        logger.Info("Число номеров в гостинице инкрементировано");
                        hotel.RoomsCount++;
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                logger.Info("Ошибка. Номер не найден.");
                return false;
            }
            return false;
        }


        public static bool DecHotel(DateTime DayComing, DateTime DayClosing, int PersonsCount, string CountryHotel)
        {
            try
            {
                foreach (Hotel hotel in db.Hotels.Where(c => c.DayComing.Equals(DayComing) && c.DayClosing.Equals(DayClosing) && c.PersonsCount.Equals(PersonsCount) && c.HotelCountry.Equals(CountryHotel)))
                {
                    if (hotel.RoomsCount > 0)
                    {
                        logger.Info("Число номеров в гостинице декрементировано");
                        hotel.RoomsCount--;
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                logger.Info("Ошибка. Номер не найден");
                return false;
            }
            return false;
        }

        private static void Main(string[] args)
        {

            db = new DBContext();
            string host_name = ConfigurationManager.AppSettings.Get("host_name");
            string queue_name = ConfigurationManager.AppSettings.Get("queue_name");
            Hotel hotel = null;

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

                        channel_for_recive.QueueBind(queue: queue_name, exchange: queue_name, routingKey: "hotel");

                        EventingBasicConsumer consumer = new EventingBasicConsumer(channel_for_recive);
                        logger.Info("Consumer was created successfully");
                        consumer.Received += (model, ea) =>
                        {
                            logger.Info("Consumer is recieving message");
                            byte[] body = ea.Body;
                            string message = Encoding.UTF8.GetString(body);

                            hotel = GetHotel(message);

                            logger.Info(" [x] Received {0}", message);
                        };
                        channel_for_recive.BasicConsume(queue: queue_name,
                                             autoAck: true,
                                             consumer: consumer);

                        string queue_name_consume = "result";
                        using (IModel channel = connection.CreateModel())
                        {
                            logger.Info("Connection and channel was created successfully");
                            channel.QueueDeclare(queue: queue_name_consume,
                                                 durable: false,
                                                 exclusive: false,
                                                 autoDelete: false,
                                                 arguments: null);
                            if (code == 0)
                            {
                                bool result = CheckHotel(hotel.DayComing, hotel.DayClosing, hotel.PersonsCount, hotel.HotelCountry);

                                try
                                {

                                    byte[] body_hotel = Encoding.UTF8.GetBytes("hotel " + result);
                                    channel.BasicPublish(exchange: queue_name_consume,
                                                         routingKey: "hotel",
                                                         basicProperties: null,
                                                         body: body_hotel);
                                    logger.Info(" [x] Sent {0}", "HOTEL");

                                }
                                catch (Exception ex1)
                                {
                                    logger.Info("Не удалось отправить результат проверки на наличие. Ошибка - " + ex1.Message);
                                }
                            }
                            if (code == 1)
                            {
                                bool result = DecHotel(hotel.DayComing, hotel.DayClosing, hotel.PersonsCount, hotel.HotelCountry);
                                try
                                {

                                    byte[] body_hotel;
                                    if (result)
                                    {
                                        body_hotel = Encoding.UTF8.GetBytes("hotel booked");
                                    }
                                    else
                                    {
                                        body_hotel = Encoding.UTF8.GetBytes("hotel not booked");
                                    }
                                    channel.BasicPublish(exchange: queue_name_consume,
                                                         routingKey: "hotel",
                                                         basicProperties: null,
                                                         body: body_hotel);
                                    logger.Info(" [x] Sent {0}", "HOTEL");

                                }
                                catch (Exception ex1)
                                {
                                    logger.Info("Не удалось забронировать номер. Ошибка - " + ex1.Message);
                                }
                            }
                            if (code == 2)
                            {
                                bool result = IncHotel(hotel.DayComing, hotel.DayClosing, hotel.PersonsCount, hotel.HotelCountry);

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
