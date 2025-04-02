using System;
using System.Collections.Generic;
using System.Net;
using Geolocation;
using ProxChat.Schema;

namespace ProxChat.SharedObjectTypes
{
    public class Error
    {
        public HttpStatusCode httpCode   { get; set; }
        public string message { get; set; }
        public Error()
        {httpCode = HttpStatusCode.OK;
            message  = string.Empty;
        }
    }

	// enums

	public enum Pronouns
	{
		PreferNot = 0,
		He        = 1,
		She       = 2,
        They      = 3 // included under protest
	}

	public enum Relationship
	{
		PreferNot       = 0,
		Single          = 1,
		InaRelationship = 2,
		Engaged         = 3,
		Married         = 4,
        Divorced        = 5,
        Widowed         = 6
	}

	public enum DistanceUnits
	{
		British = 0,
		Metric  = 1
	}
	
    public enum UserBlockState
    {
        Unblocked = 0,
        Blocked   = 1
    }

	public enum LoggingLevel
	{
		Info,
		Warning,
		Error,
		Exception
	}

    public enum UserRating
    {
        ratingUnrated    = 0,
        ratingOneStar    = 1,
		ratingTwoStars   = 2,
		ratingThreeStars = 3,
		ratingFourStars  = 4,
		ratingFiveStars  = 5
	}

	public enum ImageRejectionType
	{
		Avatar,
		ChatMessage,
		DirectMessage,
        Test
	}

	// encrypted requests

	public class EncryptionWrapperProxChat
    {
        public Int64  userId {get; set;}
        public string seed   { get; set;}
        public string encryptedRequest  {get; set;} // temporarily raw B64
    }

	public class ChatRadius
	{
        public Int16 Id           { get; set; }
        public float value        { get; set; }
        public string description { get; set; }
		public DistanceUnits units { get; set; }
	}
    
	public class Locale
    {
        public string locale        { get; set; }
        public string nameEnglish   { get; set; }
        public string nameLocalized { get; set; }
    }
    
    public class ApplicationText
    {
        public Int32  id        { get; set; }
        public string locale    { get; set; }
        public string token     { get; set; }
        public string text      { get; set; }
    }

	public class LocaleText
	{
		public string locale { get; set; }
		public string token { get; set; }
		public string text { get; set; }
	}

	public class ProvisioningData
	{
        public string locale                            { get; set; }
        public List<LocaleText> localizedStrings   { get; set; }
        public List<Locale>    localesSupported           { get; set; }
        public List<ChatRadius> radiiImperial           { get; set; }
        public List<ChatRadius> radiiMetric             { get; set; }
	}
	
	public class LoggingRequest
    {
        public Int64 userId         { get; set; }
        public LoggingLevel level   { get; set; }
        public string loggingName   { get; set; }
        public string deviceInfo    { get; set; }
        public string callStack     { get; set; }
        public string message       { get; set; }
        public string? detail       { get; set; }
	}
    

    public enum InformationLevel
    {
        Unspecified = 0,
        Lite        = 1, // e.g. ID only
        Regular     = 2,
        Full        = 3
    }

    public enum AvatarType
    {
        None    = 0,
        JPG     = 1,
        PNG     = 2
    }

    public enum UserProfileType
    {
        profileTypePublic   = 1,
        profileTypePrivate  = 2,
        profileTypeBusiness = 3
    }

	// provisioning

	// user types

	public class RegisterClientRequest
    {
        public byte[] aeskey { get; set; } // temporary AES key
        public byte[] aesiv  { get; set; } // temporary AES IV
	}

	public class RegisterUserRequest
    {
        public Int64 userId                 { get; set; }
        public string firstName             { get; set; }
        public string lastName              { get; set; }
        public string moniker               { get; set; }
        public string email                 { get; set; }
        public string locale                { get; set; }
        public DistanceUnits distanceUnits  { get; set; } = DistanceUnits.British;
        public string passwordHash          { get; set; }
        public DateTime dateOfBirth           { get; set; }
        public string phoneNumber           { get; set; }
        public List<string> interests       { get; set; }
        public Pronouns pronoun   { get; set; }
        public Relationship relationship    { get; set; }
    }

