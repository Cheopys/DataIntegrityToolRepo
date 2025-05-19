using System;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using DataIntegrityTool.Schema;
using DataIntegrityTool.Shared;
using DataIntegrityTool.Services;
using NuGet.Common;
using System.Threading.Tasks;
using Amazon.S3.Model.Internal.MarshallTransformations;

/*
	This controller is for use of DIT 
 */

namespace DataIntegrityTool.Controllers
{
	[ApiController]
	[Route("[controller]")]

	public class TestController : ControllerBase
	{
		[HttpPut, Route("RegisterCustomer_Raw")]
		public async Task<Int32> RegisterCustomer_Raw(RegisterCustomerRequest request)
		{
			return CustomersService.RegisterCustomer(request);
		}

		[HttpGet, Route("GetCustomers_Raw")]
		[Produces("application/json")]
		public async Task<string> GetCustomers_Raw()
		{
			List<Customers> customers = await CustomersService.GetCustomers();

			return JsonSeriaizer.Serialize(customers);
		}

		[HttpGet, Route("GetUsers_Raw")]
		[Produces("application/json")]
		public async Task<string> GetUsers_Raw(Int32 CustomerId)
		{
			List<Users> users = await UsersService.GetUsers(CustomerId);

			return JsonSerializer.Serialize(users);
		}
	}
}