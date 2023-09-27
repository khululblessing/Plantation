using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AgricultureApp.Models.EventManagement
{
	public class Venue
	{
		public int Id { get; set; }
		public string name { get; set; }
		public int capacity { get; set; }
		public string seatCode { get; set; }
	}
}