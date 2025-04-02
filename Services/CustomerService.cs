using Microsoft.EntityFrameworkCore;
using ProxChat.Db;
using ProxChat.Schema;
using ProxChat.Services;
using ProxChat.SharedObjectTypes;
using System.Security.Cryptography;
using Geolocation;
using NuGet.Versioning;
using Amazon.Translate;
using Amazon.Translate.Model;
using Amazon.Runtime.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Expressions.Internal;
using Humanizer;
using System.Net;
using NLog;
using NLog.LayoutRenderers;
using System.Text.Json;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Drawing;
using NuGet.Packaging;
using Amazon.Rekognition.Model;
using System.Globalization;

namespace ProxChat.Services
{
	public static class UsersService
	{
		static Logger logger;
		static UsersService()
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
		public static string MagicPronounToString(SharedObjectTypes.Pronouns pronoun)
		{
			string maicpronoun = "invalid Pronouns enum";

			switch (pronoun)
			{
				case SharedObjectTypes.Pronouns.He:
					maicpronoun = "He/Him";
					break;

				case SharedObjectTypes.Pronouns.She:
					maicpronoun = "She/Her";
					break;

				case SharedObjectTypes.Pronouns.They:
					maicpronoun = "Unsexed";
					break;

				case SharedObjectTypes.Pronouns.PreferNot:
					maicpronoun = "Prefer not to say";
					break;
			}

			return maicpronoun;
		}

		public static SharedObjectTypes.Pronouns PronounStringToID(string pronoun)
		{
			SharedObjectTypes.Pronouns ID = SharedObjectTypes.Pronouns.PreferNot;

			switch (pronoun)
			{
				case "PreferNot":
					ID = SharedObjectTypes.Pronouns.PreferNot;
					break;

				case "Female":
					ID = SharedObjectTypes.Pronouns.She;
					break;

				case "Prefer not to say":
					ID = SharedObjectTypes.Pronouns.PreferNot;
					break;
			}

			return ID;
		}

		public static string RelationshipIdToString(Relationship relationshipId)
		{
			string relationship = "invalid Pronouns enum";

			switch (relationshipId)
			{
				case Relationship.Single:
					relationship = "Single";
					break;

				case Relationship.InaRelationship:
					relationship = "In a relationship";
					break;

				case Relationship.Married:
					relationship = "Married";
					break;

				case Relationship.PreferNot:
					relationship = "Prefer not to say";
					break;
			}

			return relationship;
		}

		public static Relationship RelationshipStringToID(string relationship)
		{
			Relationship ID = Relationship.PreferNot;

			switch (relationship)
			{
				case "Single":
					ID = Relationship.Single;
					break;

				case "In a relationship":
					ID = Relationship.InaRelationship;
					break;

				case "Married":
					ID = Relationship.Married;
					break;

				case "Prefer not to say":
					ID = Relationship.PreferNot;
					break;
			}

			return ID;
		}
		public static async Task SetNewChatRadius(SetChatRadiusRequest request)
		{
			// if the user is already in a chat, yank him out

			using (DataContext dbcontext = new())
			{
				Users user = dbcontext.Users.Find(request.userId);
				UsersOnline useronline = await dbcontext.UsersOnline.Where(uo => uo.UserId == request.userId).FirstOrDefaultAsync();
				// if already set for this radius, do nothing

				if (useronline != null
				&& useronline.Radius != request.chatRadiusID)
				{

					//  if user is in a chat, remove him because
					//  his radius no longer matches the chat's

					ChatUsers chatuser = await dbcontext.ChatUsers.Where(u => u.UserId == request.userId).FirstOrDefaultAsync();

					// if user was the only member of the chat, wipe it

					if (chatuser != null)
					{
						await UsersService.RemoveUserFromChat(user.Id);
					}
				} // endif already using this radius

				user.recentRadius = request.chatRadiusID;

				await dbcontext.SaveChangesAsync();
				await dbcontext.DisposeAsync();
			}
		}


		private static void CreateUserProfiles(Int64						  userId,
												 RegisterUserLightRequest     registration,
										     out List<UserProfiles>    profiles,
											 out List<PrivacySettings> privacies)
		{
			List<UserProfiles>    profilesNew  = new();
			List<Schema.PrivacySettings> privaciesNew = new();

			UserProfiles profilePublic = new()
			{
				userId		= userId,
				profileType = UserProfileType.profileTypePublic,
				isCurrent	= true,
				name		= "Public",
				moniker		= registration.moniker,
				email		= registration.email,
			},
			profilePrivate = new()
			{
				userId		= userId,
				profileType = UserProfileType.profileTypePrivate,
				isCurrent	= false,
				name		= "Private",
				moniker		= registration.moniker,
				email		= registration.email,
			},
			profileBusiness = new()
			{
				userId		= userId,
				profileType = UserProfileType.profileTypeBusiness,
				isCurrent	= false,
				name		= "Business",
				moniker		= registration.moniker,
				email		= registration.email,
			};

			PrivacySettings privacyPublic = new Schema.PrivacySettings
			{
				UserId				= userId,
				firstNamePrivate	= false,
				lastNamePrivate		= false,
				emailPrivate		= true,
				birthdayPrivate		= false,
				pronounPrivate		= false,
				relationshipPrivate = false
			},
			privacyPrivate = new Schema.PrivacySettings
			{
				UserId				= userId,
				firstNamePrivate	= false,
				lastNamePrivate		= true,
				emailPrivate		= true,
				birthdayPrivate		= true,
				pronounPrivate		= false,
				relationshipPrivate = true
			},
			privacyBusiness = new Schema.PrivacySettings
			{
				UserId				= userId,
				firstNamePrivate	= false,
				lastNamePrivate		= true,
				emailPrivate		= false,
				birthdayPrivate		= true,
				pronounPrivate		= false,
				relationshipPrivate = true
			};

			// Save the orivacy settings first to get their primary keys, needed by the UserProfiles

			using (DataContext context = new())
			{
				privaciesNew.Add(privacyPublic);
				privaciesNew.Add(privacyPrivate);
				privaciesNew.Add(privacyBusiness);

				context.PrivacySettings.AddRange(privaciesNew);
				context.SaveChanges();

				profilePublic  .privacyId = privacyPublic.Id;
				profilePrivate .privacyId = privacyPrivate.Id;
				profileBusiness.privacyId = privacyBusiness.Id;

				profilesNew.Add(profilePublic);
				profilesNew.Add(profilePrivate);
				profilesNew.Add(profileBusiness);

				context.UserProfiles.AddRange(profilesNew);
				context.SaveChanges();
				context.Dispose();
			}

			profiles  = profilesNew;
			privacies = privaciesNew;
		}

