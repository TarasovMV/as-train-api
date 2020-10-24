using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VrRestApi.Models;
using VrRestApi.Models.Context;
using Microsoft.EntityFrameworkCore;

namespace VrRestApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : Controller
    {
        private readonly ILogger<TestingController> _logger;
        private VrRestApiContext dbContext;

        public UserController(ILogger<TestingController> logger, VrRestApiContext dbContext)
        {
            _logger = logger;
            this.dbContext = dbContext;
        }

        public async Task<int> SaveChangesAsync() => await dbContext.SaveChangesAsync();

        [HttpGet]
        public ActionResult<string> Start()
        {
            return "api user start!";
        }

        [HttpPost("user")]
        public async Task<ActionResult<User>> AddUser(User user)
        {
            if (dbContext.UserCategories.FirstOrDefault(c => c.Id == user.UserCategoryId) == null)
            {
                return BadRequest();
            }
            user.FirstName = user.FirstName ?? "";
            user.MiddleName = user.MiddleName ?? "";
            user.LastName = user.LastName ?? "";
            user.CreatedAt = DateTime.Now;
            dbContext.Users.Add(user);
            await SaveChangesAsync();
            return user;
        }

        [HttpGet("user/{id}")]
        public ActionResult<User> GetUser(int id)
        {
            var user = dbContext.Users.FirstOrDefault(u => u.Id == id);
            return user;
        }

        [HttpGet("user/all")]
        public ActionResult<ICollection<User>> GetAllUsers()
        {
            var users = dbContext.Users.Include(u => u.Category).ToList();
            return users;
        }

        [HttpPost("category")]
        public async Task<ActionResult<UserCategory>> AddCategory([FromBody] UserCategory category)
        {
            if (string.IsNullOrWhiteSpace(category?.Title))
            {
                return BadRequest();
            }
            dbContext.UserCategories.Add(category);
            await SaveChangesAsync();
            var _category = dbContext.UserCategories
                .Include(c => c.Set)
                .FirstOrDefault(el => el.Id == category.Id);
            return _category;
        }

        [HttpPut("category/{id}")]
        public async Task<ActionResult<UserCategory>> UpdateCategory(int id, [FromBody] UserCategory category)
        {
            var _category= dbContext.UserCategories
                .Include(c => c.Set)
                .FirstOrDefault(el => el.Id == id);
            if (_category == null)
            {
                return BadRequest();
            }
            if (string.IsNullOrWhiteSpace(category?.Title))
            {
                return BadRequest();
            }
            _category.Title = category.Title;
            _category.TestingSetId = category.TestingSetId;
            dbContext.UserCategories.Update(_category);
            await SaveChangesAsync();
            _category = dbContext.UserCategories
                .Include(c => c.Set)
                .FirstOrDefault(el => el.Id == _category.Id);
            return _category;
        }

        [HttpGet("category/all")]
        public ActionResult<ICollection<UserCategory>> GetAllCategories()
        {
            var categories = dbContext.UserCategories.Include(c => c.Set).Include(c => c.File).ToList();
            return categories;
        }

        [HttpGet("category/all/pack")]
        public ActionResult<JsonContainer<List<UserCategory>>> GetAllCategoriesPack()
        {
            var categories = dbContext.UserCategories.Include(c => c.Set).Include(c => c.File).ToList();
            var result = new JsonContainer<List<UserCategory>>(categories);
            return result;
        }

        [HttpDelete("category/{id}")]
        public async Task<ActionResult<ICollection<UserCategory>>> DeleteCategory(int id)
        {
            var category = dbContext.UserCategories.FirstOrDefault(el => el.Id == id);
            if (category == null)
            {
                return BadRequest();
            }
            category.TestingSetId = null;
            category.FileModelId = null;
            dbContext.Users.Where(x => x.UserCategoryId == id).ToList().ForEach(x => x.UserCategoryId = null);
            dbContext.UserCategories.Remove(category);
            await SaveChangesAsync();
            return StatusCode(200);
        }
    }
}
