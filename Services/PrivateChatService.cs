using ProxChat.Db;
using ProxChat.Schema;
using ProxChat.SharedObjectTypes;
using ProxChat.Services;
using System.Security.Cryptography;
using System.Text.Json;
//using ProxChat.Services.PushNotifications;
//using Amazon.SimpleNotificationService.Model;
using System;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Routing.Constraints;

namespace ProxChat.Services
{
	public class PrivateChatService
	{
		public static async Task<CreatePrivateChatResponse> CreatePrivateChat(CreatePrivateChatRequest request)
		{
			CreatePrivateChatResponse response = new();

			using (DataContext dbcontext = new())
			{
				Aes aes = CryptographyService.CreateAesKey();

				PrivateChats chatNew = new()
				{
					Name		= request.Name,
					userId1		= request.UserId,
					userId2		= request.OtherUserId,
					TimeCreated = DateTime.UtcNow,
					AesKeyB64	= Convert.ToBase64String(aes.Key),
					AesIVB64	= Convert.ToBase64String(aes.IV),
//					TopicARN    = await PushNotificationService.CreateSNSTopic()
				};

				// save to get new Chat ID

				await dbcontext.PrivateChats.AddAsync(chatNew);
				await dbcontext.SaveChangesAsync();
				await dbcontext.DisposeAsync	();

				response.Id   = chatNew.Id;
				response.Name = request.Name;
				// TBD: send push notification invitations to other user
			}

			return response;
		}

		public static async Task <SendPrivateMessageResponse> SendPrivateMessage(Int64					   userId, 
																				 SendPrivateMessageRequest request)
		{
			using (DataContext dbcontext = new())
			{
				PrivateChats? chat = dbcontext.PrivateChats.Find(request.PrivateChatId);

				if (chat != null)
				{ 
					PrivateChatMessages message = new()
					{
						PrivateChatId = request.PrivateChatId,
						OtherUserId   = request.OtherUserId,
						Message		  = request.Message,
						TimeSent	  = DateTime.UtcNow,
					};

					await dbcontext.PrivateChatMessages.AddAsync(message);

					await dbcontext.DisposeAsync();

					List<PublishBatchRequestEntry> entries = new();

					// message is the same for all; ID can be anything unique

					Dictionary<string, MessageAttributeValue> attachment = new();

					if (request.attachmenttype != AttachmentType.None
					&&  request.attachment     != null)
					{
						attachment["Type"] = new MessageAttributeValue
						{
							DataType	= "String",
							StringValue = request.attachmenttype == AttachmentType.Audio ? "Audio" : "Video"
						};
						attachment["Attachment"] = new MessageAttributeValue
						{
							DataType    = "Binary",
							BinaryValue = new MemoryStream(request.attachment),
						};
						attachment["Filename"] = new MessageAttributeValue
						{
							DataType    = "String",
							StringValue = request.attachmentfilename
						};
					}

					// using batch for one recipient because of attachment

					entries.Add(new PublishBatchRequestEntry
					{
						Id				  = Guid.NewGuid().ToString(),
						Message			  = request.Message,
						MessageAttributes = attachment
					});

					await PushNotificationService.PublishBatchToTopicAsync(chat.TopicARN, entries);
				}
			}

			return new SendPrivateMessageResponse();
		}
	}
}
