using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace AgricultureApp.Models.Appointment
{
	public class Booking
	{
		[Key]
		public int Id { get; set; }
		public string CustomerName { get; set; }
		public string CustomerSurname { get; set; }
		public string CustomerEmail { get; set; }
		public string CustomerContact { get; set; }
		public string CustomerAddress { get; set; }
		public string ServiceType { get; set; }
		public string Date { get; set; }
		public bool isMade { get; set; }
		public bool atDoor { get; set; }
		public bool isDone { get; set; }
		public bool isCancelRequest { get; set; }
		public bool isCancelled { get; set; }
	}

	public class Fumigation
	{
		[Key]
		public int Id { get; set; }
		public int bookingId { get; set; }
		public string FumService { get; set; }
		public string Treatment { get; set; }
		public double price { get; set; }
		public int NumberOfRooms { get; set; }
		public int width { get; set; }
		public int length { get; set; }
		public double Total { get; set; }
		public bool isPaid { get; set; }
		public bool isDone { get; set; }
		public bool isCancelRequest { get; set; }
		public bool isCancelled { get; set; }
		public byte[] signauture { get; set; }
	}

	public class FumService
	{
		[Key]
		public int Id { get; set; }
		public string service { get; set; }
		public string descr { get; set; }
	}

	public class FumTreatment
	{
		[Key]
		public int Id { get; set; }
		public int ServiceId { get; set; }
		public string Treatment { get; set; }
		public double Price { get; set; }
	}


	public class Cancel
	{
		[Key]
		public int Id { get; set; }
		public int bookingId { get; set; }
		public string Service { get; set; }
		public string treat { get; set; }
		public double price { get; set; }
		public string name { get; set; }
		public string date { get; set; }
		public string email { get; set; }
		public string reason { get; set; }
		public bool isRequested { get; set; }
		public bool isApproved { get; set; }
		public string status { get; set; }
		public string signature { get; set; }
	}

	public class Wallet
	{
		[Key]
		public int Id { get; set; }
		public string Email { get; set; }
		public string Name { get; set; }
		public string Surname { get; set; }
		public double amount { get; set; }
		public double income { get; set; }
		public double expense { get; set; }
	}

}