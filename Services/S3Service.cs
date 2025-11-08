using Amazon.Runtime.Internal;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
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
		
		//static IAmazonS3 S3client = new AmazonS3Client(Amazon.RegionEndpoint.CACentral1);

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
		public static async Task GetTool(InterfaceType interfacetype,
										OSType		   ostype)
		{
			byte[] tool = null;
			string os = null;
			string key = null;


			switch(ostype)
			{
				case OSType.Windows:
					os = "win";
					break;

				case OSType.Mac:
					os = "mac";
					break;

				case OSType.Linux:
					os = "linux";
					break;
			}

			if (interfacetype == InterfaceType.GUI)
			{
				key = $"katchano_{os}_gui.zip";
			}
			else
			{
				key = $"katchano_{os}_api.zip";
			}

			string filepath = $"/home/ec2-user/tool/{key}";

			if (File.Exists(filepath) == false)
			{
				GetObjectRequest request = new()
				{
					BucketName = "dataintegritytool",
					Key = key
				};


				using (IAmazonS3 S3client = new AmazonS3Client(Amazon.RegionEndpoint.CACentral1))
				{
					await S3client.DownloadToFilePathAsync(request.BucketName, request.Key, filepath, new Dictionary<string, Object>());

					S3client.Dispose();
				}
			}

			using (IAmazonS3 S3client = new AmazonS3Client(Amazon.RegionEndpoint.CACentral1))
			{
				TransferUtility fileTransferUtility = new TransferUtility(S3client);

				fileTransferUtility.Download($"G:\\Downloads\\{key}", "dataintegritytool", key);

				S3client.Dispose();
			}
			/*
			using (FileStream file = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous))
			{
				tool = new byte[file.Length];
				await file.ReadAsync(tool, 0, (int)file.Length);

				file.Dispose();
			}

			//tool = await File.ReadAllBytesAsync(filepath);

			//File.Delete(filepath);

			/*
						using (GetObjectResponse response = await S3client.GetObjectAsync(request))
						{
							if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
							{
								try
								{
									// there is no client method to fetch from S3 directly to binary
									// the entry must be written to a file and then loaded

									string filepath = $"/home/ec2-user/tool/{request.Key}";

									await response.WriteResponseStreamToFileAsync(filepath, false, CancellationToken.None);

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
			*/
			//return Convert.ToBase64String(tool);
		}

	}
}