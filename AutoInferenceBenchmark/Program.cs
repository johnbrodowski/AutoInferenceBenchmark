using LLama.Native;

namespace AutoInferenceBenchmark
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.


            var showLLamaCppLogs = true;

            NativeLibraryConfig
               .All
               .WithLogCallback((level, message) =>
               {
                   if (showLLamaCppLogs)
                       System.Diagnostics.Debug.WriteLine($"[llama {level}]: {message.TrimEnd('\n')}");
               });

            // Configure native library to use. This must be done before any other llama.cpp methods are called!
            NativeLibraryConfig
               .All
               .WithCuda(false)
               .WithVulkan(false);

            // Calling this method forces loading to occur now.
            NativeApi.llama_empty_call();







            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}