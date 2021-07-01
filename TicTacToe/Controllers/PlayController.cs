using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TicTacToe.Controllers
{
	public class PlayController : Controller
	{
		//
		// GET: /Play/
		public ActionResult TicTacToe()
		{
			return View();
		}
	}
}