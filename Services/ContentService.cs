using Geolocation;
using ProxChat.Schema;
using ProxChat.Db;
using ProxChat.SharedObjectTypes;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
//using ProxChat.Services.PushNotifications;
using Amazon.SimpleNotificationService.Model;
using NLog;
using System.Diagnostics.Metrics;
using System.Threading.Channels;
using Microsoft.CodeAnalysis;
using System;
using Amazon.Translate.Model;
using Amazon.Translate;
using Amazon.Rekognition.Model;

namespace ProxChat.Services
{
	public static class ContentService
	{
		private static Dictionary<string, List<string>> forbiddenWords = new();

		static Logger logger;
		static ContentService()
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

		static bool janitorOn = false;

		public static void SetForbiddenWords(string locale,
											 List<string> forbiddenWordsList)
		{
			forbiddenWords[locale] = forbiddenWordsList;
		}
		public static List<string> GetForbiddenWords(string locale)
		{
			return forbiddenWords[locale];
		}

		public static async Task<List<ChatRadius>> GetValidRadii(DistanceUnits distanceUnit)
		{
			List<ChatRadius> radii = null;

			using (DataContext context = new())
			{
				radii = await context.ChatRadii
									 .Where(r => r.distanceUnit == distanceUnit)
									 .OrderBy(r => r.value)
									 .Select(r => new ChatRadius
									 {
										 Id = r.Id,
										 description = r.description,
										 value = r.value

									 })
									 .ToListAsync();
			}

			return radii;
		}

		public static async Task<List<ChatMessage>> GetRecentChatMessages(Int64 userId,
																		  Int32 chatId, 
																		  Int32 countMessages)
		{
			List<ChatMessage> messages = new();
			Users? user = null;

			using (DataContext context = new())
			{
				Int32 count = context.ChatMessages.Where(ci => ci.ChatId.Equals(chatId)).Count();
				List<ChatMessages> chatmessages;

				if (count > countMessages)
				{
					chatmessages = await context.ChatMessages.Where  (cm => cm.ChatId.Equals(chatId))
															 .OrderBy(cm => cm.TimeSent)
															 .Skip   (countMessages - count)
															 .ToListAsync();
				}
				else
				{
					chatmessages = await context.ChatMessages.Where  (cm => cm.ChatId.Equals(chatId))
															 .OrderBy(cm => cm.TimeSent)
															 .ToListAsync();

				}

				foreach (ChatMessages cm in chatmessages)
				{
					ChatMessage chatmessage = new ChatMessage()
					{
						Id				= cm.Id,
						ChatId			= cm.ChatId,
						UserIdSender	= cm.UserIdSender,
						Message			= cm.Message,
						TimeSent		= cm.TimeSent,
						imageB64        = cm.hasImage ? String.Empty : null
					};

					if (chatmessage.imageB64 != null)
					{
						S3Attachment attachment = await S3Service.GetChatMessageImage(chatmessage.Id.ToString());

						if (attachment.image != null)
						{
							chatmessage.imageB64 = Convert.ToBase64String(attachment.image);
						}
					}

					messages.Add(chatmessage);

					user = await context.Users.FindAsync(userId);
				}

				await context.DisposeAsync();
			}

			messages = await ContentService.TranslateAndFilterMessages(messages, user);

			return messages;
		}
		public static ChatRadius GetChatRadius(Int16 radiusId)
		{
			ChatRadius? chatRadius = null;

			using (DataContext context = new())
			{
				chatRadius = context.ChatRadii.Where(r => r.Id == radiusId)
									.Select(r => new ChatRadius
									{
										units		= r.distanceUnit,
										Id			= r.Id,
										description = r.description,
										value		= r.value
									})
									.FirstOrDefault();
			}

			return chatRadius;
		}

		public class CreateChatRequest
		{
			public Int64 UserId { get; set; }
			public string Name { get; set; }
			public Int16 chatRadiusID { get; set; }
			public Coordinates coordinates { get; set; }
		}


