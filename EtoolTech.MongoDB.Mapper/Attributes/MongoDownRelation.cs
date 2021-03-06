using System;

namespace EtoolTech.MongoDB.Mapper.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class MongoDownRelation : Attribute
    {
        private string _relation;

        public string ObjectName;
        public string FieldName;

        public string Relation
        {
            get { return _relation; }
            set
            {
                _relation = value;
                string[] values = _relation.Split(',');
                ObjectName = values[0];
                FieldName = values[1];
            }
        }
    }
}