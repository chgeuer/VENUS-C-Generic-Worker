//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Principal;
using System.Net.Mail;

namespace SimpleMailNotificationConsoleApplication
{
    class Program
    {
        private const string USAGE = @"SimpleMailNotificationConsoleApp.exe -triggerfile <filename> -subject <subject> -message <messagetext> -from <sender-mailaddress> -to <recepient-mailaddress> -smtphost <hostname> -port <portnumber> -user <username> -passwd <password> [-ssl]";
        //this job only starts if the triggerfile is available at the userstorage, hence it can be used as a notifcation tool
        static void Main(string[] args)
        {
            Console.WriteLine("Mail notification job running as user {0}", WindowsIdentity.GetCurrent().Name);
            
            Func<string, string> GetArg = (name) =>
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (string.Equals(name, args[i]))
                        return args[i + 1];
                }
                return string.Empty;
            };


            string message = GetArg("-message");
            string subject = GetArg("-subject");
            string recepient = GetArg("-to");

            var mailClient = new SmtpClient();

            mailClient.Host = GetArg("-smtphost");           
            mailClient.EnableSsl = (GetArg("-ssl")!=string.Empty);
            mailClient.Port= Int16.Parse(GetArg("-port"));
            
            mailClient.Credentials = new System.Net.NetworkCredential(GetArg("-user"), GetArg("-passwd"));
            mailClient.TargetName = mailClient.Host;
            mailClient.UseDefaultCredentials = false;

            Console.WriteLine("sending a notification with subject {0} to {1} using smtphost {2} on port {3} with sslEnabled set to {4}", subject, recepient, mailClient.Host, mailClient.Port, mailClient.EnableSsl);
            mailClient.Send(GetArg("-from"), recepient, subject, message);
        }
    }
}
