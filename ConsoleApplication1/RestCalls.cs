using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Restful
{
    public class RestCalls
    {
        public static String m_rest_url; 

        public static List<dynamic> GetJobs(Utils.JobFilter jobFilter)
        {
            String res = makeRequest("/RunManager/rest/jobs?filter="+jobFilter , "GET");

            return jsonRes(res);
        }

        public static List<dynamic> GetProcesses()
        {
            String res = makeRequest("/RunManager/rest/processes", "GET");

            return jsonRes(res);
        }

        public static dynamic GetJobStatus(String jobInfoID, String processName)
        {
            //This call actually returns all the times the process has been run on the job
            String res = "[" + makeRequest("/RunManager/rest/jobs/" + jobInfoID + "/process_name/" + processName, "GET") + "]";

            //I only want the most recent one
            return jsonRes(res).First();
        }

        public static bool PostRun(String jobInfoID, String processName)
        {
            try
            {
                String resMess = "config=" + makeRequest("/RunManager/rest/processes/" + processName + "/config_info", "GET");
                String PostResMess = makeRequest("/RunManager/rest/jobs/" + jobInfoID + "/process_name/" + processName, "POST", resMess);
            }
            catch (Exception e)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Makes an HTTP request
        /// </summary>
        /// <param name="extension">rest call</param>
        /// <param name="Method">GET|POST|DELETE</param>
        /// <param name="p_params">param string must be in form 'name=value&name1=value...'</param>
        /// <param name="retries">in case a request fails try 3 times</param>
        /// <returns></returns>
        private static String makeRequest(String extension, String Method, String p_params = "config={}", int retries = 3)
        {
            String resMess = null;

            //Correctly form the http request
            WebRequest req = (HttpWebRequest)WebRequest.Create(m_rest_url + extension);
            req.Method = Method;

            //Writing to a post Method requires some work of converting to a byte array and writing that to the request stream
            if (req.Method == "POST")
            {
                req.ContentType = "application/x-www-form-urlencoded";
                byte[] b = System.Text.Encoding.UTF8.GetBytes(p_params);
                req.ContentLength = b.Length;
                Stream sw = req.GetRequestStream();
                sw.Write(b, 0, b.Length);
            }

            //get response
            WebResponse res = null;
            try
            {
                res = req.GetResponse();
            }
            catch (WebException e)
            {
                //Occasionally REST STOP returns a 400 error this will allow us to try again and move on otherwise
                if (e.Response != null)
                    Console.Error.WriteLine("\n\nFailed in {1}ing {2} with status {3} \n{0}", e.StackTrace, Method, m_rest_url + extension, (int)((HttpWebResponse)e.Response).StatusCode);
                else
                    Console.Error.WriteLine("\n\nFailed in {1}ing {2} \n{0}", e.StackTrace, Method, m_rest_url + extension);

                if (Method == "GET" && retries > 0)
                    resMess = makeRequest(extension, Method, p_params, --retries);
                else if (Method == "POST")
                    throw new Exception("Received a " + (int)((HttpWebResponse)e.Response).StatusCode + " from server");
                else
                    return "{\"status\": \"UNKNOWN\"}";
            }

            //read response string
            if (res != null)
            {
                StreamReader sr = new StreamReader(res.GetResponseStream());
                resMess = sr.ReadToEnd();
            }

            return resMess;
        }

        /// <summary>
        /// converts a string into Json it's only one line but i didn't want to write it over and over should get in-lined on compiling
        /// </summary>
        /// <param name="toConvert"></param>
        /// <returns>always return it in list form for a consistent way to get data across entire program</returns>
        private static List<dynamic> jsonRes(String toConvert)
        {
            return JsonConvert.DeserializeObject<List<dynamic>>(toConvert);
        }
    }
}
