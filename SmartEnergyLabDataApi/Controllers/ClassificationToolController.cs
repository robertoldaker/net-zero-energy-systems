using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SmartEnergyLabDataApi.Models;
using HaloSoft.EventLogger;

namespace SmartEnergyLabDataApi.Controllers
{
    [Route("ClassificationTool")]
    [ApiController]
    public class ClassificationToolController : ControllerBase
    {
        private IBackgroundTasks _backgroundTasks;
        public ClassificationToolController(IBackgroundTasks backgroundTasks)
        {
            _backgroundTasks = backgroundTasks;
        }

        /// <summary>
        /// Runs the classification tool 
        /// </summary>
        [HttpPost]
        [Route("Run")]
        public ClassificationToolOutput Run(ClassificationToolInput input)
        {
            using( var m = new ClassificationTool() ) {
                return m.Run(input);
            }
        }

        /// <summary>
        /// Runs the classification tool 
        /// </summary>
        [HttpPost]
        [Route("RunOnSubstation")]
        public void RunOnSubstation(int id)
        {
            using( var m = new ClassificationTool() ) {
                m.Run(id);                
            }
        }

        /// <summary>
        /// Runs the classification tool on distribution substations for the specified graphical area
        /// </summary>
        /// <param name="gaId">Graphical area id</param>
        /// <param name="input">Input parameters (note elexon profiles are ignored)</param>
        /// <returns></returns>
        [HttpPost]
        [Route("RunAll")]
        public void RunAll(int gaId, ClassificationToolInput input)
        {
            using( var m = new ClassificationTool() ) {
                m.RunAll(gaId,input);
            }
        }

        /// <summary>
        /// Runs the classification tool asynchonously
        /// </summary>
        /// <param name="gaId">Graphical area id</param>
        /// <param name="input">Input parameters (note elexon profiles are ignored)</param>
        /// <returns></returns>
        [HttpPost]
        [Route("RunAllAsync")]
        public IActionResult RunAllAsync(int gaId, ClassificationToolInput input)
        {
            try {
                _backgroundTasks.ClassificationTool.Run(gaId,input);
                return this.Ok();
            } catch( Exception e) {
                return this.StatusCode(500,e.Message);
            }
        }

        /// <summary>
        /// Cancels a running classification tool task
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Cancel")]
        public IActionResult Cancel() {
            try {
                _backgroundTasks.ClassificationTool.Cancel();
                return this.Ok();
            } catch( Exception e) {
                return this.StatusCode(500,e.Message);
            }
        }
    }
}
