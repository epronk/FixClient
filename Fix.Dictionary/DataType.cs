
namespace Fix
{
    public static partial class Dictionary
    {
        public class DataType
        {
            // We can't reference the base type here directly or we get initialisation exceptions.
            // eg. new DataType("Length", DataTypes.Int, "" ...)
            // To work around that we initialist BaseType in the VersionDataTypeCollection constructor.
            // Length.BaseType = Int;
            public DataType(string name, string description)
            {
                Name = name;
                Description = description;
            }

            public string Name { get; }
            //public DataType? BaseType { get; internal set; }
            public string Description { get; }
        }
    }
}