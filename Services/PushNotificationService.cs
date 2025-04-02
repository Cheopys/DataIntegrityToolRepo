using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using System.Net;

namespace ProxChat.Services.PushNotifications
{
	public class AttachmentAttribute
	{

	}

	public static class PushNotificationService
	{
		// topics 
		public static async Task<string> CreateSNSTopic()
		{
			string topic = Guid.NewGuid().ToString();

			IAmazonSimpleNotificationService client = new AmazonSimpleNotificationServiceClient();

//			string topicArn = await CreateSNSTopicAsync(client, topic);

			return topic;
		}

		public static async Task<string> CreateSNSTopicAsync(IAmazonSimpleNotificationService client, string topicName)
		{
			CreateTopicRequest request = new CreateTopicRequest
			{
				Name = topicName
			};

			CreateTopicResponse response = await client.CreateTopicAsync(request);

			return response.TopicArn;
		}
		public static async Task<bool> DeleteTopicByArn(string topicArn)
		{
			DeleteTopicResponse deleteResponse = await new AmazonSimpleNotificationServiceClient().DeleteTopicAsync(new DeleteTopicRequest()
			{
				TopicArn = topicArn
			});

			return deleteResponse.HttpStatusCode == HttpStatusCode.OK;
		}

		// subscriptions

		public static async Task<string> SubscribeTopicWithFilter(string  topicArn, 
																  string? filterPolicy, 
																  string  queueArn)
		{
			SubscribeRequest subscribeRequest = new()
			{
				TopicArn = topicArn,
				Protocol = "sqs",
				Endpoint = queueArn
			};

			if (string.IsNullOrEmpty(filterPolicy) == false)
			{
				subscribeRequest.Attributes = new Dictionary<string, string> { { "FilterPolicy", filterPolicy } };
			}

			SubscribeResponse subscribeResponse = await new AmazonSimpleNotificationServiceClient().SubscribeAsync(subscribeRequest);

			return subscribeResponse.SubscriptionArn;
		}

		// publish

		// this will be used for private chats, where there is only one recipient

		public static async Task PublishToTopicAsync(string topicArn,
													 string messageText)
		{
			PublishRequest request = new PublishRequest
			{
				TopicArn = topicArn,
				Message  = messageText,
			};

			PublishResponse response = await new AmazonSimpleNotificationServiceClient().PublishAsync(request);
		}

		// used for public chats, multiple recipients, no binary data

		public static async Task PublishBatchToTopicAsync(string topicArn,
														  List<PublishBatchRequestEntry> entries)
		{
			PublishBatchRequest request = new PublishBatchRequest
			{
				TopicArn				   = topicArn,
				PublishBatchRequestEntries = entries
			};

			PublishBatchResponse response = await new AmazonSimpleNotificationServiceClient().PublishBatchAsync(request);
		}
	}
}