		public static async Task<string> GetMatchingLocale(string localeAbbreviation) // "ro"
		{
			string localeResolved = "en";
			string localeAbbreviationTest = localeAbbreviation;

			using (DataContext context = new())
			{
				List<string> localesDB = await context.ApplicationText.Select(l => l.locale).Distinct().ToListAsync();

				if (localesDB.Contains(localeAbbreviation))
				{
					localeResolved = localeAbbreviation;
				}
			}

			return localeResolved;
		}
		
		public static async Task<UserInformation> RegisterUserLight(RegisterUserLightRequest request)
		{
			UserInformation userinformation = new();

			Aes aes = CryptographyService.CreateAesKey();

			logger.Info($"RegisterUserS: created permanent AES key {aes.Key} for user {request.userId}");

			DateTime birthday;

			DateTime.TryParseExact(request.birthdayString, "MM/dd/yyyy", null, DateTimeStyles.None, out birthday);

			Users user = new()
			{
				Id					= new Random().NextInt64(),
				locale				= request.locale,
				email				= request.email,
				birthdayDate		= birthday,
				passwordHash		= request.passwordHash,
				TimeNotifications	= DateTime.UtcNow,
				aeskey				= aes.Key,
				aesiv				= aes.IV,
			};

			using (DataContext dbcontext = new())
			{
				Users userExisting = dbcontext.Users.Where(u => u.email.ToLower().Equals(request.email.ToLower())).FirstOrDefault();

				if (userExisting == null)
				{
					await dbcontext.Users.AddAsync(user);
					await dbcontext.SaveChangesAsync();

					logger.Info($"RegisterUserS: birthday {request.birthdayString}");

					List<UserProfiles>    profiles;
					List<PrivacySettings> privacies;

					CreateUserProfiles(user.Id,
									   request,
								   out profiles,
								   out privacies);

					user.currentProfileId = profiles[0].Id;
					await dbcontext.SaveChangesAsync();

					userinformation = await GetCurrentUserInformation(user.Id);

					logger.Info($"RegisterUserS: userinformation {userinformation}");
				}
				else
				{
					userinformation.Error.httpCode = HttpStatusCode.Conflict;
					userinformation.Error.message  = $"email {request.email} already registered";
				}

				await dbcontext.DisposeAsync();
			}

			return userinformation;
		}

		public static async Task<UserInformation> Login(LoginRequest request)
		{
			UserInformation    response  = new();
			using (DataContext dbcontext = new())
			{
				Users? user = await dbcontext.Users.Where(u => u.email.Equals(request.Email)).FirstOrDefaultAsync();

				if (user != null)
				{
					Aes aes = CryptographyService.GetAesKey(request.userIdRegistering, true); // registering key

					if (user.passwordHash.Equals(request.PasswordHash))
					{
						dbcontext.UsersOnline.Add(new UsersOnline
						{
							UserId     = user.Id,
							TimeOnline = DateTime.UtcNow
						});

						response = await UsersService.GetCurrentUserInformation(user.Id);
						response.Error.httpCode = HttpStatusCode.OK;
					}
					else
					{
						response.Error.httpCode = HttpStatusCode.Unauthorized;
						response.Error.message = "invalid password";
					}
				}
				else
				{
					response.Error.httpCode = HttpStatusCode.NotFound;
					response.Error.message  = $"user {request.Email} not found";
				}

				await dbcontext.SaveChangesAsync();
				await dbcontext.DisposeAsync();
			}

			return response;
		}

		public async static Task<AddInterestResponse>	AddInterest(AddInterestRequest request)
		{
			AddInterestResponse response = new();

			using (DataContext context = new())
			{
				InterestNames? interestname;

				// check if this is already in the table

				interestname = context.InterestNames.Where(i => i.Name.ToLower().Equals(request.name.ToLower())).FirstOrDefault();

				if (interestname == null)
				{ 
					interestname = new()
					{
						Name = request.name.Substring(0, 1).ToUpper()
							 + request.name.Substring(1, request.name.Length - 1),
						locale = request.language
					};

					await context.InterestNames.AddAsync(interestname);
					await context.SaveChangesAsync();
				}
				await context.DisposeAsync();

				response.Id   = interestname.Id;
				response.name = interestname.Name;
			}

			return response;
		}

		public static async Task<List<string>> GetInterestNames(string locale)
		{
			List<string> names;

			using (DataContext context = new())
			{
				names = await context.InterestNames.Where  (na => na.locale.Equals(locale))
												   .OrderBy(na => na.Name)
												   .Select (na => na.Name)
												   .ToListAsync();

				context.DisposeAsync();
			}

			return names;
		}

		public static async Task<List<UserInterest>> GetUserInterests(Int64 userId)
		{
			List<UserInterest> userinterests = new();
			using (DataContext context		 = new())
			{
				List<InterestNames> names				= context.InterestNames.ToList();
				List<UserInterests> userinterestsSchema = context.UserInterests.Where(ui => ui.UserId.Equals(userId)).ToList();

				userinterestsSchema.ForEach(ui => 
				{
					userinterests.Add(new UserInterest()
					{
						UserId		= userId,
						InterestsId = ui.InterestId,
						Name		= names.Where(n => n.Id.Equals(ui.InterestId)).FirstOrDefault()?.Name
					});
				});

				await context.DisposeAsync();
			}

			return userinterests;
		}

		public static async Task<List<UserInterest>> GetProfileInterests(Int32 profileId)
		{
			List<UserInterest> userinterests = new();
			using (DataContext context = new())
			{
				Int64 userId = context.UserProfiles.Where(up => up.Id.Equals(profileId)).Select(up => up.userId).FirstOrDefault();
				List<ProfileInterests> userinterestsSchema = context.ProfileInterests.Where(pi => pi.ProfileId.Equals(profileId)).ToList();

				if (userinterestsSchema.Count > 0)
				{
					List<InterestNames> names = context.InterestNames.ToList();

					userinterestsSchema.ForEach(ui =>
					{
						userinterests.Add(new UserInterest()
						{
							UserId		= userId,
							InterestsId = ui.UserInterestId,
							Name		= names.Where(n => n.Id.Equals(ui.UserInterestId)).FirstOrDefault()?.Name
						});
					});
				}

				await context.DisposeAsync();
			}

			return userinterests;
		}

