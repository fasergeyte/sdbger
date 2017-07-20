namespace SDBGer
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public static class Console
    {
        #region Static Fields

        private static readonly Stream inputStream;

        private static byte[] buffer;

        private static bool isCursorBlinking = false;

        #endregion

        #region Constructors and Destructors

        static Console()
        {
            InitializeCursoreAnimation();
            BufferSize = 1536;
            inputStream = inputStream ?? System.Console.OpenStandardInput();
            buffer = new byte[BufferSize];

            System.Console.CancelKeyPress += new ConsoleCancelEventHandler((s, e) =>
            {
                System.Console.WriteLine("Are you sure to close?[y/n]");
                var a = System.Console.ReadLine();
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
                System.Console.CursorVisible = value;
                isCursorBlinking = value;
            }
        }

        #endregion

        #region Public Methods and Operators

        public static void Green(object value)
        {
            System.Console.Beep();
            Print(value, ConsoleColor.Green);
        }

        public static string ReadLine()
        {
            int length = inputStream.Read(buffer, 0, BufferSize);
            return Encoding.UTF8.GetString(buffer, 0, length);
        }

        public static void Red(object value)
        {
            System.Console.Beep();
            Print(value, ConsoleColor.Red);
        }

        public static void WriteLine(string value, params object[] par)
        {
            Print(string.Format(value, par));
        }

        #endregion

        #region Methods

        private static void InitializeCursoreAnimation()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (IsCursorBlinking)
                    {
                        System.Console.CursorVisible = !System.Console.CursorVisible;
                        Thread.Sleep(500);
                    }
                    else
                    {
                        System.Console.CursorVisible = false;
                    }
                }
            });
        }

        private static void Print(object value, ConsoleColor? color = null)
        {
            if (color != null)
            {
                System.Console.ForegroundColor = color.Value;
            }

            if (value.GetType().IsInstanceOfType(typeof(Exception)))
            {
                Exception ex = (Exception)value;
                System.Console.WriteLine(ex.Message);
                System.Console.WriteLine(ex.StackTrace);
            }
            else
            {
                System.Console.WriteLine(value);
            }

            System.Console.ResetColor();
        }

        #endregion
    }
}