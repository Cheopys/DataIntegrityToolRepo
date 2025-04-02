
using Amazon.Util;
using Geolocation;
using ProxChat.Schema;

namespace ProxChat.SharedObjectTypes
{
	public class Error
	{
		public System.Net.HttpStatusCode httpCode { get; set; }

		public string message { get; set; }

		public Error()
		{
			httpCode = System.Net.HttpStatusCode.OK;
			message  = string.Empty;
		}
	}

	// enums

	public enum Gender
	{
		Undefined = 0,
		Male	  = 1,
		Female	  = 2,
		PreferNot = 3
	}

	public enum Relationship
	{
		Undefined		= 0,
		Single			= 1,
		InaRelationship = 2,
		Married			= 3,
		PreferNot		= 4
	}

	public enum DistanceUnits
	{
		British = 0,
		Metric  = 1
	}

	public enum UserBlockState
	{
		Unblocked	= 0,
		Blocked		= 1
	}

	public enum InformationLevel
	{
		Unspecified  = 0,
		Lite		 = 1, // e.g. ID only
		Regular		 = 2,
		Full		 = 3
	}

	public enum	AttachmentType
	{
		None  = 0,
		Audio = 1,
		Video = 2
	}

	public enum LoggingLevel
	{
		Info,
		Warning,
		Error,
		Exception
	};

	public class ChatRadius
	{
		public		  Int16 Id		{ get; set; }
		public float  value			{ get; set; }
		public string description	{ get; set; }
		public DistanceUnits units { get; set; }
	}

	public class Locale
	{
		public string abbreviation	{ get; set; }
		public string nameEnglish	{ get; set; }
		public string nameLocalized	{ get; set; }
		public string nameFallback	{ get; set; }
	}

	public class LocaleText
	{
		public string locale	{ get; set; }
		public string token		{ get; set; }
		public string text		{ get; set; }
	}

	public class ProvisioningData
	{
		public string				 locale				{ get; set; }
		public List<LocaleText>		 localizedStrings	{ get; set; } 
		public List<Locale>			 localesSupported	{ get; set; } 
		public List<ChatRadius>		 radiiImperial		{ get; set; }
		public List<ChatRadius>		 radiiMetric		{ get; set; }
		public List<string>			 forbiddenWords		{ get; set; }
	}

	public class LoggingRequest
	{
		public Int64 userId { get; set; }
		public LoggingLevel level { get; set; }
		public string loggingName { get; set; }
		public string deviceInfo { get; set; }
		public string callStack { get; set; }
		public string message { get; set; }
		public string? detail { get; set; }
	}

		public class RegisterClientRequest
	{
		public string clientAESKeyHex	{ get; set; } // new client's termporary AES key
		public string clientAESIVHex	{ get; set; } // new client's termporary AES IV
	}

	public class EncryptionWrapperProxchat
	{
		public Int64 userId			{ get; set; }
		public string requestHex	{ get; set; }
	}

	// user types

	public class RegisterUserRequest
	{
		public Int64		 RegistrationId	{ get; set; }
		public string		 firstName		{ get; set; }
		public string		 lastName		{ get; set; }
		public string        moniker		{ get; set; }
		public string        phoneNumber    { get; set; }
		public string		 avatarHex		{ get; set; }
		public string		 email			{ get; set; }
		public string		 locale			{ get; set; }
		public DistanceUnits distanceUnits	{ get; set; }
		public string		passwordHash	{ get; set; }
		public DateTime		dateOfBirth		{ get; set; }
		public List<string> interests		{ get; set; }
		public Gender		gender			{ get; set; }
		public Relationship relationship	{ get; set; }
	}

	public class LoginRequest
	{
		public string Email			{ get; set; }
		public string PasswordHash	{ get; set; }
	}

	public class LoginResponse
	{ 
		public Int64 UserId { get; set; }
		public Error Error { get; set; }
	}

