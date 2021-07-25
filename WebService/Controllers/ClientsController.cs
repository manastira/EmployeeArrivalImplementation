using Flurl.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using WebService.Data;
using WebService.Data.Repository;
using WebService.Models;

namespace WebService.Controllers
{


    public class ClientsController : ApiController
    {
        #region Variables


        private readonly string token = Guid.NewGuid().ToString("N");
        private readonly string hashToken = HttpContext.Current.Session["token"] != null ? HttpContext.Current.Session["token"].ToString() : "";
        private IRepository<EmployeeData> _repository = null;
        #endregion
        
        #region Constructor
        public ClientsController()
        {
            _repository = new Repository<EmployeeData>();
        }
        #endregion

        #region web API`s
               
        // here get the token 
        [Route("api/clients/getToken")]
        public IHttpActionResult Get()
        {
            // should be implemented -> OAuth  Authorization or JWT Authentication
            string hashToken = ComputeSha256Hash(token);
            HttpContext.Current.Session["token"] = hashToken;
            return Ok(new { hashToken, Expires = DateTime.UtcNow.AddHours(8).ToString("u").Replace(" ", "T") });
        }

        //  sign up for the service and run the simulator
        [HttpGet]
        [Route("api/clients/subscribe")]
        public IHttpActionResult Subscribe(string date, string callback)
        {
            if (!Request.Headers.TryGetValues("Accept-Client", out IEnumerable<string> headerValues) || headerValues.FirstOrDefault() != hashToken)
            {
                return Unauthorized();
            }

            Client newClient = new Client()
            {
                Url = new Uri(callback),
                Token = token
            };

            new Simulator(newClient).Simulate(DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.CurrentCulture));
            Thread.Sleep(5000);


            return Ok();
        }

        // retrieve the generated employees and insert it in DB
        [Route("api/clients/employees")]
        public IHttpActionResult Post(List<SimulationData> employees)
        {
            for (int i = 0; i < employees.Count; i++)
            {
                _repository.Insert(new EmployeeData()
                {
                    EmployeeId = employees[i].EmployeeId,
                    DateWhen = Convert.ToDateTime(employees[i].When),
                    Role = employees[i].Role,
                    Email = employees[i].Email
                });
                _repository.Save();
            }

            return RedirectToRoute("Home", new { controller = "Home", action = "Index" });
        }

        // delete employee
        [HttpGet]
        [Route("api/clients/DeleteEmployee")]
        public IHttpActionResult DeleteEmployee(string id)
        {
            _repository.Delete(Convert.ToInt32(id));
            _repository.Save();
            return RedirectToRoute("Home", new { controller = "Home", action = "Index" });

        }

        // filter grid by id
        [HttpGet]
        [Route("api/clients/Filter")]
        public IHttpActionResult Filter(string searchResults)
        {
           var model = _repository.GetById(Convert.ToInt32(searchResults));
            _repository.Save();
            return Ok(model);// RedirectToRoute("Home", new { controller = "Home", action = "Index", searchResults = searchResults });

        }

        // token -> hash string
        public string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
        #endregion

        #region Simulator
        public class Simulator
        {
            private readonly IList<JsonEmployee> _employees;

            private readonly Client _client;

            public Simulator(Client client)
            {
                _client = client;
                _employees = JsonConvert.DeserializeObject<IList<JsonEmployee>>(new StreamReader(HttpContext.Current.Server.MapPath("~/bin/Data/employees.json")).ReadToEnd());
            }

            public void Simulate(DateTime when)
            {
                Task.Factory.StartNew(async () =>
                {
                    bool stop = false;
                    while (!stop)
                    {
                        Random random = new Random(Environment.TickCount);
                        Thread.Sleep(1000 * random.Next(1, 5)); //Wait between 1 and 50 secs

                        IList<SimulationData> data = SimulateData(when);
                        if (!data.Any())
                        {
                            //Finished simulating today's data
                            stop = true;
                        }
                        int count = 10;
                        while (count > 0)
                        {
                            try
                            {
                                await _client.Url.ToString().WithHeader("X-Fourth-Token", _client.Token).PostJsonAsync(data);

                                count = 0;
                            }
                            catch (Exception)
                            {
                                --count;
                                Thread.Sleep(1000);
                            }
                        }
                    }
                });
            }

            private readonly Dictionary<int, JsonEmployee> _simulated = new Dictionary<int, JsonEmployee>();
            private readonly object _locker = new object();

            private IList<SimulationData> SimulateData(DateTime when)
            {
                //So we dont overlap requests
                lock (_locker)
                {
                    Random random = new Random(Environment.TickCount);
                    int count = random.Next(5, 101); //from 5 to 100 employees
                    List<JsonEmployee> employees =
                        _employees.Where(x => !_simulated.ContainsKey(x.Id))
                            .OrderBy(x => Guid.NewGuid())
                            .Take(count)
                            .ToList();
                    foreach (JsonEmployee e in employees)
                    {
                        _simulated.Add(e.Id, e);
                    }
                    return employees.Select(x =>
                        new SimulationData
                        {
                            EmployeeId = x.Id,
                            When = when.AddHours(8).AddMinutes(random.Next(120)).ToString("u").Replace(" ", "T"),
                            Email = x.Email,
                            Role = x.Role
                        }).ToList();
                }
            }
        }
        #endregion

        #region Models
      
        public class JsonEmployee
        {
            public string Name { get; set; }
            public string SurName { get; set; }
            public string Email { get; set; }
            public int Age { get; set; }
            public string Role { get; set; }
            public int? ManagerId { get; set; }
            public int Id { get; set; }
            public List<string> Teams { get; set; }
        }

        public class SimulationData
        {
            public int EmployeeId { get; set; }
            public string When { get; set; }
            public string Email { get; set; }
            public string Role { get; set; }
        }
        #endregion
    }
}
