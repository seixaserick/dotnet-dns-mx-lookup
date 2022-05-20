
/*

Part of this code was written by @Jaimon Mathew. Thanks Jaimon for your contributoin!

Changes and improvements were made by <seixaserick77@gmail.com> to optimize the code for .NET 6.0 and implement some coding conventions.

 */

using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DnsLib
{

    public class MXRecord
    {
        public int preference = -1;
        public string? mailServer = null;
        
        public override string ToString()
        {
            return "Preference: " + preference.ToString().PadLeft(3) + " Mail Server: " + mailServer;
        }
    }

    public class DnsLite
    {
        private byte[] data;
        private int position, id, length;
        private string name;
        private List<string> dnsServers;

        private static int DNS_PORT = 53; //DNS protocol uses UDP 53 port by Default

        Encoding ASCII = Encoding.ASCII;

        public DnsLite()
        {
            id = DateTime.Now.Millisecond * 60;
            dnsServers = new List<string>();
        }

        public void setDnsServers(List<string> dnsServers)
        {
            this.dnsServers = dnsServers;
        }
        public List<MXRecord> GetMXRecords(string host)
        {
            List<MXRecord> mxRecords = new();
            for (int i = 0; i < dnsServers.Count; i++)
            {
                try
                {
                    mxRecords = GetMXRecords(host, (string)dnsServers[i]);
                    break;
                }
                catch (IOException)
                {
                    continue;
                }
            }
            return mxRecords;
        }
        private int getNewId()
        {
            //return a new id
            return ++id;
        }

        public List<MXRecord> GetMXRecords(string host, string serverAddress)
        {
            //opening the UDP socket at DNS server
            //use UDPClient, if you are still with Beta1
            UdpClient dnsClient = new UdpClient(serverAddress, DNS_PORT);

            //preparing the DNS query packet.
            MakeQuery(getNewId(), host);

            //send the data packet
            dnsClient.Send(data, data.Length);

            IPEndPoint endpoint = null;
            //receive the data packet from DNS server
            data = dnsClient.Receive(ref endpoint);

            length = data.Length;

            //unpack the byte array & makes an array of MXRecord objects.
            return MakeResponse();

        }

        //for packing the information to the format accepted by server
        public void MakeQuery(int id, String name)
        {

            data = new byte[512];

            for (int i = 0; i < 512; ++i)
            {
                data[i] = 0;
            }

            data[0] = (byte)(id >> 8);
            data[1] = (byte)(id & 0xFF);
            data[2] = (byte)1; data[3] = (byte)0;
            data[4] = (byte)0; data[5] = (byte)1;
            data[6] = (byte)0; data[7] = (byte)0;
            data[8] = (byte)0; data[9] = (byte)0;
            data[10] = (byte)0; data[11] = (byte)0;

            string[] tokens = name.Split(new char[] { '.' });
            string label;

            position = 12;

            for (int j = 0; j < tokens.Length; j++)
            {

                label = tokens[j];
                data[position++] = (byte)(label.Length & 0xFF);
                byte[] b = ASCII.GetBytes(label);

                for (int k = 0; k < b.Length; k++)
                {
                    data[position++] = b[k];
                }

            }

            data[position++] = (byte)0; data[position++] = (byte)0;
            data[position++] = (byte)15; data[position++] = (byte)0;
            data[position++] = (byte)1;

        }

        //for unpacking the byte array
        public List<MXRecord> MakeResponse()
        {

            List<MXRecord> mxRecords = new();
            MXRecord mxRecord;

            //NOTE: we are ignoring the unnecessary fields.
            //		and takes only the data required to build
            //		MX records.

            int qCount = ((data[4] & 0xFF) << 8) | (data[5] & 0xFF);
            if (qCount < 0)
            {
                throw new IOException("invalid question count");
            }

            int aCount = ((data[6] & 0xFF) << 8) | (data[7] & 0xFF);
            if (aCount < 0)
            {
                throw new IOException("invalid answer count");
            }

            position = 12;

            for (int i = 0; i < qCount; ++i)
            {
                name = "";
                position = proc(position);
                position += 4;
            }

            for (int i = 0; i < aCount; ++i)
            {

                name = "";
                position = proc(position);

                position += 10;

                int pref = (data[position++] << 8) | (data[position++] & 0xFF);

                name = "";
                position = proc(position);

                mxRecord = new MXRecord();

                mxRecord.preference = pref;
                mxRecord.mailServer = name;

                if (mxRecord.mailServer == "localhost")
                {
                    continue; //protection for some cases where owner adds invalid localhost MX servers
                }

                mxRecords.Add(mxRecord);

            }

            return mxRecords;
        }

        private int proc(int position)
        {

            int len = (data[position++] & 0xFF);

            if (len == 0)
            {
                return position;
            }

            int offset;

            do
            {
                if ((len & 0xC0) == 0xC0)
                {
                    if (position >= length)
                    {
                        return -1;
                    }
                    offset = ((len & 0x3F) << 8) | (data[position++] & 0xFF);
                    proc(offset);
                    return position;
                }
                else
                {
                    if ((position + len) > length)
                    {
                        return -1;
                    }
                    name += ASCII.GetString(data, position, len);
                    position += len;
                }

                if (position > length)
                {
                    return -1;
                }

                len = data[position++] & 0xFF;

                if (len != 0)
                {
                    name += ".";
                }
            } while (len != 0);

            return position;
        }
    }
}
