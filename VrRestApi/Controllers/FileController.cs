using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VrRestApi.Models;
using VrRestApi.Models.Context;

namespace VrRestApi.Controllers
{

    [Route("api/[controller]")]
    public class FileController : Controller
    {
        IWebHostEnvironment appEnvironment;
        private VrRestApiContext dbContext;

        public FileController(VrRestApiContext dbContext, IWebHostEnvironment appEnvironment)
        {
            this.dbContext = dbContext;
            this.appEnvironment = appEnvironment;
        }

        [HttpPost("category/{id}")]
        public async Task<ActionResult<FileModel>> AddFile(int id, IFormFile uploadedFile)
        {
            if (uploadedFile == null)
            {
                return BadRequest();
            }
            
            string path = "/images/" + uploadedFile.FileName;
            using (var fileStream = new FileStream(appEnvironment.WebRootPath + path, FileMode.Create))
            {
                await uploadedFile.CopyToAsync(fileStream);
            }
            FileModel file = new FileModel { Name = uploadedFile.FileName, Path = path };
            dbContext.Files.Add(file);
            await dbContext.SaveChangesAsync();
            var category = dbContext.UserCategories.FirstOrDefault(x => x.Id == id);
            if (category ==  null)
            {
                BadRequest();
            }
            category.FileModelId = file.Id;
            await dbContext.SaveChangesAsync();
            return Ok(file);
        }
    }
}
