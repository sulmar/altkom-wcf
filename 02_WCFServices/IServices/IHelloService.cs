﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace IServices
{
    [ServiceContract]
    public interface IHelloService
    {
        [OperationContract]
        string Ping(string message);

        [OperationContract]
        void Send(string content);
    }
}
