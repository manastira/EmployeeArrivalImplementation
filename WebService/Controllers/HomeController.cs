using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web.Mvc;
using WebService.Data;
using WebService.Data.Repository;
using WebService.Models;

namespace WebService.Controllers
{
    public class HomeController : Controller
    {
        #region Variables
        private IRepository<EmployeeData> _repository = null;
        #endregion

        #region Constructor
        public HomeController() { _repository = new Repository<EmployeeData>(); }
        #endregion

        #region Actions
        [System.Web.Http.HttpGet]
        public ActionResult Index(string searchResults)
        {         
            IEnumerable<EmployeeData> model = (searchResults == null || searchResults == "-1") ? _repository.GetAll() : new List<EmployeeData>() { _repository.GetById(Convert.ToInt32(searchResults)) };
            _repository.Save();          
           
            return View(model);
        }
        #endregion

        #region Run Console app txt -> json
        public ActionResult RunApp()
        {
            string mainFolder = AppDomain.CurrentDomain.BaseDirectory + "JsonEmpoyeeNewGenerationGenerator";
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "JsonEmployeeGenerator.exe",
                    WorkingDirectory = mainFolder
                }
            };
            process.Start();
            process.WaitForExit();
            System.IO.File.Copy($@"{mainFolder}\employees.json", $@"{AppDomain.CurrentDomain.BaseDirectory}\Data\employees.json", true);
            return RedirectToAction("Index");
        }
        #endregion
    }
}