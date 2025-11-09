using System;
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
		public static async Task StoreTool(OSType		 ostype,
										   InterfaceType interfacetype,
										   string		 toolB64)
		{
			byte[] tool = Convert.FromBase64String(toolB64);

			using (MemoryStream memstream = new())
			{
				memstream.Write(tool, 0, tool.Length);
				memstream.Position = 0;

				PutObjectRequest request = new()
				{
					BucketName	= "dataintegritytool",
					Key			= CreateToolKey(interfacetype, ostype),
					InputStream = memstream
				};

				using (AmazonS3Client S3client = new())
				{
					try
					{
						DeleteObjectRequest requestDelete = new()
						{
							BucketName	= request.BucketName,
							Key			= request.Key,
						};

						await S3client.DeleteObjectAsync(requestDelete);
					}
					catch (Exception ex)
					{
					}

					PutObjectResponse response = await S3client.PutObjectAsync(request);
				}
			}
		}

		public static async Task<string> GetTool(InterfaceType	interfacetype,
										OSType					ostype,
									    string					pathDestination)
		{
			byte[] tool = null;
			string key  = CreateToolKey(interfacetype, ostype);
			string ret = String.Empty;

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

				try
				{
					fileTransferUtility.Download(Path.Combine(pathDestination, key), "dataintegritytool", key);

					ret = $"file {key} downloaded";

				}
				catch (Exception ex)
				{
					ret = $"file {key} download failed: {ex.Message}";
				}

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

			return ret;
		}

		private static string CreateToolKey(InterfaceType	interfacetype,
										    OSType			ostype)
		{
			string key = String.Empty;
			string os = null;
			string ret = String.Empty;


			switch (ostype)
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

			return key;
		}

	}
}