		public static async Task<UserProfile> GetUserProfile(Int32 profileId)
		{
			UserProfile response = new();

			using (DataContext dbcontext = new())
			{
				UserProfiles?    profileSchema = await dbcontext.UserProfiles   .FindAsync(profileId);
				PrivacySettings? privacySchema = await dbcontext.PrivacySettings.FindAsync(profileSchema.privacyId);

				if (privacySchema != null)
				{
					Users? user = await dbcontext.Users.FindAsync(privacySchema.UserId);

					await dbcontext.DisposeAsync();

					PrivacySetting privacy = new()
					{
						Id					= profileSchema.privacyId.Value,
						birthdayPrivate		= privacySchema.birthdayPrivate,
						emailPrivate		= privacySchema.emailPrivate,
						firstNamePrivate	= privacySchema.firstNamePrivate,
						lastNamePrivate		= privacySchema.lastNamePrivate,
						pronounPrivate		= privacySchema.pronounPrivate,
						relationshipPrivate	= privacySchema.relationshipPrivate
					};

					response = new()
					{
						profileId		= profileId,
						userId			= profileSchema.userId,
						isCurrent		= profileSchema.Id == user.currentProfileId,
						privacy			= privacy,
						profiletype     = profileSchema.profileType,
						interests		= await GetProfileInterests(profileSchema.Id),
						moniker         = profileSchema.moniker,
						name			= profileSchema.name,
						avatar			= profileSchema.avatar.Value,
						Employer		= profileSchema.Employer,
						Title			= profileSchema.Title,
						Responsibilities= profileSchema.Responsibilities
					};
				}
				else
				{
					response.Error.httpCode = HttpStatusCode.NotFound;
					response.Error.message = "privacy settings not found";
				}

				await dbcontext.DisposeAsync();
			}

			return response;
		}

		public static async Task<UserProfile> GetCurrentUserProfile(Int64 userId)
		{
			UserProfile response = new();

			using (DataContext dbcontext = new())
			{
				Users? user = await dbcontext.Users.FindAsync(userId);

				if (user != null)
				{
					UserProfiles profile = dbcontext.UserProfiles.Where(up => up.userId.Equals(userId) && up.isCurrent.Value).FirstOrDefault();
					Schema.PrivacySettings? privacysettings = await dbcontext.PrivacySettings.FindAsync(profile.privacyId);

					if (privacysettings != null)
					{
						SharedObjectTypes.PrivacySetting privacy = new()
						{
							birthdayPrivate		= privacysettings.birthdayPrivate,
							emailPrivate		= privacysettings.emailPrivate,
							firstNamePrivate	= privacysettings.firstNamePrivate,
							lastNamePrivate		= privacysettings.lastNamePrivate,
							pronounPrivate		= privacysettings.pronounPrivate,
							relationshipPrivate = privacysettings.relationshipPrivate
						};

						response = new()
						{
							profileId		= privacysettings.Id,
							userId			= privacysettings.UserId,
							profiletype		= profile.profileType,
							isCurrent		= true,
							privacy			= privacy,
							interests		= await GetProfileInterests(profile.Id)
						};
					}
					else
					{
						response.Error.httpCode = HttpStatusCode.NotFound;
						response.Error.message = "profile not found";
					}
				}
				else
				{
					response.Error.httpCode = HttpStatusCode.NotFound;
					response.Error.message = "user not found";
				}

				await dbcontext.DisposeAsync();
			}

			return response;
		}

		public static async Task<UserProfile> UpdateProfile(UpdateProfileRequest request)
		{
			UserProfiles?   profile;

			using (DataContext context = new())
			{
				profile = await context.UserProfiles.FindAsync(request.profileId);

				if (request.isCurrent != null)
				{
					Users? user = await context.Users.FindAsync(profile.userId);

					user.currentProfileId = request.profileId;

					profile.isCurrent = request.isCurrent.Value;
				}

				if (request.moniker != null)
				{
					profile.moniker = request.moniker;
				}

				if (request.email != null)
				{
					profile.email = request.email;
				}

				if (request.Employer != null)
				{
					profile.Employer = request.Employer;
				}

				if (request.Title != null)
				{
					profile.Title = request.Title;
				}

				if (request.Responsibilities != null)
				{
					profile.Responsibilities = request.Responsibilities;
				}

				if (request.avatar != null)
				{
					profile.avatar = request.avatar.Value;
				}

				if (request.privacy != null)
				{
					PrivacySettings privacyOld = await context.PrivacySettings.FindAsync(profile.privacyId);
					PrivacySettings privacyNew = new()
					{
						UserId				= profile.userId,
						birthdayPrivate		= request.privacy.birthdayPrivate,
						emailPrivate		= request.privacy.emailPrivate,
						firstNamePrivate	= request.privacy.firstNamePrivate,
						lastNamePrivate		= request.privacy.lastNamePrivate,
						pronounPrivate		= request.privacy.pronounPrivate,
						relationshipPrivate = request.privacy.relationshipPrivate
					};
					context.PrivacySettings.Remove(privacyOld);
					context.PrivacySettings.Add   (privacyNew);

					await context.SaveChangesAsync();

					profile.privacyId = privacyNew.Id;
				}

				if (request.interests != null)
				{
					List<ProfileInterests> profileinterestsOld = context.ProfileInterests.Where(pi => pi.ProfileId.Equals(profile.Id)).ToList();
					List<ProfileInterests> profileinterestsNew = new();

					profile.userInterestIDs = new();

					foreach (UserInterest userinterest in request.interests)
					{
						profileinterestsNew.Add(new()
						{
							ProfileId		= profile.Id,
							UserInterestId	= userinterest.InterestsId
						});
						
						profile.userInterestIDs.Add(userinterest.InterestsId);
					}

					context.ProfileInterests.RemoveRange(profileinterestsOld);
					context.ProfileInterests.AddRange   (profileinterestsNew);
				}

				await context.SaveChangesAsync();
				await context.DisposeAsync();
			}

			return await UsersService.GetUserProfile(request.profileId);
		}

