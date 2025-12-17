namespace VideoConferencingApp.Domain.CustomAttributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class MinLengthFieldAttribute : Attribute
    {
        public string FieldName { get; }
        public int MinLength { get; }
        public string ErrorMessage { get; set; }

        public MinLengthFieldAttribute(string fieldName, int minLength)
        {
            FieldName = fieldName;
            MinLength = minLength;
            ErrorMessage = $"{fieldName} must be at least {minLength} characters";
        }
    }
}
