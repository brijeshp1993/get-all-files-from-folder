using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        public class ListofFile
        {
            
            public string Name { get; set; }
            public string ext { get; set; }
            public string path { get; set; }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            DateTime stdate = DateTime.Now;
            List<ListofFile> listofFiles = new List<ListofFile>();
            
            ListofFile obj = new ListofFile();
            listofFiles.Add(obj);
            TraverseTreeParallelForEach(@"F:\", (f) =>
            {
                // Exceptions are no-ops.
                try
                {
                    //f.
                    // Do nothing with the data except read it.
                    //byte[] data = File.ReadAllBytes(f);
                    FileInfo oFileInfo = new FileInfo(f);
                    obj.ext= oFileInfo.Extension;
                    obj.Name = oFileInfo.Name;
                    obj.path = f;
                    listofFiles.Add(obj);
                }
                catch (FileNotFoundException) { }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                catch (SecurityException) { }
                // Display the filename.
                Console.WriteLine(f);
            });
            var enddate = stdate - DateTime.Now;
            //string[] folderToCheckForFileNames = Directory.GetFiles("F:\\", "*", SearchOption.AllDirectories);
            //List<string> lst = new List<string>();
            //Parallel.ForEach(folderToCheckForFileNames, currentFile =>
            //{
            //    string filename = Path.GetFileName(currentFile);
            //    lst.Add(filename);
            //}
            //    );
            Console.WriteLine("List {0} files in {1} ", listofFiles.Count, enddate.ToString());
        }

        public static void TraverseTreeParallelForEach(string root, Action<string> action)
        {
            //Count of files traversed and timer for diagnostic output
            int fileCount = 0;
            var sw = Stopwatch.StartNew();

            // Determine whether to parallelize file processing on each folder based on processor count.
            int procCount = System.Environment.ProcessorCount;

            // Data structure to hold names of subfolders to be examined for files.
            Stack<string> dirs = new Stack<string>();

            if (!Directory.Exists(root))
            {
                throw new ArgumentException();
            }
            dirs.Push(root);

            while (dirs.Count > 0)
            {
                string currentDir = dirs.Pop();
                string[] subDirs = { };
                string[] files = { };

                try
                {
                    subDirs = Directory.GetDirectories(currentDir);
                }
                // Thrown if we do not have discovery permission on the directory.
                catch (UnauthorizedAccessException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
                // Thrown if another process has deleted the directory after we retrieved its name.
                catch (DirectoryNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }

                try
                {
                    files = Directory.GetFiles(currentDir);
                }
                catch (UnauthorizedAccessException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
                catch (DirectoryNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
                catch (IOException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }

                // Execute in parallel if there are enough files in the directory.
                // Otherwise, execute sequentially.Files are opened and processed
                // synchronously but this could be modified to perform async I/O.
                try
                {
                    if (files.Length < procCount)
                    {
                        foreach (var file in files)
                        {
                            action(file);
                            fileCount++;
                        }
                    }
                    else
                    {
                        Parallel.ForEach(files, () => 0, (file, loopState, localCount) =>
                        {
                            action(file);
                            return (int)++localCount;
                        },
                                         (c) => {
                                             Interlocked.Add(ref fileCount, c);
                                         });
                    }
                }
                catch (AggregateException ae)
                {
                    ae.Handle((ex) => {
                        if (ex is UnauthorizedAccessException)
                        {
                            // Here we just output a message and go on.
                            Console.WriteLine(ex.Message);
                            return true;
                        }
                        // Handle other exceptions here if necessary...

                        return false;
                    });
                }

                // Push the subdirectories onto the stack for traversal.
                // This could also be done before handing the files.
                foreach (string str in subDirs)
                    dirs.Push(str);
            }

            // For diagnostic purposes.
            Console.WriteLine("Processed {0} files in {1} milliseconds", fileCount, sw.ElapsedMilliseconds);
        }
    }
}

