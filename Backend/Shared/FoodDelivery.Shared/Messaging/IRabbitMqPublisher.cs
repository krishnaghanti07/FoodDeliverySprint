using System;
using System.Collections.Generic;
using System.Text;
namespace FoodDelivery.Shared.Messaging;

public interface IRabbitMqPublisher
{
    void Publish<T>(T message, string queueName) where T : class;
}
