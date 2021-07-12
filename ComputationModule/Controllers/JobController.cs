using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ComputationModule.BalticLSC;
using ComputationModule.Messages;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Serilog;

namespace ComputationModule.Controllers
{
    [ApiController]
    [Route("/")]
    public class JobController : ControllerBase
    { 
        private JobRegistry _registry;
        private DataHandler _handler;
        private TokenListener _listener;

        public JobController(JobRegistry registry, DataHandler handler, TokenListener listener)
        {
            _registry = registry;
            _handler = handler;
            _listener = listener;
        }

        [HttpPost]
        [Route("token")]
        public IActionResult ProcessTokenMessage([FromBody] object value)
        {
            try
            {
                Log.Information($"Token message received: {value}");
                var inputToken = JsonConvert.DeserializeObject<InputTokenMessage>(value.ToString() ?? "");
                _registry.RegisterToken(inputToken);
                try
                {
                    string retMessage;
                    short result = _handler.CheckConnection(inputToken.PinName,
                        JsonConvert.DeserializeObject<Dictionary<string, string>>(inputToken.Values));
                    switch (result)
                    {
                        case 0:
                            JobThread jobThread = new JobThread(inputToken.PinName, _listener, _registry, _handler);
                            _registry.RegisterThread(jobThread);
                            var task = new Task(() => jobThread.Run());
                            task.Start();
                            return Ok();
                        case -1:
                            retMessage = $"No response ({inputToken.PinName}).";
                            Log.Error(retMessage);
                            return NotFound(retMessage);
                        case -2:
                            retMessage = $"Unauthorized ({inputToken.PinName}).";
                            Log.Error(retMessage);
                            return Unauthorized(retMessage);
                        case -3:
                            retMessage = $"Invalid path ({inputToken.PinName}.";
                            Log.Error(retMessage);
                            return Unauthorized(retMessage);
                    }

                    return BadRequest();
                }
                catch (Exception e)
                {
                    Log.Error($"Error of type {e.GetType()}: {e.Message}\n{e.StackTrace}");
                    return Ok(e);
                }
            }
            catch (Exception e)
            {
                Log.Error($"Corrupted token: {e.Message}");
                return BadRequest(e);
            }
        }

        [HttpGet]
        [Route("/status")]
        public IActionResult GetStatus()
        {
           return Ok(_registry.GetJobStatus());
        }
    }
}