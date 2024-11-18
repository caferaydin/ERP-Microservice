using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.Base
{
   public enum EventBusType
    {
        RabbitMQ = 0,
        AzureService = 1,
    }
}
