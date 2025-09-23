using DataIntegrityTool.Schema;
using DataIntegrityTool.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataIntegrityTool.Controllers
{
	public class S3Controller : Controller
	{
		public async Task<byte[]> DownloadTool(OSType		 ostype, 
											   InterfaceType interfacetype)
		{
			return await S3Service.GetTool(ostype, interfacetype);
		}
	}
}