		public static async Task<List<UserProfile>> GetUserProfiles(Int64 userId) // has to be current user
		{
			List<UserProfile> response = new();

			using (DataContext dbcontext = new())
			{
				Users? user = await dbcontext.Users.FindAsync(userId);

				if (user != null)
				{
					List<Int32> profileIds = await dbcontext.UserProfiles
															.Where(p => p.userId == userId)
															.Select(p => p.Id)
															.ToListAsync();

					foreach (Int32 profileId in profileIds)
					{
						response.Add(await GetUserProfile(profileId));
					} // end foreach profileId
				};

				await dbcontext.DisposeAsync();
			}

			return response;
		}

		public static async Task<List<UserInformation>> GetUsersInformation(List<Int64> userIDs,
																			InformationLevel informationlevel,
																			DataContext dbcontext)
		{
			List<UserInformation> userinformations = new(userIDs.Count);

			foreach (Int64 userId in userIDs)
			{
				Users?			 user    = dbcontext.Users.Find(userId);
				UserProfiles	 profile = await dbcontext.UserProfiles.FindAsync(user.currentProfileId);
				PrivacySettings? privacy = await dbcontext.PrivacySettings.FindAsync(profile.privacyId);
				UsersOnline?	 online  = dbcontext.UsersOnline.Where(uo => uo.UserId == userId).FirstOrDefault();

				// this should never happen but better an extra line of code than a crash

				UserInformation userinformation = new()
				{
					userId			 = userId,
					currentProfileId = user.currentProfileId,
					online			 = online != null,
				};

				S3Attachment? attachment = await S3Service.GetAvatar($"{profile.Id}");

				if (informationlevel >= InformationLevel.Regular)
				{
					userinformation.firstName	= privacy.firstNamePrivate	? String.Empty : user.firstName;
					userinformation.lastName	= privacy.lastNamePrivate	? String.Empty : user.lastName;
					userinformation.email		= privacy.emailPrivate		? String.Empty : user.email;
				}

				if (informationlevel == InformationLevel.Full)
				{
					if (privacy.birthdayPrivate)
					{
						userinformation.birthdayString  = String.Empty;
					}
					else
					{
						userinformation.birthdayString = user.birthdayDate.ToString();
					}

					userinformation.pronoun		 = privacy.pronounPrivate		? Pronouns.PreferNot : user.pronoun;
					userinformation.relationship = privacy.relationshipPrivate	? Relationship.PreferNot : user.relationshipId;
				}

				userinformation.interests = await dbcontext.UserInterests
														  .Where(i => i.UserId == userId)
														  .Select(ui => new UserInterest() 
														  { 
															  UserId	  = userId,
															  InterestsId = ui.Id,
															  Name        = dbcontext.InterestNames.Where(i => i.Id.Equals(ui.Id)).Select(n => n.Name).FirstOrDefault()
														  })
														  .ToListAsync();

				userinformation.translationOptOuts = await dbcontext.TranslationOptOuts
																	.Where(i => i.UserId.Equals(userId))
																	.Select(i => i.LocaleOptOut)
																	.ToListAsync();

				userinformations.Add(userinformation);
			} // end foreach friend Id

			return userinformations;
		}


		public static async Task<SetUserLocationResponse> UpdateUserLocation(SetUserLocationAndChatRequest request)
		{
			SetUserLocationResponse response = new();

			using (DataContext dbcontext = new())
			{
				UsersOnline? useronline = await dbcontext.UsersOnline.Where(uo => uo.UserId == request.userId).FirstOrDefaultAsync();
				/*
				if (useronline == null)
				{
					// user was not online. Put him online and try to find a chat to put him in

					dbcontext.UsersOnline.Add(new UsersOnline
					{
						UserId		= request.userId,
						Radius		= request.chatRadiusID,
						Latitude	= request.coordinates.latitude,
						Longitude	= request.coordinates.longitude,
						Altitude	= request.coordinates.altitude,
						TimeOnline	= DateTime.UtcNow
					});

					await dbcontext.SaveChangesAsync();

					response.locationresponse |= LocationResponse.locationResponseNowOnline;

					useronline = await dbcontext.UsersOnline.Where(s => s.UserId == request.userId).FirstOrDefaultAsync();

					ChatResolutionRequest crq = new ChatResolutionRequest
					{
						userId = request.userId,
						chatRadiusID = request.chatRadiusID,
						coordinateUser = request.coordinates,
						createNew = false
					};

					ChatResolutionResponse crs = await ChatService.ResolveRadiusToChat(crq);

					response.chatId = crs.chatId;

					if (crs.chatId != null)
					{
						response.locationresponse |= LocationResponse.locationResponseAddedToChat;
					}
					else
					{
						response.locationresponse |= LocationResponse.locationResponseNotInChat;
					}
				}
				else*/

				if (useronline  != null)
				{
					useronline.Latitude	 = request.coordinates.latitude;
					useronline.Longitude = request.coordinates.longitude;
					useronline.Altitude	 = request.coordinates.altitude;

					await dbcontext.SaveChangesAsync();

					ChatUsers? chatUser = await dbcontext.ChatUsers.Where(c => c.UserId == request.userId).FirstOrDefaultAsync();

					if (chatUser != null)
					{
						Chats? chat = await dbcontext.Chats.FindAsync(chatUser.ChatId);

						response.locationresponse |= LocationResponse.locationResponseInChat;
						response.chatId = chat.Id;

						if (ContentService.IsUserInRadius(useronline, chat) == false)
						{
							response.locationresponse |= LocationResponse.locationResponseOutsideRadius;
						} // new user location no longer in chat radius
					} // endif user in a chat
				} // endif user online

				await dbcontext.DisposeAsync();
			}

			return response;
		} //UpdateUserLocation