	public class RegisterUserLightRequest
	{
		public Int64    userId          { get; set; }
		public string   moniker         { get; set; }
		public string   email           { get; set; }
        public string   locale          { get; set; }
		public string   passwordHash    { get; set; }
		public string   birthdayString   { get; set; }
	}

	public class LoginRequest
    {
       public Int64 userIdRegistering { get; set; } 
        public string Email         { get; set; }
        public string PasswordHash  { get; set; }
    }

    public class UserInterest
    {
        public Int64 UserId         { get; set; }
        public Int32 InterestsId    { get; set; }
        public string? Name         { get; set; }
    }

    public class UserInformation
    {
        public Int64 userId                     { get; set; }    // Lite
        public string? moniker                  { get; set; }
        public bool online                      { get; set; }
        public int currentProfileId             { get; set; }
        public string? firstName                { get; set; }   // regular
        public string? lastName                 { get; set; }
        public string email                     { get; set; }
        public string? locale                   { get; set; }
        public DistanceUnits distanceUnit       { get; set; } = DistanceUnits.British;
        public byte[]? aesKey                   { get; set; }
        public byte[]? aesIV                    { get; set; }
        public string? birthdayString           { get; set; } // full
        public Pronouns? pronoun                { get; set; }
        public Relationship? relationship       { get; set; }
        public List<UserInterest> interests     { get; set; }
        public UserRating   rating              { get; set; }
        public List<string> translationOptOuts  { get; set; }
        public Error Error                      { get; set; }

        public UserInformation()
        {
            Error = new();
        }
    }

    public class UserUpdateRequest
    {
        public Int64 userId              { get; set; }
        public string firstName          { get; set; }
        public string lastName           { get; set; }
        public string email              { get; set; }
        public string  birthdayString    { get; set; }
        public string locale             { get; set; }
        public Pronouns pronoun          { get; set; }
        public Relationship relationship { get; set; }
        public List<string> interestNames { get; set; }
		public DistanceUnits distanceunit { get; set; }
		public Error error { get; set; }

        public UserUpdateRequest()
        {
            error = new();
        }
    }

    public class UserUpdateResponse
    {
        public Int64        userId          { get; set; }
        public string       firstName       { get; set; }
        public string       lastName        { get; set; }
        public string       email           { get; set; }
        public string       birthday        { get; set; }
        public string       locale          { get; set; }
        public Pronouns     pronoun         { get; set; }
        public Relationship relationship    { get; set; }
        public List<string> interests       { get; set; }
        public DistanceUnit distanceunit    { get; set; }
    }

	public class S3Attachment
	{
        public string name { get; set; }
		public byte[] image { get; set; }
		public Error error { get; set; }

		public S3Attachment()
		{
			error = new();
		}
	}

	    public class SetTranslationOptOutRequest
	    {
	        public Int64  userId { get; set; }
	        public string locale { get; set; }
	        public bool   optout { get; set; }
		}

		public class GetTranslationOptOutRequest
		{
			public Int64  userId { get; set; }
			public string locale { get; set; }
		}


	public class GetDirectMessagesRequest
    {
        public Int64  userIdSender    { get; set; }
		public Int64? userIdRecipient { get; set; }
		public Int16  countSkips      { get; set; }
        public Int16  countFetch      { get; set; }
    }

    public class GetDirectMessagesResponse
    {
        public Int16 countSkipped { get; set; }
        public List<DirectMessage> directMessages { get; set; }
    }

    public class UserNotificationResponse
    {
        public DateTime notificationTime             { get; set; }
        public Int64 userId                          { get; set; }
        public Int32 chatId                          { get; set; }
        public Int32 countChatUsers                  { get; set; }
		public Int32 countChatFriends                { get; set; }
		public List<UserInformation> newUsersInChat  { get; set; }
        public List<ChatMessage> newChatMessages     { get; set; }
        public List<DirectMessage> newDirectMessages { get; set; }
        public List<FriendRequest> friendRequests    { get; set; }
    }

