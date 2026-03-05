using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections;
using Calcifer.Api.DbContexts.Models;
using Calcifer.Api.DTOs;
using Calcifer.Api.Interface.Common;
using Calcifer.Api.Services;

namespace Calcifer.Api.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class HomeController : Controller
    {
        //service
        private readonly IPublicInterface _publicinterface;
        public HomeController(IPublicInterface publicInterface  )
        {
            _publicinterface =publicInterface;
            

        }

        [HttpGet]
        [Route("/" , Name ="getPublicData")]
        public async Task<IActionResult> GetPublicData()
        {
            dynamic responseInterface =await _publicinterface.GetPublicDataAsync();

            if (responseInterface == null)
            {
                return NotFound();
            }
            return Ok(new ApiResponseDto<dynamic>
            {
                Status = true,
                Message = "Public data retrieve successfully.",
                Data = responseInterface
            }) ;
        }
    }
}
