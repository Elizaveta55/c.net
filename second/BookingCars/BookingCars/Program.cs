using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Configuration;
using System.Linq;
using System.Text;

namespace BookingCars
{
    internal class Program
    {
        public static Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public static DBContext db;
        public static int code;

        public static Car GetCar(string message)
        {
            string[] parts = message.Split();
            code = Convert.ToInt32(parts[0]);
            string[] parts_datebegin = parts[1].Split();
            DateTime datebegin = new DateTime(year: Convert.ToInt32(parts_datebegin[2]), month: Convert.ToInt32(parts_datebegin[1]), day: Convert.ToInt32(parts_datebegin[0]));
            string[] parts_dateend = parts[2].Split();
            DateTime dateend = new DateTime(year: Convert.ToInt32(parts_dateend[2]), month: Convert.ToInt32(parts_dateend[1]), day: Convert.ToInt32(parts_dateend[0]));
            return new Car { CarCountry = parts[3], DayComing = datebegin, DayClosing = dateend, CarInformation = "sacva", CarsCount = 1, FirmInformation = "best driver" };

        }

        public static bool CheckCar(DateTime DayComing, DateTime DayClosing, string CountryCar)
        {
            foreach (Car car in db.Cars.Where(c => c.DayComing.Equals(DayComing) && c.DayClosing.Equals(DayClosing) && c.CarCountry.Equals(CountryCar)))
            {
                logger.Info("Нашел подходящий автомаобиль {0}", car.CarInformation);
                if (car.CarsCount > 0)
                {
                    logger.Info("Есть свободные автомаобили");
                    return true;
                }
                else
                {
                    logger.Info("Нет свободных автомаобилей");
                }
            }
            return false;
        }

        public Car FindVacant(DateTime DayComing, DateTime DayClosing, string CountryCar)
        {
            foreach (Car car in db.Cars.Where(c => c.DayComing.Equals(DayComing) && c.DayClosing.Equals(DayClosing) && c.CarCountry.Equals(CountryCar)))
            {
                logger.Info("Нашел подходящий автомаобиль");
                if (car.CarsCount > 0)
                {
                    logger.Info("Есть свободные автомаобили");
                    return car;
                }
                else
                {
                    logger.Info("Нет свободных автомаобилей");
                }
            }
            return null;
        }

        public static bool IncCar(DateTime DayComing, DateTime DayClosing, string CountryCar)
        {
            try
            {
                foreach (Car car in db.Cars.Where(c => c.DayComing.Equals(DayComing) && c.DayClosing.Equals(DayClosing) && c.CarCountry.Equals(CountryCar)))
                {
                    if (car.CarsCount > 0)
                    {
                        logger.Info("Число автомаобилей инкрементировано");
                        car.CarsCount++;
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                logger.Info("Ошибка. Автомаобиль не найден.");
                return false;
            }
            return false;
        }


        public static bool DecCar(DateTime DayComing, DateTime DayClosing, string CountryCar)
        {
            try
            {
                foreach (Car car in db.Cars.Where(c => c.DayComing.Equals(DayComing) && c.DayClosing.Equals(DayClosing) && c.CarCountry.Equals(CountryCar)))
                {
                    if (car.CarsCount > 0)
                    {
                        logger.Info("Число автомаобилей инкрементировано");
                        car.CarsCount--;
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                logger.Info("Ошибка. Автомаобиль не найден.");
                return false;
            }
            return false;
        }

        private static void Main(string[] args)
        {
            db = new DBContext();
            string host_name = ConfigurationManager.AppSettings.Get("host_name");
            string queue_name = ConfigurationManager.AppSettings.Get("queue_name");
            Car car = null;

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

                        channel_for_recive.QueueBind(queue: queue_name, exchange: queue_name, routingKey: "car");

                        EventingBasicConsumer consumer = new EventingBasicConsumer(channel_for_recive);
                        logger.Info("Consumer was created successfully");
                        consumer.Received += (model, ea) =>
                        {
                            logger.Info("Consumer is recieving message");
                            byte[] body = ea.Body;
                            string message = Encoding.UTF8.GetString(body);

                            car = GetCar(message);

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
                                bool result = CheckCar(car.DayComing, car.DayClosing, car.CarCountry);
                                try
                                {

                                    byte[] body_car = Encoding.UTF8.GetBytes("car " + result);
                                    channel_to_send.BasicPublish(exchange: queue_name_consume,
                                                         routingKey: "car",
                                                         basicProperties: null,
                                                         body: body_car);
                                    logger.Info(" [x] Sent {0}", "CAR");

                                }
                                catch (Exception ex1)
                                {
                                    logger.Info("Не удалось отправить результат проверки на наличие. Ошибка - " + ex1.Message);
                                }

                            }

                            if (code == 1)
                            {
                                bool result = DecCar(car.DayComing, car.DayClosing, car.CarCountry);
                                try
                                {
                                    byte[] body_car;
                                    if (result)
                                    {
                                        body_car = Encoding.UTF8.GetBytes("car booked");
                                    }
                                    else
                                    {
                                        body_car = Encoding.UTF8.GetBytes("car not booked");
                                    }
                                    channel_to_send.BasicPublish(exchange: queue_name_consume,
                                                         routingKey: "car",
                                                         basicProperties: null,
                                                         body: body_car);
                                    logger.Info(" [x] Sent {0}", "CAR");

                                }
                                catch (Exception ex1)
                                {
                                    logger.Info("Не удалось забронировать car. Ошибка - " + ex1.Message);
                                }
                            }
                            if (code == 2)
                            {
                                bool result = IncCar(car.DayComing, car.DayClosing, car.CarCountry);

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
