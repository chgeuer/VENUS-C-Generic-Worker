//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.EMIC.Cloud.GenericWorker;
using System.ComponentModel.Composition;
using ManagementWeb;
using MVCWIFHelpers;
using PagedList;
using Microsoft.EMIC.Cloud.GenericWorker.AzureSecurity;
using Microsoft.WindowsAzure;
using System.ComponentModel.Composition.Hosting;
using Microsoft.EMIC.Cloud;

namespace SecureJobManagement.Controllers
{
    public class HomeController : Controller
    {

        [Import(typeof(IGWRuntimeEnvironment))]
        public IGWRuntimeEnvironment RuntimeEnvironment { get; set; }

        public HomeController()
        {
            AppStart_MefContribMVC3.Container.SatisfyImportsOnce(this);
        }

        internal UserTableTableDataContext CreateUserTableContext(CloudStorageAccount account, string userTableTableName)
        {
            return new UserTableTableDataContext(account.TableEndpoint.AbsoluteUri, account.Credentials,
                                                 userTableTableName);
        }
        
        [Authorize]
        public ActionResult Index(int? page, int? pagesize)
        {
            var CompositionContainer = AppStart_MefContribMVC3.Container;

            var userTableConnectionString = CompositionContainer.GetExportedValue<string>(CompositionIdentifiers.STSOnAzureConnectionString);
            var userTableTableName = CompositionContainer.GetExportedValue<string>(CompositionIdentifiers.DevelopmentSecurityTokenServiceUserTableName);
            var account = CloudStorageAccount.Parse(userTableConnectionString);

            var ctx = CreateUserTableContext(account, userTableTableName);

            var pageNumber = page ?? 1; // if no page was specified in the querystring, default to the first page (1)
            var ps = pagesize ?? 10;
            var owner = this.HttpContext.User.Identity.Name;

            var user = ctx.Users.Where(u => u.RowKey == owner).FirstOrDefault();
            PagedList.IPagedList model;
            ViewBag.NumOwnTerminatedJobs = this.RuntimeEnvironment.AllJobsOfOwner(owner).Where(j=> j.Status == JobStatus.Finished || j.Status == JobStatus.Failed || j.Status==JobStatus.Cancelled).AsQueryable().Count();

            if (user!=null && user.IsComputeAdministrator) //only admins are allowed to see everything
            {
                model = this.RuntimeEnvironment.AllJobs.AsQueryable().Where(j => !JobID.IsGroupHead(j.CustomerJobID)).OrderBy(j => j.Submission).ToPagedList(pageNumber, ps);
            }
            else //even in unsecured mode users may want to login to only see their own jobs
            {
                model = this.RuntimeEnvironment.AllJobsOfOwner(owner).AsQueryable().OrderBy(j => j.Submission).ToPagedList(pageNumber, ps);
            }
            return View(model);
        }

        public ActionResult Details(string internalJobId)
        {
            IJob job = this.RuntimeEnvironment.GetJobByID(internalJobId);
            if (job == null)
                return RedirectToAction("Index");
            return View(job);
        }

        public ActionResult Stop(string internalJobId)
        {
            IJob job = RuntimeEnvironment.GetJobByID(internalJobId);
            if (job == null || job.Status == JobStatus.CancelRequested)
                return RedirectToAction("Index");

            return View(job);
        }

        [HttpPost]
        public ActionResult Stop(string internalJobId, FormCollection collection)
        {
            var job = this.RuntimeEnvironment.CurrentJobs.Where(jb => jb.InternalJobID == internalJobId).SingleOrDefault();
            if (job != null)
            {
                if (job.Status != JobStatus.Running)
                {
                    this.RuntimeEnvironment.MarkJobAsCancelled(job, "job was cancelled");
                }
                else
                {
                    this.RuntimeEnvironment.MarkJobAsCancellationPending(job);
                }
            }

            if (Request != null && Request.UrlReferrer != null)
            {
                return Redirect(Request.UrlReferrer.AbsoluteUri);
            }
            else
            {
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public RedirectToRouteResult ClearTable(string owner)
        {
            this.RuntimeEnvironment.DeleteTerminatedJobs(owner);

            return RedirectToAction("Index");
        }
        public ActionResult About()
        {
            return View();
        }
    }
}
