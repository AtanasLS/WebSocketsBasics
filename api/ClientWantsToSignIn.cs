using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Externalities.QueryModels;
using Externalities.Repositories;
using Fleck;
using lib;

namespace api
{

    public class ClientWantsToSignInDto : BaseDto
    {
        //public int userId { get; set; }
        public string? username { get; set; }
    }
    public class ClientWantsToSignIn(UserRepository userRepository) : BaseEventHandler<ClientWantsToSignInDto>
    {
        public override Task Handle(ClientWantsToSignInDto dto, IWebSocketConnection socket)
        {

            
            var user = userRepository.FindUserByUsername(dto.username!);
            
            Console.WriteLine("HErreeee:  " + user.username);
            
            if(user.username != dto.username)
                throw new ValidationException("User with that username doesn't exist!");

            StateService.Connections[socket.ConnectionInfo.Id].currentUser = user;
            var messageToClient = new ServerAddsClientToRoom(){
                message = "User with username: " + dto.username,
                username = dto.username
            };

            socket.Send(JsonSerializer.Serialize(messageToClient));
            return Task.CompletedTask;
        }
    }

    public class ServerAddsUserToClient() : BaseDto
    {
        public string? message { get; set; }
        public string? username { get; set; }
    }
}