    public class FriendRequest
    {
        public Int32 Id { get; set; }
        public Int64 userIdSender { get; set; }
        public Int64 userIdRecipient { get; set; }
        public string Moniker { get; set; }
        public DateTime TimeSent { get; set; }
    }

    public class DirectMessage
    {
        public Int64 Id { get; set; }
        public Int64 UserIdSender   { get; set; }
        public Int64 UserIdRecipient { get; set; }
        public string Message       { get; set; }
        public string?  ImageB64     { get; set; }
        public DateTime TimeSent     { get; set; }
    }

    public class ChatMessage
    {
        public Int64 Id             { get; set; }
        public Int32 ChatId         { get; set; }
        public Int64 UserIdSender   { get; set; }
        public string Message       { get; set; }
        public DateTime TimeSent    { get; set; }
		public string?  imageB64    { get; set; }

		public ChatMessage()
        {
            Message = string.Empty;
        }
    }

    public class AddInterestRequest
    {
        public string name { get; set; }
        public string language { get; set; }
    }

    public class AddInterestResponse
    {
        public Int32 Id { get; set; }
        public string name { get; set; }
    }

    public class PrivacySetting
    {
        public Int32 Id                 { get; set; }
        public bool firstNamePrivate    { get; set; }
        public bool lastNamePrivate     { get; set; }
        public bool emailPrivate        { get; set; }
        public bool birthdayPrivate     { get; set; }
        public bool pronounPrivate       { get; set; }
        public bool relationshipPrivate { get; set; }
    }

    public class UserProfile
    {
        public Int32 profileId              { get; set; }
        public Int64 userId                 { get; set; }
        public UserProfileType profiletype  { get; set; }
		public bool  isCurrent              { get; set; }
		public string name                  { get; set; } // profile name
        public string  moniker              { get; set; }
        public PrivacySetting privacy       { get; set; }
        public List<UserInterest> interests { get; set; }
        public bool  avatar                 { get; set; }
		public string Employer              { get; set; }
		public string Title                 { get; set; }
		public string Responsibilities      { get; set; }
		public Error Error                  { get; set; }

        public UserProfile()
        {
            Error       = new();
            interests   = new();
            privacy     = new();
        }
    }

    public class UpdateProfileRequest
    {
        public Int32 profileId { get; set; }

		// data to replace existing

		public bool?            isCurrent           { get; set; }
		public string?          moniker             { get; set; }
        public string?          email               { get; set; } 
        public Pronouns?        pronoun             { get; set; }
        public Relationship?    relationship        { get; set; }             
		public PrivacySetting?  privacy             { get; set; }
        public bool?            avatar              { get; set; }
		public List<UserInterest>? interests        { get; set; }
		public string?          Employer            { get; set; }
		public string?          Title               { get; set; }
		public string?          Responsibilities    { get; set; }
    }

    public class AddFriendRequestRequest
    {
        public Int64 userId { get; set; }
        public Int64 friendId { get; set; }
    }

    public class UnFriendRequest
    {
        public Int64 userId { get; set; }
        public Int64 friendId { get; set; }
    }

    public class GetFriendRequestsResponse
    {
        public List<FriendRequest> friendRequestsSent       { get; set; }
        public List<FriendRequest> friendRequestsReceived   { get; set; }

		public GetFriendRequestsResponse()
        {
            friendRequestsSent     = new();
            friendRequestsReceived = new();
        }
	}

    public class ProcessFriendRequestRequest
    {
        public Int64 userId     { get; set; }
        public Int64 friendId   { get; set; }
        public bool  accept     { get; set; }
    }

    public class SetChatRadiusRequest
    {
        public Int64 userId { get; set; }
        public Int16 chatRadiusID { get; set; }
    }

    // chat types

