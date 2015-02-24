using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace hammerTesting
{
    public class Program
    {
        public const string PROCESSNAME = "ScannerSuite";

        static void Main(string[] args)
        {
            String usage = "hammerTesting.exe {options}\n" +
                "\t --help (-h) displays help message\n" +
                "\t --mode (-m) Mode to run ['serially' (s), 'concurrently' (c), 'random' (r), 'double' (d)]\n" +
                "\t --rest_url (-r) Url for the rest stop you are connecting to ie. http://slc-ub-v5-test:8080\n" + 
                "\t --region (-g) Region your runManager is in\n" + 
                "\t --customer (-c) Costumer of Jobs\n";
                

            string mode = null;
            string rest_url = null;
            Utils.JobFilter jf = new Utils.JobFilter();
            RunTester tester = null;
            Thread testerThread;

            //Parse commandline
            for (int i = 0; i < args.Count(); ++i)
            {
                if (args[i] == "-h" || args[i] == "--help")
                {
                    Console.WriteLine(usage);
                    return;
                }
                else if (args[i] == "--mode" || args[i] == "-m")
                    mode = args[++i];
                else if (args[i] == "--rest_url" || args[i] == "-r")
                    rest_url = args[++i];
                else if (args[i] == "--region" || args[i] == "-g")
                    jf.Region = args[++i];
                else if (args[i] == "--customer" || args[i] == "-c")
                    jf.Customer = args[++i];
                else
                    throw new IOException("Incorrect parameter Usage: " + usage);
            }

            //instantiate appropriate subclass
            try
            {
                if (rest_url != null && Regex.Match(rest_url, @"^http://[a-z A-Z 0-9 -]+:[0-9{4}$]").Success)
                {
                    mode.ToLower();
                    if (rest_url.EndsWith("/"))
                        rest_url = rest_url.Substring(0, rest_url.Length - 2);
                    if (mode[0] == 's')
                    {
                        mode = "Sequential";
                        testerThread = new Thread(() =>
                        {
                            tester = new SerialRunTester(rest_url, jf, PROCESSNAME);
                            tester.queueJobs();
                        });
                        testerThread.Start();
                    }
                    else if (mode[0] == 'c')
                    {
                        mode = "Concurrent";
                        testerThread = new Thread(() =>
                        {
                            tester = new ConcurrentRunTester(rest_url, jf, PROCESSNAME);
                            tester.queueJobs();
                        });
                        testerThread.Start();
                    }
                    else if (mode[0] == 'r')
                    {
                        mode = "Random";
                        testerThread = new Thread(() =>
                        {
                            tester = new RandomRunTester(rest_url, jf, PROCESSNAME);
                            tester.queueJobs();
                        });
                        testerThread.Start();
                    }
                    else if (mode[0] == 'd')
                    {
                        mode = "Double Input";
                        testerThread = new Thread(() =>
                        {
                            tester = new DoubleInputTester(rest_url, jf, PROCESSNAME);
                            tester.queueJobs();
                        });
                        testerThread.Start();
                    }
                    else
                        throw new IOException("Incorrect mode selection Usage: " + usage);
                }
                else
                    throw new IOException("Rest URL is null or invalid. Usage: " + usage);

                //wait for the tester to finish instantiating
                while (tester == null)
                    ;

                //report status of run every 30 seconds
                while (!tester.isDone())
                {
                    tester.printStatus();
                    System.Threading.Thread.Sleep(30000);
                }

                Console.WriteLine("Completed the testing in {0} mode", mode);
                tester.printStatus();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + " " + e.StackTrace);
            }
        }
    }
}
