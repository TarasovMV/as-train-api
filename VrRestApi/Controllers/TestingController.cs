using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using VrRestApi.Models;
using VrRestApi.Models.Context;
using VrRestApi.Services;

namespace VrRestApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestingController : ControllerBase
    {
        private TestingService testingService;
        private readonly ILogger<TestingController> _logger;
        private VrRestApiContext dbContext;

        public TestingController(ILogger<TestingController> logger, VrRestApiContext dbContext, TestingService testingService)
        {
            _logger = logger;
            this.dbContext = dbContext;
            this.testingService = testingService;
        }

        public async Task<int> SaveChangesAsync() => await dbContext.SaveChangesAsync();

        public ActionResult<string> Start()
        {
            return "api testing start!";
        }

        [HttpDelete("result/{id}")]
        public async Task<ActionResult> DeleteResultById(int id)
        {
            var result = dbContext.Results.FirstOrDefault(el => el.Id == id);
            if (result == null)
            {
                return BadRequest();
            }
            dbContext.Results.Remove(result);
            await SaveChangesAsync();
            return StatusCode(200);
        }

        [HttpGet("result")]
        public ActionResult<ICollection<CompetitionResult>> GetResults()
        {
            return dbContext.Results.Include(res => res.User).ToList();
        }

        [HttpGet("result/uids")]
        public ActionResult<JsonContainer<List<string>>> GetUidResults()
        {
            return new JsonContainer<List<string>>(dbContext.Results.Select(res => res.Uid.ToString()).ToList());
        }

        [HttpGet("addresult")]
        public async Task<ActionResult<CompetitionResult>> AddResult()
        {
            var user = new User();
            //user.UserCategoryId = null;
            user.FirstName = "f";
            user.MiddleName = "m";
            user.LastName = "l";
            dbContext.Users.Add(user);
            await SaveChangesAsync();
            var body = new CompetitionResult();
            body.UserId = user.Id;
            body.Uid = System.Guid.NewGuid();
            body.Testings = "123";
            dbContext.Results.Add(body);
            await SaveChangesAsync();
            return body;
        }

        [HttpGet("result/all")]
        public ActionResult<ICollection<CompetitionResult>> GetAllResults()
        {
            var results = dbContext.Results.Include(res => res.User).Include(res => res.Scores);
            results.AsNoTracking();
            foreach (var res in results)
            {
                try
                {
                    res.TestingsObj = JsonConvert.DeserializeObject<List<Testing>>(res.Testings);
                    res.TestingsObj.ForEach((testing) => testing.Questions = null);
                    res.Testings = null;
                }
                catch { }
            }
            return results.ToList();
        }

        [HttpPost("score/{id}")]
        public async Task<ActionResult<TestingScore>> SetScoreById(int id, [FromBody] ApiHandler body)
        {
            var result = dbContext.Scores.FirstOrDefault(res => res.Id == id);
            if (result == null)
            {
                return BadRequest();
            }
            result.Score = body.d;
            await SaveChangesAsync();
            return result;
        }

        [HttpGet("result/{id}")]
        public ActionResult<CompetitionResult> GetResultById(int id)
        {
            var result = dbContext.Results.Include(res => res.User).Include(res => res.Scores).FirstOrDefault(el => el.Id == id);
            try
            {
                result.TestingsObj = JsonConvert.DeserializeObject<List<Testing>>(result.Testings);
            }
            catch
            {
                //result.TestingsObj = null;
            }
            result.Testings = null;
            return result;
        }

        [HttpPost("result")]
        public async Task<ActionResult<CompetitionResult>> SaveResult([FromBody] CompetitionResult body)
        {
            if (dbContext.Results.FirstOrDefault(res => res.Uid == body.Uid) != null)
            {
                StatusCode(400);
                return BadRequest();
            }
            var user = body.User;
            if ((dbContext.UserCategories.FirstOrDefault(cat => cat.Id == user.UserCategoryId) ?? null) == null)
            {
                user.UserCategoryId = null;
            }
            user.Category = null;
            dbContext.Users.Add(user);
            await SaveChangesAsync();
            body.Testings = JsonConvert.SerializeObject(
                body.TestingsObj,
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                }
            );
            var scores = new List<TestingScore>();
            body.TestingsObj.SelectMany(testing => testing.Questions).ToList().ForEach(question =>
            {
                double? score;

                if (body.TestingsObj?.FirstOrDefault(x => x.Id == question.TestingId)?.Type == TestingType.Vr)
                {
                    score = question.Result.vrResult;
                }
                else
                {
                    if (question.Type == TestingQuestionType.FreeAnswer)
                    {
                        score = null;
                    }
                    else
                    {
                        double validCount = question.Answers.Count(answer => answer.IsValid);
                        double validResult = question.Result.chooseResult.Count(res => question.Answers.Where(ans => ans.IsValid).Select(validAns => validAns.Id).Contains(res));
                        if (validCount == 0)
                        {
                            score = 0;
                        }
                        else
                        {
                            score = validResult / validCount;
                        }
                    }
                }

                scores.Add(new TestingScore
                {
                    TestingId = question.TestingId,
                    TestingQuestionId = question.Id,
                    Score = score,
                });
            });
            body.TestingsObj = null;
            dbContext.Results.Add(body);
            await SaveChangesAsync();

            scores.ForEach(el => el.CompetitionResultId = body.Id);
            dbContext.Scores.AddRange(scores);
            await SaveChangesAsync();

            return body;
        }

        [HttpGet("local/file")]
        public ActionResult PackLocalData()
        {
            var testings = dbContext.Testings
                .Include(test => test.Questions)
                .ThenInclude(question => question.Answers)
                .AsNoTracking()
                .ToList();

            var sets = dbContext.TestingSets
                .Include(set => set.Stages)
                .AsNoTracking()
                .ToList();

            var categories = dbContext.UserCategories
                .Include(cat => cat.Set)
                .ThenInclude(set => set.Stages)
                .Where(cat => cat.Set != null)
                .AsNoTracking()
                //.Where(cat => cat.Set != null && (cat.Set.Stages.FirstOrDefault(stage => stage.TestingId == null) ?? null) == null)
                .ToList();

            string fileName = "LocalPack.txt";
            Response.Headers.Add("Access-Control-Expose-Headers", "Content-Disposition");
            var byteArray = testingService.LocalTestingCreate(categories, sets, testings);
            string mimeType = "text/plain";
            return new FileContentResult(byteArray, mimeType)
            {
                FileDownloadName = fileName
            };
        }

        [HttpGet("set/all")]
        public ActionResult<ICollection<TestingSet>> GetAllSets()
        {
            return dbContext.TestingSets
                .Include(s => s.Stages)
                .ThenInclude(s => s.Test)
                .ToList();
        }

        [HttpGet("set/{id}")]
        public ActionResult<TestingSet> GetSetById(int id)
        {
            var set = dbContext.TestingSets
                .Include(set => set.Stages)
                .ThenInclude(stage => stage.Test)
                .ThenInclude(test => test.Questions)
                .ThenInclude(question => question.Answers)
                .FirstOrDefault(el => el.Id == id);


            if (set == null)
            {
                return BadRequest();
            }
            return set;
        }

        [HttpPost("set")]
        public async Task<ActionResult<TestingSet>> CreateSet([FromBody] TestingSet set)
        {
            dbContext.TestingSets.Add(set);
            set.Stages.ForEach(s => s.TestingSetId = set.Id);
            dbContext.TestingStages.AddRange(set.Stages);
            await SaveChangesAsync();
            var answer = dbContext.TestingSets
                .Include(s => s.Stages)
                .ThenInclude(s => s.Test)
                .FirstOrDefault(s => s.Id == set.Id);
            return answer;
        }

        [HttpPut("set/{id}")]
        public async Task<ActionResult<TestingSet>> UpdateSet(int id, [FromBody] TestingSet set)
        {
            var dbSet = dbContext.TestingSets.FirstOrDefault(el => el.Id == id);
            if (dbSet == null)
            {
                return BadRequest();
            }
            dbSet.Title = set.Title;
            dbContext.TestingSets.Update(dbSet);
            set.Stages.ForEach(s => {
                var tempSet = dbContext.TestingStages.FirstOrDefault(el => el.Id == s.Id);
                tempSet.TestingId = s.TestingId;
                dbContext.TestingStages.Update(tempSet);
            });
            dbContext.TestingStages.AddRange(set.Stages.Where(s => s.Id == 0));
            await SaveChangesAsync();
            var answer = dbContext.TestingSets
                .Include(s => s.Stages)
                .ThenInclude(s => s.Test)
                .FirstOrDefault(s => s.Id == set.Id);
            return answer;
        }

        [HttpDelete("set/{id}")]
        public async Task<IActionResult> DeleteSet(int id)
        {
            var set = dbContext.TestingSets.FirstOrDefault(el => el.Id == id);
            if (set == null)
            {
                return BadRequest();
            }
            dbContext.UserCategories.Where(x => x.TestingSetId == id).ToList().ForEach(x => x.TestingSetId = null);
            dbContext.TestingSets.Remove(set);
            await SaveChangesAsync();
            return StatusCode(200);
        }

        [HttpPost("testing/copy/{id}")]
        public async Task<ActionResult<Testing>> CopyTestingById(int id)
        {
            var testing = dbContext.Testings
                .AsNoTracking()
                .Include(t => t.Questions)
                .ThenInclude(q => q.Answers)
                .FirstOrDefault(x => x.Id == id);
            if (testing == null)
            {
                return BadRequest();
            }
            var testingClone = new Testing()
            {
                Title = testing.Title + " (копия)",
                Type = testing.Type,
                IsShuffleQuestions = testing.IsShuffleQuestions,
                QuestionsCount = testing.QuestionsCount,
                Time = testing.Time,
                Version = testing.Version,
                CreatedAt = testing.CreatedAt,
                ModifiedAt = testing.ModifiedAt,
                Questions = testing.Questions.Select(x =>
                    new TestingQuestion() {
                        Title = x.Title,
                        Pano = x.Pano,
                        VrExperience = x.VrExperience,
                        Type = x.Type,
                        Result = x.Result,
                        Answers = x.Answers.Select(a =>
                            new TestingAnswer() {
                                Title = a.Title,
                                IsValid = a.IsValid,
                            })
                        .ToList(),
                    }
                ).ToList(),
            };
            dbContext.Testings.Add(testingClone);
            await SaveChangesAsync();
            return testingClone;
        }

        [HttpGet("testing/{id}")]
        public ActionResult<Testing> GetTestingById(int id)
        {
            var testing = dbContext.Testings
                .Include(t => t.Questions)
                .ThenInclude(q => q.Answers)
                .FirstOrDefault(el => el.Id == id);
            if (testing == null)
            {
                return BadRequest();
            }
            return testing;
        }

        // test ok
        [HttpGet("testing/all")]
        public ActionResult<ICollection<Testing>> GetAllTestings ()
        {
            return dbContext.Testings.ToList();
        }

        [HttpGet("testing/pack/{id}")]
        public ActionResult<JsonContainer<List<Testing>>> GetTestingsBySet(int id)
        {
            var set = dbContext.TestingSets
                .Include(s => s.Stages)
                .ThenInclude(st => st.Test)
                .ThenInclude(t => t.Questions)
                .ThenInclude(q => q.Answers)
                .FirstOrDefault(el => el.Id == id);
            //set.Stages.ForEach(x => x.Test.Questions.ForEach(x => x.Answers.Shuffle()));
            if (set == null)
            {
                return BadRequest();
            }
            var result = set.Stages.Select(st => st.Test).ToList();
            result.ForEach(x => testingService.PrepeareData(x));
            return new JsonContainer<List<Testing>>(result);
        }

        // test ok
        [HttpPost("testing")]
        public async Task<ActionResult<Testing>> CreateTesting([FromBody] Testing testing)
        {
            testing.CreatedAt = DateTime.Now;
            testing.ModifiedAt = DateTime.Now;
            dbContext.Testings.Add(testing);
            await SaveChangesAsync();
            return testing;
        }

        // test ok
        [HttpDelete("testing/{id}")]
        public async Task<ActionResult<Testing>> DeleteTesting(int id)
        {
            var testing = dbContext.Testings.FirstOrDefault(el => el.Id == id);
            if (testing == null)
            {
                return BadRequest();
            }
            await dbContext.TestingStages.Where(el => el.TestingId == id).ForEachAsync(el => el.TestingId = null);
            dbContext.Testings.Remove(testing);
            await SaveChangesAsync();
            return StatusCode(200);
        }

        // test ok
        [HttpPut("testing/title/{id}")]
        public async Task<ActionResult<Testing>> UpdateTestingTitle(int id, [FromBody] ApiHandler apiHandler)
        {
            var testing = dbContext.Testings.FirstOrDefault(el => el.Id == id);
            if (testing == null)
            {
                return BadRequest();
            }
            testing.Title = apiHandler.title;
            dbContext.Testings.Update(testing);
            await SaveChangesAsync();
            return testing;
        }

        [HttpPut("testing/shuffle/{id}")]
        public async Task<ActionResult<Testing>> UpdateTestingShuffle(int id, [FromBody] ApiHandler apiHandler)
        {
            var testing = dbContext.Testings.FirstOrDefault(el => el.Id == id);
            if (testing == null)
            {
                return BadRequest();
            }
            testing.IsShuffleQuestions = apiHandler.isActive;
            dbContext.Testings.Update(testing);
            await SaveChangesAsync();
            return testing;
        }

        [HttpPut("testing/time/{id}")]
        public async Task<ActionResult<Testing>> UpdateTestingTime(int id, [FromBody] ApiHandler apiHandler)
        {
            var testing = dbContext.Testings.FirstOrDefault(el => el.Id == id);
            if (testing == null)
            {
                return BadRequest();
            }
            testing.Time = apiHandler.i ?? 0;
            dbContext.Testings.Update(testing);
            await SaveChangesAsync();
            return testing;
        }

        [HttpPut("testing/count/{id}")]
        public async Task<ActionResult<Testing>> UpdateTestingCount(int id, [FromBody] ApiHandler apiHandler)
        {
            var testing = dbContext.Testings.FirstOrDefault(el => el.Id == id);
            if (testing == null)
            {
                return BadRequest();
            }
            testing.QuestionsCount = apiHandler.i ?? 0;
            dbContext.Testings.Update(testing);
            await SaveChangesAsync();
            return testing;
        }

        [HttpPut("testing/type/{id}")]
        public async Task<ActionResult<Testing>> UpdateTestingType(int id, [FromBody] ApiHandler apiHandler)
        {
            var testing = dbContext.Testings.FirstOrDefault(el => el.Id == id);
            if (testing == null)
            {
                return BadRequest();
            }
            testing.Type = (TestingType)apiHandler.i;
            dbContext.Testings.Update(testing);
            await SaveChangesAsync();
            return testing;
        }

        // test ok
        [HttpGet("question/{id}")]
        public ActionResult<TestingQuestion> GetQuestionById(int id)
        {
            var question = dbContext.TestingQuestions.Include(q => q.Answers).FirstOrDefault(el => el.Id == id);
            if (question == null)
            {
                return BadRequest();
            }
            return question;
        }

        // test ok
        [HttpPost("question/{id}")]
        public async Task<ActionResult<TestingQuestion>> CreateQuestion(int id, [FromBody] TestingQuestion question)
        {
            var testing = dbContext.Testings.FirstOrDefault(el => el.Id == id);
            if (testing == null)
            {
                return BadRequest();
            }
            question.TestingId = id;
            dbContext.TestingQuestions.Add(question);
            await SaveChangesAsync();
            return question;
        }

        // test ok
        [HttpPut("question")]
        public async Task<ActionResult<TestingQuestion>> UpdateQuestion([FromBody] ApiTestingQuestion clientQuestion)
        {
            var question = dbContext.TestingQuestions.FirstOrDefault(el => el.Id == clientQuestion.Id);
            if (question == null)
            {
                return BadRequest();
            }
            question.Title = clientQuestion.Title;
            dbContext.TestingQuestions.Update(question);
            await SaveChangesAsync();
            return question;
        }

        // test ok
        [HttpPut("question/{id}/title")]
        public async Task<ActionResult<TestingQuestion>> UpdateQuestionTitle(int id, [FromBody] ApiHandler apiHandler)
        {
            var question = dbContext.TestingQuestions.FirstOrDefault(el => el.Id == id);
            if (question == null)
            {
                return BadRequest();
            }
            question.Title = apiHandler.title;
            dbContext.TestingQuestions.Update(question);
            await SaveChangesAsync();
            return question;
        }

        [HttpPut("question/{id}/vr/{vr}")]
        public async Task<ActionResult<TestingQuestion>> UpdateQuestionPano(int id, VrSceneType vr)
        {
            var question = dbContext.TestingQuestions.Include(q => q.Answers).FirstOrDefault(el => el.Id == id);
            if (question == null)
            {
                return BadRequest();
            }
            question.VrExperience = vr;
            dbContext.TestingQuestions.Update(question);

            await SaveChangesAsync();
            return question;
        }

        [HttpPut("question/{id}/pano/{pano}")]
        public async Task<ActionResult<TestingQuestion>> UpdateQuestionPano(int id, TestingPanoType pano)
        {
            var question = dbContext.TestingQuestions.Include(q => q.Answers).FirstOrDefault(el => el.Id == id);
            if (question == null)
            {
                return BadRequest();
            }
            question.Pano = pano;
            dbContext.TestingQuestions.Update(question);

            await SaveChangesAsync();
            return question;
        }

        [HttpPut("question/{id}/type/{type}")]
        public async Task<ActionResult<TestingQuestion>> UpdateQuestionMultiply(int id, TestingQuestionType type)
        {
            var question = dbContext.TestingQuestions.Include(q => q.Answers).FirstOrDefault(el => el.Id == id);
            if (question == null)
            {
                return BadRequest();
            }
            question.Type = type;
            dbContext.TestingQuestions.Update(question);
            if (type == TestingQuestionType.SingleAnswer)
            {
                var validQuestions = question.Answers.FindAll(q => q.IsValid);
                if (validQuestions?.Count > 1)
                {
                    validQuestions.ForEach(vQ => vQ.IsValid = false);
                    validQuestions[0].IsValid = true;
                    dbContext.TestingAnswers.UpdateRange(validQuestions);
                }
            }
            await SaveChangesAsync();
            return question;
        }

        // test ok
        [HttpDelete("question/{id}")]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var question = dbContext.TestingQuestions.FirstOrDefault(el => el.Id == id);
            if (question == null)
            {
                return BadRequest();
            }
            dbContext.TestingQuestions.Remove(question);
            await SaveChangesAsync();
            return StatusCode(200);
        }

        // test ok
        [HttpPost("answer/{id}")]
        public async Task<ActionResult<TestingAnswer>> CreateAnswer(int id, [FromBody] TestingAnswer answer)
        {
            if (dbContext.TestingQuestions.FirstOrDefault(el => el.Id == id) == null)
            {
                return BadRequest();
            }
            answer.TestingQuestionId = id;
            dbContext.TestingAnswers.Add(answer);
            await SaveChangesAsync();
            return answer;
        }

        // test ok
        [HttpPut("answer")]
        public async Task<ActionResult<TestingAnswer>> UpdateAnswer([FromBody] ApiTestingAnswer clientAnswer)
        {
            var answer = dbContext.TestingAnswers.FirstOrDefault();
            if (answer == null)
            {
                return BadRequest();
            }
            answer.Title = clientAnswer.Title;
            answer.IsValid = clientAnswer.IsValid;
            //dbContext.TestingAnswers.Update(answer);
            await SaveChangesAsync();
            return answer;
        }

        // test ok
        [HttpPut("answer/{id}/title")]
        public async Task<ActionResult<TestingAnswer>> UpdateAnswerTitle(int id, [FromBody] ApiHandler apiHandler)
        {
            var answer = dbContext.TestingAnswers.FirstOrDefault(el => el.Id == id);
            if (answer == null)
            {
                return BadRequest();
            }
            answer.Title = apiHandler.title;
            dbContext.Entry(answer).Property("IsValid").IsModified = false;
            dbContext.Entry(answer).Property("Title").IsModified = true;
            //dbContext.TestingAnswers.Update(answer);
            await SaveChangesAsync();
            return answer;
        }

        // test ok
        [HttpPut("answer/{id}/valid")]
        public async Task<ActionResult<TestingAnswer>> UpdateAnswerValid(int id, [FromBody] ApiHandler apiHandler)
        {
            var answer = dbContext.TestingAnswers.FirstOrDefault(el => el.Id == id);
            if (answer == null)
            {
                return BadRequest();
            }
            var question = dbContext
                .TestingQuestions
                .Include(q => q.Answers)
                .FirstOrDefault(el => el.Id == answer.TestingQuestionId);
            if (question == null)
            {
                return BadRequest();
            }
            if (question.Type == TestingQuestionType.SingleAnswer)
            {
                question.Answers.ForEach(q => { if (q.Id != id) q.IsValid = false; });
            }
            answer.IsValid = apiHandler.isActive;
            dbContext.Entry(answer).Property("IsValid").IsModified = true;
            dbContext.Entry(answer).Property("Title").IsModified = false;
            //dbContext.TestingAnswers.Update(answer);
            await SaveChangesAsync();
            return answer;
        }

        // test ok
        [HttpDelete("answer/{id}")]
        public async Task<ActionResult<TestingAnswer>> DeleteAnswer(int id)
        {
            var answer = dbContext.TestingAnswers.FirstOrDefault(el => el.Id == id);
            if (answer == null)
            {
                return BadRequest();
            }
            dbContext.TestingAnswers.Remove(answer);
            await SaveChangesAsync();
            return answer;
        }
    }
}
