using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ProxChat.Schema;
using ProxChat.Services;
using ProxChat.SharedObjectTypes;

namespace ProxChat.Controllers
{
	[ApiController]
	[Route("[controller]")]
		
	// for populating the choices on the radius slider

	public class ContentController : ControllerBase
    {
		[HttpGet, Route("GetValidRadii")]
		[Produces("application/json")]
		public string GetValidRadii(DistanceUnits distanceUnits)
		{
			return JsonSerializer.Serialize(ContentService.GetValidRadii(distanceUnits));
		}

		// get details for a particular radius slider position

		[HttpGet, Route("GetRadiusData")]
		[Produces("application/json")]
		public string GetRadiusData(Int16 radiusId)
		{
			return JsonSerializer.Serialize(ContentService.GetChatRadius(radiusId));
		}

		// user details for a particular radius

		[HttpPut, Route("GetUsersInRadius")]
		[Consumes("application/json")]
		[Produces("application/json")]
		public async Task<string> GetUsersInRadius([FromBody] UsersInRadiusRequest request)
		{
//			UsersInRadiusRequest request = JsonSerializer.Deserialize<UsersInRadiusRequest>(jsonRequest);
			List<ChatUsersOnline>    users   = await ContentService.GetUsersInRadius(request);

			return await CryptographyService.EncryptAndEncodeResponse(request.userId, users);
		}

		// counts only (users and friends) for a given radius

		[HttpPut, Route("GetUserCountsInRadius")]
		[Consumes("application/json")] // UsersInRadiusRequest 
		[Produces("application/json")] // UsersInRadiusResponse
		public async Task<string> GetUserCountsInRadius([FromBody] UserCountsInRadiusRequest request)
		{
			UsersInRadiusResponse response = await ContentService.GetUserCountsInRadius(request);

			return JsonSerializer.Serialize(response);
		}

		// gets counts of the total users and ucurrent user's friends in a chat

		[HttpGet, Route("UsersFriendsInChat")]
		[Produces("application/json")]
		public async Task<string> UsersFriendsInChat(Int32 chatId, // URI parameters
													 Int64 userId)
		{
			UsersInRadiusResponse response = await ContentService.UsersFriendsInChat(chatId, userId);

			return await CryptographyService.EncryptAndEncodeResponse(userId, response);
		}

		// remove current user from the solitary public chat he is in
		// returns true if the chat continues with other users

		[HttpDelete, Route("RemoveUserFromChat")]
		public async Task<bool> RemoveUserFromChat(Int64 userId)
		{
			return await ContentService.RemoveUserFromChat(userId);
		}

		[HttpPut, Route("ChatSendMessage")]
		[Consumes("application/json")] // ChatSendMessageRequest
		public async Task ChatSendMessage([FromBody] EncryptionWrapperProxChat wrapper)
		{
			ChatSendMessageRequest request;

			CryptographyService.DecodeAndDecryptRequest<ChatSendMessageRequest>(wrapper, out request);

			await ContentService.ChatSendMessage(request);
		}

		[HttpGet, Route("GetRecentChatMessages")]
		[Produces("application/json")]
		public async Task<string> ChatGetRecentMessages(Int64 userId,
														Int32 chatId,
														Int32 count)
		{
			List<ChatMessage> messages = await ContentService.GetRecentChatMessages(userId,
																				 chatId, 
																				 count);

			return await CryptographyService.EncryptAndEncodeResponse(userId, messages);
		}

	}
}
