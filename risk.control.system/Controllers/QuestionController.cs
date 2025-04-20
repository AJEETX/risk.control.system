using System.Text.RegularExpressions;

using Amazon.Rekognition.Model;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using risk.control.system.Data;
using risk.control.system.Models;

namespace risk.control.system.Controllers
{
    public class QuestionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment webHostEnvironment;

        public QuestionController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            this._context = context;
            this.webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index()
        {
            var currentUserEmail = HttpContext.User.Identity.Name;

            var currentUser = _context.ClientCompanyApplicationUser.FirstOrDefault(x => x.Email == currentUserEmail);

            var questions = _context.CaseQuestionnaire.Include(c=>c.Questions).FirstOrDefault(x => x.ClientCompanyId == currentUser.ClientCompanyId && x.InsuranceType == InsuranceType.CLAIM);
            if (questions != null)
            {
                var model = new QuestionFormViewModel
                {
                    Questions = questions.Questions.ToList()
                };
                return View(model);
            }
            var newmodel = new QuestionFormViewModel
            {
                Questions = new List<Question>()
            };

            return View(newmodel);
        }

        [HttpPost]
        public IActionResult AddQuestion(QuestionFormViewModel model)
        {
            var currentUserEmail = HttpContext.User.Identity.Name;

            var currentUser = _context.ClientCompanyApplicationUser.FirstOrDefault(x => x.Email == currentUserEmail);
            if (ModelState.IsValid)
            {
                var question = new Question
                {
                    QuestionText = model.QuestionText,
                    QuestionType = model.QuestionType,
                    Options = model.Options,
                    IsRequired = model.IsRequired
                };
                var existingQuestion = _context.CaseQuestionnaire.Include(c=>c.Questions)
                    .FirstOrDefault(x => x.ClientCompanyId == currentUser.ClientCompanyId && x.InsuranceType == model.InsuranceType);
                if(existingQuestion != null)
                {
                    existingQuestion.Questions.Add(question);
                }
                else
                {
                    var caseQuestionnaire = new CaseQuestionnaire
                    {
                        ClientCompanyId = currentUser.ClientCompanyId,
                        InsuranceType = model.InsuranceType,
                        CreatedUser = currentUserEmail
                    };
                    caseQuestionnaire.Questions.Add(question);
                    _context.CaseQuestionnaire.Add(caseQuestionnaire);
                }
                _context.SaveChanges();

            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> SubmitAnswers(QuestionFormViewModel model)
        {
           var fileNames = new Dictionary<int,string>();
            var files = Request.Form.Files;
            // Handle file uploads separately
            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        using var dataStream = new MemoryStream();
                        file.CopyTo(dataStream);
                        var bytes = dataStream.ToArray();


                        string path = Path.Combine(webHostEnvironment.WebRootPath, "company");
                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }
                        var fileName =$"{DateTime.Now.ToString("dd-MMM-yyyy-hh-mm-ss")}-{Path.GetFileName(file.FileName)}";
                        var upload = Path.Combine(webHostEnvironment.WebRootPath, "company", fileName);
                        file.CopyTo(new FileStream(upload, FileMode.Create));
                        var uploaded = "/company/" + fileName;

                        var match = Regex.Match(file.Name, @"answers\[(?<id>\d+)\]");

                        if (match.Success)
                        {
                            int number = int.Parse(match.Groups["id"].Value); // Convert string to int
                            fileNames.Add(number,file.FileName);
                            Console.WriteLine(number); // Output: 23
                        }
                        Console.WriteLine($"File Uploaded: {file.FileName}");
                    }
                }
            }

            foreach (var answer in model.Answers)
            {
                var question = await _context.Questions.FirstOrDefaultAsync(q => q.Id == answer.Key);
                int questionId = answer.Key;
                string value = answer.Value;
                if(string.IsNullOrWhiteSpace(value))
                {
                    value = fileNames[answer.Key];
                }
                // Save to DB or process as needed
                Console.WriteLine($"Question: {question.QuestionText}, Answer: {value}");
            }
            TempData["Message"] = "Answers submitted successfully!";
            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> DeleteQuestion([FromBody] DeleteQuestionModel model)
        {
            var question = await _context.Questions.FindAsync(model.Id);
            if (question == null)
            {
                return NotFound();
            }

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();

            return Ok(); // Return a success response
        }

    }
    public class DeleteQuestionModel
    {
        public int Id { get; set; }
    }

}
