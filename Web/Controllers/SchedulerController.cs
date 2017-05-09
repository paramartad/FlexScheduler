using System.Web.Http;
using FlexScheduler.Tools;

namespace Web.Controllers
{
    public class SchedulerController : ApiController
    {
        // POST: api/Scheduler
        [HttpPost]
        public string Post(JsonClasses.Input inputVal)
        {
            var schedule = JsonTools.GenerateScheduleToJson(inputVal.Template, inputVal.Employees,
                inputVal.Availabilities, inputVal.Options);

            return schedule;
        }
    }
}
