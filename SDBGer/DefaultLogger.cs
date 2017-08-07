namespace SDBGer
{
    using System;

    public interface ILogger
    {
        #region Public Methods and Operators

        void Error(string st, params object[] parameters);

        void Error(Exception exception);

        void Success(string st, params object[] parameters);

        void Trace(string st, params object[] parameters);

        #endregion
    }

    [Serializable]
    public class DefaultLogger : MarshalByRefObject, ILogger
    {
        #region Public Methods and Operators

        public void Error(string st, params object[] parameters)
        {
            this.Print(ConsoleColor.Red, st, parameters);
        }

        public void Error(Exception ex)
        {
            this.Print(ConsoleColor.Red, ex);
        }

        // https://social.msdn.microsoft.com/Forums/en-US/3ab17b40-546f-4373-8c08-f0f072d818c9/remotingexception-when-raising-events-across-appdomains?forum=netfxremoting
        public override object InitializeLifetimeService()
        {
            return null;
        }

        public void Success(string st, params object[] parameters)
        {
            this.Print(ConsoleColor.Green, st, parameters);
        }

        public void Trace(string st, params object[] parameters)
        {
            this.Print(null, st, parameters);
        }

        #endregion

        #region Methods

        private void Print(ConsoleColor? color, object value, params object[] parameters)
        {
            if (color != null)
            {
                Console.ForegroundColor = color.Value;
            }

            if (value.GetType().IsInstanceOfType(typeof(Exception)))
            {
                Exception ex = (Exception)value;
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            else
            {
                Console.WriteLine(value);
            }

            Console.ResetColor();
        }

        #endregion
    }
}