## Remoting Service

1. Utwórz projekt _IServices_ i dodaj interfejs 

~~~ csharp
public interface IHelloRemotingService
{
    string Ping();
    string Ping(string message);
}
~~~

2. Utwórz projekt _RemotingServices_ i dodaj implementację
~~~ csharp
public class HelloRemotingService : MarshalByRefObject, IHelloRemotingService
{
     public string Ping()
    {
        return "Pong";
    }

    public string Ping(string message)
    {
        return message;
    }
}
~~~

3. Utwórz projekt _RemotingServiceHost_

~~~ csharp
static void Main(string[] args)
{
    // add reference System.Runtime.Remoting
    int port = 8080;

    RemotingServices.HelloRemotingService remotingService = new RemotingServices.HelloRemotingService();
    TcpChannel channel = new TcpChannel(port);
    ChannelServices.RegisterChannel(channel);
    RemotingConfiguration.RegisterWellKnownServiceType(typeof(RemotingServices.HelloRemotingService), "Ping", WellKnownObjectMode.Singleton);

    Console.WriteLine($"Remoting service started on {port}");
    Console.ReadLine();
}
~~~


4. Utwórz projekt _HelloRemotingServiceConsoleClient_

~~~ csharp
private static void PingTest()
{
    IServices.IHelloRemotingService client;

    TcpChannel channel = new TcpChannel();
    ChannelServices.RegisterChannel(channel);

    client = (IServices.IHelloRemotingService)Activator.GetObject(typeof(IServices.IHelloRemotingService), "tcp://localhost:8080/Ping");

    while (true)
    {
        Console.WriteLine("Ping");
        string response = client.Ping("Pong");

        Console.WriteLine(response);

        Thread.Sleep(TimeSpan.FromSeconds(2));
    }
}
~~~

## WCF Service

1. Utwórz projekt _HelloService_

- Utwórz interfejs 
~~~ csharp
 // add reference System.ServiceModel
[ServiceContract]
public interface IHelloService
{
    [OperationContract]
    string Ping(string message);
}
~~~

- Utwórz implementację

~~~ csharp
public class HelloService : IHelloService
{
    public string Ping(string message)
    {
        return message;
    }
}
~~~

### Self-Hosting

1. Utwórz projekt _HelloServiceHost_

~~~ csharp
 static void Main(string[] args)
{
    using(ServiceHost host = new ServiceHost(typeof(HelloService.HelloService)))
    {
        host.Open();
        Console.WriteLine("Host started on");

        foreach (var uri in host.BaseAddresses)
        {
            Console.WriteLine(uri);

        }

        Console.ReadLine();
    }
}
~~~

1. Dodaj konfigurację w pliku _app.config_

~~~ xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
    <startup> 
        <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.7.2" />
    </startup>


  <system.serviceModel>
    <services>
      <service name="HelloService.HelloService" behaviorConfiguration="mexBehavior">
        
        <endpoint address="HelloService" binding="basicHttpBinding" contract="HelloService.IHelloService">
        </endpoint>

        <endpoint address="HelloService" binding="netTcpBinding" contract="HelloService.IHelloService">
        </endpoint>

        <endpoint address="mex" binding="mexHttpBinding" contract="IMetadataExchange">
          
        </endpoint>

        <host>
          <baseAddresses>
            <add baseAddress="http://localhost:8080/" />
            <add baseAddress="net.tcp://localhost:8090/ "/>
          </baseAddresses>
        </host>

      </service>
    </services>

    <behaviors>
      <serviceBehaviors>
        <behavior name="mexBehavior">
          <serviceMetadata httpGetEnabled="true" />
        </behavior>
      </serviceBehaviors>
    </behaviors>
  </system.serviceModel>
</configuration>
~~~

3. Uruchom jako administator

## Klient

### Utworzenie klienta z użyciem wygenerowanej klasy Proxy

1. Utwórz projekt _HelloServiceConsoleClient_

