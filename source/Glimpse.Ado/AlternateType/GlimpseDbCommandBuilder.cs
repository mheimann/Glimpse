using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Text;

namespace Glimpse.Ado.AlternateType
{
    public class GlimpseDbCommandBuilder : DbCommandBuilder
    {
        private static readonly PropertyInfo canRaiseEventsProperty;
        private static readonly PropertyInfo catalogLocationProperty;
        private static readonly PropertyInfo catalogSeparatorProperty;
        private static readonly PropertyInfo conflictOptionProperty;

        private static readonly MethodInfo applyParameterInfoMethod;
        private static readonly MethodInfo getParameterNameMethod1;
        private static readonly MethodInfo getParameterNameMethod2;
        private static readonly MethodInfo getParameterPlaceholderMethod;
        private static readonly MethodInfo getSchemaTableMethod;
        private static readonly MethodInfo getServiceMethod;
        private static readonly MethodInfo initializeCommandMethod;
        private static readonly MethodInfo setRowUpdatingHandlerMethod;

        static GlimpseDbCommandBuilder()
        {
            Type type = typeof(DbCommandBuilder);

            BindingFlags defaultBinding = BindingFlags.Instance | BindingFlags.NonPublic;
        
            // Properties
            canRaiseEventsProperty = type.GetProperty("CanRaiseEvents", defaultBinding);
            catalogLocationProperty = type.GetProperty("CatalogLocation", defaultBinding | BindingFlags.SetProperty);
            catalogSeparatorProperty = type.GetProperty("CatalogSeparator", defaultBinding | BindingFlags.SetProperty);
            conflictOptionProperty = type.GetProperty("ConflictOption", defaultBinding | BindingFlags.SetProperty);

            // Methods
            applyParameterInfoMethod = type.GetMethod("ApplyParameterInfo", defaultBinding);

            getParameterNameMethod1 = type.GetMethod("GetParameterName", defaultBinding, 
                Type.DefaultBinder, CallingConventions.Any, new Type[] { typeof(int) }, null);

            getParameterNameMethod2 = type.GetMethod("GetParameterName", defaultBinding,
                Type.DefaultBinder, CallingConventions.Any, new Type[] { typeof(string) }, null);

            getParameterPlaceholderMethod = type.GetMethod("GetParameterPlaceholder", defaultBinding);
            getSchemaTableMethod = type.GetMethod("GetSchemaTable", defaultBinding);
            getServiceMethod = type.GetMethod("GetService", defaultBinding);
            initializeCommandMethod = type.GetMethod("InitializeCommand", defaultBinding);
            setRowUpdatingHandlerMethod = type.GetMethod("SetRowUpdatingHandler", defaultBinding);
        }

        private DbCommandBuilder InnerCommandBuilder { get; set; }

        public GlimpseDbCommandBuilder(DbCommandBuilder innerCommandBuilder)
        {
            if (innerCommandBuilder == null)
                throw new ArgumentNullException("innerCommandBuilder");

            InnerCommandBuilder = innerCommandBuilder;
        }

        private T Get<T>(PropertyInfo propertyInfo)
        {
            return (T)propertyInfo.GetValue(InnerCommandBuilder, null);
        }

        private void Set(PropertyInfo propertyInfo, object value)
        {
            propertyInfo.SetValue(InnerCommandBuilder, value, null);
        }
        
        protected override bool CanRaiseEvents
        {
            get
            {
                return Get<bool>(canRaiseEventsProperty);
            }
        }

        public override CatalogLocation CatalogLocation
        {
            get
            {
                return InnerCommandBuilder.CatalogLocation;
            }
            set
            {
                InnerCommandBuilder.CatalogLocation = value;
            }
        }

        public override string CatalogSeparator
        {
            get
            {
                return InnerCommandBuilder.CatalogSeparator;
            }
            set
            {
                InnerCommandBuilder.CatalogSeparator = value;
            }
        }

        public override ConflictOption ConflictOption
        {
            get
            {
                return InnerCommandBuilder.ConflictOption;
            }
            set
            {
                InnerCommandBuilder.ConflictOption = value;
            }
        }

        protected override void ApplyParameterInfo(DbParameter parameter, DataRow row, StatementType statementType, bool whereClause)
        {
            applyParameterInfoMethod.Invoke(InnerCommandBuilder, new object[] { parameter, row, statementType, whereClause });
        }

        protected override string GetParameterName(int parameterOrdinal)
        {
            return (string)getParameterNameMethod1.Invoke(InnerCommandBuilder, new object[] { parameterOrdinal });
        }

        protected override string GetParameterName(string parameterName)
        {
            return (string)getParameterNameMethod2.Invoke(InnerCommandBuilder, new object[] { parameterName });
        }

        protected override string GetParameterPlaceholder(int parameterOrdinal)
        {
            return (string)getParameterPlaceholderMethod.Invoke(InnerCommandBuilder, new object[] { parameterOrdinal });
        }

        protected override DataTable GetSchemaTable(DbCommand sourceCommand)
        {
            return (DataTable)getSchemaTableMethod.Invoke(InnerCommandBuilder, new object[] { sourceCommand });
        }

        protected override object GetService(Type service)
        {
            return getServiceMethod.Invoke(InnerCommandBuilder, new object[] { service });
        }

        protected override DbCommand InitializeCommand(DbCommand command)
        {
            InnerCommandBuilder.DataAdapter = ((GlimpseDbDataAdapter) DataAdapter).InnerDataAdapter;
            return (DbCommand)initializeCommandMethod.Invoke(InnerCommandBuilder, new object[] { command });
        }

        public override object InitializeLifetimeService()
        {
            return InnerCommandBuilder.InitializeLifetimeService();
        }

        public override string QuoteIdentifier(string unquotedIdentifier)
        {
            return InnerCommandBuilder.QuoteIdentifier(unquotedIdentifier);
        }

        public override ObjRef CreateObjRef(Type requestedType)
        {
            return InnerCommandBuilder.CreateObjRef(requestedType);
        }

        public override string QuotePrefix
        {
            get
            {
                return InnerCommandBuilder.QuotePrefix;
            }
            set
            {
                InnerCommandBuilder.QuotePrefix = value;
            }
        }

        public override string QuoteSuffix
        {
            get
            {
                return InnerCommandBuilder.QuoteSuffix;
            }
            set
            {
                InnerCommandBuilder.QuoteSuffix = value;
            }
        }

        public override System.ComponentModel.ISite Site
        {
            get
            {
                return InnerCommandBuilder.Site;
            }
            set
            {
                InnerCommandBuilder.Site = value;
            }
        }

        protected override void SetRowUpdatingHandler(DbDataAdapter adapter)
        {
            setRowUpdatingHandlerMethod.Invoke(InnerCommandBuilder, new object[] { ((GlimpseDbDataAdapter) adapter).InnerDataAdapter });
        }

        public override string SchemaSeparator
        {
            get
            {
                return InnerCommandBuilder.SchemaSeparator;
            }
            set
            {
                InnerCommandBuilder.SchemaSeparator = value;
            }
        }

        public override void RefreshSchema()
        {
            InnerCommandBuilder.RefreshSchema();
        }

        public override string UnquoteIdentifier(string quotedIdentifier)
        {
            return InnerCommandBuilder.UnquoteIdentifier(quotedIdentifier);
        }
    }
}
