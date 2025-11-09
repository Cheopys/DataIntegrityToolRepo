using DataIntegrityTool.Schema;
using DataIntegrityTool.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataIntegrityTool.Controllers
{
	public class S3Controller : ControllerBase
	{
		[HttpGet, Route("DownloadTool")]
		public async Task<string> DownloadTool(InterfaceType interfacetype,
											   OSType		 ostype,
											   string        pathDestination)
		{
			return await S3Service.GetTool(interfacetype, ostype, pathDestination);
		}

		[HttpPut, Route("UploadTool")]
		public async Task UploadTool(OSType			ostype,
									 InterfaceType	interfacetype, 
									 string			toolB64)
		{
			await S3Service.StoreTool(ostype, interfacetype, toolB64);
		}
	}
}