2. Wybierz opcję *Add Service Reference* i wskaż adres _http://localhost:8080/_

3. Utwórz kod klienta
~~~ csharp 
static void Main(string[] args)
    {
        HelloService.HelloServiceClient client = new HelloService.HelloServiceClient();
        string response = client.Ping("Hello");

        Console.WriteLine(response);
    }
~~~

**uwaga** - W przypadku gdy w pliku konfiguracyjnym zdefiniowania wiele bindingów należy w konstruktorze przekazać nazwę konfiguracji.

Zalety:
- łatwe generowanie

Wady:
- Brak odporności na zmianę (po każdej zmianie kontraktu należy wygenerować klienta na nowo

 ~~~ csharp 
static void Main(string[] args)
    {
        HelloService.HelloServiceClient client = new HelloService.HelloServiceClient("BasicHttpBinding_IHelloService");
            string response = client.Ping("Hello");

            Console.WriteLine(response);
    }
~~~

### Utworzenie klienta z własną klasą Proxy

~~~ csharp
  public class HelloServiceProxy : ClientBase<IHelloService>, IHelloService
    {
        public string Ping(string message)
        {
            return base.Channel.Ping(message);
        }

        public Task<string> PingAsync(string message)
        {
            return base.Channel.PingAsync(message);
        }
    }
~~~



### Utworzenie klienta z użyciem fabryki **ChannelFactory**

~~~ csharp
private static void ChannelFactoryTest()
    {
        BasicHttpBinding myBinding = new BasicHttpBinding();
        EndpointAddress myEndpoint = new EndpointAddress("http://localhost:8080/HelloService");

        ChannelFactory<IHelloService> proxy = new ChannelFactory<IHelloService>(myBinding, myEndpoint);
        IHelloService client = proxy.CreateChannel();

        string response = client.Ping("Hello");

        ((IClientChannel)client).Close();
    }
~~~

Zalety:
- odporność na zmianę (nie wymaga żadnych modyfikacji przy zmianie kontraktu)


## Serializacja złożonych typów

~~~ csharp
public class Employee
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
    }
~~~

http://localhost:8080/?xsd=xsd2


### Typy atrybutów

**brak** - domyślnie używa DataContractSerializer. Serializuje wszystkie publiczne właściwości w porządku alfabetycznym. Prywatne pola i właściwości nie są serializowane.

**[Serializable]** - serializacuje wszystkie pola. Nie mamy kontroli nad tym, które pola będą zawarte lub pominięte w danych.

**[DataContract]** - serializacuje wszystkie pola oznaczone **[DataMember]**. Pola, które nie są oznaczone atrybutem [DataMember] są pomijane z serializacji. Atrybut [DataMember] może być stosowany również do prywatnych pól i publicznych właściwości.


~~~ csharp
[DataContract]
public class Employee
{
    [DataMember]
    public int Id { get; set; }
    [DataMember]
    public string FirstName { get; set; }
    [DataMember]
    public string LastName { get; set; }
    public DateTime DateOfBirth { get; set; }
}
~~~

### Dodatkowe modyfikatory

namespace, nazwy pól, kolejność

~~~ csharp
[DataContract(Namespace = "http://www.altkom.pl/Employee")]
public class Employee
{
    [DataMember(Name = "ID", Order = 1)]
    public int Id { get; set; }

    [DataMember(Order = 2)]
    public string FirstName { get; set; }

    [DataMember(Order = 3, IsRequired = true)]
    public string LastName { get; set; }
    public DateTime DateOfBirth { get; set; }
}
~~~


## Znane typy (KnowTypes)

### Opcja 1 - atrybut **[KnownType]** na klasie bazowej

~~~ csharp
  [KnownType(typeof(FullTimeEmployee))]
    [KnownType(typeof(PartTimeEmployee))]
    [DataContract(Namespace = "http://www.altkom.pl/Employee")]
    public class Employee
    {
        [DataMember(Name = "ID", Order = 1)]
        public int Id { get; set; }

        [DataMember(Order = 2)]
        public string FirstName { get; set; }

        [DataMember(Order = 3, IsRequired = true)]
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
    }


    public class FullTimeEmployee : Employee
    {
        public decimal AnnualSalary { get; set; }
    }

    public class PartTimeEmployee : Employee
    {
        public decimal HourlyPay { get; set; }
    }
  ~~~