		private static async Task<CreateChatResponse> CreateChat(CreateChatRequest request)
		{
			CreateChatResponse response = new();

			if (janitorOn == false)
			{
				logger.Info($"CreateChat: starting janitor thread");

				JanitorService.Initialize();

				janitorOn = true;
			}

			using (DataContext dbcontext = new())
			{
				UsersOnline? useronline = await dbcontext.UsersOnline.Where(uo => uo.UserId == request.UserId).FirstOrDefaultAsync();

				if (useronline == null)
				{
					// user was not marked online. opening app on a registered user does this

					dbcontext.UsersOnline.Add(new UsersOnline
					{
						UserId		= request.UserId,
						Radius		= request.chatRadiusID,
						Latitude	= request.coordinates.latitude,
						Longitude	= request.coordinates.longitude,
						Altitude	= request.coordinates.altitude,
						TimeOnline	= DateTime.UtcNow
					});

					await dbcontext.SaveChangesAsync();

					useronline = await dbcontext.UsersOnline.Where(uo => uo.UserId == request.UserId).FirstOrDefaultAsync();
				}

				if (useronline != null)
				{
					Chats chatNew = new()
					{
						Name = request.Name.Length > 0 ? request.Name : $"Chat{request.UserId % 100000}",
						Latitude = useronline.Latitude,
						Longitude = useronline.Longitude,
						Altitude = useronline.Altitude,
						RadiusId = request.chatRadiusID,
						TimeCreated = DateTime.UtcNow,
						//					TopicARN	= await PushNotificationService.CreateSNSTopic()
					};

					// save to get new Chat ID

					await dbcontext.Chats.AddAsync(chatNew);
					dbcontext.SaveChanges();

					logger.Info($"CreateChat: new chat ID = {chatNew.Id}");

					response.ChatId = chatNew.Id;
					response.Name = chatNew.Name;

					UsersInRadiusRequest uirq = new()
					{
						userId = request.UserId,
						chatRadiusID = request.chatRadiusID,
						coordinateCenter = new Coordinates
						{
							latitude = useronline.Latitude,
							longitude = useronline.Longitude,
							altitude = useronline.Altitude
						},
						filterChatUsers = true,
						filterBlocked = true
					};

					response.usersonline = await GetUsersInRadius(uirq);

					logger.Info($"CreateChat: adding {response.usersonline.Count} users to chat");

					foreach (ChatUsersOnline uol in response.usersonline)
					{
						if (uol.UserId != 0) // swagger bug; prefills the array of user with a zero
						{
							dbcontext.ChatUsers.Add(new ChatUsers
							{
								ChatId = chatNew.Id,
								UserId = uol.UserId,
								TimeAdded = DateTime.UtcNow,
								/*						SubscriptionARN = await PushNotificationService.SubscribeTopicWithFilter(chatNew.TopicARN, 
																																 String.Empty, 
																																 chatNew.QueueARN)*/
							});
						}
					}
				} // endif usersonline != null
				await dbcontext.SaveChangesAsync();
				await dbcontext.DisposeAsync();
				// TBD: send push notification invitations to all users
			}

			return response;
		}

		public static async Task<List<ObscenityViolations>> ChatSendMessage(ChatSendMessageRequest request)
		{
			List<ObscenityViolations> violations = new();
			logger.Info($"userID {request.userId}, chat ID {request.chatId}, message {request.message}");

			if (request.imageB64        != null
			&&  request.imageB64.Length == 0)
			{
				request.imageB64 = null;
			}

			if (request.message == null)
			{
				request.message = string.Empty;
			}

			using (DataContext dbcontext = new())
			{
				Chats? chat = dbcontext.Chats.Find(request.chatId);

				logger.Info($"chat found in DB");

				chat.TimeMessage = DateTime.UtcNow;

				ChatMessages message = new()
				{
					UserIdSender = request.userId,
					ChatId		 = request.chatId,
					Message		 = request.message,
					TimeSent	 = DateTime.UtcNow,
					hasImage     = request.imageB64 != null
				};
				dbcontext.ChatMessages.Add(message);

				await dbcontext.SaveChangesAsync();

				if (request.imageB64 != null)
				{
					byte[] image = Convert.FromBase64String(request.imageB64);

					string key = message.Id.ToString();

					violations = await ImageModerationService.ModerateImage(request.userId, 
																			ImageRejectionType.ChatMessage,
																		    key, 
																			image);

					if (violations.Count == 0)
					{
						await S3Service.AddChatMessageImage(request.userId,
															key,
															image);
					}
				}

				await dbcontext.SaveChangesAsync();
				await dbcontext.DisposeAsync();
			}

			return violations;
		}

