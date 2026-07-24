using Common.Dtos;
using EduCore_BusinessLayer;
using EduCoreAPI.Helpers.Dtos.RequestDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EduCoreAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogsController : ControllerBase
    {
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpGet]
        public async Task<ActionResult> GetAllLogs([FromQuery] PageRequest pageRequest)
        {
            List<DtoLog> logs =  await clsLog.GetAllAsync(pageRequest.PageNumber, pageRequest.PageSize);

            return Ok(logs);
        }


        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpGet("{id:int}")]
        public async Task<ActionResult> GetLogById([FromRoute] int Id)
        {
            DtoLog log = await clsLog.FindAsync(Id);

            return Ok(log);
        }

    }
}
