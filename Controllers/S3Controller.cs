using DataIntegrityTool.Schema;
using DataIntegrityTool.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;

namespace DataIntegrityTool.Controllers
{
	public class S3Controller : ControllerBase
	{
		[HttpGet, Route("DownloadTool")]
		public async Task<IActionResult> DownloadTool(InterfaceType interfacetype,
													  OSType ostype)
		{
			string key = S3Service.CreateToolKey(interfacetype, ostype);

			string filepath = $"/home/ec2-user/DataIntegrityToolRepo/{key}";
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
		public async Task UploadTool(OSType ostype,
											 InterfaceType interfacetype,
											 string toolB64)
		{
			S3Service.StoreTool(ostype, interfacetype, toolB64);
		}
	}
}