		public static async Task<List<ChatUsersOnline>> GetUsersInRadius(UsersInRadiusRequest request)
		{
			List<ChatUsersOnline> users = new();
			Coordinate center = new()
			{
				Latitude  = request.coordinateCenter.latitude,
				Longitude = request.coordinateCenter.longitude
			};

			ChatRadius data = ContentService.GetChatRadius(request.chatRadiusID);
			CoordinateBoundaries boundaries;

			if (data.units == DistanceUnits.British)
			{
				boundaries = new CoordinateBoundaries(center, data.value, DistanceUnit.Miles);
			}
			else
			{
				boundaries = new CoordinateBoundaries(center, data.value, DistanceUnit.Kilometers);
			}

			using (DataContext dbcontext = new())
			{
				// get the online users in a square side sides of radius

				users = dbcontext.UsersOnline.Where(o => o.Latitude >= boundaries.MinLatitude
													  && o.Latitude <= boundaries.MaxLatitude
													  && o.Longitude >= boundaries.MinLongitude
													  && o.Longitude <= boundaries.MaxLongitude)
											.Select(u => new ChatUsersOnline
											{ Id = u.Id,
												Altitude = u.Altitude,
												Latitude = u.Latitude,
												Longitude = u.Longitude,
												Radius = u.Radius,
												TimeOnline = u.TimeOnline,
												UserId = u.UserId
											})
											.ToList();

				// narrow the square to a circle

				users = users.Where(u => GeoCalculator.GetDistance(center.Latitude,
																   center.Longitude,
																   u.Latitude,
																   u.Longitude,
																   3,
																   data.units == DistanceUnits.British ? DistanceUnit.Miles : DistanceUnit.Kilometers) <= data.value)
													   .ToList();

				// remove users who are already in other chats

				if (request.filterChatUsers)
				{
					List<ChatUsersOnline> usersInChats = new();

					foreach (ChatUsersOnline user in users)
					{
						if (dbcontext.ChatUsers
									 .Where(cu => cu.UserId == user.UserId)
									 .FirstOrDefault() != null)
						{
							usersInChats.Add(user);
						}
					}

					usersInChats.ForEach(uo => users.Remove(uo));
				}
				// if elected, remove users that the chat creator has blocked

				if (request.filterBlocked)
				{
					List<ChatUsersOnline> usersBlocked = new();

					foreach (ChatUsersOnline user in users)
					{
						if (dbcontext.UserBlocks.Where(ub => ub.userId == request.userId
														  && ub.UserIdBlocked == user.UserId)
												.FirstOrDefault() != null)
						{
							usersBlocked.Add(user);
						}
					}

					usersBlocked.ForEach(uo => users.Remove(uo));
				}

				await dbcontext.DisposeAsync();
			}

			return users;
		}

