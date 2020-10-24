using System;
using System.Collections.Generic;

namespace VrRestApi.Models
{
    public class User
    {
        public int Id { get; set; }
        public int? UserCategoryId { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public DateTime CreatedAt { get; set; }
        public UserCategory Category { get; set; }
        //public Guid ResultUid { get; set; }
    }

    public class UserCategory
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int? TestingSetId { get; set; }
        public int? FileModelId { get; set; }
        public TestingSet Set { get; set; }
        public FileModel File { get; set; }
        public List<User> Users { get; set; }
    }
}
