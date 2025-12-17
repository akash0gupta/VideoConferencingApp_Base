using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Domain.CustomAttributes
{

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class RequiredFieldAttribute : Attribute
    {
        public string FieldName { get; }
        public string ErrorMessage { get; set; }

        public RequiredFieldAttribute(string fieldName)
        {
            FieldName = fieldName;
            ErrorMessage = $"{fieldName} is required";
        }
    }
}