		public static async Task<ChatResolutionResponse> SetUserLocationAndChat(SetUserLocationAndChatRequest request)
		{
			ChatResolutionResponse? chatResponse = null;

			JanitorService.Initialize();

			using (DataContext dbcontext = new())
			{
				// if the user does not show as online, fix that

				UsersOnline? userOnline = dbcontext.UsersOnline.Where(uo => uo.UserId == request.userId).FirstOrDefault();
				Users user = dbcontext.Users.Find(request.userId);

				if (userOnline == null)
				{
					logger.Info($"SetUserLocationAndChat: user {request.userId} offline; setting to online");

					dbcontext.UsersOnline.Add(new UsersOnline()
					{
						UserId		= request.userId,
						Latitude	= request.coordinates.latitude,
						Longitude	= request.coordinates.longitude,
						Altitude	= request.coordinates.altitude,
						Radius		= request.chatRadiusID,
						TimeOnline	= DateTime.UtcNow
					});
				}
				else
				{
					// user online, update location

					logger.Info($"SetUserLocationAndChat: user {request.userId} online");

					userOnline.Radius		= request.chatRadiusID;
					userOnline.Latitude		= request.coordinates.latitude;
					userOnline.Longitude	= request.coordinates.longitude;
					userOnline.Altitude		= request.coordinates.altitude;
				}

				user.recentRadius = request.chatRadiusID;

				await dbcontext.SaveChangesAsync();

				// refresh to get primary key of new UsersOnline row

				userOnline = await dbcontext.UsersOnline.Where(uo => uo.UserId == request.userId).FirstOrDefaultAsync();

				logger.Info($"SetUserLocationAndChat: useronline ID = {userOnline.Id}");

				ChatUsers? chatuser = await dbcontext.ChatUsers.Where(cu => cu.UserId == request.userId).FirstOrDefaultAsync();

				if (chatuser != null)
				{
					Chats? chat = await dbcontext.Chats.FindAsync(chatuser.ChatId);

					if (chat != null)
					{
						logger.Info($"user {user.Id} is in chat {chat.Id} with radius {ContentService.GetChatRadius(chat.RadiusId).value} miles");

						if (chat.RadiusId != request.chatRadiusID)
						{
							logger.Info($"chat {chat.Id} has different radius from request ({ContentService.GetChatRadius(request.chatRadiusID).value} miles); removing user from chat");

							await RemoveUserFromChat(request.userId);
							chatuser = null;
						}
					}
					else
					{
						// this is unlikely but handle it anyway

						logger.Warn($"anomaly: chatuser row refers to nonexistent chat {chatuser.ChatId}");
						dbcontext.ChatUsers.Remove(chatuser);
						chatuser = null;
					}
				}
				// if the user is not in a chat, see what we can do

				if (chatuser == null)
				{
					logger.Info($"user {user.Id} is not currently in a chat");

					// look for a chat with matching radius in range

					ContentService.ChatResolutionRequest chatRequest = new()
					{
						userId			= request.userId,
						coordinateUser	= request.coordinates,
						chatRadiusID	= request.chatRadiusID,
						createNew		= true
					};

					logger.Info($"seeking chat of radius {ContentService.GetChatRadius(request.chatRadiusID).value}");

					chatResponse = await ContentService.ResolveRadiusToChat(chatRequest);

					// he's in a chat now; update the DB and return the data

					if (chatResponse		!= null
					&&  chatResponse.chatId != null)
					{
						logger.Info($"returned from RRTC with chat response {chatResponse.chatId}, chat created = {chatResponse.chatCreated}");

						// users were already added to table in CreateChat call in ResolveRadiusToChat

						if (chatResponse.chatCreated == false)
						{
							await dbcontext.ChatUsers.AddAsync(new ChatUsers
							{
								UserId		= request.userId,
								ChatId		= chatResponse.chatId.Value,
								TimeAdded	= DateTime.UtcNow
							});

							await dbcontext.SaveChangesAsync();
						}
					}
					else
					{
						logger.Info($"returned from RRTC with null chat response");

						chatResponse = new()
						{
							chatId = null,
						};
					}
				} // endif not in a chat
				else if (userOnline.Radius == request.chatRadiusID)
				{
					logger.Info($"user is in a chat with matching radius");

					Chats? chat = await dbcontext.Chats.Where(chat => chat.Id == chatuser.ChatId).FirstOrDefaultAsync();
					Coordinates coordinate = new()
					{
						latitude  = chat.Latitude,
						longitude = chat.Longitude,
						altitude  = chat.Altitude
					};

					List<Int64> userIds = await dbcontext.ChatUsers
											 			 .Where(cu => cu.ChatId == chat.Id)
														 .Select(cu => cu.UserId)
														 .ToListAsync();
					List<UserFriends> friends = await dbcontext.UserFriends.Where(uf => uf.userId == request.userId
																					 && userIds.Contains(uf.userIdFriend))
																		   .ToListAsync();
					chatResponse = new()
					{
						chatId			= chatuser.ChatId,
						chatRadiusID	= request.chatRadiusID,
						center			= coordinate,
						chatName		= chat.Name,
						countUsers		= userIds.Count,
						countFriends	= friends.Count
					};
				}

				await dbcontext.DisposeAsync();
			} // endif dbcontext

			return chatResponse;
		}
		public static async Task<Int16> GetUserRating(GetUserRatingRequest request)
		{
			Int16 score = 0;

			using (DataContext dbcontext = new())
			{
				ProxChat.Schema.UserRatings? rating = await dbcontext.UserRatings
													 .Where(r => r.UserId	   == request.userId
															  && r.UserIdRated == request.userIdRated)
													 .FirstOrDefaultAsync();
				await dbcontext.DisposeAsync();

				if (rating != null)
				{
					score = rating.Rating;
				}

				await dbcontext.DisposeAsync();
			}

			return score;
		}
		public static async Task<UserRating> GetGlobalUserRating(Int64 userId)
		{
			UserRating rating = UserRating.ratingUnrated;

			using (DataContext context = new())
			{
				Int16 minimumRatings = context.ApplicationParameters.Select(ap => ap.minimalUserRatings).First();
				Int16 maximumStars   = context.ApplicationParameters.Select(ap => ap.maximumStars).First();

				List<Int16> ratings = await context.UserRatings
												   .Where (ur => ur.UserIdRated.Equals(userId))
												   .Select(ur => ur.Rating)
												   .ToListAsync();
				if (ratings.Count >= minimumRatings)
				{
					Int32 sum = 0;

					ratings.ForEach(e => sum += e);

					double average = (double) sum / ((double) maximumStars);
					Int32  floor   = (Int32) Math.Floor(average);

					rating = (UserRating) floor;

					if (floor < maximumStars)
					{
						if ((average - (double) floor) >= 0.5)
						{
							rating++;
						}
					}
				}
			}

			return rating;
		}


