namespace SDBGer
{
    using System;
    using System.Linq;
    using System.Reflection;

    internal static class ReflectionExtensions
    {
        #region Public Methods and Operators

        public static object DoActionWithMember(this object obj, string fieldName, Func<object, MemberInfo, object> action)
        {
            var propAndFieldsFlag = BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.GetField | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var allMembersFlag = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            var fields = fieldName.Split('.');

            for (int i = 0; i < fields.Length; i++)
            {
                var field = fields[i];

                var type = obj is Type ? (Type)obj : obj.GetType();

                MemberInfo info = null;
                var typeToFindField = type;

                while (info == null)
                {
                    info = typeToFindField.GetMember(
                        field,
                        i == fields.Length - 1 ? allMembersFlag : propAndFieldsFlag).SingleOrDefault();

                    if (info != null)
                    {
                        continue;
                    }

                    if (typeToFindField.BaseType == null)
                    {
                        throw new Exception(string.Format("Field '{0}' in object of type '{1}' is not found.", field, type));
                    }

                    typeToFindField = typeToFindField.BaseType;
                }

                if (i != fields.Length - 1)
                {
                    obj = GetValueFromMember(obj, info);
                }
                else
                {
                    return action(obj, info);
                }
            }

            return null;
        }

        public static TOutput GetFieldValue<TOutput>(this Type type, string fieldName)
        {
            var fieldInfo = type.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (fieldInfo == null)
            {
                throw new Exception(string.Format("Field '{0}' in class '{1}' is not found.", fieldName, type));
            }

            return (TOutput)fieldInfo.GetValue(null);
        }

        public static object GetMemberValue(this object target, string fieldName)
        {
            return GetMemberValue<object>(target, fieldName);
        }

        public static TOutput GetMemberValue<TOutput>(this object target, string fieldName)
        {
            return (TOutput)DoActionWithMember(target, fieldName, GetValueFromMember);
        }

        public static TOutput GetPropertyValue<TOutput>(this Type target, string propertyName)
        {
            var fieldInfo = target.GetProperty(
                propertyName,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (fieldInfo == null)
            {
                throw new Exception(string.Format("Static field '{0}' in Class '{1}' is not found.", propertyName, target));
            }

            return (TOutput)fieldInfo.GetValue(null);
        }

        public static void InvokeMethod(this object target, string methodName, params object[] parameters)
        {
            var type = target.GetType();
            var methodInfo = type.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (methodInfo == null)
            {
                throw new Exception(string.Format("method '{0}' in object of type '{1}' is not found.", methodInfo, type));
            }

            methodInfo.Invoke(target, parameters);
        }

        public static TOutput InvokeMethod<TOutput>(this object target, string methodName, params object[] parameters)
        {
            var type = target.GetType();
            var methodInfo = type.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (methodInfo == null)
            {
                throw new Exception(string.Format("method '{0}' in object of type '{1}' is not found.", methodName, type));
            }

            return (TOutput)methodInfo.Invoke(target, parameters);
        }

        public static TOutput InvokeMethod<TOutput>(this Type type, string methodName, params object[] parameters)
        {
            var methodInfo = type.GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (methodInfo == null)
            {
                throw new Exception(string.Format("method '{0}' in object of type '{1}' is not found.", methodName, type));
            }

            return (TOutput)methodInfo.Invoke(null, parameters);
        }

        public static void SetFieldValue(this object target, string fieldName, object value)
        {
            var type = target.GetType();
            var fieldInfo = type.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (fieldInfo == null)
            {
                throw new Exception(string.Format("Field '{0}' in object of type '{1}' is not found.", fieldName, type));
            }

            fieldInfo.SetValue(target, value);
        }

        public static void SetMemberValue(this object target, string fieldName, object value)
        {
            DoActionWithMember(target, fieldName, (o, info) =>
            {
                SetValueToMember(o, info, value);
                return null;
            });
        }

        public static void SetPropertyValue(this object target, string propertyName, object value)
        {
            var type = target.GetType();
            var fieldInfo = type.GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (fieldInfo == null)
            {
                throw new Exception(string.Format("Property '{0}' in object of type '{1}' is not found.", propertyName, target));
            }

            fieldInfo.SetValue(target, value);
        }

        #endregion

        #region Methods

        private static object GetValueFromMember(object obj, MemberInfo info)
        {
            switch (info.MemberType)
            {
                case MemberTypes.Field:
                    obj = ((FieldInfo)info).GetValue(obj);
                    break;
                case MemberTypes.Property:
                    obj = ((PropertyInfo)info).GetValue(obj);
                    break;
            }
            return obj;
        }

        private static void SetValueToMember(object obj, MemberInfo info, object value)
        {
            switch (info.MemberType)
            {
                case MemberTypes.Field:
                    ((FieldInfo)info).SetValue(obj, value);
                    break;
                case MemberTypes.Property:
                    ((PropertyInfo)info).SetValue(obj, value);
                    break;
            }
        }

        #endregion
    }
}