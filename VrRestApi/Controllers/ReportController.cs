using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VrRestApi.Services;

namespace VrRestApi.Controllers
{
    [Route("api/[controller]")]
    public class ReportController : Controller
    {
        private ReportService reportService;

        public ReportController(ReportService reportService)
        {
            this.reportService = reportService;
        }

        [HttpGet]
        public ActionResult<string> Start()
        {
            return "api start!";
        }

        [HttpGet("all")]
        public ActionResult GetCommonReport()
        {
            var memoryStream = reportService.ReportCommonCreate();
            string fileName = $"Report.xls";
            Response.Headers.Add("Access-Control-Expose-Headers", "Content-Disposition");
            return File(memoryStream, "application/vnd.ms-excel", fileName);
        }

        [HttpGet("{id}")]
        public ActionResult GetUserReport(int id)
        {
            var memoryStream = reportService.ReportUserCreate(id);
            string fileName = $"UserReport.xls";
            Response.Headers.Add("Access-Control-Expose-Headers", "Content-Disposition");
            return File(memoryStream, "application/vnd.ms-excel", fileName);
        }
    }
}
