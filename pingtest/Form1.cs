using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace pingtest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {


            results = new System.Net.NetworkInformation.PingReply[nPings];
            sendTimes = new double[nPings];
            areGo = new System.Threading.AutoResetEvent(false);
            mreDone = new System.Threading.ManualResetEvent(false);

            nStarted = 0;
            nReady = 0;
            nDone = 0;

            var threads = new List<System.Threading.Thread>();
            for (int i = 0; i < nPings; i++)
            {
                threads.Add(new System.Threading.Thread(() => DoPing()));
                threads[i].Start();
            }
            while (nReady < nPings) { System.Threading.Thread.MemoryBarrier(); System.Threading.Thread.Sleep(1); }

            swMain = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < nPings; i++)
            {
                while (swMain.Elapsed.TotalSeconds < i * spacing) { System.Threading.Thread.Sleep(0); }
                areGo.Set();
            }
            while (nDone < nPings) { System.Threading.Thread.MemoryBarrier(); System.Threading.Thread.Sleep(1); }
            mreDone.Set();
            var sb = new System.Text.StringBuilder();
            var sorted = Enumerable.Range(0, nPings).Select(x => (SendTime: sendTimes[x], RoundTripTime: results[x].RoundtripTime)).OrderBy(x => x.SendTime).ToArray();
            sb.AppendLine($"Sent at(ms)\tRTT(int ms)\tSending {nPings} pings, spaced {spacing} sec apart, to {address})");
            for (int i = 0; i < nPings; i++)
            {
                sb.AppendLine($"{sorted[i].SendTime:000.000000}\t{sorted[i].RoundTripTime:000}");//6dp on a long is just for consistency with send time
            }
            //Clipboard.SetText(sb.ToString());
            txtResult.Text = sb.ToString();


        }

        string address = "1.1.1.1";
        int nPings = 100;
        double spacing = 0.0005f;
        System.Threading.AutoResetEvent areGo;
        System.Threading.ManualResetEvent mreDone;
        volatile int nStarted;
        volatile int nReady;
        volatile int nDone;
        System.Diagnostics.Stopwatch swMain;
        System.Net.NetworkInformation.PingReply[] results;
        double[] sendTimes;


        private void DoPing()
        {
            var myId = System.Threading.Interlocked.Increment(ref nStarted) - 1;
            var myPing = new System.Net.NetworkInformation.Ping();
            System.Threading.Interlocked.Increment(ref nReady);
            areGo.WaitOne();
            sendTimes[myId] = swMain.Elapsed.TotalMilliseconds;
            results[myId] = myPing.Send(address, 3000);
            System.Threading.Interlocked.Increment(ref nDone);
            mreDone.WaitOne();

        }

        private void txtResult_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
