using System.Threading.Tasks;

namespace SqlServices
{
    internal class ConnectionHelper
    {
        public static bool CheckCanConnect(string hostNameOrAddress)
        {
            try
            {
                System.Net.Dns.GetHostEntry(hostNameOrAddress);
                return true;
            }
            catch
            {
                return false;
            }
        }

        //TODO request command for undo/hand
        internal static async Task<string> Request()
        {
            string result = string.Empty;
            await Task.Run(() =>
            {
                //TODO send request and hand receive
            });
            return result;
        }
        //TODO request message
        internal static async Task<string> SendMessage()
        {
            string result = string.Empty;
            await Task.Run(() =>
            {
                //TODO send request and hand receive
            });
            return result;
        }
    }
}
