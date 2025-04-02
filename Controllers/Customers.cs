using System.Text.Json;
using System.Net;
using ProxChat.Db;
using ProxChat.Schema;
using ProxChat.SharedObjectTypes;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using ProxChat.Services;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using Amazon.Runtime.Internal;
using NLog;

namespace ProxChat.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class Customers : ControllerBase
	{
		static Logger logger;
		public Customers()
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


		[HttpPut, Route("RegisterUser")]
		[Produces("application/json")] // UserInformation
		public async Task<string> RegisterUser([FromBody] EncryptionWrapperProxChat wrapper)
		{
			logger.Info($"RegisterUserC: wrapper encrypted request is {wrapper.encryptedRequest.Length} characters");
			logger.Info($"RegisterUserC: wrapper user ID {wrapper.userId}");

			RegisterUserLightRequest request;

			CryptographyService.DecodeAndDecryptRequest(wrapper, out request, true);

			logger.Info($"RegisterUserC: RegisterUserRequest {request}");
			UserInformation userinformation = await UsersService.RegisterUserLight(request);
			logger.Info($"RegisterUserC: UserInformation {userinformation}");

			// encrypt to the registeringg user key

			string userinformationEncrypted = await CryptographyService.EncryptAndEncodeResponse(request.userId, userinformation, true);

			// remove the registering user

			using (DataContext context = new())
			{
				UserRegistering? userRegistering = context.UserRegistering.Where(i => i.Id == request.userId).FirstOrDefault();
				if (userRegistering != null)
				{
					context.UserRegistering.Remove(userRegistering);
					await context.SaveChangesAsync();
				}
				await context.DisposeAsync();
			}

			return userinformationEncrypted;
		}

		[HttpPut, Route("AddInterest")]
		[Consumes("application/json")]
		[Produces("application/json")]
		public async Task<string> AddInterest([FromBody] AddInterestRequest request)
		{
			AddInterestResponse response = await UsersService.AddInterest(request);

			return JsonSerializer.Serialize(response);
		}

		[HttpPut, Route("Login")]
		[Produces("application/json")] // UserInformation
		public async Task<string> Login([FromBody] EncryptionWrapperProxChat wrapper)
		{
			LoginRequest request;
			CryptographyService.DecodeAndDecryptRequest(wrapper, out request, true);
			UserInformation response = await  UsersService.Login(request);

			return await CryptographyService.EncryptAndEncodeResponse(request.userIdRegistering, response, true);
		}

		[HttpPut, Route("GetUserProfile")]
		[Produces("application/json")] // ProfileResponse
		public async Task<string> GetUserProfile(Int32 profileId)
		{
			UserProfile response = await UsersService.GetUserProfile(profileId);

			return await CryptographyService.EncryptAndEncodeResponse(response.userId, response);
		}

		[HttpPut, Route("GetCurrentUserProfile")]
		[Produces("application/json")] // ProfileResponse
		public async Task<string> GetCurrentUserProfile(Int64 userId)
		{
			UserProfile response = await UsersService.GetCurrentUserProfile(userId);

			return await CryptographyService.EncryptAndEncodeResponse(response.userId, response);
		}

		[HttpGet, Route("SetCurrentProfile")]
		public async Task<Error> SetCurrentProfile(Int32 profileId)
		{
			return await UsersService.SetCurrentProfile(profileId);
		}

		[HttpPut, Route("SetUserLocationAndChat")]
		[Consumes("application/json")] // SetUserLocationAndChatRequest
		[Produces("application/json")] // ChatResolutionResponse
		public async Task<string> SetUserLocationAndChat([FromBody] SetUserLocationAndChatRequest request)
		{
			ChatResolutionResponse response = await UsersService.SetUserLocationAndChat(request);

			return JsonSerializer.Serialize(response);
		}

		[HttpPut, Route("UpdateUserLocation")]
		[Consumes("application/json")] // SetUserLocationAndChatRequest: same request, but only updates location
		[Produces("application/json")] // SetUserLocationResponse
		public async Task<string> UpdateUserLocation([FromBody] SetUserLocationAndChatRequest request)
		{
			SetUserLocationResponse response = await UsersService.UpdateUserLocation(request);

			return JsonSerializer.Serialize(response);
		}

		[HttpPut, Route("GetUserRating")]
		[Consumes("application/json")] // GetUserRatingRequest
		public async Task<Int16> GetUserRating([FromBody] GetUserRatingRequest request)
		{
			//GetUserRatingRequest? request = JsonSerializer.Deserialize<GetUserRatingRequest>(jsonRequest);

			Int16 rating = await UsersService.GetUserRating(request);

			return rating;
		}

		[HttpPut, Route("SetUserRating")]
		[Consumes("application/json")] // SetUserRatingRequest
		public async Task SetUserRating([FromBody] SetUserRatingRequest request)
		{
			await UsersService.SetUserRating(request);
		}

		[HttpPut, Route("SetUserBlockState")]
		[Consumes("application/json")] // SetUserBlockRequest
		public async Task SetUserBlockState([FromBody] SetUserBlockRequest request)
		{
			await UsersService.SetUserBlockState(request);
		}

		[HttpGet, Route("GetBlockedUsers")]
		public async Task<List<Int64>> GetBlockedUsers(Int64 userId)
		{
			return await UsersService.GetBlockedUsers(userId);
		}

		[HttpGet, Route("GetUserNotifications")]
		[Produces("application/json")]

		public async Task<string> GetUserNotifications(Int64 userId)
		{
			UserNotificationResponse response = await UsersService.GetUserNotifications(userId);

			return await CryptographyService.EncryptAndEncodeResponse(userId, response);
		}

		[HttpGet, Route("GetCurrentUserInformation")]
		[Produces("application/json")]

		public async Task<string> GetCurrentUserInformation(Int64 userId)
		{
			UserInformation response = await UsersService.GetCurrentUserInformation(userId);
			
			return await CryptographyService.EncryptAndEncodeResponse(userId, response);
		}

		[HttpGet, Route("GetOtherUserInformation")]
		[Produces("application/json")]
		public async Task<string> GetOtherUserInformation(Int64  userIdCurrent, 
														  Int64  userIdOther, 
														  string localeCurrentUser = "en")
		{
			UserInformation response = await UsersService.GetOtherUserInformation(userIdOther, localeCurrentUser);

			return await CryptographyService.EncryptAndEncodeResponse(userIdCurrent, response);
		}

		[HttpPut, Route("UpdateProfile")]
		[Consumes("application/json")]
		[Produces("application/json")]
		public async Task UpdateProfile([FromBody] EncryptionWrapperProxChat wrapper)
		{
			UpdateProfileRequest request;

			CryptographyService.DecodeAndDecryptRequest<UpdateProfileRequest>(wrapper, out request);

			await UsersService.UpdateProfile(request);
		}

		[HttpGet, Route("GetInterestNames")]
		public async Task<List<string>> GetInterestNames(string locale)
		{
			return await UsersService.GetInterestNames(locale);
		}

		[HttpGet, Route("GetUserInterests")]
		[Produces("application/json")]
		public async Task<string> GetUserInterests(Int64 userId)
		{
			List<UserInterest> interests = await UsersService.GetUserInterests(userId);

			return JsonSerializer.Serialize(interests);
		}

		[HttpGet, Route("GetUserProfiles")]
		[Produces("application/json")]
		public async Task<string> GetUserProfiles(Int64 userId)
		{
			List<UserProfile> response = await UsersService.GetUserProfiles(userId);

			return await CryptographyService.EncryptAndEncodeResponse(userId, response);
		}

		[HttpPut, Route("UpdateUser")]
		[Consumes("application/json")]
		[Produces("application/json")]
		public async Task<string> UpdateUser([FromBody] EncryptionWrapperProxChat wrapper)
		{
			UserUpdateRequest request;

			CryptographyService.DecodeAndDecryptRequest<UserUpdateRequest>(wrapper, out request);
			UserUpdateResponse response = await UsersService.UpdateUser(request);

			return await CryptographyService.EncryptAndEncodeResponse(wrapper.userId, response);
		}

		[HttpPut, Route("SetNewChatRadius")]
		[Consumes("application/json")]
		public async Task SetNewChatRadius([FromBody] SetChatRadiusRequest request)
		{
			await UsersService.SetNewChatRadius(request);
		}

		[HttpPut, Route("RemoveUserFromChat")]
		public async Task RemoveUserFromChat(Int64 userId)
		{
			await UsersService.RemoveUserFromChat(userId);
		}

		[HttpPut, Route("SetUserOffline")]
		public async Task SetUserOffline(Int64 userId)
		{
			await UsersService.SetUserOffline(userId);
		}

		[HttpGet, Route("GetTranslationOptOutRequest")]
		public async Task<bool> GetTranslationOptOut(GetTranslationOptOutRequest request)
		{
			return await UsersService.GetTranslationOptOut(request);
		}

		[HttpPut, Route("SetTranslationOptOut")]
		public async Task SetTranslationOptOut(SetTranslationOptOutRequest request)
		{
			await UsersService.SetTranslationOptOut(request);
		}

		[HttpGet, Route("GetAvatar")]
		public async Task<string?> GetAvatar(Int32 profileId)
		{
			return await UsersService.GetAvatar(profileId);
		}

		[HttpPut, Route("SetAvatar")]
		public async Task<List<ObscenityViolations>> SetAvatar([FromBody] SetAvatarRequest request)
		{
			return await UsersService.SetAvatar(request.profileId, request.avatarB64);
		}

		[HttpGet, Route("GetGlobalUserRating")]
		public async Task<UserRating> GetGlobalUserRating(Int64 userId)
		{
			return await UsersService.GetGlobalUserRating(userId);
		}
	}
}
