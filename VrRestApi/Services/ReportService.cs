using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ExcelLibrary.SpreadSheet;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using VrRestApi.Models;
using VrRestApi.Models.Context;

namespace VrRestApi.Services
{
    /// <summary>
    /// https://csharp.hotexamples.com/ru/examples/ExcelLibrary.SpreadSheet/Workbook/-/php-workbook-class-examples.html
    /// примеры для работы с Excel Library
    /// </summary>
    public class ReportService
    {
        private VrRestApiContext dbContext;
        private AdditionalContext dbAddContext;

        public ReportService(VrRestApiContext dbContext, AdditionalContext dbAddContext)
        {
            this.dbContext = dbContext;
            this.dbAddContext = dbAddContext;
        }

        public MemoryStream ReportAdditionalCreate()
        {
            Workbook workbook = new Workbook();
            Worksheet worksheet = new Worksheet("Report");
            reportAdditionalSheetFill(worksheet);
            workbook.Worksheets.Add(worksheet);
            MemoryStream memoryStream = new MemoryStream();
            workbook.SaveToStream(memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }

        private void reportAdditionalSheetFill(Worksheet worksheet)
        {
            var results = dbAddContext.Participants.Where(x => x.Result != null).Include(res => res.Result);

            reportAdditionalFillTitileRow(worksheet);

            int stringCounter = 1;
            foreach (var res in results)
            {
                reportAdditionalFillDataRow(stringCounter, res, worksheet);
                stringCounter++;
            }
        }

        private void reportAdditionalFillDataRow(int stringCounter, Participant p, Worksheet worksheet)
        {
            worksheet.Cells[stringCounter, 0] = new Cell(p.Id);
            worksheet.Cells[stringCounter, 1] = new Cell($"{p.LastName} {p.FirstName} {p.MiddleName}");
            worksheet.Cells[stringCounter, 2] = new Cell(p.Company ?? "-");
            worksheet.Cells[stringCounter, 3] = new Cell(p.Result.Timestamp.ToString("dd/MM/yyyy HH:mm"));
            worksheet.Cells[stringCounter, 4] = new Cell(p.Result.FirstScore * 10);
            worksheet.Cells[stringCounter, 5] = new Cell(p.Result.SecondScore * 10);
            worksheet.Cells[stringCounter, 6] = new Cell((p.Result.FirstScore + p.Result.SecondScore) * 10);
        }

        private void reportAdditionalFillTitileRow(Worksheet worksheet)
        {
            worksheet.Cells.ColumnWidth[1] += 15000;

            worksheet.Cells[0, 0] = new Cell("ID");
            worksheet.Cells[0, 1] = new Cell("ФИО");
            worksheet.Cells[0, 2] = new Cell("Компания");
            worksheet.Cells[0, 3] = new Cell("Время окончания");
            worksheet.Cells[0, 4] = new Cell("Насос, %");
            worksheet.Cells[0, 5] = new Cell("Резервуар, %");
            worksheet.Cells[0, 6] = new Cell("Итого, %");
        }

        public MemoryStream ReportUserCreate(int id)
        {
            Workbook workbook = new Workbook();
            Worksheet worksheet = new Worksheet("UserReport");
            reportUserSheetFill(worksheet, id);
            workbook.Worksheets.Add(worksheet);
            MemoryStream memoryStream = new MemoryStream();
            workbook.SaveToStream(memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }

        private void reportUserSheetFill(Worksheet worksheet, int id)
        {
            var result = dbContext.Results
                .Include(res => res.User)
                .ThenInclude(u => u.Category)
                .Include(res => res.Scores)
                .FirstOrDefault(res => res.Id == id);

            if (result == null)
            {
                return;
            }

            try
            {
                result.TestingsObj = JsonConvert.DeserializeObject<List<Testing>>(result.Testings);
                result.Testings = null;
            }
            catch { }

            reportUserFillTitileRow(worksheet);

            var stagesScore = new List<double?>();
            result.TestingsObj.ForEach(x =>
            {
                if (result.Scores.Where(r => r.TestingId == x.Id).Select(r => r.Score).Any(s => s == null))
                {
                    stagesScore.Add(null);
                }
                else
                {
                    var sum = result.Scores.Where(r => r.TestingId == x.Id).Select(r => r.Score).Sum();
                    stagesScore.Add(sum);
                }
            });
            reportCommonFillDataRow(1, result, stagesScore, worksheet);
            reportUserFillData(3, result.TestingsObj, result.Scores, worksheet);
        }

        private void reportUserFillData(int startRowIdx, List<Testing> testings, List<TestingScore> scores, Worksheet worksheet)
        {
            int row = startRowIdx;
            for (int i = 0; i < testings.Count; i++)
            {
                worksheet.Cells[row, 0] = new Cell($"{i + 1} ЭТАП");
                row++;
                worksheet.Cells[row, 0] = new Cell(testings[i].Title);
                row += 2;

                for (int j = 0; j < testings[i].Questions.Count; j++)
                {
                    worksheet.Cells[row, 0] = new Cell($"{j + 1}. {testings[i].Questions[j].Title}");
                    row++;
                    switch(testings[i].Questions[j].Type)
                    {
                        case TestingQuestionType.FreeAnswer:
                            
                            worksheet.Cells[row, 1] = new Cell(testings[i].Questions[j].Result.freeResult ?? "Нет ответа");
                            row++;
                            break;
                        default:
                            testings[i].Questions[j].Answers.ForEach(ans =>
                            {
                                if (ans.IsValid && testings[i].Questions[j].Result.chooseResult.Any(r => r == ans.Id))
                                {
                                    worksheet.Cells[row, 0] = new Cell("+");
                                }
                                else if (!ans.IsValid && testings[i].Questions[j].Result.chooseResult.Any(r => r == ans.Id))
                                {
                                    worksheet.Cells[row, 0] = new Cell("-");
                                }
                                else if(ans.IsValid)
                                {
                                    worksheet.Cells[row, 0] = new Cell("*");
                                }
                                worksheet.Cells[row, 1] = new Cell($"{ans.Title}");
                                row++;
                            });
                            break;
                    }
                }
                row++;
            }
        }

        private void reportUserFillTitileRow(Worksheet worksheet, int stageCounter = 4)
        {
            worksheet.Cells[0, 0] = new Cell("№");
            worksheet.Cells[0, 1] = new Cell("ФИО");
            worksheet.Cells[0, 2] = new Cell("Категория");
            for (int i = 0; i < stageCounter; i++)
            {
                worksheet.Cells[0, i + 3] = new Cell($"{i + 1} Этап");
            }
            worksheet.Cells[0, 7] = new Cell("Итого");
            worksheet.Cells[0, 8] = new Cell("Время, c");
        }

        public MemoryStream ReportCommonCreate()
        {
            Workbook workbook = new Workbook();
            Worksheet worksheet = new Worksheet("CommonReport");
            reportCommonSheetFill(worksheet);
            workbook.Worksheets.Add(worksheet);
            MemoryStream memoryStream = new MemoryStream();
            workbook.SaveToStream(memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }

        private void reportCommonSheetFill(Worksheet worksheet)
        {
            var results = dbContext.Results
                .Include(res => res.User)
                .ThenInclude(u => u.Category)
                .Include(res => res.Scores);

            reportCommonFillTitileRow(worksheet);

            int stringCounter = 1;
            foreach (var res in results) 
            {
                try
                {
                    res.TestingsObj = JsonConvert.DeserializeObject<List<Testing>>(res.Testings);
                    res.TestingsObj.ForEach((testing) => testing.Questions = null);
                    res.Testings = null;
                }
                catch { }

                var stagesScore = new List<double?>();
                res.TestingsObj.ForEach(x =>
                {
                    if (res.Scores.Where(r => r.TestingId == x.Id).Select(r => r.Score).Any(s => s == null))
                    {
                        stagesScore.Add(null);
                    }
                    else
                    {
                        var sum = res.Scores.Where(r => r.TestingId == x.Id).Select(r => r.Score).Sum();
                        stagesScore.Add(sum);
                    }
                });
                reportCommonFillDataRow(stringCounter, res, stagesScore, worksheet);
                stringCounter++;
            }
        }

        private void reportCommonFillDataRow(int stringCounter, CompetitionResult result, List<double?> stages, Worksheet worksheet)
        {
            worksheet.Cells[stringCounter, 0] = new Cell(stringCounter);
            worksheet.Cells[stringCounter, 1] = new Cell($"{result.User.LastName} {result.User.FirstName} {result.User.MiddleName}");
            worksheet.Cells[stringCounter, 2] = new Cell(result.User?.Category?.Title ?? "-");
            for (int i = 0; i < stages.Count; i++)
            {
                worksheet.Cells[stringCounter, 3 + i] = stages[i] == null ?
                    new Cell("-") :
                    new Cell(stages[i], "#,##0.00");
            }
            worksheet.Cells[stringCounter, 6] = stages.Any(x => x == null) ?
                    new Cell("-") :
                    new Cell(stages.Sum(), "#,##0.00");
            worksheet.Cells[stringCounter, 7] = new Cell(result.TestingsObj.Select(x => x.ResultTime).Sum());
        }

        private void reportCommonFillTitileRow(Worksheet worksheet, int stageCounter = 4)
        {
            List<Cell> cells = new List<Cell>();
            //worksheet.Cells.ColumnWidth[0, 1] = 7000;
            worksheet.Cells.ColumnWidth[1] += 15000;

            worksheet.Cells[0, 0] = new Cell("№");
            worksheet.Cells[0, 1] = new Cell("ФИО");
            worksheet.Cells[0, 2] = new Cell("Категория");
            for (int i = 0; i < stageCounter; i++)
            {
                worksheet.Cells[0, i + 3] = new Cell($"{i + 1} Этап");
            }
            worksheet.Cells[0, 7] = new Cell("Итого");
            worksheet.Cells[0, 8] = new Cell("Время, c");
        }

        private void reportSheetFillTemplate(Worksheet worksheet)
        {
            worksheet.Cells[0, 1] = new Cell((short)1);
            worksheet.Cells[2, 0] = new Cell(9999999);
            worksheet.Cells[3, 3] = new Cell((decimal)3.45);
            worksheet.Cells[2, 2] = new Cell("Text string");
            worksheet.Cells[2, 4] = new Cell("Second string");
            worksheet.Cells[4, 0] = new Cell(32764.5, "#,##0.00");
            worksheet.Cells[5, 1] = new Cell(DateTime.Now, @"YYYY\-MM\-DD");
            worksheet.Cells.ColumnWidth[0, 1] = 6000;
        }
    }
}