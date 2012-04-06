using System;

namespace ProjectNavi
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (NaviControllerGame game = new NaviControllerGame())
            {
                game.Run();
            }
        }
    }
#endif
}

