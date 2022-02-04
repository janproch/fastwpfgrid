using System.IO;
using System.Reflection;

namespace FastWpfGridTest
{
    public static class GridModelFunctions
    {
        public static string PathFromOutputDir(
            string fileName,
            string subDirectory)
        {
            var outputDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var basePath = Path.Combine(outputDir, subDirectory);

            return Path.Combine(basePath, fileName);
        }
    }
}
