using System.Reflection;
using System.Text.Json;
using api;
using Externalities.Repositories;
using Fleck;
using infrastructure;
using lib;



var builder = WebApplication.CreateBuilder(args);



builder.Services.AddNpgsqlDataSource(Utilities.ProperlyFormattedConnectionString,
        sourceBuilder => { sourceBuilder.EnableParameterLogging();});


builder.Services.AddSingleton<MessageRepository>();
builder.Services.AddSingleton<UserRepository>();
builder.Services.AddSingleton<RoomRepository>();



var clientEventHandlers = builder.FindAndInjectClientEventHandlers(Assembly.GetExecutingAssembly());

var app = builder.Build();

var server = new WebSocketServer("ws://0.0.0.0:8181");

var wsConnections = new List<IWebSocketConnection>();



server.Start(socket =>
{

    socket.OnOpen = () => 
    {
        
        StateService.AddConnection(socket);
        //WebSocketConnections.wsConnections.Add(socket);
    };

    socket.OnMessage = async message => 
    {   
        
        try
        {   
          await app.InvokeClientEventHandler(clientEventHandlers, socket, message);
        }
        catch(Exception e)
        {
            await socket.Send(JsonSerializer.Serialize(new ServerSendsErrorToTheClient
            {
                message = e.Message
            }));

            Console.WriteLine(e.Message);
            Console.WriteLine(e.InnerException);
            Console.WriteLine(e.StackTrace);
            throw e = new Exception("It couldn't send the message!");   
        }
        
        
    };
});




Console.ReadLine();


public class ServerSendsErrorToTheClient
{
    public string? message { get; set; }
}
public class WebSocketConnections
{
    public static List<IWebSocketConnection> wsConnections = new List<IWebSocketConnection>();
    // class members
}


