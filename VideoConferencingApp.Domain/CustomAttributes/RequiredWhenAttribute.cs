namespace VideoConferencingApp.Domain.CustomAttributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class RequiredWhenAttribute : Attribute
    {
        public string FieldName { get; }
        public string DependentProperty { get; }
        public object DependentValue { get; }
        public string ErrorMessage { get; set; }

        public RequiredWhenAttribute(string fieldName, string dependentProperty, object dependentValue)
        {
            FieldName = fieldName;
            DependentProperty = dependentProperty;
            DependentValue = dependentValue;
            ErrorMessage = $"{fieldName} is required when {dependentProperty} is {dependentValue}";
        }
    }
}
