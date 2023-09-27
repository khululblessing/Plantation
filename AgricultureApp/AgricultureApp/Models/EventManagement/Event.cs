using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AgricultureApp.Models.EventManagement
{
    public class Event
    {
        public int Id { get; set; }

		public int VenueId { get; set; }
		public string vName { get; set; }
		public int vCapacity { get; set; }
		public int ticketSold { get; set; }
		public int attended { get; set; }

		public string Title { get; set; }
        public string Description { get; set; }

        public string Date { get; set; }
		public int day { get; set; }
		public int month { get; set; }
		public int year { get; set; }
		public string Time { get; set; }

        public bool IsFree { get; set; }
        public double TicketPrice { get; set; }
        public string target { get; set; }

        public byte[] EventImageUrl { get; set; }
		public bool isStarted { get; set; }
		public bool isEnded { get; set; }
		public string signature { get; set; }
	}

    public class Ticket
	{
		public int Id { get; set; }
		public int eventId { get; set; }
		public int code { get; set; }
		public byte[] codeImage { get; set; }
		public int seatNo { get; set; }
		public string email { get; set; }
		public bool isChecked { get; set; }
	}

    public class Reciept
	{
		public int Id { get; set; }
		public int eventId { get; set; }
		public string email { get; set; }
		public double total { get; set; }
	}

}