	public class ChatUsersOnline
	{
		public Int32    Id          { get; set; }
		public Int64    UserId      { get; set; }
		public double   Latitude    { get; set; }
		public double   Longitude   { get; set; }
		public double   Altitude    { get; set; }
		public Int16    Radius      { get; set; }
		public DateTime TimeOnline  { get; set; } = DateTime.UtcNow;
	}


	public class CreateChatResponse
    {

        public Int32 ChatId { get; set; }
		public string Name { get; set; }
		public List<ChatUsersOnline> usersonline { get; set; }

	}

    [Flags]
    public enum LocationResponse
    {
        locationResponseNowOnline = 1,
        locationResponseNotInChat = 2,
        locationResponseAddedToChat = 4,
        locationResponseInChat = 8,
        locationResponseOutsideRadius = 16
    }

    public class Coordinates
    {
        public double latitude { get; set; }
		public double longitude { get; set; }
		public double altitude { get; set; }
	}

    public class SetUserLocationAndChatRequest
    {
        public Int64 userId { get; set; }
		public Int16 chatRadiusID { get; set; }
		public Coordinates coordinates { get; set; }
	}

    public class SetUserLocationResponse
    {
        public Int32? chatId { get; set; }
		public LocationResponse locationresponse { get; set; }
	}

    public class UsersInRadiusRequest
    {
        public Int64 userId { get; set; }
		public Coordinates coordinateCenter { get; set; }
		public Int16 chatRadiusID { get; set; }
		public bool filterChatUsers { get; set; }
		public bool filterBlocked { get; set; }
	}

	public class UserCountsInRadiusRequest
	{
		public Int64 userId { get; set; } // currently unused, left by request
		public Coordinates coordinateCenter { get; set; }
        public Int16 chatRadiusID { get; set; }
	}

	public class UsersInRadiusResponse
    {
        public int countUsers { get; set; }
        public int countFriends { get; set; }
    }

    public class ChatResolutionResponse
    {
        public Int32? chatId { get; set; }     // PK of Chats table
		public bool chatCreated { get; set; }
		public string chatName { get; set; }
		public Coordinates center { get; set; }
		public bool filterChatUsers { get; set; }
		public bool filterBlocked { get; set; }
		public Int16 chatRadiusID { get; set; }
		public Int32 countUsers { get; set; }
		public Int32 countFriends { get; set; }
	}

    public class ChatSendMessageRequest
    {
        public Int64 userId         { get; set; }
		public Int32 chatId         { get; set; }
		public string message       { get; set; }
		public string? imageB64     { get; set; }
	}

	public class SendDirectMessageRequest
    {
        public Int64 userIdSender    { get; set; }
		public Int64 userIdRecipient { get; set; }
		public string message        { get; set; }
        public DateTime timeSent     { get; set; } = DateTime.UtcNow;
        public string? imageB64      { get; set; } 
	}

    public class DeleteDirectMessagesRequest
    {
        public Int64 userIdSender { get; set; }
		public Int64 userIdRecipient { get; set; }
	}

    // friends

    // ratings and blocks

    // also serves as response
    public class SetUserRatingRequest
    {
        public Int64 userId { get; set; }
		public Int64 userIdRated { get; set; }
		public Int16 rating { get; set; }
	}

    public class GetUserRatingRequest
    {
        public Int64 userId { get; set; }
		public Int64 userIdRated { get; set; }
	}

        public class SetUserBlockRequest
    {
        public Int64 userId { get; set; }
		public Int64 userIdBlocked { get; set; }
		public UserBlockState blockstate { get; set; }
	}

    
	// private chats

    public class SetAvatarRequest
    {
        public Int32 profileId { get; set; }
        public string avatarB64 { get; set; }
    }

	public class ObscenityViolations
	{
		public string label { get; set; }
		public float  confidence { get; set; }
		public string? parent { get; set; }
	}

	public class ImageRejectionLogResponse
	{
		public string name              { get; set; }
		public string violationsJSON    { get; set; }
	}

}


