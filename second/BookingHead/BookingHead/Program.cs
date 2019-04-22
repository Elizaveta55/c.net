using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;

namespace BookingHead
{
    internal class Program
    {

        private static void Main(string[] args)
        {
           
            NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
            bool isPlane = true;
            bool isHotel = true;
            bool isCar = true;
            bool isPlaneBooked = true;
            bool isHotelBooked = true;
            bool isCarBooked = true;

            string host_name = "localhost";
            string queue_name = "head";
            logger.Info("Я родился.");

            try
            {
                string request = null;

                ConnectionFactory factory = new ConnectionFactory()
                {
                    HostName = host_name,
                };
                logger.Info("1 Connection factory was created successfully");
                try
                {
                    using (IConnection connection = factory.CreateConnection())
                    using (IModel channel = connection.CreateModel())
                    {
                        logger.Info("1 Connection and channel was created successfully");
                        channel.QueueDeclare(queue: queue_name,
                                             durable: false,
                                             exclusive: false,
                                             autoDelete: false,
                                             arguments: null);

                        channel.QueueBind(queue: queue_name, exchange: queue_name, routingKey: queue_name);

                        EventingBasicConsumer consumer = new EventingBasicConsumer(channel);
                        logger.Info("Consumer was created successfully");
                        consumer.Received += (model, ea) =>
                        {
                            logger.Info("Consumer is recieving message");
                            byte[] body = ea.Body;
                            request = Encoding.UTF8.GetString(body);
                            logger.Info(" [x] Received {0}", request);
                        };
                        channel.BasicConsume(queue: queue_name,
                                             autoAck: true,
                                             consumer: consumer);
                    }
                }
                catch (Exception ex)
                {
                    logger.Info("Objects wasn't received. Exception: {0}", ex.Message);
                }

                try
                {
                    string queue_name_recive = "result";
                    using (IConnection connection = factory.CreateConnection())
                    using (IModel channel_recive_result = connection.CreateModel())
                    using (IModel channel_send_to_check = connection.CreateModel())
                    {
                        logger.Info("Connection and channel was created successfully");
                        channel_recive_result.QueueDeclare(queue: queue_name_recive,
                                             durable: false,
                                             exclusive: false,
                                             autoDelete: false,
                                             arguments: null);

                        channel_recive_result.QueueBind(queue: queue_name_recive, exchange: queue_name_recive, routingKey: queue_name_recive);

                        EventingBasicConsumer consumer = new EventingBasicConsumer(channel_recive_result);
                        logger.Info("Consumer was created successfully");
                        consumer.Received += (model, ea) =>
                        {
                            logger.Info("Consumer is recieving message");
                            byte[] body = ea.Body;
                            string message = Encoding.UTF8.GetString(body);
                            logger.Info(" [x] Received {0}", message);
                            if (message.Equals("plane false")) isPlane = false;
                            if (message.Equals("hotel false")) isHotel = false;
                            if (message.Equals("car false")) isCar = false;
                            if (message.Equals("plane not booked")) isPlaneBooked = false;
                            if (message.Equals("hotel not booked")) isHotelBooked = false;
                            if (message.Equals("car not booked")) isCarBooked = false;
                        };
                        channel_recive_result.BasicConsume(queue: queue_name_recive,
                                             autoAck: true,
                                             consumer: consumer);


                        channel_send_to_check.QueueDeclare(queue: queue_name,
                                             durable: false,
                                             exclusive: false,
                                             autoDelete: false,
                                             arguments: null);
                        try
                        {
                            logger.Info("Проверим наличие свободных билетов на самолет  в указанные даты");

                            byte[] body_plane = Encoding.UTF8.GetBytes("0 Россия Испания 20.04.2019");
                            channel_send_to_check.BasicPublish(exchange: queue_name,
                                                 routingKey: "plane",
                                                 basicProperties: null,
                                                 body: body_plane);
                            logger.Info(" [x] Sent {0}", "PLANE");

                            if (isPlane)
                            {
                                logger.Info("Проверим наличие свободных номеров  в указанные даты");

                                byte[] body_hotel = Encoding.UTF8.GetBytes("0 20.04.2019 24.04.2019 2 Испания");
                                channel_send_to_check.BasicPublish(exchange: queue_name,
                                                     routingKey: "hotel",
                                                     basicProperties: null,
                                                     body: body_hotel);
                                logger.Info(" [x] Sent {0}", "HOTEL");

                                if (isHotel)
                                {
                                    logger.Info("Проверим наличие свободных cars  в указанные даты");

                                    byte[] body_car = Encoding.UTF8.GetBytes("0 20.04.2019 24.04.2019 Испания");
                                    channel_send_to_check.BasicPublish(exchange: queue_name,
                                                         routingKey: "car",
                                                         basicProperties: null,
                                                         body: body_car);
                                    logger.Info(" [x] Sent {0}", "CARS");

                                    if (isCar)
                                    {
                                        logger.Info("Все доступно. Бронируем.");

                                        byte[] body_plane_booking = Encoding.UTF8.GetBytes("1 Россия Испания 20.04.2019");
                                        channel_send_to_check.BasicPublish(exchange: queue_name,
                                                             routingKey: "plane",
                                                             basicProperties: null,
                                                             body: body_plane_booking);
                                        logger.Info(" [x] Sent {0}", "PLANE");

                                        if (isPlaneBooked)
                                        {
                                            byte[] body_hotel_booking = Encoding.UTF8.GetBytes("1 20.04.2019 24.04.2019 2 Испания");
                                            channel_send_to_check.BasicPublish(exchange: queue_name,
                                                                 routingKey: "hotel",
                                                                 basicProperties: null,
                                                                 body: body_hotel_booking);
                                            logger.Info(" [x] Sent {0}", "HOTEL");

                                            if (isHotelBooked)
                                            {

                                                byte[] body_car_booking = Encoding.UTF8.GetBytes("1 20.04.2019 24.04.2019 Испания");
                                                channel_send_to_check.BasicPublish(exchange: queue_name,
                                                                     routingKey: "car",
                                                                     basicProperties: null,
                                                                     body: body_car_booking);
                                                logger.Info(" [x] Sent {0}", "CARS");

                                                if (isCarBooked)
                                                {
                                                    logger.Info("Все успешно забронировано.");
                                                }
                                                else
                                                {
                                                    logger.Info("Сбой при бронировании машины. Бронирование самолета и отеля отменяется.");
                                                    byte[] body_plane_stop_booking = Encoding.UTF8.GetBytes("2 Россия Испания 20.04.2019");
                                                    channel_send_to_check.BasicPublish(exchange: queue_name,
                                                                         routingKey: "plane",
                                                                         basicProperties: null,
                                                                         body: body_plane_stop_booking);
                                                    logger.Info(" [x] Sent {0}", "PLANE");

                                                    byte[] body_hotel_stop_booking = Encoding.UTF8.GetBytes("2 20.04.2019 24.04.2019 2 Испания");
                                                    channel_send_to_check.BasicPublish(exchange: queue_name,
                                                                         routingKey: "hotel",
                                                                         basicProperties: null,
                                                                         body: body_hotel_stop_booking);
                                                    logger.Info(" [x] Sent {0}", "HOTEL");

                                                }
                                            }
                                            else
                                            {
                                                logger.Info("Сбой при бронировании отеля. Бронирование самолета отменяется.");
                                                byte[] body_plane_stop_booking = Encoding.UTF8.GetBytes("2 Россия Испания 20.04.2019");
                                                channel_send_to_check.BasicPublish(exchange: queue_name,
                                                                     routingKey: "plane",
                                                                     basicProperties: null,
                                                                     body: body_plane_stop_booking);
                                                logger.Info(" [x] Sent {0}", "PLANE");
                                            }

                                        }
                                        else
                                        {
                                            logger.Info("Сбой при бронировании самолета.");
                                        }


                                    }
                                    else
                                    {
                                        logger.Info("Нет свободных машин.");
                                    }

                                }
                                else
                                {
                                    logger.Info("Нет свободных номеров.");
                                }
                            }
                            else
                            {
                                logger.Info("Нет доступных билетов на самолет.");
                            }

                        }
                        catch (Exception ex1)
                        {
                            logger.Info("Не удалось осуществить проверку на наличие и бронирование. Ошибка - " + ex1.Message);
                        }
                    }

                }
                catch (Exception ex)
                {
                    logger.Info("Objects wasn't received. Exception: {0}", ex.Message);
                }


            }
            catch (Exception ex2)
            {
                logger.Info("Connection factory wasn't initaliaze. Exception: {0}", ex2.Message);
            }
            Console.ReadKey();
        }
    }
}
