using DataIntegrityTool.Schema;
using DataIntegrityTool.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataIntegrityTool.Controllers
{
	public class S3Controller : ControllerBase
	{
		[HttpGet, Route("DownloadTool")]
		public async Task<string> DownloadTool(InterfaceType interfacetype,
											   OSType		 ostype)
		{
			return await S3Service.GetTool("GUI", "Windows");
		}

		/*
		[HttpPut, Route("UploadTool")]
		public async Task UploadTool(OSType			ostype,
									 InterfaceType	interfacetype, 
									 byte[]			tool)
		{
			await S3Service.StoreTool(ostype, interfacetype, tool);
		}
		*/
	}
}