	public class UserInterest
	{
		public Int64  UserId		{ get; set; }
		public Int32  InterestsId	{ get; set; }
		public string Name			{ get; set; }
	}

	public class User
	{
		public Int64    Id				{ get; set; }
		
		public Int16    currentProfile	{ get; set; }
		
		public string   firstName		{ get; set; }
		
		public string   lastName		{ get; set; }
		
		public byte[]	avatar			{ get; set; }
		public string   email			{ get; set; }
		
		public string   birthday		{ get; set; }
		
		public Gender   gender			{ get; set; }
		
		public string   passwordHash	{ get; set; }
		
		public Relationship relationship{ get; set; }
	}

		public class UserInformation
		{
			public Int64		userId			{ get; set; }	// Lite
			public string		moniker			{ get; set; }
			public bool			online			{ get; set; }
			public string		avatarHex		{ get; set; }
			public Int32		profileId		{ get; set; }
			public string		firstName		{ get; set; }	// regular
			public string		lastName		{ get; set; }
			public string		email			{ get; set; }
			public string		locale			{ get; set; }
			public DistanceUnit distanceUnit	{ get; set; }
			public string?		aesKey			{ get; set; }
			public string?		aesIV			{ get; set; }
			public DateTime 	birthday		{ get; set; }	// full
			public Gender		gender			{ get; set; }
			public Relationship relationship	{ get; set; }
			public List<UserInterest> interests	{ get; set; }
			public Error error { get; set; }

			public UserInformation()
			{
				error = new();
			}
		}

	public class UserNotificationResponse
		{
			public DateTime				notificationTime	{ get; set; }
			public Int64				userId				{ get; set; }
			public Int32				chatId				{ get; set; }
			public List<UserInformation> newUsersInChat		{ get; set; }
			public List<ChatMessage>    newChatMessages		{ get; set; }
			public List<DirectMessage>  newDirectMessages	{ get; set; }
			public List<FriendRequest>  friendRequests		{ get; set; }
		}

	//
	// begin notification classes
	//

        public class FriendRequest
        {
			public Int32 Id				 { get; set; }
            public Int64 userIdSender    { get; set; }
            public Int64 userIdRecipient { get; set; }
            public string Moniker        { get; set; }
            public DateTime TimeSent	 { get; set; }
			public string avatarHex		 { get; set; }

		}

        public class DirectMessage
        {
			public Int32    Id				{ get; set; }
            public Int64    userIdSender	{ get; set; }
            public Int64    userIdRecipient	{ get; set; }
            public string   message			{ get; set; }
            public DateTime timeSent		{ get; set; }
			public string?  imageName		{ get; set; }
			public string?  imageHex		{ get; set; }
	}

	public class ChatMessage
        {
			public Int64 Id { get; set; }
            public Int64 UserId { get; set; }
	    	public Int32 ChatId  { get; set; }
            public string Message { get; set; }
            public DateTime TimeSent { get; set; }

            public ChatMessage()
            {
                Message = string.Empty;
            }
        }

	//
	// end notification classes
	//

	public class UserUpdateRequest
	{
		public Int64 userId					{ get; set; }   
		public string avatarHex				{ get; set; }
		public string firstName				{ get; set; }
		public string lastName				{ get; set; }
		public string email					{ get; set; }
		public DateTime birthday			{ get; set; }
		public Gender       gender			{ get; set; }
		public Relationship relationship	{ get; set; }
		public List<string> interestNames	{ get; set; }
	}
	public class UserUpdateResponse
	{
		public Int64				userId			{ get; set; }
		public string				firstName		{ get; set; }
		public string				lastName		{ get; set; }
		public string				email			{ get; set; }
		public DateTime				birthday		{ get; set; }
		public string				gender			{ get; set; }
		public string			    relationship	{ get; set; }
		public List<UserInterest>	userInterests		{ get; set; }
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
	public class PrivacyProfile
	{
		public bool firstNamePrivate { get; set; }
		public bool lastNamePrivate { get; set; }
		public bool emailPrivate { get; set; }
		public bool birthdayPrivate { get; set; }
		public bool genderPrivate { get; set; }
		public bool relationshipPrivate { get; set; }
	}
	public class AddProfileRequest
	{
		public Int64 userId { get; set; }

