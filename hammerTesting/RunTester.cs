using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Restful;
using System.Threading;

namespace hammerTesting
{
    abstract class RunTester
    {
        protected List<dynamic> m_jobs = null; //Check REST STOP's help call for more details
        protected List<dynamic> m_processes = null;
        protected Dictionary<string, int> m_statsMap;
        protected string m_process;

        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="p_rest_url"></param>
        public RunTester(string p_rest_url, Utils.JobFilter p_params, string p_process)
        {
            RestCalls.m_rest_url = p_rest_url;
           
            m_jobs = RestCalls.GetJobs(p_params);
            m_processes = RestCalls.GetProcesses();
            m_process = p_process;
            if (!m_processes.Contains(m_process))
                m_jobs.Clear();
            m_statsMap = new Dictionary<string, int>();
            m_statsMap.Add("Total", m_jobs.Count);
            m_statsMap.Add("Running", 0);
            m_statsMap.Add("Successful", 0);
            m_statsMap.Add("Failed", 0);
            m_statsMap.Add("Unavailable", 0);
        }

        /// <summary>
        /// Keeps track of the running, successful and failed jobs. Check RunManager for further details
        /// </summary>
        /// <param name="jobInfoID"></param>
        /// <param name="processName"></param>
        /// <returns>Whether the job was successful or not</returns>
        protected void runJob(string jobInfoID, string processName)
        {
            dynamic status = RestCalls.GetJobStatus(jobInfoID, processName);
            if (status.status == "COMPLETE")
            {
                m_statsMap["Successful"]++;
                return;
            }

            bool posted = RestCalls.PostRun(jobInfoID, processName);

            if (posted == true)
            {
                m_statsMap["Running"]++;
                while (!isJobComplete(jobInfoID, processName))
                { System.Threading.Thread.Sleep(30000); }

                m_statsMap["Running"]--;
            }
            else
                m_statsMap["Failed"]++;
        }

        /// <summary>
        /// Queries REST STOP to see if the job is completed then updates the status
        /// </summary>
        /// <param name="jobInfoID"></param>
        /// <param name="processName"></param>
        /// <returns>bool saying whether the job is no longer running</returns>
        protected bool isJobComplete(string jobInfoID, string processName)
        {
            dynamic status = RestCalls.GetJobStatus(jobInfoID, processName);
            string compStatus = status.status;
            if (compStatus != "QUEUED" && compStatus != "IN_PROGRESS" && compStatus != "LAUNCHING")
            {
                if (compStatus == "COMPLETE")
                    m_statsMap["Successful"]++;
                else
                    m_statsMap["Failed"]++;
                return true;
            }
                
            return false;
        }

        /// <summary>
        /// Determines if the tester has completed all the jobs
        /// </summary>
        /// <returns></returns>
        public bool isDone()
        {
            return m_statsMap["Successful"] + m_statsMap["Failed"] + m_statsMap["Unavailable"] == m_statsMap["Total"];
        }

        public void printStatus()
        {
            Console.WriteLine("There are {0} total jobs, {1} are running, {2} have succeeded, {3} have failed and {4} were unavailable",
                               m_statsMap["Total"], m_statsMap["Running"], m_statsMap["Successful"], m_statsMap["Failed"], m_statsMap["Unavailable"]);
        }

        abstract public void queueJobs();
    }

    /// <summary>
    /// Implements queueJobs for serial testing
    /// </summary>
    class SerialRunTester : RunTester
    {
        public SerialRunTester(string p_rest_url, Utils.JobFilter p_params, string p_process)
            : base(p_rest_url, p_params, p_process)
        { }

        /// <summary>
        /// Runs the jobs then waits for it to finish before moving on
        /// </summary>
        override public void queueJobs()
        {
            if (m_processes.Contains(m_process))
            {
                foreach(dynamic d in m_jobs)
                {
                    Console.WriteLine(d.modeInfo.mode);
                    if (d.modeInfo.mode == "AVAILABLE")
                    {
                        runJob((string)d.jobInfoID, m_process);
                    }
                    else
                        m_statsMap["Unavailable"]++;
                }
            }
        }
    }

    /// <summary>
    /// Implements queueJobs for double input testing
    /// </summary>
    class DoubleInputTester : RunTester
    {
        public DoubleInputTester(string p_rest_url, Utils.JobFilter p_params, string p_process)
            : base(p_rest_url, p_params, p_process)
        { }

        /// <summary>
        /// Runs the jobs twice (checking for double input) then waits for it to finish before moving on
        /// </summary>
        override public void queueJobs()
        {
            m_statsMap["Total"] *= 2;
            if (m_processes.Contains(m_process))
            {
                foreach (dynamic d in m_jobs)
                {
                    Console.WriteLine(d.modeInfo.mode);
                    if (d.modeInfo.mode == "AVAILABLE")
                    {
                        new Thread(() =>
                        {
                            runJob((string)d.jobInfoID, m_process);
                        }).Start();
                        runJob((string)d.jobInfoID, m_process);
                    }
                    else
                        m_statsMap["Unavailable"]+=2;
                }
            }
        }
    }

    /// <summary>
    /// Implements queueJobs for concurrent testing
    /// </summary>
    class ConcurrentRunTester : RunTester
    {
        public ConcurrentRunTester(string p_rest_url, Utils.JobFilter p_params, string p_process)
            : base(p_rest_url, p_params, p_process)
        { }

        /// <summary>
        /// spawns a thread for each job running them immediately
        /// </summary>
        override public void queueJobs()
        {
            if (m_processes.Contains(m_process))
            {
                foreach (dynamic d in m_jobs)
                {
                    Console.WriteLine(d.modeInfo.mode);
                    if (d.modeInfo.mode == "AVAILABLE")
                    {
                        new Thread(() =>
                        {
                            runJob((string)d.jobInfoID, m_process);
                        }).Start();
                    }
                    else
                        m_statsMap["Unavailable"]++;
                }
            }
        }
    }

    /// <summary>
    /// Implements queueJobs for form a list which then gets randomized
    /// </summary>
    class RandomRunTester : RunTester
    {
        private const int HOUR = 60 * 60 * 1000;

        public RandomRunTester(string p_rest_url, Utils.JobFilter p_params, string p_process)
            : base(p_rest_url, p_params, p_process)
        { }

        /// <summary>
        /// puts unstarted threads into a list to get randomly started
        /// </summary>
        override public void queueJobs()
        {
            List<Thread> threads = new List<Thread>();
            if (m_processes.Contains(m_process))
            {
                foreach (dynamic d in m_jobs)
                {
                    Console.WriteLine(d.modeInfo.mode);
                    if (d.modeInfo.mode == "AVAILABLE")
                    {
                        threads.Add(new Thread(() =>
                        {
                            runJob((string)d.jobInfoID, m_process);
                        }));
                    }
                    else
                        m_statsMap["Unavailable"]++;
                }

                randomize(threads);
            }
        }

        /// <summary>
        /// Randomly select a thread then randomly start it over the week.
        /// </summary>
        /// <param name="threads"></param>
        private void randomize(List<Thread> threads)
        {
            int jobNum = 0;
            int index;
            Random r = new Random();

            while (threads.Count > 0)
            {
                Thread.Sleep(jobNum++ * r.Next(2, 6) * HOUR);
                index = r.Next(0, threads.Count());
                threads.ElementAt(index).Start();
                threads.RemoveAt(index);
            }
        }
    }
}
