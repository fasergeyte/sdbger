namespace SDBGer
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public static class Log
    {
        #region Static Fields

        private static readonly Stream inputStream;

        private static byte[] buffer;

        private static bool isCursorBlinking = false;

        #endregion

        #region Constructors and Destructors

        static Log()
        {
            BufferSize = 1536;
            inputStream = inputStream ?? Console.OpenStandardInput();
            buffer = new byte[BufferSize];

            Console.CancelKeyPress += new ConsoleCancelEventHandler((s, e) =>
            {
                Console.WriteLine("Are you sure to close?[y/n]");
                var a = Console.ReadLine();
                if (a != "y")
                {
                    e.Cancel = true;
                    return;
                }

                SpecflowManager.KillChromeDriver();
                Environment.Exit(0);
            });
        }

        #endregion

        #region Public Properties

        public static int BufferSize
        {
            get
            {
                return buffer.Length;
            }
            set
            {
                buffer = new byte[value];
            }
        }

        public static bool IsCursorBlinking
        {
            get
            {
                return isCursorBlinking;
            }
            set
            {
                Console.CursorVisible = value;
                isCursorBlinking = value;
            }
        }

        #endregion

        #region Public Methods and Operators

        public static void Green(object value)
        {
            Console.Beep();
            Print(value, ConsoleColor.Green);
        }

        public static string ReadLine()
        {
            int length = inputStream.Read(buffer, 0, BufferSize);
            return Encoding.UTF8.GetString(buffer, 0, length);
        }

        public static void Red(object value)
        {
            Console.Beep();
            Print(value, ConsoleColor.Red);
        }

        public static void WriteLine(string value, params object[] par)
        {
            Print(string.Format(value, par));
        }

        #endregion

        #region Methods

        public static void InitializeCursoreAnimation()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (IsCursorBlinking)
                    {
                        Console.CursorVisible = !Console.CursorVisible;
                        Thread.Sleep(500);
                    }
                    else
                    {
                        Console.CursorVisible = false;
                    }
                }
            });
        }

        private static void Print(object value, ConsoleColor? color = null)
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