		public string name { get; set; }

		public string moniker { get; set; }

		public List<string> interestNames { get; set; }

		public PrivacyProfile privacy { get; set; }
	}

	public class UpdateProfileRequest
	{
		public Int32 profileId { get; set; }

		public string name { get; set; }

		public string moniker { get; set; }

		public List<Int32> interestIDs { get; set; }

		public PrivacyProfile privacy { get; set; }
			public UpdateProfileRequest()
			{
				profileId		= 0;
				name			= String.Empty;
				moniker			= String.Empty;
				interestIDs	    = new();
				privacy			= new();
			}
		}

	public class UserProfile
	{
		public Int32 profileId	{ get; set; }
		public Int64 userId		{ get; set; }
		public Int16 ordinal	{ get; set; }
		public bool  isCurrent	{ get; set; }
		public string name		{ get; set; } // profile name
		public string moniker	{ get; set; }
		public PrivacyProfile	  privacy		{ get; set; }
		public List<UserInterest> userInterests { get; set; }

		public Error Error { get; set; }

		public UserProfile()
		{
			Error		  = new();
			userInterests = new();
			privacy       = new();
		}
	}

	public class UserInterestsResponse
	{
		public Int64 userId { get; set; }

		public List<UserInterest> interests { get; set; }
	}

	public class GetFriendsRequest
	{
		public Int64 userId { get; set; }
		public InformationLevel informationlevel { get; set; }
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
		public List<FriendRequest> friendRequestsSent		{ get; set; }
		public List<FriendRequest> friendRequestsReceived { get; set; }

		public GetFriendRequestsResponse()
		{
			friendRequestsSent		= new();
			friendRequestsReceived	= new();
		}
	}

	public class ProcessFriendRequestRequest
	{
		public Int64 userId { get; set; }
		public Int64 FriendId { get; set; }
		public bool Accept { get; set; }
	}

	public class SetChatRadiusRequest
	{
		public Int64 userId { get; set; }
		public Int16 chatRadiusID { get; set; }
	}

	public class SetChatRadiusResponse
	{

		public Int16 radius { get; set; }

		public Int16 countUsers { get; set; }

		public Int16 countFriends { get; set; }
	}

	// chat types

	public class CreateChatRequest
	{

		public Int64 UserId { get; set; }

		public string Name { get; set; }

		public Int16 Radius { get; set; }
	}

	public class CreateChatResponse
	{
		public Int32 ChatId { get; set; }

		public string Name { get; set; }

		public List<UsersOnline> usersonline { get; set; }
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

		public Int16 radius { get; set; }

		public Coordinates coordinates { get; set; }
	}

	[Flags]
	public enum LocationResponse
	{
		locationResponseNowOnline		= 1,
		locationResponseNotInChat		= 2,
		locationResponseAddedToChat		= 4,
		locationResponseInChat			= 8,
		locationResponseOutsideRadius	= 16
	}
	public class SetUserLocationResponse
	{
		public Int32?			chatId			 { get; set; } 
		public LocationResponse locationresponse { get; set; }
	}

	public class UserCountsInRadiusRequest
	{
		public Int64	   userId			{ get; set; } // currently unused, left by request
		public Coordinates coordinateCenter	{ get; set; }
		public Int16 radius			{ get; set; }
	}
	
	public class UsersInRadiusRequest
	{
		public Int64 userId					{ get; set; }

		public Coordinates coordinateCenter { get; set; }

		public Int16 radius			{ get; set; }

		public bool filterChatUsers			{ get; set; } = true;

		public bool filterBlocked			{ get; set; } = true;
	}