		public static async Task SetUserRating(SetUserRatingRequest request)
		{
			using (DataContext dbcontext = new())
			{
				UserRatings? rating = await dbcontext.UserRatings.Where(r => r.UserId == request.userId
																		  && r.UserIdRated == request.userIdRated)
																 .FirstOrDefaultAsync();
				if (rating != null)
				{
					rating.Rating = request.rating;
				}
				else
				{
					dbcontext.UserRatings.Add(new UserRatings
					{
						UserId		= request.userId,
						UserIdRated = request.userIdRated,
						Rating		= request.rating
					});
				}

				await dbcontext.SaveChangesAsync();
				await dbcontext.DisposeAsync();
			}
		}

		public static async Task SetUserBlockState(SetUserBlockRequest request)
		{
			using (DataContext dbcontext = new())
			{
				UserBlocks? blocked = await dbcontext.UserBlocks.Where(u => u.userId	    == request.userId
																		 && u.UserIdBlocked == request.userIdBlocked)
																.FirstOrDefaultAsync();

				if (blocked != null
				&& request.blockstate == UserBlockState.Unblocked)
				{
					dbcontext.UserBlocks.Remove(blocked);
				}
				else if (blocked == null
					 && request.blockstate == UserBlockState.Blocked)
				{
					dbcontext.UserBlocks.Add(new UserBlocks
					{
						userId        = request.userId,
						UserIdBlocked = request.userIdBlocked,
					});
				}

				await dbcontext.SaveChangesAsync();
				await dbcontext.DisposeAsync();
			}
		}
		public static async Task<List<Int64>> GetBlockedUsers(Int64 userId)
		{
			List<Int64> userIDsBlocked = null;

			using (DataContext dbcontext = new())
			{
				userIDsBlocked = await dbcontext.UserBlocks.Where(u => u.userId.Equals(userId)).Select(u => u.UserIdBlocked).ToListAsync();

				await dbcontext.DisposeAsync();
			}

			return userIDsBlocked;
		}

		public static async Task<UserNotificationResponse> GetUserNotifications(Int64 userId)
		{
			UserNotificationResponse response = new()
			{
				userId = userId
			};

			using (DataContext context = new())
			{
				Users? user = await context.Users.Where(u => u.Id.Equals(userId)).FirstOrDefaultAsync();

				if (user != null)
				{
					ChatUsers? chatuser = await context.ChatUsers.Where(u => u.UserId == userId).FirstOrDefaultAsync();
					List<TranslationOptOut> optouts = context.TranslationOptOuts.Where(u => u.UserId == userId).ToList();

					if (chatuser != null)
					{
						response.chatId = chatuser.ChatId;

						List<Int64> chatuserIDs   = await context.ChatUsers  .Where(cu => cu.ChatId.Equals(chatuser.ChatId))    .Select(cu => cu.UserId)      .ToListAsync();
						List<Int64> chatfriendIDs = await context.UserFriends.Where(uf => chatuserIDs.Contains(uf.userIdFriend)).Select(uf => uf.userIdFriend).ToListAsync();
						response.countChatUsers   = chatuserIDs.Count;
						response.countChatFriends = chatfriendIDs.Count;

						List <ChatMessages> messages = await context.ChatMessages
																	.Where(m => m.ChatId == chatuser.ChatId)
																	.Where(m => m.TimeSent > user.TimeNotifications)
																	.ToListAsync();


						List<ChatMessage> newChatMessages = messages.OrderBy(m => m.TimeSent)
																	 .Select(m => new ChatMessage
																	 {
																		 Id				= m.Id,
																		 UserIdSender	= m.UserIdSender,
																		 ChatId			= m.ChatId,
																		 Message		= m.Message,
																		 TimeSent		= m.TimeSent,
																		 imageB64		= m.hasImage ? String.Empty : null
																	 })
																	 .ToList();
						response.newChatMessages = await ContentService.TranslateAndFilterMessages(newChatMessages, user);

						foreach (ChatMessage message in response.newChatMessages)
						{
							if (message.imageB64 != null)
							{
								S3Attachment attachment = await S3Service.GetChatMessageImage(message.Id.ToString());

								if (attachment.image.Length > 0
								&&  attachment.error.httpCode == HttpStatusCode.OK)
								{
									message.imageB64 = Convert.ToBase64String(attachment.image);
								}
							}
						}

						if (response.newChatMessages.Count > 0)
						{
							List<Int64> blockedUsers = await context.UserBlocks
																	.Where(ub => ub.userId == userId)
																	.Select(ub => ub.UserIdBlocked)
																	.ToListAsync();

							List<ChatMessage> messagesForTranslation = response.newChatMessages.Where(cm => blockedUsers.Contains(cm.UserIdSender) == false).ToList();
						}
					
						List<Int64> newUserIDs = await context.ChatUsers.Where(u => u.ChatId == chatuser.ChatId
																				  && u.TimeAdded > user.TimeNotifications)
																		.Select(u => u.UserId)
																		.ToListAsync();

						response.newUsersInChat = await GetUsersInformation(newUserIDs, InformationLevel.Regular, context);
					}
					else
					{
						response.newUsersInChat  = new();
						response.newChatMessages = new();
					}

					response.newDirectMessages = await context.DirectMessages.Where(dm => dm.userIdRecipient == userId
																					   || dm.userIdSender == userId)
																			 .Where(dm => dm.timeSent > user.TimeNotifications)
																			 .OrderBy(dm => dm.timeSent)
																			 .Select(dm => new DirectMessage
																			 {
																				 Id                 = dm.Id,
																				 UserIdSender		= dm.userIdSender,
																				 UserIdRecipient	= dm.userIdRecipient,
																				 Message			= dm.message,
																				 TimeSent			= dm.timeSent,
																				 ImageB64           = dm.hasImage ? String.Empty : null,
																			 })
																			 .ToListAsync();
					response.newDirectMessages = await ContentService.TranslateAndFilterDirectMessages(response.newDirectMessages, user);

					foreach (DirectMessage message in response.newDirectMessages)
					{
						if (message.ImageB64 != null)
						{
							S3Attachment attachment = await S3Service.GetDirectMessageAttachment(message.Id.ToString());

							if (attachment.error.httpCode == HttpStatusCode.OK)
							{
								message.ImageB64 = Convert.ToBase64String(attachment.image);
							}
						}
					}

					response.friendRequests = await context.FriendRequests.Where(fr => fr.FriendId == userId)
																		  .Where(fr => fr.TimeSent > user.TimeNotifications)
																		  .Select(fr => new FriendRequest
																		   {
																				Id					= fr.Id,
																				 userIdRecipient	= fr.FriendId,
																				 userIdSender		= fr.UserId,
																				 TimeSent			= fr.TimeSent
																		   })
																		  .ToListAsync();
					response.notificationTime = DateTime.UtcNow;
					user.TimeNotifications    = DateTime.UtcNow;
				}
				else
				{
					logger.Error($"user {userId} not found");
				}

				await context.SaveChangesAsync();
				await context.DisposeAsync();
			}

			return response;
		}

