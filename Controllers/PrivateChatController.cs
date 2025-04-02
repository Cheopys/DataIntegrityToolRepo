using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Geolocation;
using ProxChat.Db;
using ProxChat.Schema;
using ProxChat.Services;
using ProxChat.SharedObjectTypes;

namespace ProxChat.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class PrivateChatController : ControllerBase
	{
		[HttpPut, Route("CreatePrivateChat")]
		[Consumes("application/json")]
		[Produces("application/json")]
		public async Task<string> CreatePrivateChat([FromBody] CreatePrivateChatRequest  request)
		{
			CreatePrivateChatResponse response = await PrivateChatService.CreatePrivateChat(request);

			return JsonSerializer.Serialize(response);
		}

		[HttpPut, Route("SendPrivateChatMessage")]
		[Consumes("application/json")] // SendPrivateMessageRequest
		[Produces("application/json")] // SendPrivateMessageResponse
		public async Task<string> SendPrivateChatMessage(Int64  userId, 
														 string jsonRequest)
		{
			SendPrivateMessageRequest  request  = JsonSerializer.Deserialize<SendPrivateMessageRequest>(jsonRequest);

			SendPrivateMessageResponse response = await PrivateChatService.SendPrivateMessage(userId, request);

			return JsonSerializer.Serialize(response);
		}
	}
}