		// get counts of users and friends where there is no chat
		public static async Task<UsersInRadiusResponse> GetUserCountsInRadius(UserCountsInRadiusRequest request)
		{
			List<UsersOnline> users;
			UsersInRadiusResponse response = new();

			Coordinate center = new()
			{
				Latitude = request.coordinateCenter.latitude,
				Longitude = request.coordinateCenter.longitude
			};

			ChatRadius data = ContentService.GetChatRadius(request.chatRadiusID);
			CoordinateBoundaries boundaries;

			if (data.units == DistanceUnits.British)
			{
				boundaries = new CoordinateBoundaries(center, data.value, DistanceUnit.Miles);
			}
			else
			{
				boundaries = new CoordinateBoundaries(center, data.value, DistanceUnit.Kilometers);
			}

			using (DataContext dbcontext = new())
			{
				// get the online users in a square of radius sides

				users = await dbcontext.UsersOnline.Where(u => u.Latitude >= boundaries.MinLatitude
															&& u.Latitude <= boundaries.MaxLatitude
															&& u.Longitude >= boundaries.MinLongitude
															&& u.Longitude <= boundaries.MaxLatitude)
											 .ToListAsync();
				// users in a circle

				users = users.Where(u => GeoCalculator.GetDistance(center.Latitude,
																   center.Longitude,
																   u.Latitude,
																   u.Longitude,
																   3,
																   data.units == DistanceUnits.British ? DistanceUnit.Miles : DistanceUnit.Kilometers) <= data.value)
													  .ToList();

				users = users.Where(u => u.Radius.Equals(request.chatRadiusID)).ToList();

				List<Int64> userIds = users.Select(u => u.UserId).ToList();

				// get the friends 

				List<UserFriends> userFriends = await dbcontext.UserFriends
															   .Where(f => userIds.Contains(f.userId))
															   .ToListAsync();

				await dbcontext.DisposeAsync();

				response.countUsers = users.Count;
				response.countFriends = userFriends.Count;
			}

			return response;
		}

		public class ChatResolutionRequest
		{
			public Int64 userId { get; set; }
			public bool createNew { get; set; }
			public Coordinates coordinateUser { get; set; }
			public Int16 chatRadiusID { get; set; }
		}

