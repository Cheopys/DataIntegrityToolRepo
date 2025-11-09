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
		public async Task<string> UploadTool(OSType	ostype,
									 InterfaceType	interfacetype, 
									 string			pathSource)
		{
			return await S3Service.StoreTool(ostype, interfacetype, pathSource);
		}
	}
}
