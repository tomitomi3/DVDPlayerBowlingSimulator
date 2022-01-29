using BepuUtilities;
using DemoContentLoader;
using DemoUtilities;
using OpenTK;

namespace Demos
{
    class Program
    {
        static void Main(string[] args)
        {
            var window = new Window("DVD Player Bowling simulator for MAKAIZOU NO YORU(NIGHT OF THE MAKAIZOU SOCIETY)",
                new Int2((int)(DisplayDevice.Default.Width * 0.75f), (int)(DisplayDevice.Default.Height * 0.75f)), WindowMode.Windowed);
            var loop = new GameLoop(window);
            ContentArchive content;
            using (var stream = typeof(Program).Assembly.GetManifestResourceStream("Demos.Demos.contentarchive"))
            {
                content = ContentArchive.Load(stream);
            }

            //シミュレータ
            var demo = new DemoHarness(loop, content);
            loop.Run(demo);

            //終了
            loop.Dispose();
            window.Dispose();
        }
    }
}