		public static async Task<ChatResolutionResponse> ResolveRadiusToChat(ChatResolutionRequest request)
		{
			ChatResolutionResponse? response = null;
			List<Chats> chats;
			Coordinate userLocation = new()
			{
				Latitude = request.coordinateUser.latitude,
				Longitude = request.coordinateUser.longitude
			};


			logger.Info($"ResolveRadiusToChat: seeking chat of radius {ContentService.GetChatRadius(request.chatRadiusID).value} miles");

			ChatRadius data = ContentService.GetChatRadius(request.chatRadiusID);
			CoordinateBoundaries boundaries;

			if (data.units == DistanceUnits.British)
			{
				boundaries = new CoordinateBoundaries(userLocation, data.value, DistanceUnit.Miles);
			}
			else
			{
				boundaries = new CoordinateBoundaries(userLocation, data.value, DistanceUnit.Kilometers);
			}

			using (DataContext dbcontext = new())
			{
				// get the active chats with matching radius

				chats = dbcontext.Chats.Where(c => c.RadiusId == request.chatRadiusID
												&& c.Latitude >= boundaries.MinLatitude
												&& c.Latitude <= boundaries.MaxLatitude
												&& c.Longitude >= boundaries.MinLongitude
												&& c.Longitude <= boundaries.MaxLongitude)
									   .ToList();

				await dbcontext.DisposeAsync();

				logger.Info($"ResolveRadiusToChat: {chats.Count} chats matching radius {ContentService.GetChatRadius(request.chatRadiusID).value} miles");
			} // endif using

			if (chats.Count() > 0)
			{
				// find the closest 

				Chats? chatNearest = null;
				ChatRadius radiusdata = ContentService.GetChatRadius(request.chatRadiusID);
				double distance = radiusdata.value * 2.0;

				foreach (Chats chatTest in chats)
				{
					double distanceTest = GeoCalculator.GetDistance(userLocation.Latitude,
																	userLocation.Longitude,
																	chatTest.Latitude,
																	chatTest.Longitude,
																	3,          // number of decimal places
																	radiusdata.units == DistanceUnits.British ? DistanceUnit.Miles : DistanceUnit.Kilometers);
					if (distanceTest < distance)
					{
						chatNearest = chatTest;
						distance = distanceTest;
					}
				} // end foreach

				if (chatNearest != null
				&& distance <= ContentService.GetChatRadius(request.chatRadiusID).value)  // just check
				{
					logger.Info($"ResolveRadiusToChat: chat {chatNearest.Id} is nearest, {distance} miles");
					UsersInRadiusResponse counts = await UsersFriendsInChat(chatNearest.Id, request.userId);
					response = new()
					{
						chatId = chatNearest.Id,
						chatName = chatNearest.Name,
						center = new Coordinates
						{
							latitude = chatNearest.Latitude,
							longitude = chatNearest.Longitude,
							altitude = chatNearest.Altitude
						},
						chatRadiusID = chatNearest.RadiusId,
						countUsers = counts.countUsers,
						countFriends = counts.countFriends,
						chatCreated = false
					};
					logger.Info($"ResolveRadiusToChat: {counts.countUsers} users, {counts.countFriends} friends");
				}
				else
				{
					logger.Info($"ResolveRadiusToChat: no qualifying chats");
				}
			} // endif any chats

			if (response == null)
			{
				if (request.createNew)
				{
					logger.Info($"ResolveRadiusToChat: attempting to create chat");

					CreateChatRequest createchatrequest = new()
					{
						UserId = request.userId,
						Name = $"Chat{request.userId % 100000}",
						chatRadiusID = request.chatRadiusID,
						coordinates = request.coordinateUser
					};

					CreateChatResponse createchatresponse = await CreateChat(createchatrequest);
					List<Int64> userIds = createchatresponse.usersonline.Select(uo => uo.UserId).ToList();

					logger.Info($"ResolveRadiusToChat: chat {createchatresponse.ChatId} created");

					response = new();

					response.chatId = createchatresponse.ChatId;
					response.chatName = createchatrequest.Name;
					response.chatRadiusID = request.chatRadiusID;
					response.center = request.coordinateUser;
					response.chatCreated = true;
					response.countUsers = createchatresponse.usersonline.Count;
					response.countFriends = (new DataContext()).UserFriends.Where(uf => uf.userId == request.userId
																					   && userIds.Contains(uf.userIdFriend))
																			.Count();

				}
				else
				{
					response = new()
					{
						chatId = null
					};
				}
			} // endif response is null

			return response;
		} // end ResolveRadius

		// get the user/friend counts for an existing chat

		public static async Task<UsersInRadiusResponse> UsersFriendsInChat(Int32 chatId,
																		   Int64 userId)
		{
			UsersInRadiusResponse response = new();

			using (DataContext dbcontext = new())
			{
				List<Int64> userIds = dbcontext.ChatUsers   // all users in chat
												.Where(u => u.ChatId == chatId)
												.Select(u => u.UserId)
												.ToList();
				List<Int64> friendIds = dbcontext.UserFriends
												 .Where(f => f.userId == userId
														   && userIds.Contains(f.userIdFriend))
												 .Select(f => f.userIdFriend)
												 .ToList();
				await dbcontext.DisposeAsync();

				response.countUsers = userIds.Count;
				response.countFriends = friendIds.Count;
			}

			return response;
		}

		// true if chat still exists

		public static async Task<bool> RemoveUserFromChat(Int64 userId)
		{
			bool chatContinues = true;

			using (DataContext dbcontext = new())
			{
				ChatUsers? chatuser = await dbcontext.ChatUsers.Where(cu => cu.UserId == userId).FirstOrDefaultAsync();

				if (chatuser != null)
				{
					dbcontext.ChatUsers.Remove(chatuser);

					// if user was the only member of the chat, wipe it

					if (dbcontext.ChatUsers.Where(c => c.ChatId == chatuser.ChatId).Count() == 0)
					{
						await DeleteChat(chatuser.ChatId);

						chatContinues = false;
					}

					await dbcontext.SaveChangesAsync();
				}
				await dbcontext.DisposeAsync();
			}

			return chatContinues;
		}

