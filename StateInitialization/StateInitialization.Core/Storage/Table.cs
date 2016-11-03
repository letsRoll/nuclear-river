using System;

namespace NuClear.StateInitialization.Core.Storage
{
    public sealed class Table
    {
        private string _fullName;

        public Table(string tableName, string schemaName = null)
        {
            TableName = tableName;
            SchemaName = schemaName;
        }

        public string TableName { get; }

        public string SchemaName { get; }

        public override string ToString()
        {
            return _fullName ?? (_fullName = string.IsNullOrEmpty(SchemaName)
                                                 ? $"[{TableName}]"
                                                 : $"[{SchemaName}].[{TableName}]");
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj.GetType() == GetType() &&
                string.Equals(ToString(), obj.ToString(), StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
