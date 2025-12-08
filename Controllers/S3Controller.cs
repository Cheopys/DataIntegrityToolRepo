using Amazon.S3;
using Amazon.S3.Transfer;
using DataIntegrityTool.Schema;
using DataIntegrityTool.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Net.Http.Headers;
using System.Text;

namespace DataIntegrityTool.Controllers
{
	public class S3Controller : ControllerBase
	{
		[HttpPut, Route("RefreshTool")]
		public async Task<string> RefreshTool(InterfaceType interfacetype,
											  OSType		ostype)
		{
			string _ret = String.Empty;
			string key = S3Service.CreateToolKey(interfacetype, ostype);

			string filepath = $"/home/ec2-user/DataIntegrityToolRepo/{key}";

			using (IAmazonS3 S3client = new AmazonS3Client(Amazon.RegionEndpoint.CACentral1))
			{

				using (TransferUtility fileTransferUtility = new TransferUtility(S3client))
				{
					try
					{
						await fileTransferUtility.DownloadAsync(filepath, "dataintegritytool", key);
						_ret = $"file {filepath} downloaded";
					}
					catch (Exception ex)
					{
						_ret = $"file download failed: {ex.Message}";
					}

					fileTransferUtility.Dispose();
				}

				S3client.Dispose();
			}

			return _ret;
		}

		[HttpGet, Route("DownloadTool")]
		public async Task<IActionResult> DownloadTool(InterfaceType interfacetype,
													  OSType ostype)
		{
			string key = S3Service.CreateToolKey(interfacetype, ostype);

			string filepath = $"/home/ec2-user/DataIntegrityToolRepo/{key}";
/*
			using (IAmazonS3 S3client = new AmazonS3Client(Amazon.RegionEndpoint.CACentral1))
			{
				using (TransferUtility fileTransferUtility = new TransferUtility(S3client))
				{
					try
					{
						await fileTransferUtility.DownloadAsync(filepath, "dataintegritytool", key);
						//					ret = $"file {pathDestination} downloaded";
					}
					catch (Exception ex)
					{
						string ret = $"file download failed: {ex.Message}";
					}

					fileTransferUtility.Dispose();
				}

				S3client.Dispose();
			}
*/
			Response.Headers.Add("Content-Disposition", new ContentDispositionHeaderValue("attachment") { FileName = key }.ToString());
			Response.Headers.Add("Content-Length", new FileInfo(filepath).Length.ToString());

			// Stream the file directly to the response body
			using (FileStream stream = new FileStream(filepath, FileMode.Open, FileAccess.Read))
			{
				await stream.CopyToAsync(Response.Body);
			}

			// Return an empty result as the response has already been written to
			return new EmptyResult();
			//return await S3Service.GetTool(interfacetype, ostype);
		}

		[HttpPut, Route("UploadTool")]
		public async Task<string> UploadTool([FromBody] UploadToolRequest request)
		{
			return S3Service.StoreTool(request.OSType, request.InterfaceType, request.base64);
		}
	}
}