	public class UsersInRadiusResponse
		{
			public int countUsers { get; set; }

			public int countFriends { get; set; }
		}

		public class ChatResolutionRequest
		{

			public Int64 userId { get; set; }

			public Coordinates coordinateUser { get; set; }

			public Int16 radius { get; set; }

			public bool createNew { get; set; }
		}

		public class ChatResolutionResponse
		{

			public Int32? chatId { get; set; }     // PK of Chats table

			public string chatName { get; set; }

			public Coordinates	center { get; set; }

			public Int16 radius { get; set; }

			public Int32 countUsers { get; set; }

			public Int32 countFriends { get; set; }

			public bool  chatCreated { get; set; }

			public ChatResolutionResponse()
			{
				chatCreated = false;
			}
	}

		public class ChatSendMessageRequest
		{
			public Int64 userId { get; set; }
			public Int32 chatId { get; set; }
			public string message { get; set; }
		}

		public class SendDirectMessageRequest
		{
			public Int64 userIdSender		{ get; set; }
			public Int64 userIdRecipient	{ get; set; }
			public string message			{ get; set; }
			public string? imageName		{ get; set; }
			public string? imageHex			{ get; set; }
		}
		public class S3Attachment
		{
			public byte[] image { get; set; }
			public Error error { get; set; }

			public S3Attachment()
			{
				error = new();
			}
		}

		public class GetDirectMessagesRequest
		{
			public Int64  userIdSender { get; set; }
			public Int64? recipientId  { get; set; }
			public Int16  countSkips   { get; set; }
			public Int16  countFetch   { get; set; }
		}

		public class GetDirectMessagesResponse 
		{
			public Int16				countSkipped	{ get; set; }
			public List<DirectMessage>	directMessages	{ get; set; }
		}


		public class DeleteDirectMessagesRequest
		{
			public Int64 userIdSender    { get; set; }
			public Int64 userIdRecipient { get; set; }
		}


	// friends

		public class FriendOnline
		{
			public Int64  userId	{ get; set; }
			public string firstName	{ get; set; }
			public string lastName	{ get; set; }
		}

	// ratings and blocks

	// also serves as response
		public class SetUserRatingRequest
		{
			public Int64 userId			{ get; set; }
			public Int64 userIdRated	{ get; set; }
			public Int16 rating			{ get; set; }
		}

		public class GetUserRatingRequest
		{
			public Int64 userId		 { get; set; }
			public Int64 userIdRated { get; set; }
		}

		public class SetUserRatingresponse : SetUserRatingRequest { }
		public class GetUserRatingresponse : SetUserRatingRequest { }

		public class GetBlockedUsersResponse
		{
			public Int64   userId	 { get; set; }
			public string? firstName { get; set; }
			public string? lastName	 { get; set; }
		}
		public class SetUserBlockRequest
		{
			public Int64		  userId		{ get; set; }
			public Int64		  userIdBlocked	{ get; set; }
			public UserBlockState blockstate	{ get; set; }
		}

		// private chats

		public class CreatePrivateChatRequest
		{
	
			public Int64 UserId			{ get; set; }
		
			public Int64 OtherUserId	{ get; set; }
		
			public string Name			{ get; set; }
		
			public DateTime TimeCreated	{ get; set; }
		}

		public class CreatePrivateChatResponse
		{
		
			public Int32 Id		{ get; set; }
		
			public string Name	{ get; set; }
		}

		public class SendPrivateMessageRequest
		{
			public Int64 Id				{ get; set; }
			public Int64 OtherUserId	{ get; set;}
			public Int32 PrivateChatId	{ get; set; }
			public string Message			{ get; set; }

			// attachment
			public AttachmentType?	attachmenttype		{ get; set; }
			public string?			attachmentfilename	{ get; set; }
			public byte[]?			attachment			{ get; set; }
		}

		public class SendPrivateMessageResponse
		{
			public Error error { get; set; }
		}

}