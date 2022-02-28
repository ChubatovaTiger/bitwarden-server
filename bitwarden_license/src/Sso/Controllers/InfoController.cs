﻿using System;
using Bit.SharedKernel.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace Bit.Sso.Controllers
{
    public class InfoController : Controller
    {
        [HttpGet("~/alive")]
        [HttpGet("~/now")]
        public DateTime GetAlive()
        {
            return DateTime.UtcNow;
        }

        [HttpGet("~/version")]
        public JsonResult GetVersion()
        {
            return Json(VersionHelper.GetVersion());
        }
    }
}
