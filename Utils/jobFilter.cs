using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public class JobFilter
    {
        public string region;
        public string customerName;

        public string Region
        {
            get { return region; }
            set { region = value; }
        }
        public string Customer
        {
            get { return customerName; }
            set { customerName = value; }
        }

        public JobFilter(string p_region = null, string p_customer = null)
        {
            region = p_region;
            customerName = p_customer;
        }

        public string asURLParam()
        {
            var bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            List<string> names = typeof(JobFilter).GetFields(bf).Select(x => x.Name).ToList();
            Dictionary<string, string> vars = new Dictionary<string, string>();
            foreach (var name in names)
            {
                var val = this.GetType().GetField(name).GetValue(this);
                if (val != null) {
                    vars[name] = (string) val;
                }

            }

            return "{" + string.Join(",", vars.Select(x => string.Format("\'{0}\':\'{1}\'", x.Key, x.Value)).ToArray()) + "}";
        }

        public override string ToString()
        {
            return asURLParam();
        }
    }
}