### Opcja 2 - atrybut **[ServiceKnownType]** na kontrakcie usługi

Dotyczy wszystkich operacji tylko w kontrakcie usługi.

~~~ csharp
[ServiceKnownType(typeof(PartTimeEmployee))]
[ServiceKnownType(typeof(FullTimeEmployee))]
[ServiceContract]
public interface IEmployeeService
{
    [OperationContract]
    Employee Get(int id);

    [OperationContract]
    void Add(Employee employee);
}
~~~

### Opcja 3 - tylko wybrane operacje
~~~ csharp
[ServiceContract]
public interface IEmployeeService
{
    [ServiceKnownType(typeof(PartTimeEmployee))]
    [ServiceKnownType(typeof(FullTimeEmployee))]
    [OperationContract]
    Employee Get(int id);

    [OperationContract]
    void Add(Employee employee);
}
~~~

## MessageContract


~~~ csharp
 [MessageContract(IsWrapped = true, WrapperName = "EmployeeRequestObject", WrapperNamespace = "http://altkom.pl")]
    public class EmployeeRequest
    {
        [MessageHeader(Namespace = "http://altkom.pl")]
        public string LicenseKey { get; set; }

        [MessageBodyMember(Namespace = "http://altkom.pl")]
        public int EmployeeId { get; set; }
    }

    [MessageContract(IsWrapped = true, WrapperName = "EmployeeInfoObject", WrapperNamespace = "http://altkom.pl")]
    public class EmployeeInfo
    {
        [MessageBodyMember(Order = 1, Namespace = "http://altkom.pl")]
        public int Id { get; set; }

        [MessageBodyMember(Order = 2, Namespace = "http://altkom.pl")]
        public string FirstName { get; set; }

        [MessageBodyMember(Order = 3, Namespace = "http://altkom.pl")]
        public string LastName { get; set; }

        public EmployeeInfo()
        {
        }

        public EmployeeInfo(Employee employee)
        {
            this.Id = employee.Id;
            this.FirstName = employee.FirstName;
            this.LastName = employee.LastName;
        }

    }
~~~


## Obsługa wyjątków

### Metoda 1 plik konfiguracyjny

~~~ xml
 <behavior name="includeExceptionDetails">
          <serviceDebug includeExceptionDetailInFaults="true" />
        </behavior>
~~~

### Metoda 2 kod

~~~ csharp
  [ServiceBehavior(IncludeExceptionDetailInFaults =  true)]
    public class CalculatorService : ICalculatorService
    {
        public int Divide(int numerator, int denominator)
        {
            return numerator / denominator;
        }
    }
~~~

W WCF należy rzucać wyjątkami **FaultException** lub **FaultException<T>** zamiast wyjątków .NET

Są tego 2 przyczyny:
- po wystąpieniu wyjątku kanał komunikacyjny przechodzi w stan Fault
- wyjątki są specyficzne dla platformy .NET


~~~ csharp
public class CalculatorService : ICalculatorService
    {
        public int Divide(int Numerator, int Denominator)
        {
            if (Denominator == 0)
                throw new DivideByZeroException();

            return Numerator / Denominator;
        }
    }

~~~


~~~ csharp
public class CalculatorService : ICalculatorService
    {
        public int Divide(int Numerator, int Denominator)
        {
            if (Denominator == 0)
                throw new FaultException("Denomintor cannot be ZERO", new FaultCode("DivideByZeroFault"));

            return Numerator / Denominator;
        }
    }

~~~

### Silnie typowane błędy SOAP

~~~ csharp
[DataContract]
    public class DivideByZeroFault
    {
        [DataMember]
        public string Error { get; set; }
        [DataMember]
        public string Details { get; set; }
    }