		// DO NOT add SaveChanges!

		public static async Task DeleteChat(Int32 chatId)
		{
			using (DataContext dbcontext = new())
			{ 
				List<ChatMessages>		messages    = await dbcontext.ChatMessages.Where(m => m.ChatId == chatId).ToListAsync();
				List<ChatUsers>			users		= await dbcontext.ChatUsers			.Where(m => m.ChatId == chatId).ToListAsync();
				List<ChatUserTranslate> translates	= await dbcontext.ChatUserTranslate	.Where(m => m.chatId == chatId).ToListAsync();
				Chats?					chat		= await dbcontext.Chats.FindAsync(chatId);

				List<Int64> usersIDs = users.Select(u => u.UserId).ToList();

				List<UsersOnline> online = dbcontext.UsersOnline.Where(uo => usersIDs.Contains(uo.UserId)).ToList();

				List<string> keys = messages.Where(m => m.hasImage).Select(m => m.Id.ToString()).ToList();

				foreach (string s in  keys)
				{
					S3Service.DeleteChatMessageImage(s);
				};

				dbcontext.ChatMessages		.RemoveRange(messages);
				dbcontext.ChatUsers			.RemoveRange(users);
				dbcontext.UsersOnline		.RemoveRange(online);
				dbcontext.ChatUserTranslate	.RemoveRange(translates);
				dbcontext.Chats				.Remove(chat);

			    await dbcontext.SaveChangesAsync();
				await dbcontext.DisposeAsync();
			}

		}

		public static bool IsUserInRadius(UsersOnline useronline,
										  Chats chat)
		{
			bool inside = (useronline.Radius == chat.RadiusId);

			if (inside)
			{
				Coordinate location = new()
				{
					Latitude = chat.Latitude,
					Longitude = chat.Longitude
				};

				ChatRadius data = ContentService.GetChatRadius(chat.RadiusId);
				CoordinateBoundaries boundaries;

				if (data.units == DistanceUnits.British)
				{
					boundaries = new CoordinateBoundaries(location, data.value, DistanceUnit.Miles);
				}
				else
				{
					boundaries = new CoordinateBoundaries(location, data.value, DistanceUnit.Kilometers);
				}

				inside = useronline.Latitude >= boundaries.MinLatitude
					  && useronline.Latitude <= boundaries.MaxLatitude
					  && useronline.Longitude >= boundaries.MinLongitude
					  && useronline.Latitude <= boundaries.MaxLongitude;
			}

			return inside;
		}

		public static async Task<List<ChatMessage>> TranslateAndFilterMessages(List<ChatMessage> messagesOriginal,
																				Users			 userRecipient)
		{
			List<ChatMessage> messages = new();

			using (DataContext context = new())
			{
				List<string> forbiddenWords = context.ForbiddenWords
													 .Where(fw => fw.locale.Equals(userRecipient.locale))
													 .Select(fw => fw.word)
													 .ToList();

				List<ChatMessage> messagesToTranslate = new();

				foreach (ChatMessage m in messagesOriginal)
				{
					Users? userSender = await context.Users.Where(u => u.Id.Equals(m.UserIdSender)).FirstOrDefaultAsync();

					if (userSender.locale    != userRecipient.locale
					&&  userSender.locale    != null
					&&  userRecipient.locale != null)
					{
						// AWS will filter naughty words when the languages are different

						messagesToTranslate.Add(m);
					}
					else
					{
						// forbidden words filter

						string asterisks = "******************************************************";

						forbiddenWords.ForEach(w =>
						{
							m.Message = m.Message.Replace(w, asterisks.Substring(0, w.Length), StringComparison.CurrentCultureIgnoreCase);
						});

						messages.Add(m);
					}
				};

				if (messagesToTranslate.Count > 0)
				{
					List<string> optouts = await context.TranslationOptOuts
														.Where (oo => oo.UserId.Equals(userRecipient.Id))
														.Select(oo => oo.LocaleOptOut)
														.ToListAsync();
					foreach (ChatMessage mt in messagesToTranslate)
					{
						Users userSender = context.Users.Where(m => m.Id.Equals(mt.UserIdSender)).FirstOrDefault();

						if (optouts.Contains(userSender.locale))
						{
							messages.Add(mt);
						}
						else
						{
							if (String.IsNullOrEmpty(mt.Message) == false)
							{
								TranslateTextRequest requestTranslate = new();
								requestTranslate.Settings = new();

								requestTranslate.Settings.Profanity = Profanity.MASK;
								requestTranslate.SourceLanguageCode = userSender.locale;
								requestTranslate.TargetLanguageCode = userRecipient.locale;
								requestTranslate.Text				= mt.Message;

								using (AmazonTranslateClient clientTranslate = new())
								{
									TranslateTextResponse responseTranslate = await clientTranslate.TranslateTextAsync(requestTranslate);
									mt.Message = responseTranslate.TranslatedText;

									clientTranslate.Dispose();
								}
							}
							messages.Add(mt);
						}
					}
				}

				await context.DisposeAsync();
			}
			return messages;
		}

