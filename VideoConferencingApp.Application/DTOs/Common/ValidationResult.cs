using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Application.DTOs.Common
{
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();

        public ValidationResult()
        {
            IsValid = true;
        }

        public void AddError(string error)
        {
            IsValid = false;
            Errors.Add(error);
        }

        public void AddErrors(IEnumerable<string> errors)
        {
            IsValid = false;
            Errors.AddRange(errors);
        }
    }
}
