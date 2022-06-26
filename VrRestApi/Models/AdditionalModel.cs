using System;
using System.Collections.Generic;

namespace VrRestApi.Models
{
    public class Participant
    {
        public int Id { get; set; }
        public int? ParticipantResultId { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string Company { get; set; }
        public string Code { get; set; }
        public ParticipantResult Result { get; set; }
    }

    public class ParticipantResult
    {
        public int Id { get; set; }
        public int FirstScore { get; set; }
        public int SecondScore { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
