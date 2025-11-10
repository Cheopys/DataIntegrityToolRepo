using Amazon.Runtime.Internal;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using DataIntegrityTool.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NLog;
using System;
using System.IO;
using System.Net;
using System.Text;

namespace DataIntegrityTool.Services
{
	public static class S3Service
	{
		public static async Task<string> StoreTool(OSType		 ostype,
										   InterfaceType interfacetype,
										   string		 pathSource)
		{
			string key = CreateToolKey(interfacetype, ostype);
			string ret = String.Empty;

			using (IAmazonS3 S3client = new AmazonS3Client(Amazon.RegionEndpoint.CACentral1))
			{
				using (TransferUtility fileTransferUtility = new TransferUtility(S3client))
				{
//					string pathFull = Path.Combine(pathSource, key);
					try
					{
						await fileTransferUtility.UploadAsync(pathSource, "dataintegritytool", key);
						ret = $"file {pathSource} uploaded";
					}
					catch (Exception ex)
					{
						ret = $"file {pathSource} upload failed: {ex.Message}";
					}

					fileTransferUtility.Dispose();
				}
					
				S3client.Dispose();
			}

			return ret;
		}

		public static async Task<byte[]>	GetTool(InterfaceType	interfacetype,
										OSType					ostype)
		{
			string key  = CreateToolKey(interfacetype, ostype);

			string filepath = $"/home/ec2-user/DataIntegrityToolRepo/{key}";

			return await File.ReadAllBytesAsync(filepath);

			/*
			using (IAmazonS3 S3client = new AmazonS3Client(Amazon.RegionEndpoint.CACentral1))
			{
				using (TransferUtility fileTransferUtility = new TransferUtility(S3client))
				{

					try
					{
						await fileTransferUtility.DownloadAsync(pathDestination, "dataintegritytool", key);
						ret = $"file {pathDestination} downloaded";
					}
					catch (Exception ex)
					{
						ret = $"file {pathDestination} download failed: {ex.Message}";
					}

					fileTransferUtility.Dispose();
				}
				S3client.Dispose();
			}*/
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

			//return ret;
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