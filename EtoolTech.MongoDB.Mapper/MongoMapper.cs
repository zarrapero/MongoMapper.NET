﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Serialization;
using EtoolTech.MongoDB.Mapper.Core;
using EtoolTech.MongoDB.Mapper.Interfaces;
using EtoolTech.MongoDB.Mapper.enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;

namespace EtoolTech.MongoDB.Mapper
{
    [Serializable]
    public abstract class MongoMapper
    {
        #region Eventos

        #region Delegates

        public delegate void OnAfterCompleteEventHandler(object sender, EventArgs e);

        public delegate void OnAfterDeleteEventHandler(object sender, EventArgs e);

        public delegate void OnAfterInsertEventHandler(object sender, EventArgs e);

        public delegate void OnAfterModifyEventHandler(object sender, EventArgs e);

        public delegate void OnBeforeDeleteEventHandler(object sender, EventArgs e);

        public delegate void OnBeforeInsertEventHandler(object sender, EventArgs e);

        public delegate void OnBeforeModifyEventHandler(object sender, EventArgs e);

        #endregion

        public event OnBeforeInsertEventHandler OnBeforeInsert;

        public event OnAfterInsertEventHandler OnAfterInsert;

        public event OnBeforeModifyEventHandler OnBeforeModify;

        public event OnAfterModifyEventHandler OnAfterModify;

        public event OnBeforeDeleteEventHandler OnBeforeDelete;

        public event OnAfterDeleteEventHandler OnAfterDelete;

        public event OnAfterCompleteEventHandler OnAfterComplete;

        #endregion

        private static readonly IFinder Finder = new Finder();
        private static readonly IRelations Relations = new Relations();
        private static readonly IEvents Events = new Events();
        private readonly Dictionary<string,object> RelationBuffer = new Dictionary<string, object>();

        private readonly Type _classType;        
        private BsonDocument _originalObject;
        [BsonIgnore] internal bool RepairCollection;

        [BsonId] 
        [NonSerialized]
        [XmlIgnore]
        public Guid MongoMapper_Id;

        //TODO: Pendiente temas de transacciones
        //public bool Commited;
        //public CommitOperation CommitOp;
        //public string TransactionID;

        protected MongoMapper()
        {
            _classType = GetType();
            Helper.RebuildClass(_classType, RepairCollection);          
        }

        public static IQueryable<T> QueryContext<T>()
        {
            return Helper.GetCollection<T>(typeof(T).Name).AsQueryable<T>();            
        }
      
        public List<T> GetRelation<T>(string relation)
        {
            if (!RelationBuffer.ContainsKey(relation))
            {
                RelationBuffer.Add(relation,Relations.GetRelation<T>(this, relation, _classType, Finder));
            }
            return (List<T>)RelationBuffer[relation];
        }


        private void EnsureUpRelations()
        {
            Relations.EnsureUpRelations(this, _classType, Finder);
        }

        private void EnsureDownRelations()
        {
            Relations.EnsureDownRelations(this, _classType, Finder);
        }

        private Dictionary<string, object> GetKeyValues()
        {
            var result = new Dictionary<string, object>();
            foreach (string keyField in Helper.GetPrimaryKey(_classType))
            {
                PropertyInfo propertyInfo = _classType.GetProperty(keyField);
                result.Add(keyField, propertyInfo.GetValue(this, null));
            }
            return result;
        }

        public object GetOriginalValue<T>(string fieldName)
        {
            if (MongoMapper_Id == Guid.Empty) return null;

            if (_originalObject == null)
                _originalObject = Finder.FindBsonDocumentById<T>(MongoMapper_Id);
            return _originalObject[fieldName];
        }

        public static List<T> All<T>()
        {
            return Finder.All<T>();
        }

        #region Write Methods

        public void Save<T>()
        {
            PropertyValidator.Validate(this, _classType);

            if (MongoMapper_Id == Guid.Empty)
            {
                Guid id = Finder.FindGuidByKey<T>(GetKeyValues());
                if (id == Guid.Empty)
                {
                    InsertDocument();
                }
                else
                {
                    UpdateDocument(id);
                }
            }
            else
            {
                UpdateDocument(MongoMapper_Id);
            }
        }

        private void UpdateDocument(Guid id)
        {
            Events.BeforeUpdateDocument(this, OnBeforeModify, _classType);

            EnsureUpRelations();

            MongoMapper_Id = id;

            Helper.GetCollection(Helper.GetCollectioName(_classType.Name)).Save(this);

            Events.AfterUpdateDocument(this, OnAfterModify, OnAfterComplete, _classType);
        }

        private void InsertDocument()
        {
            Events.BeforeInsertDocument(this, OnBeforeInsert, _classType);

            EnsureUpRelations();

            Helper.GetCollection(Helper.GetCollectioName(_classType.Name)).Insert(this);

            Events.AfterInsertDocument(this, OnAfterInsert, OnAfterComplete, _classType);
        }


        public void Delete<T>()
        {
            Events.BeforeDeleteDocument(this, OnBeforeDelete, _classType);

            EnsureDownRelations();

            DeleteDocument<T>();

            Events.AfterDeleteDocument(this, OnAfterDelete, OnAfterComplete, _classType);
        }


        private void DeleteDocument<T>()
        {
            if (MongoMapper_Id == Guid.Empty)
            {
                MongoMapper_Id = Finder.FindGuidByKey<T>(GetKeyValues());
            }
            QueryComplete query = Query.EQ("_id", MongoMapper_Id);
            Helper.GetCollection(Helper.GetCollectioName(_classType.Name)).FindAndRemove(query, null);
        }

        #endregion

        #region FindAsList Methods

        public static T FindByKey<T>(params object[] values)
        {
            return Finder.FindByKey<T>(values);
        } 

 
        public static List<T> FindAsList<T>(QueryComplete query)
        {
            return Finder.FindAsList<T>(query);
        }

        public static MongoCursor<T> FindAsCursor<T>(QueryComplete query = null)
        {
            return Finder.FindAsCursor<T>(query);
        }

        public static List<T> FindAsList<T>(string field, object value, FindCondition findCondition = FindCondition.Equal)
        {
            return Finder.FindAsList<T>(field, value, findCondition);
        }


        public static List<T> FindAsList<T>(Expression<Func<T, object>> exp)
        {            
            return Finder.FindAsList<T>(exp);
        }


        #endregion

    
    }
}