~~~

~~~ csharp
 public int Divide(int numerator, int denominator)
        {
            if (denominator == 0)
            {
                DivideByZeroFault divideByZeroFault = new DivideByZeroFault
                {
                    Error = "DivideByZero",
                    Details = "denominator is zero"
                };

                throw new FaultException<DivideByZeroFault>(divideByZeroFault);
            }

            return numerator / denominator;
        }
~~~

- Przechwytywanie 

~~~ csharp
 public int Calculate()
 {

    try
   {
   	 int result = client.Divide(10, 0);
   } catch(FaultException<CalculatorService.DivideByZeroFault> e)
{ 
}  
 } 

~~~

`




## Rest Api

SOAP/WCF -> RCP (Remote Call Procedures)
REST API -> resources



- Pobranie zasobu

request:
~~~
  GET http://localhost:8080/api/products HTTP/1.1
  Host: localhost
  Accept: application/xml
  {blank line}
~~~

response:
~~~
 200 OK
 Content-Type: application/xml
 
 <xml>
   <product></product>
   <product></product>
   <product></product>
</xml>
~~~


- Utworzenie zasobu
~~~
 POST http://localhost:8080/api/products HTTP/1.1
  Host: localhost
  Content-Type: application/json
  Accept: application/xml
  {"name":"komputer","unitprice":150 }

  {blank line}
~~~

response:
~~~
 201 Created
 Content-Type: application/xml
~~~

- Podmiana zasobu
~~~
PUT http://localhost:8080/api/products/10
Host: localhost
  Content-Type: application/json
  Accept: application/xml
  {"name":"komputer","unitprice":250 }
~~~

- Aktualizacja zasobu

~~~
PATCH http://localhost:8080/api/products/10
Host: localhost
  Content-Type: application/json
  Accept: application/xml
  {"unitprice":250 }
~~~

- Usunięcie zasobu

~~~
DELETE http://localhost:8080/api/products/10
~~~

- Przykłowe trasy
~~~
 GET http://localhost:8080/api/products
 GET http://localhost:8080/api/products/10 
 GET http://localhost:8080/api/products?from=100&to=200
 GET http://localhost:8080/api/products/10/customers
~~~



~~~ csharp
 [ServiceContract(Namespace = "http://altkom.pl")]
    public interface IProductService
    {
        [OperationContract]
        [WebGet(BodyStyle = WebMessageBodyStyle.Bare, ResponseFormat = WebMessageFormat.Json, UriTemplate = "api/products")]
        IEnumerable<Product> Get();

        [OperationContract]
        [WebGet(BodyStyle = WebMessageBodyStyle.Bare, ResponseFormat = WebMessageFormat.Json, UriTemplate = "api/products?from={from}&to={to}")]
        IEnumerable<Product> GetByPrice(decimal from, decimal to);

        [OperationContract]
        [WebGet(BodyStyle = WebMessageBodyStyle.Bare, ResponseFormat = WebMessageFormat.Json, UriTemplate = "api/products/{id}")]
        Product GetById(string id);

        [OperationContract]
        [WebInvoke(UriTemplate = "api/products", Method = "POST",  RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json,
          BodyStyle = WebMessageBodyStyle.Bare)]
        void Add(Product product);

        [OperationContract]
        [WebInvoke(UriTemplate = "api/products", Method = "PUT", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json,
        BodyStyle = WebMessageBodyStyle.Bare)]
        void Update(Product product);

        [OperationContract]
        [WebInvoke(UriTemplate = "api/products/{id}", Method = "DELETE", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json,
       BodyStyle = WebMessageBodyStyle.Bare)]
        void Remove(string id);
    }

~~~

## WCF NET Core

https://github.com/dotnet/wcf

## gPRC jako zamiennik WCF

https://www.codemag.com/article/1911102?utm_source=msft-focus-tw&amp;utm_medium=social-promoted&amp;utm_campaign=sm-articles
