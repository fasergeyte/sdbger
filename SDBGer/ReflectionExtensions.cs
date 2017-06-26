namespace SDBGer
{
    using System;
    using System.Reflection;

    internal static class ReflectionExtensions
    {
        #region Public Methods and Operators

        public static TOutput GetFieldValue<TOutput>(this object target, string fieldName)
        {
            var type = target.GetType();
            var fieldInfo = type.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (fieldInfo == null)
            {
                throw new Exception(string.Format("Field '{0}' in object of type '{1}' is not found.", fieldName, type));
            }

            return (TOutput)fieldInfo.GetValue(target);
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
                throw new Exception(string.Format("method '{0}' in object of type '{1}' is not found.", methodInfo, type));
            }

            return (TOutput)methodInfo.Invoke(target, parameters);
        }

        #endregion
    }
}