using System;

namespace AraturkaMaster
{
    public static class ColorfulConsole
    {
        public static class Write
        {
            public static void _default(Object text)
            {
                Console.ResetColor();
                Console.Write(text);
            }

            public static void success(Object text)
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.Write(text);
                Console.ResetColor();
            }

            public static void error(Object text)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(text);
                Console.ResetColor();
            }

            public static void warning(Object text)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(text);
                Console.ResetColor();
            }

            public static void primary(Object text)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(text);
                Console.ResetColor();
            }

            public static void secondary(Object text)
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write(text);
                Console.ResetColor();
            }
        }

        public static class WriteLine
        {
            public static void _default(Object text)
            {
                Console.ResetColor();
                Console.WriteLine(text);
            }

            public static void success(Object text)
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine(text);
                Console.ResetColor();
            }

            public static void error(Object text)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(text);
                Console.ResetColor();
            }

            public static void warning(Object text)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(text);
                Console.ResetColor();
            }

            public static void primary(Object text)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(text);
                Console.ResetColor();
            }

            public static void secondary(Object text)
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine(text);
                Console.ResetColor();
            }
        }
    }
}
