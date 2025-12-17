namespace VideoConferencingApp.Domain.CustomAttributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class MaxLengthFieldAttribute : Attribute
    {
        public string FieldName { get; }
        public int MaxLength { get; }
        public string ErrorMessage { get; set; }

        public MaxLengthFieldAttribute(string fieldName, int maxLength)
        {
            FieldName = fieldName;
            MaxLength = maxLength;
            ErrorMessage = $"{fieldName} must not exceed {maxLength} characters";
        }
    }
}