		public static async Task<List<DirectMessage>> TranslateAndFilterDirectMessages(List<DirectMessage> messagesOriginal,
																					   Users userRecipient)
		{
			List<DirectMessage> messages = new();

			using (DataContext context = new())
			{
				List<string> forbiddenWords = context.ForbiddenWords
													 .Where(fw => fw.locale.Equals(userRecipient.locale))
													 .Select(fw => fw.word)
													 .ToList();

				List<DirectMessage> messagesToTranslate = new();

				foreach (DirectMessage m in messagesOriginal)
				{
					string? localeSender = await context.Users.Where(u => u.Id.Equals(m.UserIdSender)).Select(u => u.locale).FirstOrDefaultAsync();

					if (localeSender != userRecipient.locale
					&&  localeSender != null
					&&  userRecipient.locale != null)
					{
						// AWS will filter naughty words when the languages are different

						messagesToTranslate.Add(m);
					}
					else
					{
						// forbidden words filter

						string asterisks = "******************************************************";

						forbiddenWords.ForEach(w =>
						{
							m.Message = m.Message.Replace(w, asterisks.Substring(0, w.Length), StringComparison.CurrentCultureIgnoreCase);
						});

						messages.Add(m);
					}
				};

				if (messagesToTranslate.Count > 0)
				{
					logger.Info($"TAFDM {messagesToTranslate.Count} messages to translate");

					List<string> optouts = await context.TranslationOptOuts
														.Where(oo => oo.UserId.Equals(userRecipient.Id))
														.Select(oo => oo.LocaleOptOut)
														.ToListAsync();
					foreach(DirectMessage mt in  messagesToTranslate)
					{
						string? localeSender = await context.Users.Where(u => u.Id.Equals(mt.UserIdSender)).Select(u => u.locale).FirstOrDefaultAsync();

						logger.Info($"TAFDM sender locale {localeSender}, message {mt.Message}");

						if (optouts.Contains(localeSender))
						{
							messages.Add(mt);
						}
						else
						{
							if (String.IsNullOrEmpty(mt.Message) == false)
							{
								TranslateTextRequest requestTranslate = new();
								requestTranslate.Settings = new()
								{
									Profanity = Profanity.MASK
								};

								requestTranslate.SourceLanguageCode = localeSender;
								requestTranslate.TargetLanguageCode = userRecipient.locale;
								requestTranslate.Text = mt.Message;

								using (AmazonTranslateClient clientTranslate = new())
								{
									TranslateTextResponse responseTranslate = await clientTranslate.TranslateTextAsync(requestTranslate);
									mt.Message = responseTranslate.TranslatedText;

									clientTranslate.Dispose();
								}

								messages.Add(mt);
							}
						}
					}
				}

				await context.DisposeAsync();
			}
			return messages;
		}
	} // end class
} // end namespace
