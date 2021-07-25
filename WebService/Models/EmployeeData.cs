using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.ModelBinding;

namespace WebService.Models
{
    public class EmployeeData : IModelBinder
    {
        [Key]      
        public int EmployeeId { get; set; }
        public DateTime DateWhen { get; set; }
        public string Role { get; set; }
        public string Email { get; set; }

        public bool BindModel(HttpActionContext actionContext, ModelBindingContext bindingContext)
        {
            throw new NotImplementedException();
        }
    }
}