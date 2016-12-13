﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;
using System.Runtime;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Data;
using IrcDotNet;
using IrcDotNet.Collections;
using IrcDotNet.Ctcp;
using System.Text.RegularExpressions;

namespace FoxBot
{
    public class Bot
    {
        //private Client client;
        private IrcDotNet.IrcClient client = new IrcClient();
        private bool isQuitting = false;
        private SqlCommand sqlCom;
        private const string CHANNEL = "#channel";
        private const string NICK = "foxbot";

        public delegate void DataReceivedEventHandler(object sender, EventArgs e);
        public event DataReceivedEventHandler DataReceived;

        public Bot(string nick)
        {
            this.Nick = nick;
            sqlCom = new SqlCommand(string.Empty, new SqlConnection(ConfigurationManager.ConnectionStrings["foxbot"].ConnectionString));
        }

        public string Nick
        {
            get;
            set;
        }

        public void Connect()
        {
            this.client = new IrcClient();
            this.client.ErrorMessageReceived += client_ErrorMessageReceived;
            this.client.Connected += client_Connected;
            this.client.RawMessageReceived += client_RawMessageReceived;
            this.client.ConnectFailed += client_ConnectFailed;
            this.client.MotdReceived += client_MotdReceived;
            this.client.Error += client_Error;
            this.client.ProtocolError += client_ProtocolError;
            this.client.Disconnected += client_Disconnected;
            this.client.NetworkInformationReceived += client_NetworkInformationReceived;
            this.client.ClientInfoReceived += client_ClientInfoReceived;
            this.client.ValidateSslCertificate += client_ValidateSslCertificate;
            IrcUserRegistrationInfo serviceReg = new IrcUserRegistrationInfo();
            serviceReg.RealName = "foxbot";
            serviceReg.UserName = "foxbot";
            serviceReg.NickName = NICK;
            serviceReg.Password = "";
            this.client.Connect("irc.choopa.net", 6667, false, serviceReg);
            while(!isQuitting)
                Thread.Sleep(10000);
        }

        void client_ValidateSslCertificate(object sender, IrcValidateSslCertificateEventArgs e)
        {
            
        }

        void client_ClientInfoReceived(object sender, EventArgs e)
        {
            System.Console.Out.WriteLine(e.ToString());
        }

        void client_NetworkInformationReceived(object sender, EventArgs e)
        {
            System.Console.Out.WriteLine(e.ToString());
        }

        void client_Disconnected(object sender, EventArgs e)
        {
            isQuitting = true;
        }

        void client_ProtocolError(object sender, IrcProtocolErrorEventArgs e)
        {
            System.Console.Out.WriteLine(e.Message.ToString());
        }

        void client_Error(object sender, IrcErrorEventArgs e)
        {
            System.Console.Out.WriteLine(e.Error.ToString());
        }

        void client_ErrorMessageReceived(object sender, IrcErrorMessageEventArgs e)
        {
            System.Console.Out.WriteLine(e.Message.ToString());
        }

        void client_MotdReceived(object sender, EventArgs e)
        {
            this.client.SendRawMessage("JOIN :" + CHANNEL);//JOIN :#channel //PRIVMSG #channel :
        }

        void client_ConnectFailed(object sender, IrcErrorEventArgs e)
        {
            System.Console.Out.WriteLine(e.Error.ToString());
        }

        void client_RawMessageReceived(object sender, IrcRawMessageEventArgs e)
        {
            System.Console.Out.WriteLine(e.RawContent);
            if(!string.IsNullOrWhiteSpace(e.Message.Parameters[1]))
            {
                try
                {
                    sqlCom.Connection.Open();
                    sqlCom.CommandText = "insert into rawmessage (message) values ('" + e.RawContent.Replace("'", "''") + "')";
                    sqlCom.ExecuteNonQuery();
                }
                catch (Exception ex) { Console.Out.WriteLine(ex.Message); }
                finally { sqlCom.Connection.Close(); }
                Listen(e.Message.Parameters[1]);
            }
        }

        void client_Connected(object sender, EventArgs e)
        {
            System.Console.Out.WriteLine(e.ToString());
        }

        public void SendMessage(string message)
        {
            this.client.LocalUser.SendMessage(this.client.Channels[0], message);
        }

        public void Listen(string message)
        {
            Regex reg = new Regex(@"[\w]+");
            int outvar = 0;
            if (message.ToLower().StartsWith(NICK))
                foreach(Match m in reg.Matches(message))
                    switch(m.Value.ToLower())
                    {
                        case "speak":
                            this.client.LocalUser.SendMessage(this.client.Channels[0], "I AM FOXBOT! :V");
                            return;
                            break;
                        case "weather":
                            Match zip = m.NextMatch();
                            if (zip != null &&  zip.Length == 5 && int.TryParse(zip.Value, out outvar))
                            {
                                Weather w = new Weather(outvar.ToString());
                                ParameterizedThreadStart pts = new ParameterizedThreadStart(ProcessRequest);
                                Thread t = new Thread(pts);
                                t.Start(w);
                            }   
                            return;
                            break;
                    }
        }

        protected void ProcessRequest(object o)
        {
            try
            {
                Weather w = (Weather)o;
                w.GetWeather();
                SendMessage(string.Format("{0}°F wind {1} @ {2} MPH with {3} skies", new string[] { w.ImmediateWeather.temp.ToString(), w.ImmediateWeather.windDir.Trim(), w.ImmediateWeather.windSpeed.ToString(), w.ImmediateWeather.clouds.Trim() }));
            }
            catch (Exception e)
            {

            }
            finally
            {
                
            }
        }

        protected void OnData()
        {
            this.DataReceived(this, EventArgs.Empty);
        }
    }
}