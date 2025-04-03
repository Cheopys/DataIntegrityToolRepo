using Amazon.S3.Model;
using Amazon.Translate;
using Amazon.Translate.Model;
using Geolocation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NLog;
using ProxChat.Db;
using ProxChat.Schema;
using ProxChat.Services;
using ProxChat.SharedObjectTypes;
using System.ComponentModel;
using System.Runtime.Intrinsics.X86;
using System.Text.Json;

namespace ProxChat.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class TestController : ControllerBase
	{
		static Logger logger;
		public TestController()
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

		[HttpPut, Route("SendDirectMessage")]
		public async Task SendDirectMessage([FromBody] SendDirectMessageRequest request)
		{
			request.timeSent = DateTime.UtcNow;

			await DirectMessageService.SendDirectMessage(request);
		}

		public class rettype
		{
			public UserInformation userinfo { get; set; }
			public Schema.PrivacySettings privacy { get; set; }
			public List<ProfileInterests> interests { get; set; }
		}

		[HttpPut, Route("CreateClientKeys")]
		[Produces("application/json")]
		public async Task<string> CreateClientKeys()
		{
			return ClientCryptographyService.CreateClientKeys();
		}

		[HttpPut, Route("CreateUserAndChat")]
		[Produces("application/json")]
		public async Task<object> CreateUserAndChat(string locale)
		{
			string json = ClientCryptographyService.CreateClientKeys();

			RegisterClientRequest? request = JsonSerializer.Deserialize<RegisterClientRequest>(json);
			Int64 userIdRegistering = await CryptographyService.RegisterClient(json);

			RegisterUserLightRequest requestU = new()
			{
				userId = userIdRegistering,
				moniker = $"Test Moniker {userIdRegistering % 10000}",
				email = $"testuser{userIdRegistering % 100000}@example.com",
				passwordHash = "5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8",
				birthdayString = "01/01/2001",
				locale = locale
			};

			UserInformation userinformation = await UsersService.RegisterUserLight(requestU);

			SetUserLocationAndChatRequest requestSULAC = new()
			{
				userId = userinformation.userId,
				chatRadiusID = 39,
				coordinates = new()
				{
					latitude  = 36.00141666f,
					longitude = -115.15958333f,
					altitude  = 1f
				}
			};

			ChatResolutionResponse response = await UsersService.SetUserLocationAndChat(requestSULAC);

			return new
			{
				userId = userinformation.userId,
				chatId = response.chatId
			};
		}

		[HttpGet, Route("ChatSendMessaage")]
		public async Task ChatSendMessaage(Int64 userIdSender,
										   Int32 chatId,
											string message)
		{
			ChatSendMessageRequest request = new()
			{
				userId = userIdSender,
				chatId = chatId,
				message = message
			};

			await ContentService.ChatSendMessage(request);
		}

		[HttpPut, Route("RegisterUserCrypto")]
		[Produces("application/json")]
		public async Task<UserInformation> RegisterUserCrypto()
		{
			string json = ClientCryptographyService.CreateClientKeys();

			RegisterClientRequest requestC = JsonSerializer.Deserialize<RegisterClientRequest>(json);

			Int64 userIdRegistering = await CryptographyService.RegisterClient(json);

			RegisterUserLightRequest requestU = new()
			{
				userId = userIdRegistering,
				moniker = $"Test Moniker{userIdRegistering % 100000}",
				email = $"testuser{userIdRegistering % 100000}@example.com",
				passwordHash = "5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8",
				birthdayString = "2000-01-01",
				locale = "en"
			};

			// client

			EncryptionWrapperProxChat? wrapper = await ClientCryptographyService.EncodeAndEncryptRequest(userIdRegistering,
																										  requestC.aeskey,
																										 requestC.aesiv,
																										 requestU);

			// server

			CryptographyService.DecodeAndDecryptRequest(wrapper, out requestU, true);

			UserInformation userinformation = await UsersService.RegisterUserLight(requestU);

			string responseJson = await CryptographyService.EncryptAndEncodeResponse(userIdRegistering, userinformation, true);

			// client

			responseJson = ClientCryptographyService.DecodeAndDecryptResponse<UserInformation>(responseJson,
																								requestC.aeskey,
																								requestC.aesiv);

			// remove the registering user

			using (DataContext context = new())
			{
				UserRegistering? userRegistering = context.UserRegistering.Where(i => i.Id == requestU.userId).FirstOrDefault();
				if (userRegistering != null)
				{
					context.UserRegistering.Remove(userRegistering);
					await context.SaveChangesAsync();
				}

				await context.DisposeAsync();
			}

			return JsonSerializer.Deserialize<UserInformation>(responseJson);
		}

		[HttpPost, Route("ChatSendMessageRaw")]
		public async Task ChatSendMessageRaw(ChatSendMessageRequest requestRaw)
		{
			await ContentService.ChatSendMessage(requestRaw);
		}

		[HttpPost, Route("ClearDataTables")]
		public async Task ClearDataTables()
		{
			using (DataContext context = new())
			{
				List<UserRegistering> registers		= context.UserRegistering.ToList();
				List<Schema.Customers> users					= context.Users.ToList();
				List<PrivacySettings> privacies		= context.PrivacySettings.ToList();
				List<UserProfiles> profiles			= context.UserProfiles.ToList();
				List<Products> chats					= context.Chats.ToList();
				List<UsersOnline> usersonline		= context.UsersOnline.ToList();
				List<ChatUsers> chatusers			= context.ChatUsers.ToList();
				List<Licenses> messages			= context.ChatMessages.ToList();
				List<UserInterests> interests		= context.UserInterests.ToList();
				List<UserFriends> friends			= context.UserFriends.ToList();
				List<FriendRequests> friendrequests = context.FriendRequests.ToList();
				List<DirectMessages> directmessages	= context.DirectMessages.ToList();
				List<ProfileInterests> profints		= context.ProfileInterests.ToList();

				context.Users				.RemoveRange(users);
				context.UserRegistering     .RemoveRange(registers);
				context.PrivacySettings		.RemoveRange(privacies);
				context.UserProfiles		.RemoveRange(profiles);
				context.Chats				.RemoveRange(chats);
				context.UsersOnline			.RemoveRange(usersonline);
				context.ChatUsers			.RemoveRange(chatusers);
				context.ChatMessages		.RemoveRange(messages);
				context.DirectMessages		.RemoveRange(directmessages);
				context.UserInterests		.RemoveRange(interests);
				context.UserFriends			.RemoveRange(friends);
				context.FriendRequests      .RemoveRange(friendrequests);
				context.ProfileInterests	.RemoveRange(profints);

				await context.SaveChangesAsync();
				await context.DisposeAsync();
			}

			S3Service.DeleteAllS3Storage();
		}

		[HttpPost, Route("MoveUserInsideChat")]
		[Produces("application/json")]

		public async Task<string> MoveUserInsideChat(Int64 userId,
													 Int32 chatId)
		{
			List<ChatUsers> chatusers = new();

			using (DataContext context = new())
			{
				Products? chat = await context.Chats.FindAsync(chatId);
				Coordinates location = GetLocationInsideRadius(chatId, context);

				chatusers = await context.ChatUsers.Where(cu => cu.ChatId == chatId).ToListAsync();

				logger.Info($"chat {chat.Name} has {chatusers.Count} users");

				SetUserLocationAndChatRequest request = new()
				{
					userId = userId,
					chatRadiusID = chat.RadiusId,
					coordinates = location
				};

				logger.Info($"setting user {userId} to {location.latitude} x {location.longitude}");

				await UsersService.SetUserLocationAndChat(request);

				chatusers = await context.ChatUsers.Where(cu => cu.ChatId == chatId).ToListAsync();

				logger.Info($"chat {chat.Name} now has {chatusers.Count} users");

				await context.SaveChangesAsync();
				await context.DisposeAsync();
			}

			return JsonSerializer.Serialize(chatusers);
		}

		[HttpPost, Route("MoveUserOutsideChat")]
		[Produces("application/json")]

		public async Task<string> MoveUserOutsideChat(Int64 userId)
		{
			SetUserLocationResponse response = new();
			List<ChatUsers> chatusers = new();

			using (DataContext context = new())
			{
				ChatUsers? chatuser = await context.ChatUsers.Where(cu => cu.UserId == userId).FirstOrDefaultAsync();

				if (chatuser != null)
				{
					Products? chat = await context.Chats.FindAsync(chatuser.ChatId);
					Coordinates location = GetLocationOutsideRadius(chatuser.ChatId, context);

					logger.Info($"user {userId} is in chat {chatuser.ChatId}");

					SetUserLocationAndChatRequest request = new()
					{
						userId = userId,
						chatRadiusID = chat.RadiusId,
						coordinates = location
					};

					logger.Info($"setting user {userId} to {location.latitude} x {location.longitude}");

					response = await UsersService.UpdateUserLocation(request);

					chatusers = await context.ChatUsers.Where(cu => cu.ChatId == chat.Id).ToListAsync();

					logger.Info($"chat {chat.Name} now has {chatusers.Count} users");

					await context.SaveChangesAsync();
				}
				else
				{
					logger.Info($"user {userId} is not in a chat; aborting");
				}

				await context.DisposeAsync();
			}

			return JsonSerializer.Serialize(response);
		}
		/*
		[HttpPost, Route("ChangeChatUserRadius")]
		[Produces("application/json")]
		public async Task<string> ChangeChatUserRadius()
		{
			RegisterClientRequest requestC =  ClientCryptographyService.CreateClientKeys();
			Int64 userId         = await CryptographyService.RegisterClient(requestC);
			Int32? chatIdInitial = 0;
			Int32? chatIdAfter   = 0;

			RegisterUserRequest requestU = new()
			{
				userId   	 = userId,
				firstName	 = "Test",
				lastName	 = "User",

				email		 = $"testuser{userId % 100000}@example.com",
				passwordHash = "5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8",
				pronoun		 = SharedObjectTypes.Pronouns.PreferNot,
				relationship = Relationship.InaRelationship,
				dateOfBirth	 = DateTime.Parse("2000-01-01"),
				interests	 = new List<string>()
			};

			await UsersService.RegisterUser(requestU, null);

			// put user in a chat

			SetUserLocationAndChatRequest requestS = new()
			{
				userId = userId,
				chatRadiusID = 37, //Hundred Feet,
				coordinates = new()
				{
					latitude  = 10.652,
					longitude = 105.135,
					altitude  = 0
				}
			};

			ChatResolutionResponse response = await UsersService.SetUserLocationAndChat(requestS);

			chatIdInitial = response.chatId;

			// now change user's chat radius, which disqualifies him from the chat he's already in

			requestS.chatRadiusID = 39; //TwoThousandFeet;

			response = await UsersService.SetUserLocationAndChat(requestS);

			chatIdAfter = response.chatId;

			// clean up

			await UsersService.RemoveUserFromChat(userId);

			await DeleteUser(userId);

			return $"chat Id initial = {chatIdInitial}, after radius change, chat Id = {chatIdAfter}";
		}
		*/
		// classes for encryption test

		public class TestRequest
		{
			public Int64 userId { get; set; }
			public string message { get; set; }
		}
		public class TestResponse
		{
			public Int64 userId { get; set; }
			public string message { get; set; }
		}

		public class TestOutput
		{
			public TestRequest request { get; set; }
			public TestRequest requestDecrypted { get; set; }
			public TestResponse response { get; set; }
			public TestResponse responseDecrypted { get; set; }
		}
		/*
		[HttpPut, Route("EncryptionTest")]
		[Produces("application/json")]
		public async Task<string> EncryptionTest()
		{
			string returnString;
			TestOutput output = new();

			output.request  = new();
			output.response = new();

			Users? user = new DataContext().Users.FirstOrDefault();

			if (user != null)
			{
				byte[] aesKey = user.aeskey;
				byte[] aesIV  = user.aesiv;

				logger.Info($"key {Convert.ToBase64String(aesKey)}, IV = {Convert.ToBase64String(aesIV)}");

				// client to server

				output.request.userId  = user.Id;
				output.request.message = "request message";

				logger.Info($"request with user ID {user.Id}");

				 EncryptionWrapperProxChat? wrapper = await ClientCryptographyService.EncodeAndEncryptRequest<TestRequest>(output.request.userId,
																														   aesKey,
																														   aesIV,
																														   output.request);

				logger.Info($"encoded/encrypted request {wrapper.encryptedRequest}");

				TestRequest requestDecrypted;
				CryptographyService.DecodeAndDecryptRequest<TestRequest>(wrapper, out requestDecrypted);

				logger.Info($"decrypted request {JsonSerializer.Serialize(requestDecrypted)}");

				output.requestDecrypted = requestDecrypted;

				// server to client

				output.response.userId  = output.request.userId;
				output.response.message = "response message";

				string encryptedResponseJSON = await CryptographyService.EncryptAndEncodeResponse<TestResponse>(output.request.userId, 
																												output.response);

				logger.Info($"encoded response length: {encryptedResponseJSON.Length}");

				string json = ClientCryptographyService.DecodeAndDecryptResponse(encryptedResponseJSON, 
																   aesKey,
																   aesIV);
				output.responseDecrypted = JsonSerializer.Deserialize<TestResponse>(json);

				returnString = JsonSerializer.Serialize(output);
			}
			else
			{
				returnString = "no users in database";
			}

			return returnString;
		}
		*/
		// helper functions
		private List<string> GetAllInterests()
		{
			List<string> names = null;

			using (DataContext context = new())
			{
				names = context.InterestNames.Select(n => n.Name).ToList();

				context.Dispose();
			}

			return names;
		}

		private Coordinates GetLocationInsideRadius(Int32 chatId,
													DataContext context)
		{
			Products? chat = context.Chats.Find(chatId);
			Coordinate location = new();

			if (chat != null)
			{
				location = new()
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

				location.Latitude += (boundaries.MaxLatitude - location.Latitude) / 2.0;
				location.Longitude += (boundaries.MaxLongitude - location.Longitude) / 2.0;
			}
			else
			{
				location.Latitude = 0.0;
				location.Longitude = 0.0;
			}

			return new Coordinates
			{
				latitude = location.Latitude,
				longitude = location.Longitude,
				altitude = 0.0
			};
		}

		private Coordinates GetLocationOutsideRadius(Int32 chatId,
													DataContext context)
		{
			Products? chat = context.Chats.Find(chatId);
			Coordinate location = new();

			if (chat != null)
			{
				location = new()
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

				location.Latitude += (boundaries.MaxLatitude - location.Latitude) * 2.0;
				location.Longitude += (boundaries.MaxLongitude - location.Longitude) * 2.0;
			}
			else
			{
				location.Latitude = 0.0;
				location.Longitude = 0.0;
			}

			return new Coordinates
			{
				latitude = location.Latitude,
				longitude = location.Longitude,
				altitude = 0.0
			};
		}

		[HttpPut, Route("CreateTestUserLight")]
		[Produces("application/json")]
		public async Task<string> CreateTestUserLight()
		{
			string json = ClientCryptographyService.CreateClientKeys();
			Int64 userId = await CryptographyService.RegisterClient(json);

			RegisterUserLightRequest requestU = new()
			{
				userId			= userId,
				email			= $"testuser{userId % 100000}@example.com",
				passwordHash	= "5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8",
				birthdayString	= "2000-01-01",
				moniker			= "testuser",
			};

			UserInformation userinformation = await UsersService.RegisterUserLight(requestU);

			return JsonSerializer.Serialize(userinformation);
		}

		[HttpDelete, Route("DeleteUser")]
		public async Task DeleteUser(Int64 userId)
		{
			using (DataContext context = new())
			{
                Schema.Customers? user = await context.Users.FindAsync(userId);

				if (user != null)
				{
					List<UserInterests> userinterests = await context.UserInterests.Where(ui => ui.UserId == userId).ToListAsync();
					List<ChatUsers> chatusers = await context.ChatUsers.Where(cu => cu.UserId == userId).ToListAsync();
					List<Licenses> messages = await context.ChatMessages.Where(cm => cm.UserIdSender == userId).ToListAsync();
					List<UserFriends> friends = await context.UserFriends.Where(uf => uf.userId == userId
																							  || uf.userIdFriend == userId).ToListAsync();
					List<FriendRequests> friendrequests = await context.FriendRequests.Where(uf => uf.UserId == userId
																							   || uf.FriendId == userId).ToListAsync();
					List<UserRatings> ratings = await context.UserRatings.Where(uf => uf.UserId == userId
																							   || uf.UserIdRated == userId).ToListAsync();
					List<DirectMessages> directmessages = await context.DirectMessages.Where(uf => uf.userIdSender == userId
																							   || uf.userIdRecipient == userId).ToListAsync();
					List<Schema.PrivacySettings> privacysettings = await context.PrivacySettings.Where(pr => pr.UserId == userId).ToListAsync();

					List<TranslationOptOut> optouts = await context.TranslationOptOuts.Where(to => to.UserId.Equals(userId)).ToListAsync();

					foreach (Schema.PrivacySettings privacysetting in privacysettings)
					{
						List<ProfileInterests> interests = await context.ProfileInterests.Where(pi => pi.ProfileId == privacysetting.Id).ToListAsync();
						context.ProfileInterests.RemoveRange(interests);
						context.PrivacySettings.Remove(privacysetting);
					}

					context.DirectMessages.RemoveRange(directmessages);
					context.UserRatings.RemoveRange(ratings);
					context.FriendRequests.RemoveRange(friendrequests);
					context.UserFriends.RemoveRange(friends);
					context.ChatMessages.RemoveRange(messages);
					context.UserInterests.RemoveRange(userinterests);
					context.TranslationOptOuts.RemoveRange(optouts);
					context.Users.Remove(user);

					await context.SaveChangesAsync();
				}

				await context.DisposeAsync();
			};
		}

		[HttpGet, Route("GetCurrentUserInformation")]
		public async Task<UserInformation> GetCurrentUserInformation(Int64 userId)
		{
			return await UsersService.GetCurrentUserInformation(userId);
		}

		[HttpGet, Route("GetCurrentUserInformationJSON")]
		public async Task<UserInformation> GetCurrentUserInformationJSON(Int64 userId)
		{
			UserInformation userinformation = await UsersService.GetCurrentUserInformation(userId);

			string json = JsonSerializer.Serialize(userinformation);

			return JsonSerializer.Deserialize<UserInformation>(json);
		}

		[HttpPut, Route("CreateTestUser")]
		public async Task<UserInformation> CreateTestUser()
		{
			UserInformation userinformation = new();

			string json = ClientCryptographyService.CreateClientKeys();

			Int64 userIdRegistering = await CryptographyService.RegisterClient(json);

			RegisterUserRequest request = new()
			{
				userId = userIdRegistering,
				firstName = "Test",
				lastName = "User",
				email = $"testuser{userIdRegistering % 100000}@example.com",
				passwordHash = "5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8",
				pronoun = Pronouns.PreferNot,
				relationship = Relationship.InaRelationship,
				phoneNumber = "206-666-6666",
				dateOfBirth = DateTime.Parse("2000-01-01"),
				interests = new List<string>(),
				locale = "en",
				distanceUnits = DistanceUnits.British
			};

			using (DataContext dbcontext = new())
			{
				System.Security.Cryptography.Aes aes = CryptographyService.CreateAesKey();

				logger.Info($"RegisterUserS: created permanent AES key {aes.Key} for user {request.userId}");

				if (request.phoneNumber == null)
				{
					request.phoneNumber = "PN came in null";
				}

                Schema.Customers user = new()
				{
					Id = new Random().NextInt64(),
					email = request.email,
					firstName = request.firstName,
					lastName = request.lastName,
					phoneNumber = request.phoneNumber,
					birthdayDate = request.dateOfBirth,
					passwordHash = request.passwordHash,
					pronoun = request.pronoun,
					relationshipId = request.relationship,
					TimeNotifications = DateTime.UtcNow,
					locale = "en",
					units = request.distanceUnits,
					aeskey = aes.Key,
					aesiv = aes.IV,
				};

				await dbcontext.Users.AddAsync(user);

				logger.Info($"RegisterUserS: birthday {request.dateOfBirth} parsed to {user.birthdayDate.ToString()}");

				await dbcontext.UsersOnline.AddAsync(new UsersOnline
				{
					UserId = user.Id,
					TimeOnline = DateTime.UtcNow,
					Radius = (Int16)((request.distanceUnits == DistanceUnits.British) ? 37 : 38)
				});

				await dbcontext.SaveChangesAsync();

				UserProfiles profile = null;
				PrivacySettings privacy = TestController.CreateDefaultPrivacy(request, out profile);

				user.currentProfileId = profile.Id;
				await dbcontext.SaveChangesAsync();

				List<InterestNames> interestnames = dbcontext.InterestNames.ToList();
				List<UserInterests> interests = new();
				List<ProfileInterests> profileInterests = new(request.interests.Count);

				// if this interest is not in the master list, add it

				await dbcontext.UserInterests.AddRangeAsync(interests);

				logger.Info($"RegisterUserS: user has {interests.Count} interests");

				userinformation = await UsersService.GetCurrentUserInformation(user.Id);
				await dbcontext.SaveChangesAsync();

				logger.Info($"RegisterUserS: userinformation {userinformation}");

				await dbcontext.DisposeAsync();
			}

			return userinformation;
		}

		private static PrivacySettings CreateDefaultPrivacy(RegisterUserRequest request, out UserProfiles profilePublicOut)
		{
			UserProfiles profilePublic = new()
			{
				userId = request.userId,
				profileType = UserProfileType.profileTypePublic,
				isCurrent = true,
				name = "Public",
				moniker = request.moniker,
				email = request.email,
			};

			PrivacySettings privacyPublic = new Schema.PrivacySettings
			{
				UserId = request.userId,
				firstNamePrivate = false,
				lastNamePrivate = false,
				emailPrivate = true,
				birthdayPrivate = false,
				pronounPrivate = false,
				relationshipPrivate = false
			};

			// Save the orivacy settings first to get their primary keys, needed by the UserProfiles

			using (DataContext context = new())
			{
				context.PrivacySettings.Add(privacyPublic);
				context.SaveChanges();

				profilePublic.privacyId = privacyPublic.Id;

				context.UserProfiles.Add(profilePublic);
				context.SaveChanges();
				context.Dispose();
			}

			profilePublicOut = profilePublic;

			return privacyPublic;
		}

		[HttpGet, Route("GetForbiddenWords")]
		public List<string> GetForbiddenWords(string locale)
		{
			return ContentService.GetForbiddenWords(locale);
		}

		[HttpGet, Route("AWSTranslateTest")]

		public async Task<string> AWSTranslateTest(string message, string localeSender = "es")
		{
			TranslateTextRequest req = new();

			req.Settings = new();

			req.Settings.Profanity = Profanity.MASK;
			req.SourceLanguageCode = localeSender;
			req.TargetLanguageCode = "en";
			req.Text = message;

			TranslateTextResponse resp = new();
			using (AmazonTranslateClient clientTranslate = new())
			{
				resp = await clientTranslate.TranslateTextAsync(req);

				clientTranslate.Dispose();
			}

			return resp.TranslatedText;
		}
		/*

		[HttpGet, Route("TranslationTest")]
		public async Task<List<string>> TranslationText()
		{
			List<string> messages = new();
			Int64 userIdRegistering = 0;
			Int64 userEnglish;
			Int64 userGerman;

			// create users and chat

			RegisterClientRequest request = ClientCryptographyService.CreateClientKeys();

			userIdRegistering = await CryptographyService.RegisterClient(request);

			RegisterUserRequest requestU = new()
			{
				userId			= userIdRegistering,
				firstName		= "Test",
				lastName		= "User",
				email			= $"testuser{userIdRegistering % 100000}@example.com",
				passwordHash	= "5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8",
				pronoun			= SharedObjectTypes.Pronouns.PreferNot,
				relationship	= Relationship.InaRelationship,
				phoneNumber		= "206-666-6666",
				dateOfBirth		= DateTime.Parse("2000-01-01"),
				interests		= GetAllInterests(),
				locale			= "en",
				distanceUnits	= DistanceUnits.British
			};

			UserInformation userinfo = await UsersService.RegisterUser(requestU, null);

			userEnglish = userinfo.userId;

			request = ClientCryptographyService.CreateClientKeys();
			userIdRegistering = await CryptographyService.RegisterClient(request);

			requestU.locale = "de";

			userinfo = await UsersService.RegisterUser(requestU, null);

			userGerman = userinfo.userId;

			SetUserLocationAndChatRequest requestSULAC = new()
			{
				userId			= userEnglish,
				chatRadiusID	= 37,
				coordinates		= new()
				{
					latitude  = 50,
					longitude = 50
				}
			};

			await UsersService.SetUserLocationAndChat(requestSULAC);

			requestSULAC.userId = userGerman;

			ChatResolutionResponse response = await UsersService.SetUserLocationAndChat(requestSULAC);

			ChatSendMessageRequest requestSendMessage = new()
			{
				userId  = userEnglish,
				chatId  = response.chatId.Value,
				message = "English message"
			};

			await ChatService.ChatSendMessage(requestSendMessage);

			messages.Add(requestSendMessage.message);

			requestSendMessage.userId  = userGerman;
			requestSendMessage.message = "Nachricht auf Deutsch nicht übersetzt";

			await ChatService.ChatSendMessage(requestSendMessage);

			messages.Add(requestSendMessage.message);

			AmazonTranslateClient clientTranslate = new();

			TranslateTextRequest requestTranslate = new TranslateTextRequest
			{
				SourceLanguageCode = "de",
				TargetLanguageCode = "en",
				Text			   = "Nachricht übersetzt aus dem Deutschen",
			};

			TranslateTextResponse responseTranslate = await clientTranslate.TranslateTextAsync(requestTranslate);

			messages.Add(responseTranslate.TranslatedText);

			requestTranslate.SourceLanguageCode = "en";
			requestTranslate.TargetLanguageCode = "de";
			requestTranslate.Text				= "English response translated into German";
		
			responseTranslate = await clientTranslate.TranslateTextAsync(requestTranslate);

			messages.Add(responseTranslate.TranslatedText);

			DeleteUser(userEnglish);
			DeleteUser(userGerman);
			ChatService.DeleteChat(requestSendMessage.chatId, new DataContext());

			return messages;
		}
		
		[HttpGet, Route("TranslationTestService")]
		public async Task<List<string>> TranslationTextService(bool testOptOut = false)
		{
			List<string> messages = new();
			Int64 userIdRegistering = 0;
			Users userEnglish;
			Users userTaiwan;

			// create users and chat

			RegisterClientRequest request = ClientCryptographyService.CreateClientKeys();

			userIdRegistering = await CryptographyService.RegisterClient(request);

			RegisterUserRequest requestU = new()
			{
				userId			= userIdRegistering,
				firstName		= "Test",
				lastName		= "User",
				email			= $"testuser{userIdRegistering % 100000}@example.com",
				passwordHash	= "5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8",
				pronoun	= SharedObjectTypes.Pronouns.PreferNot,
				relationship	= Relationship.InaRelationship,
				phoneNumber		= "206-666-6666",
				dateOfBirth		= DateTime.Parse("2000-01-01"),
				interests		= GetAllInterests(),
				locale			= "en",
				distanceUnits	= DistanceUnits.British
			};

			UserInformation userinfo = await UsersService.RegisterUser(requestU, null);

			using (DataContext context = new())
			{
				userEnglish = await context.Users.FindAsync(userinfo.userId);

				request = ClientCryptographyService.CreateClientKeys();
				userIdRegistering = await CryptographyService.RegisterClient(request);

				requestU.locale = "zh-TW";

				userinfo = await UsersService.RegisterUserLight(requestU, null);

				userTaiwan = await context.Users.FindAsync(userinfo.userId);

				SetUserLocationAndChatRequest requestSULAC = new()
				{
					userId = userEnglish.Id,
					chatRadiusID = 37,
					coordinates = new()
					{
						latitude = 50,
						longitude = 50
					}
				};

				ChatResolutionResponse response = await UsersService.SetUserLocationAndChat(requestSULAC);

				requestSULAC.userId = userTaiwan.Id;

				ChatResolutionResponse responseChat = await UsersService.SetUserLocationAndChat(requestSULAC);

				// create messages

				List<ChatMessage> chatmessages = new();

				ChatMessage chatmessage = new ChatMessage()
				{
					UserId = userEnglish.Id,
					Message = "English message"
				};

				chatmessages.Add(chatmessage);

				messages.Add(chatmessage.Message);

				chatmessage.UserId = userTaiwan.Id;
				chatmessage.Message = "台語留言";

				messages.Add("Taiwanese message, no opt-out");
				messages.Add(chatmessage.Message);

				List<ChatMessage> messagesTranslated = await TranslationService.TranslateMessages(userEnglish, chatmessages);

				messages.Add(messagesTranslated[0].Message);

				chatmessages.RemoveRange(0, chatmessages.Count);

				chatmessage.Message = "另一則台語留言";
				chatmessages.Add(chatmessage);

				messages.Add("second Taiwanese message, with opt-out");
				messages.Add(chatmessage.Message);

				context.TranslationOptOuts.Add(new TranslationOptOut
				{
					UserId = userEnglish.Id,
					LocaleOptOut = "zh-TW"
				}); ;

				messagesTranslated = await TranslationService.TranslateMessages(userEnglish, chatmessages);
				messages.Add(messagesTranslated[0].Message);

				DeleteUser(userEnglish.Id);
				DeleteUser(userTaiwan.Id);
				ChatService.DeleteChat(responseChat.chatId.Value, context);

				await context.SaveChangesAsync();
				await context.DisposeAsync();

				messages.ForEach(m =>
				{
					Console.WriteLine($"message\n");
				});
			}
			return messages;
		}
		*/

		public class DetectObsceneImageRequest
		{
			public string name { get; set; }
			public string b64 { get; set; }
		};

		[HttpPost, Route("DetectObsceneImages")]
		[Produces("application/json")]
		public async Task<string> DetectObsceneImages([FromBody] DetectObsceneImageRequest request)
		{
			byte[] image = Convert.FromBase64String(request.b64);

			List<ObscenityViolations> violations = await ImageModerationService.ModerateImage(0, ImageRejectionType.Test, request.name, image);

			if (violations.Count == 0)
			{
				violations.Add(new ObscenityViolations()
				{
					label = "no violations detected",
					confidence = 0f,
					parent = String.Empty
				});
			}

			return JsonSerializer.Serialize(violations);
		}

		[HttpGet, Route("GetUserProfilesRaw")]
		public async Task<List<UserProfile>> GetUserProfilesRaw(Int64 userId)
		{
			return await UsersService.GetUserProfiles(userId);
		}

		[HttpPost, Route("UpdateProfileRaw")]
		public async Task UpdateProfileRaw([FromBody] UpdateProfileRequest request)
		{
			await UsersService.UpdateProfile(request);
		}

		[HttpPost, Route("UpdateUserRaw")]
		public async Task<UserUpdateResponse> UpdateUserRaw([FromBody] UserUpdateRequest request)
		{
			UserUpdateResponse response = await UsersService.UpdateUser(request);

			return response;
		}

		[HttpGet, Route("GetUserNotificationsRaw")]
		public async Task<UserNotificationResponse> GetUserNotificationsRaw(Int64 userId)
		{
			return await UsersService.GetUserNotifications(userId);
		}

		[HttpGet, Route("LocalesList")]
		public List<Locale> LocalesList()
		{
			List<Locale> locales = new();

			using (DataContext context = new())
			{
				List<string> localesDB      = context.ApplicationText.Select(l => l.locale).Distinct().ToList();
				List<Locales> localesSchema = context.Locales.Where(l => localesDB.Contains(l.locale)).ToList();

				locales = localesSchema.OrderBy(l => l.nameLocalized)
									.Select(l => new Locale
									{
										locale        = l.locale,
										nameEnglish   = l.nameEnglish,
										nameLocalized = l.nameLocalized
									})
									.ToList();
				context.Dispose();
			}

			return locales;
		}

		[HttpGet, Route("GetFriends")]
		public async Task<List<Int64>> GetFriends(Int64 userId)
		{
			return await FriendsService.GetFriends(userId);
		}

		[HttpGet, Route("GetBlockedUsers")]
		public async Task<List<Int64>> GetBlockedUsers(Int64 userId)
		{
			return await UsersService.GetBlockedUsers(userId);
		}

		[HttpGet, Route("GetChatImage")]
		public async Task<string> GetChatImage(Int32 chatId)
		{
			string imageB64 = String.Empty;

			S3Attachment attachment = await S3Service.GetChatMessageImage(chatId.ToString());

			if (attachment.error.httpCode == System.Net.HttpStatusCode.OK) 
			{
				imageB64 = Convert.ToBase64String(attachment.image);
			}

			return imageB64;
		}

		[HttpGet, Route("GetDirectMessageImage")]
		public async Task<string> GetDirectMessageImage(Int32 dmId)
		{
			string imageB64 = String.Empty;

			S3Attachment attachment = await S3Service.GetDirectMessageAttachment(dmId.ToString());

			if (attachment.error.httpCode == System.Net.HttpStatusCode.OK)
			{
				imageB64 = Convert.ToBase64String(attachment.image);
			}

			return imageB64;
		}

		[HttpPost, Route("UpdateProfile")]
		public async Task UpdateProfile(UpdateProfileRequest request)
		{
			using (DataContext context = new())
			{
				UserProfiles? profileSchema = await context.UserProfiles.FindAsync(request.profileId);

				await UsersService.UpdateProfile(request);

				await context.SaveChangesAsync();
				await context.DisposeAsync();
			}
		}

		[HttpPost, Route("JanitorCleanup")]
		public void JanitorCleanuo()
		{
			JanitorService.Initialize();
		}
	}
}	