		public static async Task<UserInformation> GetCurrentUserInformation(Int64 userId)
		{
			UserInformation response = new();

			Users? user;
			List<TranslationOptOut> translationOptOuts = new();

			using (DataContext context = new())
			{
				user = await context.Users.FindAsync(userId);

				if (user != null)
				{
					UserProfiles? profile = context.UserProfiles.FirstOrDefault(p => p.Id == user.currentProfileId);

					response.moniker			= profile != null ? profile.moniker : $"User{user.Id % 100000}";
					response.userId				= userId;
					response.firstName			= user.firstName;
					response.lastName			= user.lastName;
					response.online				= true;
					response.pronoun			= user.pronoun;
					response.relationship		= user.relationshipId;
					response.email				= user.email;
					response.birthdayString		= user.birthdayDate.ToString();
					response.locale				= user.locale;
					response.aesKey				= user.aeskey;
					response.aesIV				= user.aesiv;
					response.interests			= await GetUserInterests(userId);
					response.currentProfileId	= user.currentProfileId;
					response.translationOptOuts	= context.TranslationOptOuts.Where(o => o.UserId == userId).Select(o => o.LocaleOptOut).ToList();
					response.rating             = await GetGlobalUserRating(userId);
				}
				else
				{
					response.Error.httpCode = HttpStatusCode.NotFound;
					response.Error.message = $"user ID {userId} does not exist";
					logger.Error(response.Error.message);
				}

				await context.DisposeAsync();
			}

			return response;
		}

		private static bool CompareIDLists(List<Int32> currentIDs, 
										   List<Int32> newIDs, 
									   out List<Int32> addIDs, 
									   out List<Int32> removeIDs)
		{
			List<Int32> toAdd    = new();
			List<Int32> toRemove = new();

			foreach(Int32 id in currentIDs)
			{
				if (newIDs.Contains(id) == false)
				{
					toRemove.Add(id);
				}
			}

			foreach (Int32 id in newIDs)
			{
				if (currentIDs.Contains(id) == false)
				{
					toAdd.Add(id);
				}
			}

			addIDs    = toAdd;
			removeIDs = toRemove;


			return toAdd   .Count > 0
				|| toRemove.Count > 0;
		}

		public static async Task<UserUpdateResponse> UpdateUser(UserUpdateRequest request)
		{
			UserUpdateResponse response = new();

			using (DataContext context = new())
			{
				Users? user = await context.Users.Where(u => u.Id.Equals(request.userId)).FirstOrDefaultAsync();
				List<string> forbidden = context.ForbiddenWords.Where(fw => fw.locale.Equals(user.locale)).Select(fw => fw.word).ToList();

				DateTime birthday;

				DateTime.TryParseExact(request.birthdayString, "MM/dd/yyyy", null, DateTimeStyles.None, out birthday);


				user.email			= request.email;
				user.firstName		= request.firstName;
				user.lastName		= request.lastName;
				user.locale			= request.locale;
				user.pronoun		= request.pronoun;
				user.relationshipId = request.relationship;
				user.units			= request.distanceunit;
				user.birthdayDate	= birthday;

				// add any new interests

				context.RemoveRange(context.UserInterests.Where(ui => ui.UserId.Equals(request.userId)).ToList());

				List<UserInterests> userinterests = new();

				foreach (string name in request.interestNames)
				{
					if (forbidden.Contains(name) == false)
					{
						AddInterestRequest requestAI = new()
						{
							name	 = name,
							language = user.locale
						};

						// returns existing interst or adds a new one

						AddInterestResponse responseAI = await AddInterest(requestAI);

						userinterests.Add(new UserInterests()
						{
							InterestId	= responseAI.Id,
							UserId		= user.Id,
						});
					}
				}

				context.UserInterests.AddRange(userinterests);

				await context.SaveChangesAsync();

				response = new UserUpdateResponse()
				{
					userId			= request.userId,
					birthday		= request.birthdayString,
					email			= request.email,
					firstName		= request.firstName,
					lastName		= request.lastName,
					locale          = request.locale,
					pronoun			= request.pronoun,
					relationship	= request.relationship,
					interests		= request.interestNames
				};

				await context.DisposeAsync();
			}

			return response;
		}

		private static async Task<List<UserInterest>> UserInterestsFromInterestIDs(Int64 userId,
																				   List<Int32> interestIDs)
		{
			List<UserInterest> interests = new();

			using (DataContext context = new())
			{
				interestIDs.ForEach(id =>
				{
					string? name = context.InterestNames.Where(n => n.Id == id).Select(n => n.Name).FirstOrDefault();

					interests.Add(new UserInterest
					{
						UserId = userId,
						InterestsId = id,
						Name = name
					});
				});

				await context.DisposeAsync();
			}

			return interests;
		}

