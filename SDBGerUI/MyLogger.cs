namespace SDBGerUI
{
    using System;
    using System.Linq;
    using System.Text;

    using SDBGer;

    [Serializable]
    internal class MyLogger : MarshalByRefObject, ILogger
    {
        #region Constructors and Destructors

        public MyLogger()
        {
            this.Log = new StringBuilder();
        }

        #endregion

        #region Public Properties

        public bool IsUptodate { get; set; }

        public StringBuilder Log { get; set; }

        #endregion

        #region Public Methods and Operators

        public void Error(string st, params object[] parameters)
        {
            this.Write(st, parameters);
            Console.Beep();
        }

        public void Error(Exception exception)
        {
            this.Write(exception.Message + "\n" + exception.StackTrace);
            Console.Beep();
        }

        // https://social.msdn.microsoft.com/Forums/en-US/3ab17b40-546f-4373-8c08-f0f072d818c9/remotingexception-when-raising-events-across-appdomains?forum=netfxremoting
        public override object InitializeLifetimeService()
        {
            return null;
        }

        public void Success(string st, params object[] parameters)
        {
            this.Write(st, parameters);
            Console.Beep();
        }

        public void Trace(string st, params object[] parameters)
        {
            this.Write(st, parameters);
        }

        #endregion

        #region Methods

        private void Write(string value, params object[] parameters)
        {
            if (parameters.Any())
            {
                this.Log.AppendLine(string.Format(value, parameters));
            }
            else
            {
                this.Log.AppendLine(value);
            }
            this.IsUptodate = false;
        }

        #endregion
    }
}