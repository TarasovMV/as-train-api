using System;
using System.Collections.Generic;
using System.Text.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using VrRestApi.Models;

namespace VrRestApi.Services
{
    public class TestingService
    {
        private Random rng = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);


        public void PrepeareData(Testing testing)
        {
            if (!testing.IsShuffleQuestions)
            {
                return;
            }
            var list = testing.Questions;
            list.Shuffle();
            list.Resize(testing.QuestionsCount);
        }

        public byte[] LocalTestingCreate(List<UserCategory> categories, List<TestingSet> sets, List<Testing> testings)
        {
            categories.ForEach((cat) => cat.Set = null);
            sets.ForEach((set) => set.Stages.ForEach(stage => stage.Test = null));
            testings.ForEach(x => PrepeareData(x));
            var pack = new LocalPack(categories, sets, testings);
            var json = JsonConvert.SerializeObject(
                pack,
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                }
            );
            return System.Text.Encoding.Default.GetBytes(json);
        }
    }
}
