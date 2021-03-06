﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EtoolTech.MongoDB.Mapper.Attributes;
using EtoolTech.MongoDB.Mapper.Exceptions;

namespace EtoolTech.MongoDB.Mapper.Core
{
    public class PropertyValidator
    {
        private static readonly Dictionary<string, MethodInfo> BufferPropertyValidatorMethods = new Dictionary<string, MethodInfo>();
        private static readonly List<string> ProcessedTypes = new List<string>();

        private static void GetPropertyValidators(Type t)
        {
            if (ProcessedTypes.Contains(t.Name))
                return;

            ProcessedTypes.Add(t.Name);

            List<MethodInfo> publicMethodInfos = t.GetMethods().Where(c => c.GetCustomAttributes(typeof(MongoPropertyValidator), false).FirstOrDefault() != null).ToList();
            foreach (MethodInfo methodInfo in publicMethodInfos)
            {
                var propValidatorAtt = (MongoPropertyValidator)methodInfo.GetCustomAttributes(typeof(MongoPropertyValidator), false).FirstOrDefault();
                if (propValidatorAtt != null)
                {
                    string ClassName = t.Name;
                    string FieldName = propValidatorAtt.PropertyName;
                    string key = String.Format("{0}|{1}", ClassName, FieldName);
                    if (!BufferPropertyValidatorMethods.ContainsKey(key))
                        BufferPropertyValidatorMethods.Add(key, methodInfo);
                }
            }
        }

        private static void ExecutePropertyValidator(object sender, Type t, MethodInfo m, string PropertyName)
        {
            try
            {
                m.Invoke(sender, new object[] { t.GetProperty(PropertyName).GetValue(sender, null) });
            }
            catch (Exception ex)
            {
                throw new ValidatePropertyException(PropertyName,
                    ex.InnerException != null ? ex.InnerException.Message : ex.Message);
            }
        }


        public static void Validate(object sender, Type t)
        {
            GetPropertyValidators(t);
            var validatorList = from b in BufferPropertyValidatorMethods where b.Key.StartsWith(t.Name + "|") select b;
            foreach (var v in validatorList)
            {
                ExecutePropertyValidator(sender, t, v.Value, v.Key.Split('|')[1]);
            }
        }

    }
}
