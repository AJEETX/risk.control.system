﻿using System.Data;

using Microsoft.AspNetCore.Mvc;

using risk.control.system.Models.ViewModel;

namespace risk.control.system.Controllers
{
    public class UploadsController : Controller
    {
        private readonly IWebHostEnvironment webHostEnvironment;

        public UploadsController(IWebHostEnvironment webHostEnvironment)
        {
            this.webHostEnvironment = webHostEnvironment;
        }
        public async Task<IActionResult> Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Index(IFormFile postedFile)
        {
            if (postedFile != null)
            {
                string path = Path.Combine(webHostEnvironment.WebRootPath, "upload-case");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string fileName = Path.GetFileName(postedFile.FileName);
                string filePath = Path.Combine(path, fileName);
                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    postedFile.CopyTo(stream);
                }

                string csvData = await System.IO.File.ReadAllTextAsync(filePath);
                DataTable dt = new DataTable();
                bool firstRow = true;
                foreach (string row in csvData.Split('\n'))
                {
                    if (!string.IsNullOrEmpty(row))
                    {
                        if (!string.IsNullOrEmpty(row))
                        {
                            if (firstRow)
                            {
                                foreach (string cell in row.Split(','))
                                {
                                    dt.Columns.Add(cell.Trim());
                                }
                                firstRow = false;
                            }
                            else
                            {
                                dt.Rows.Add();
                                int i = 0;
                                foreach (string cell in row.Split(','))
                                {
                                    dt.Rows[dt.Rows.Count - 1][i] = cell.Trim();
                                    i++;
                                }
                            }
                        }
                    }
                }

                return View(dt);
            }
            return Problem();
        }
    }
}
