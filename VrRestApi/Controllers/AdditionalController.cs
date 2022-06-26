using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VrRestApi.Models;
using VrRestApi.Models.Context;
using VrRestApi.Services;

namespace VrRestApi.Controllers
{
    [Route("api/[controller]")]
    public class AdditionalController : Controller
    {
        private AdditionalContext dbContext;
        private AdditionalService additionalService;

        public AdditionalController(AdditionalContext dbContext, AdditionalService additionalService)
        {
            this.dbContext = dbContext;
            this.additionalService = additionalService;
        }

        [HttpGet("participant/{id}/result/{code}")]
        public async Task<ActionResult<Participant>> AddResultByCode(int id, string code)
        {
            var participant = dbContext.Participants.FirstOrDefault(x => x.Id == id);
            if (participant == null)
            {
                return BadRequest("Participant not found!");
            }
            var decode = additionalService.DecodeResult(code);
            if (decode == null)
            {
                return BadRequest("Code is invalid!");
            }
            ParticipantResult participantResult = new ParticipantResult {
                Id = 0,
                FirstScore = decode[0],
                SecondScore = decode[1],
                Timestamp = DateTime.Now
            };
            dbContext.ParticipantResults.Add(participantResult);
            await dbContext.SaveChangesAsync();
            participant.ParticipantResultId = participantResult.Id;
            participant.Result = participantResult;
            await dbContext.SaveChangesAsync();
            return participant;
        }

        [HttpGet("test/decode/{code}")]
        public int[] TestDecode(string code)
        {
            return additionalService.DecodeResult(code);
        }

        [HttpGet("participant/{code}")]
        public ActionResult<Participant> GetParticipant(string code)
        {
            var participant = dbContext.Participants.FirstOrDefault(x => x.Code == code);
            if (participant == null)
            {
                return BadRequest();
            }
            return participant;
        }

        [HttpGet("participant")]
        public ActionResult<ICollection<Participant>> GetParticipants()
        {
            var participants = dbContext.Participants.Include(x => x.Result).ToList();
            return participants;
        }

        // TODO: add validation
        [HttpPost("participant")]
        public async Task<ActionResult<Participant>> CreateParticipant([FromBody] Participant participant)
        {
            string code = "";
            while (true)
            {
                code = additionalService.GenerateCode(3);
                var codePerson = dbContext.Participants.FirstOrDefault(x => x.Code == code);
                if (codePerson == null)
                {
                    break;
                }
            }
            participant.Code = code;
            dbContext.Participants.Add(participant);
            await dbContext.SaveChangesAsync();
            return participant;
        }

        [HttpDelete("participant/{id}")]
        public async Task<ActionResult<Participant>> DeleteParticipant(int id)
        {
            var participant = dbContext.Participants.FirstOrDefault(x => x.Id == id);
            if (participant == null)
            {
                return BadRequest();
            }
            dbContext.Participants.Remove(participant);
            await dbContext.SaveChangesAsync();
            return StatusCode(200);
        }

        // TODO: add validation
        [HttpPost("participant/{id}/result")]
        public async Task<ActionResult<Participant>> AddResult(int id, [FromBody] ParticipantResult result)
        {
            var participant = dbContext.Participants.FirstOrDefault(x => x.Id == id);
            if (participant == null)
            {
                return BadRequest();
            }
            result.Timestamp = DateTime.Now;
            dbContext.ParticipantResults.Add(result);
            await dbContext.SaveChangesAsync();
            participant.ParticipantResultId = result.Id;
            participant.Result = result;
            await dbContext.SaveChangesAsync();
            return participant;
        }
    }
}
