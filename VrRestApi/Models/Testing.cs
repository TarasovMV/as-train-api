using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;

namespace VrRestApi.Models
{
    public enum TestingType : int
    {
        Quiz = 1,
        Pano = 2,
        Vr = 3,
    }

    public enum TestingQuestionType : int
    {
        SingleAnswer = 1,
        MultiplyAnswer = 2,
        FreeAnswer = 3,
    }

    // TODO Rename
    public enum TestingPanoType : int
    {
        Pano1 = 1,
        Pano2 = 2,
        Pano3 = 3,
        Pano4 = 4,
        Pano5 = 5,
        Pano6 = 6,
        Pano7 = 7,
        Pano8 = 8,
        Pano9 = 9,
    }

    public enum VrSceneType : int
    {
        Tank = 1,
        RectificationScheme = 2,
        CompressorScheme = 3,
        Pump = 4,
        GasSeparator = 5,
    }

    //public class TestingSetup
    //{
    //    public int Id { get; set; }
    //    public int UserCategoryId { get; set; }
    //    public int TestingSetId { get; set; }
    //    public UserCategory Category { get; set; }
    //    public TestingSet Set { get; set; }
    //}

    //// TODO think temp mb
    //public class TestingSetSetupConnect
    //{
    //    public int Id { get; set; }
    //    public int TestingSetupId { get; set; }
    //    public int TestingSetId { get; set; }
    //    public TestingSet Set { get; set; }
    //}

    public class TestingSet
    {
        [Key]
        public int Id { get; set; }
        public string Title { get; set; }
        public List<TestingStage> Stages { get; set; }
    }

    public class TestingStage
    {
        public int Id { get; set; }
        public int TestingSetId { get; set; }
        public int? TestingId { get; set; }
        public string Title { get; set; }
        public int SortOrder { get; set; }
        public Testing Test { get; set; }
    }

    public class Testing
    {
        public Testing()
        {
            Questions = new List<TestingQuestion>();
        }
        public int Id { get; set; }
        public string Title { get; set; }
        public TestingType Type { get; set; }
        public bool IsShuffleQuestions { get; set; }
        public int QuestionsCount { get; set; }
        public int Time { get; set; }
        public int Version { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        public List<TestingQuestion> Questions { get; set; }
        [NotMapped]
        public int? ResultTime { get; set; }
    }

    public class TestingQuestion
    {
        public TestingQuestion()
        {
            Answers = new List<TestingAnswer>();
        }
        public int Id { get; set; }
        public int TestingId { get; set; }
        public string Title { get; set; }
        public TestingPanoType? Pano { get; set; }
        public VrSceneType? VrExperience { get; set; }
        public TestingQuestionType Type { get; set; }
        public List<TestingAnswer> Answers { get; set; }
        public TestingQuestionResult Result { get; set; }
    }

    [NotMapped]
    public class TestingQuestionResult
    {
        public List<int> chooseResult { get; set; }
        public string freeResult { get; set; }
        public int? vrResult { get; set; }

        public TestingQuestionResult()
        {
            chooseResult = null;
            freeResult = null;
        }
    }

    public class TestingScore
    {
        public int Id { get; set; }
        public int CompetitionResultId { get; set; }
        public int? TestingId { get; set; }
        public int TestingQuestionId { get; set; }
        public double? Score { get; set; }
    }

    public class TestingAnswer
    {
        public int Id { get; set; }
        public int TestingQuestionId { get; set; }
        public string Title { get; set; }
        public bool IsValid { get; set; }
    }

    public class TestingResult
    {
        public TestingResult()
        {
            QuestionResults = new List<QuestionResult>();
            VrResults = new List<VrResult>();
        }
        public int Id { get; set; }
        public int UserId { get; set; }
        public int TestingId { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<QuestionResult> QuestionResults { get; set; }
        public List<VrResult> VrResults { get; set; }
    }

    public class QuestionResult
    {
        public int Id { get; set; }
        public int TestingResultId { get; set; }
        public int TestingQuestionId { get; set; }
        public int TestingAnswerId { get; set; }
    }

    public class VrResult
    {
        public int Id { get; set; }
        public int TestingResultId { get; set; }
        public string ResultCode { get; set; }
    }

    public class CompetitionResult
    {
        public int Id { get; set; }   
        public Guid Uid { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public string Testings { get; set; }
        public List<Testing> TestingsObj { get; set; }
        public List<TestingScore> Scores { get; set; }
    }

    public class LocalPack
    {
        public List<UserCategory> UserCategories { get; set; }
        public List<TestingSet> Sets { get; set; }
        public List<Testing> Testings { get; set; }

        public LocalPack(List<UserCategory> UserCategories, List<TestingSet> Sets, List<Testing> Testings)
        {
            this.UserCategories = UserCategories;
            this.Sets = Sets;
            this.Testings = Testings;
        }
    }
}
