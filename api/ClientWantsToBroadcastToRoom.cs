using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Externalities.Repositories;
using Fleck;
using lib;

namespace api
{
    public class ClientWantsToBroadcastToRoomDto : BaseDto
    {
        public string? message { get; set; }
        public int roomId { get; set; }
        
    }
    public class ClientWantsToBroadcastToRoom(MessageRepository messageRepository) : BaseEventHandler<ClientWantsToBroadcastToRoomDto>
    {
        public override async Task Handle(ClientWantsToBroadcastToRoomDto dto, IWebSocketConnection socket)
        {
            
            await isMessageHateSpeech(dto.message!);
            
            var userId = StateService.Connections[socket.ConnectionInfo.Id]!.currentUser!.id;

            Console.WriteLine("Hereeeeeee" + userId);

            messageRepository.CreateMessage(dto.message!, DateTimeOffset.UtcNow, userId, dto.roomId);

            var message = new ServerBroadcastsMessageWithUsername()
            {
                message = dto.message,
                username = StateService.Connections[socket.ConnectionInfo.Id].currentUser!.username
            };
            StateService.BroadCastToRoom(dto.roomId, JsonSerializer.Serialize(message));
        }

        private async Task isMessageHateSpeech(string message)
        {
            HttpClient httpClient = new HttpClient();

            var requestUri = "https://hatespeechfilter.cognitiveservices.azure.com/contentsafety/text:analyze?api-version=2023-10-01";
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post,
            requestUri);

            request.Headers.TryAddWithoutValidation("accept", "application/json");
            request.Headers.TryAddWithoutValidation("Ocp-Apim-Subscription-Key", Environment.GetEnvironmentVariable("subKey"));

            var req = new
            {
                text = message,
                categories = new List<string>() { "Hate", "Violence" },
                outputType = "FourSeverityLevels"
            };


            request.Content = new StringContent(JsonSerializer.Serialize(req));
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

            HttpResponseMessage response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();

            var obj = JsonSerializer.Deserialize<ContentFilterResponse>(responseBody);
            var isToxic = obj!.categoriesAnalysis!.Count(e => e.severity > 1) >= 1;
            if (isToxic)
                throw new ValidationException("Such speech is not allowed!");

        }
    }



    public class ServerBroadcastsMessageWithUsername : BaseDto
    {
        public string? message { get; set; }
        public string? username { get; set; }
    }

    public class CategoriesAnalysis
    {
        public string? category { get; set; }
        public int severity { get; set; }
    }

    public class ContentFilterResponse
    {
        public List<object>? blockListsMatch { get; set; }
        public List<CategoriesAnalysis>? categoriesAnalysis { get; set; }
    }



}
