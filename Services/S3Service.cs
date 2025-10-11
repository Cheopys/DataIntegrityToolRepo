using Amazon.S3;
using Amazon.S3.Model;
using DataIntegrityTool.Schema;
using NLog;
using System.IO;
using System.Net;
using System.Text;

namespace DataIntegrityTool.Services
{
	public static class S3Service
	{
		static Logger logger;
		static S3Service()
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
		
		static IAmazonS3 S3client = new AmazonS3Client(Amazon.RegionEndpoint.USWest1);

		/*

		public static async Task StoreTool(OSType		 ostype,
										   InterfaceType interfacetype,
										   byte[]		 tool)
		{
			using (MemoryStream memstream = new())
			{
				memstream.Write(tool, 0, tool.Length);
				memstream.Position = 0;

				PutObjectRequest request = new()
				{
					BucketName	= "dataintegritytool",
					Key			= $"tool{ostype}{interfacetype}",
					InputStream = memstream
				};

				PutObjectResponse response = await S3client.PutObjectAsync(request);
			}
		}
*/
		public static async Task<byte[]> GetTool(string interfacetype,
												 string ostype)
		{
			byte[] tool = null;

			GetObjectRequest request = new()
			{
				BucketName = "dataintegritytool",
				Key		   = $"{interfacetype}/{ostype}",
			};

			using (GetObjectResponse response = await S3client.GetObjectAsync(request))
			{
				if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
				{
					try
					{
						// there is no client method to fetch from S3 directly to binary
						// the entry must be written to a file and then loaded

						string filepath = $"/home/ec2-user/tool/";

						response.WriteResponseStreamToFileAsync(filepath, false, CancellationToken.None);

						tool = File.ReadAllBytes(filepath);

						File.Delete(filepath);
					}
					catch (AmazonS3Exception exception)
					{
						logger.ForExceptionEvent(exception);
					}
				}
				else
				{
					logger.Error($"HTTP Status {response.HttpStatusCode}");
				}

				response.Dispose();
			}

			return tool;
		}
	}
}