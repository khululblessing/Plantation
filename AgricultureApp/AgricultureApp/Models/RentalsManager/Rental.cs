using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AgricultureApp.Models.EventsManager
{
	public class Rental
	{
		public int Id { get; set; }
		public string name { get; set; }
		public string surname { get; set; }
		public string email { get; set; }


		public int itemId { get; set; }
		public string itemName { get; set; }
		public byte[] itemImage { get; set; }
		public double itemCost { get; set; }

		public string startDate { get; set; }
		public string endDate { get; set; }
		public int days { get; set; }
		public double total { get; set; }

		public bool isRequested { get; set; }
		public bool isPaid { get; set; }
		public bool isConfirmed { get; set; }
		public bool isCollected { get; set; }
		public string signature { get; set; }
		public string assistantSign { get; set; }
		public bool isReturned { get; set; }
	}
}