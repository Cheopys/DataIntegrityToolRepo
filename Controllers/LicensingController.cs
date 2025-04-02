using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProxChat.Db;
using ProxChat.Schema;
using ProxChat.Services;
using ProxChat.SharedObjectTypes;

namespace ProxChat.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class LicensingController
	{
		[HttpPut, Route("AddFriendRequest")]
		public async Task AddFriendRequest(AddFriendRequestRequest request)
		{
			await FriendsService.AddFriendRequest(request);
		}

		[HttpGet, Route("GetFriendRequests")]
		public async Task<GetFriendRequestsResponse> GetFriendRequests(Int64 UserId)
		{
			return await FriendsService.GetFriendRequests(UserId);
		}

		[HttpPost, Route("UnfriendUser")]
		public async Task UnfriendUser(UnFriendRequest request)
		{
			await FriendsService.UnfriendUser(request);
		}

		[HttpGet, Route("GetFriends")]
		public async Task<List<Int64>> GetFriends(Int64 userId)
		{
			return await FriendsService.GetFriends(userId);
		}

		[HttpGet, Route("GetFriendsOnline")]
		public async Task<List<Int64>> GetFriendsOnline(Int64 userId)
		{
			return await FriendsService.GetFriendsOnline(userId);
		}

		[HttpPost, Route("ProcessFriendRequest")]
		public async Task ProcessFriendRequest(ProcessFriendRequestRequest request)
		{
			await FriendsService.ProcessFriendRequest(request);
		}
	}
}
