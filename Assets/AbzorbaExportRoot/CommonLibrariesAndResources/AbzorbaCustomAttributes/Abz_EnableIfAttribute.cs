namespace AbzorbaExportRoot.CommonLibrariesAndResources.AbzorbaCustomAttributes
{
    /// <summary> Allows a property to dynamically draw/or not draw it self in the editor. </summary>
    public class Abz_EnableIfAttribute : Abz_CustomAttribute
    {
        public readonly string PropertyName;
        public readonly object[] Values;
        
        /// <summary> Property will be enabled if value parameter matches the property value </summary>
        public Abz_EnableIfAttribute(string propertyName, object value)
        {
            PropertyName = propertyName;
            Values = new object[1];
            Values[0] = value;
        }

        /// <summary> Property will be enabled if any value matches the property value </summary>
        public Abz_EnableIfAttribute(string propertyName, params object[] values)
        {
            PropertyName = propertyName;
            Values = values;
        }
    }
}