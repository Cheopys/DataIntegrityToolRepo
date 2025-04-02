using System;
using ProxChat.SharedObjectTypes;
using ProxChat.Schema;
using ProxChat.Db;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.LayoutRenderers.Wrappers;

namespace ProxChat.Services
{
	public static class FriendsService
	{
		static Logger logger;
		static FriendsService()
		{
			var config = new NLog.Config.LoggingConfiguration();

			// Targets where to log to: File and Console
			var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

			// Rules for mapping loggers to targets            
			config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, logconsole);

			// Apply config           
			NLog.LogManager.Configuration = config;
			logger = LogManager.GetCurrentClassLogger();
		}
		private static void GetBothUserFriendEntries(Int64		  userId, 
													 Int64		  friendId,
												 out UserFriends? userfriendSelf,
												 out UserFriends? userfriendOther)	
		{
			using (DataContext context = new())
			{
				userfriendSelf = context.UserFriends.Where(uf => uf.userId == userId
															   && uf.userIdFriend == friendId)
													 .FirstOrDefault();

				userfriendSelf  = context.UserFriends.Where(uf => uf.userId == userId
								 							   && uf.userIdFriend == friendId)
													 .FirstOrDefault();
				userfriendOther = context.UserFriends.Where(uf => uf.userId		  == friendId
															   && uf.userIdFriend == userId)
													 .FirstOrDefault();

				context.Dispose();
			}
		}
		public static async Task AddFriendRequest(AddFriendRequestRequest request)
		{
			logger.Info($"AFR caled with user {request.userId} and friend {request.friendId}");

			using (DataContext dbcontext = new())
			{
				// if there is already a FR in the other direction, accept the friend

				FriendRequests? friendRequestOpposite = dbcontext.FriendRequests
														 .Where(fr => fr.UserId   == request.friendId
																   && fr.FriendId == request.userId)
														 .FirstOrDefault();
				if (friendRequestOpposite != null)
				{
					logger.Info($"found opposite FR with user {friendRequestOpposite.UserId} and friend {friendRequestOpposite.FriendId}; adding rows to DB");

					await dbcontext.UserFriends.AddAsync(new UserFriends
					{
						userId       = request.userId,
						userIdFriend = request.friendId,
					});

					await dbcontext.UserFriends.AddAsync(new UserFriends
					{
						userId       = request.friendId,
						userIdFriend = request.userId,
					});

					dbcontext.FriendRequests.Remove(friendRequestOpposite);
				}
				
				// block duplicates

				else if (dbcontext.FriendRequests
								  .Where(fr => fr.UserId   == request.userId
											&& fr.FriendId == request.friendId)
								  .FirstOrDefault() == null)
				{
					logger.Info($"did not find existing FR with user {request.userId} and friernd {request.friendId}; adding");

					dbcontext.FriendRequests.Add(new FriendRequests
					{
						UserId   = request.userId,
						FriendId = request.friendId,
						TimeSent = DateTime.UtcNow,
					});

					await dbcontext.SaveChangesAsync();
				}

				await dbcontext.SaveChangesAsync();
				await dbcontext.DisposeAsync();
			}
		}

		public static async Task<GetFriendRequestsResponse> GetFriendRequests(Int64 UserId)
		{
			GetFriendRequestsResponse response = new();

			using (DataContext dbcontext = new())
			{
				List<FriendRequests> requests = await dbcontext.FriendRequests
															   .Where(r => r.UserId == UserId)
															   .ToListAsync();
				foreach (FriendRequests request in requests)
				{
					Users? user = await dbcontext.Users.FindAsync(request.FriendId);

					if (user != null)
					{
						response.friendRequestsSent.Add(new FriendRequest()
						{
							Id                  = request.Id,
							userIdSender		= UserId,
							userIdRecipient		= request.FriendId,
							TimeSent			= request.TimeSent,
						});
					}
					else
					{
						// user has been deleted

						dbcontext.Remove(request);
					}
				};

				requests = await dbcontext.FriendRequests
										  .Where(r => r.FriendId == UserId)
										  .ToListAsync();

				foreach (FriendRequests request in requests)
				{
					Users? user = await dbcontext.Users.FindAsync(request.UserId);

					if (user != null)
					{
						response.friendRequestsReceived.Add(new FriendRequest()
						{
							Id              = request.Id,
							userIdSender	= request.FriendId,
							userIdRecipient = UserId,
							TimeSent		= request.TimeSent,
						});
					}
					else
					{
						// user has been deleted

						dbcontext.Remove(request);
					}
				};

				await dbcontext.SaveChangesAsync();
				await dbcontext.DisposeAsync();
			}

			return response;
		}

		public async static Task UnfriendUser(UnFriendRequest request)
		{
			UserFriends userfriendSelf;
			UserFriends userfriendOther;

			GetBothUserFriendEntries(request.userId, 
									 request.friendId, 
								 out userfriendSelf, 
								 out userfriendOther);

			using (DataContext context = new())
			{
				context.UserFriends.Remove(userfriendSelf);
				context.UserFriends.Remove(userfriendOther);
				await context.SaveChangesAsync();
				await context.DisposeAsync();
			}
		}

		public static async Task<List<Int64>> GetFriends(Int64 userId)
		{
			using (DataContext dbcontext = new())
			{
				List<Int64> friendIds = await dbcontext.UserFriends
													   .Where(f => f.userId == userId)
													   .Select(f => f.userIdFriend)
													   .ToListAsync();

				await dbcontext.DisposeAsync();

				return friendIds;
			}
		}

		public static async Task<List<Int64>> GetFriendsOnline(Int64 userId)
		{
			using (DataContext dbcontext = new())
			{
				List<Int64> friendIds = await dbcontext.UserFriends
													   .Where(f => f.userId == userId)
													   .Select(f => f.userIdFriend)
													   .ToListAsync();

				friendIds = dbcontext.UsersOnline.Where(u => friendIds.Contains(u.UserId))
												 .Select(u => u.UserId)
												 .ToList();
				await dbcontext.DisposeAsync();

				return friendIds;
			}
		}

		public static async Task ProcessFriendRequest(ProcessFriendRequestRequest request)
		{
			using (DataContext dbcontext = new())
			{
				// verify the request still exists

				logger.Info($"seeking user {request.friendId}, friend {request.userId}");

				FriendRequests? friendrequest = await dbcontext.FriendRequests.Where(fr => fr.UserId  .Equals(request.friendId)
																					    && fr.FriendId.Equals(request.userId))
																			  .FirstOrDefaultAsync();
				if (friendrequest != null) 
				{
					logger.Info("friend request found");

					// it's real; add to the two friends list

					if (request.accept)
					{
						logger.Info("friend request accepted");

						dbcontext.UserFriends.Add(new UserFriends
						{
							userId			= request.userId,
							userIdFriend	= request.friendId,
						});

						dbcontext.UserFriends.Add(new UserFriends
						{
							userId		 = request.friendId,
							userIdFriend = request.userId,
						});
					}

					dbcontext.FriendRequests.Remove(friendrequest);

					// check fo a request in the other direction

					FriendRequests? friendrequestReverse = await dbcontext.FriendRequests.Where(fr => fr.UserId  .Equals(request.userId)
																								   && fr.FriendId.Equals(request.friendId))
																						 .FirstOrDefaultAsync();
					if (friendrequestReverse != null)
					{
						dbcontext.FriendRequests.Remove(friendrequestReverse);
					}
				}
				else
				{
					logger.Info("friend request not found");
				}

				await dbcontext.SaveChangesAsync();
				await dbcontext.DisposeAsync();
			}
		}
	}
}