		public static async Task<UserInformation> GetOtherUserInformation(Int64 userId, string localeCurrentUser)
		{
			UserInformation response = await UsersService.GetCurrentUserInformation(userId);

			if (response.Error.httpCode == HttpStatusCode.OK)
			{
				PrivacySettings privacy = null;

				using (DataContext context = new())
				{
					UserProfiles profile = await context.UserProfiles.FindAsync(response.currentProfileId);
					privacy				 = await context.PrivacySettings.FindAsync(profile.privacyId);
					await context.DisposeAsync();
				}

				if (privacy.firstNamePrivate)
				{
					response.firstName = String.Empty;
				}

				if (privacy.lastNamePrivate)
				{
					response.lastName = String.Empty;
				}

				if (privacy.pronounPrivate)
				{
					response.pronoun = Pronouns.PreferNot;
				}

				if (privacy.relationshipPrivate)
				{
					response.relationship = Relationship.PreferNot;
				}

				if (privacy.emailPrivate)
				{
					response.email = String.Empty;
				}

				response.interests = await GetProfileInterests(response.currentProfileId);

				if (response.locale.Equals(localeCurrentUser) == false)
				{
					using (AmazonTranslateClient client = new())
					{
						foreach (UserInterest ui in response.interests)
						{
							ui.Name = await TranslationService.TranslateText(client, response.locale, localeCurrentUser, ui.Name);
						}

						client.Dispose();
					}
				}
			}

			return response;
		}

		// if all goes well, a user will never be in more than one chat in the database
		// however,there will be events like abnormal shutdowns of the app or phone,
		// leaving dead chats held in the database by a single user.
		//
		// therefore, this function removes all ChatUsers rows for the user, since
		// the user is probably about to ber put into another chat

		public static async Task RemoveUserFromChat(Int64 userId)
		{
			using (DataContext context = new())
			{
				ChatUsers chatuser = context.ChatUsers.Where(cu => cu.UserId == userId).FirstOrDefault();
				/*
				foreach (ChatUsers chatuser in chatusers)
				{
					if (context.ChatUsers.Where(cu => cu.ChatId == chatuser.ChatId).ToList().Count == 1)
					{
						// he's the only user in the chat; delete it

						await ChatService.DeleteChat(chatuser.ChatId);
					}
				}
				*/
				context.ChatUsers.Remove(chatuser);

				await context.SaveChangesAsync();
				await context.DisposeAsync();
			}
		}

		public static async Task SetUserOffline(Int64 userId)
		{
			using (DataContext context = new())
			{
				UsersOnline? user   = await context.UsersOnline.Where(uo => uo.UserId == userId).FirstOrDefaultAsync();
				ChatUsers? chatuser = await context.ChatUsers  .Where(cu => cu.UserId == userId).FirstOrDefaultAsync();

				if (chatuser != null)
				{
					await RemoveUserFromChat(userId);
				}

				if (user != null)
				{
					context.UsersOnline.Remove(user);
				}

				await context.SaveChangesAsync();
				await context.DisposeAsync();
			}
		}

		public static async Task<bool> GetTranslationOptOut(GetTranslationOptOutRequest request)
		{
			bool optedout = false;

			using (DataContext context = new())
			{
				optedout = await context.TranslationOptOuts.Where(to => to.UserId		== request.userId 
				                                                     && to.LocaleOptOut == request.locale)
														   .FirstOrDefaultAsync() != null;

				await context.DisposeAsync();
			}

			return optedout;
		}

		public static async Task SetTranslationOptOut(SetTranslationOptOutRequest request)
		{
			using (DataContext context = new())
			{
				string localeISO = await context.Locales.Where(l => l.locale == request.locale).Select(l => l.locale).FirstOrDefaultAsync();

				TranslationOptOut? optout = await context.TranslationOptOuts.Where(to => to.UserId == request.userId && to.LocaleOptOut == localeISO).FirstOrDefaultAsync();

				if (request.optout)
				{
					if (optout == null)
					{
						context.TranslationOptOuts.Add(new TranslationOptOut
						{
							UserId       = request.userId,
							LocaleOptOut = localeISO
						});
					}
				}
				else
				{
					if (optout != null)
					{
						context.TranslationOptOuts.Remove(optout);
					}
				}

				await context.SaveChangesAsync();
				await context.DisposeAsync();
			}
		}

		public static async Task<string?> GetAvatar(Int32 profileId)
		{
			S3Attachment? attachment = await S3Service.GetAvatar(profileId.ToString());
			string? avatarB64 = null;

			if (attachment != null
			&&  attachment.error.httpCode == HttpStatusCode.OK)
			{
				avatarB64 = Convert.ToBase64String(attachment.image);

				logger.Info($"image {attachment.name} of size {attachment.image.Length} converted to {avatarB64.Length} base64 characaters");
			}

			return avatarB64;
		}

		public static async Task<List<ObscenityViolations>> SetAvatar(Int32  profileId, 
																	  string imageB64)
		{
			logger.Info($"SetAvatar called for profile {profileId} with {imageB64.Length} B64 characters");

			UserProfiles? profile = new DataContext().UserProfiles.Find(profileId);
			List<ObscenityViolations> violations = await ImageModerationService.ModerateImage(profile.userId, ImageRejectionType.Avatar, $"{profileId}",  Convert.FromBase64String(imageB64));

			if (violations.Count == 0)
			{
				await S3Service.AddAvatar(profileId.ToString(), Convert.FromBase64String(imageB64));
			}

			return violations;
		}

		public static async Task<Error> SetCurrentProfile(Int32 profileId)
		{
			Error error = new();

			using (DataContext context = new())
			{
				Int64 userId = context.UserProfiles.Where(up => up.Id.Equals(profileId)).Select(up => up.userId).FirstOrDefault();
				Users? user = await context.Users.Where(u => u.Id.Equals(userId)).FirstOrDefaultAsync();

				if (user != null)
				{
					bool found = false;

					context.UserProfiles.Where(up => up.userId.Equals(userId)).ToList().ForEach(up =>
					{
						if (up.Id.Equals(profileId))
						{
							found = true;
						}

						up.isCurrent = (up.Id.Equals(profileId));
					});

					if (found)
					{
						user.currentProfileId = profileId;

						error.httpCode = HttpStatusCode.OK;
						error.message  = $"user {userId} set to profile {profileId}";
					}
					else
					{
						error.httpCode = HttpStatusCode.NotFound;
						error.message  = $"profile {profileId} not found for user {userId}";
					}
				}
				else
				{
					error.httpCode = HttpStatusCode.NotFound;
					error.message  = $"user {userId} not found for profile {profileId}";
				}

				await context.SaveChangesAsync();
				await context.DisposeAsync();
			}

			return error;
		}
	}
}
