using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using AgricultureApp.Models;
using AgricultureApp.Models.Appointment;
using AgricultureApp.Models.Product;
using Microsoft.AspNet.Identity;

namespace AgricultureApp.Controllers.Appointment
{
    public class BookingsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Bookings
        [Authorize]
        public ActionResult Index(int id)
        {
            return View(db.Bookings.Where(d => d.Id == id).ToList());
        }

        [Authorize]
        public ActionResult Deliveries()
        {
            var user = User.Identity.GetUserName();
            return View(db.AppDeliveries.Where(d => d.FumEmail == user).ToList());
        }

        [Authorize]
        public ActionResult BookingIndex()
        {
            var user = User.Identity.GetUserName();
            return View(db.Bookings.Where(d => d.CustomerEmail == user && d.isMade == true).ToList());
        }

        public ActionResult BookingInvoice(int id)
        {
            var book = db.Bookings.Find(id);
            ViewBag.Name = book.CustomerName;
            ViewBag.Surname = book.CustomerSurname;
            ViewBag.Email = book.CustomerEmail;
            return View(db.Fumigations.Where(d => d.bookingId == id).ToList());
        }

        // GET: Bookings/Details/5
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Booking booking = db.Bookings.Find(id);
            if (booking == null)
            {
                return HttpNotFound();
            }
            return View(booking);
        }

        // GET: Bookings/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Bookings/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,CustomerName,CustomerSurname,CustomerEmail,CustomerContact,CustomerAddress,ServiceType,Date")] Booking booking, DateTime Date)
        {
            if (ModelState.IsValid)
            {
                booking.Date = Date.ToString("dddd, dd MMMM yyyy");
                db.Bookings.Add(booking);
                db.SaveChanges();
                return RedirectToAction("PickIndex", "FumServices", new { id = booking.Id });

            }

            return View(booking);
        }

        // GET: Bookings/Edit/5
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Booking booking = db.Bookings.Find(id);
            if (booking == null)
            {
                return HttpNotFound();
            }
            return View(booking);
        }

        // POST: Bookings/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,CustomerName,CustomerSurname,CustomerEmail,CustomerContact,CustomerAddress,ServiceType,Date")] Booking booking)
        {
            if (ModelState.IsValid)
            {
                db.Entry(booking).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(booking);
        }

        // GET: Bookings/Delete/5
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Booking booking = db.Bookings.Find(id);
            if (booking == null)
            {
                return HttpNotFound();
            }
            return View(booking);
        }

        // POST: Bookings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            Booking booking = db.Bookings.Find(id);
            db.Bookings.Remove(booking);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        public ActionResult Start(int id)
        {
            var del = db.AppDeliveries.Find(id);
            ViewBag.Address = del.Address;
            return View();
        }

		[HttpPost]
        public ActionResult Start(int id, int bookId)
        {
            var del = db.AppDeliveries.Find(id);
            var book = db.Bookings.Where(d => d.Id == bookId).FirstOrDefault();
            del.isPickUp = true;
            book.atDoor = true;

            db.Entry(book).State = EntityState.Modified;
            db.Entry(del).State = EntityState.Modified;
            db.SaveChanges();
            TempData["Deliveries"] = "Arrived, inform the customer to enter their fumigation code to start";
            return RedirectToAction("Deliveries");
        }

        public ActionResult StartFum()
        {
            return View();
        }

		[HttpPost]
        public ActionResult StartFum(string pass, int id)
        {
            var del = db.AppDeliveries.Any(d => d.pass == pass && d.Id==id);
            var deli = db.AppDeliveries.Where(d => d.pass == pass && d.Id==id).FirstOrDefault();

            if (del)
            {
                deli.isConfirmed = true;
                db.Entry(deli).State = EntityState.Modified;
                db.SaveChanges();
                TempData["Deliveries"] = "The client has been confirmed, you may start fumigating.";
                return RedirectToAction("Deliveries");
            }
            else
            {
                TempData["StartFum"] = "Invalid start password, please try again";
                return RedirectToAction("StartFum");
            }
        }


        public ActionResult Finish(int id)
		{
            var b = db.Bookings.Find(id);
           ViewBag.Name= b.CustomerName;
            ViewBag.ID = id;
            return View();
		}

		[HttpPost]
        public ActionResult Finish(int id, string signature)
		{
            var book = db.Bookings.Find(id);
            var del = db.AppDeliveries.Where(d => d.orderId == book.Id).FirstOrDefault();
            var details = db.Fumigations.Where(d => d.bookingId == book.Id).FirstOrDefault();


            details.signauture = Convert.FromBase64String(signature);
            del.isDelivered = true;
            book.isDone = true;
            details.isDone = true;

            db.Entry(del).State = EntityState.Modified;
            db.Entry(details).State = EntityState.Modified;
            db.Entry(book).State = EntityState.Modified;
            db.SaveChanges();
            TempData["DeliveryNote"] = "This appointment has been finished";
            return RedirectToAction("DeliveryNote", new {id=details.Id});
		}


        public ActionResult DeliveryNote(int id)
		{
            var details = db.Fumigations.Where(d => d.Id == id).ToList();
            return View(details);
		}

        public ActionResult Cancel(int id)
		{
            var f = db.Fumigations.Find(id);
            var b = db.Bookings.Find(f.bookingId);
            ViewBag.Name = b.CustomerName;
            return View();
		}
		[HttpPost]
        public ActionResult Cancel(int id, string reason)
		{
			if (!string.IsNullOrEmpty(reason))
			{
                var f = db.Fumigations.Find(id);
                var b = db.Bookings.Find(f.bookingId);
                var c = new Cancel
                {
                    bookingId = b.Id,
                    name = b.CustomerName,
                    email = b.CustomerEmail,
                    Service = b.ServiceType,
                    treat = f.Treatment,
                    price = f.Total,
                    reason=reason,
                    isRequested=true,
                    date=b.Date,
                    status="Requested"
                };
                f.isCancelRequest = true;
                b.isCancelRequest = true;
                db.Cancels.Add(c);
                db.Entry(f).State = EntityState.Modified;
                db.Entry(b).State = EntityState.Modified;
                db.SaveChanges();
                TempData["Cancels"] = "Cancel request sent, please wait while the store reviews the request";
                return RedirectToAction("Cancels", new { c.Id });
            }
            return View();
		}

        public ActionResult Cancels()
		{
			if (User.IsInRole("Customer"))
			{
                var user = User.Identity.GetUserName();
                var b = db.Cancels.Where(d => d.email == user).ToList();
                return View(b);
            }
            var book = db.Cancels.ToList();
            return View(book);
		}

        public ActionResult Review(int Id)
		{
            return View(db.Cancels.Where(d=>d.Id==Id).ToList());
		}

		[HttpPost]
        public ActionResult Review(int Id, string rejreason, string decision)
        {
            var c = db.Cancels.Find(Id);
            if (decision == "approve" && string.IsNullOrEmpty(rejreason))
            {
                c.status = "Approved";
                c.isApproved = true;
                var callbackUrl = Url.Action("Confirm", "Bookings", new { Id }, protocol: Request.Url.Scheme);
                MailMessage mm = new MailMessage("Plantationgrp33@gmail.com", c.email);
                mm.Subject = "Cancell Approved";
                mm.Body = "<p>Hello " + c.name + ",</p>" +
                "<p>Please note that your request to cancell the appointment has been accepted, please follow the following link and confirm your cancellation to complete.</p>" +
                 "<ul style=\"list-style:none\">" +
                      "     <li><a style=\"color:white;background-color:yellow; padding:2%; border-radius:5px;text-decoration:none\" href=\"" + callbackUrl + "\">Confirm Cancellation<a></li>" +
                      "</ul>" +
                "<p>Best regards,</p>" +
                "<p>Plantation Management</p>";

                mm.IsBodyHtml = true;

                SmtpClient smtp = new SmtpClient();
                smtp.Host = "smtp.gmail.com";
                smtp.Port = 587;
                smtp.EnableSsl = true;

                NetworkCredential nc = new NetworkCredential("Plantationgrp33@gmail.com", "jkmabstufjzldlsc");
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = nc;
                smtp.Send(mm);
                db.Entry(c).State = EntityState.Modified;
                db.SaveChanges();
                TempData["Cancels"] = "The customer has been notified of the store decision";
                return RedirectToAction("Cancels");
            }
            else if (decision == "reject" && !string.IsNullOrEmpty(rejreason))
            {
                c.status = "Rejected";
                c.isApproved = true;
                MailMessage mm = new MailMessage("Plantationgrp33@gmail.com", c.email);
                mm.Subject = "Cancell Rejected";
                mm.Body = "<p>Hello " + c.name + ",</p>" +
                "<p>" + rejreason + "</p>" +
                "<p>Best regards,</p>" +
                "<p>Plantation Management</p>";

                mm.IsBodyHtml = true;

                SmtpClient smtp = new SmtpClient();
                smtp.Host = "smtp.gmail.com";
                smtp.Port = 587;
                smtp.EnableSsl = true;

                NetworkCredential nc = new NetworkCredential("Plantationgrp33@gmail.com", "jkmabstufjzldlsc");
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = nc;
                smtp.Send(mm);
                db.Entry(c).State = EntityState.Modified;
                db.SaveChanges();
                TempData["Cancels"] = "The customer has been notified of the store decision";
                return RedirectToAction("Cancels");
            }
			else
			{
                TempData["Review"] = "Something went wrong";
                return RedirectToAction("Review", new {id=Id});
            }
		}


		[Authorize(Roles ="Customer")]
        public ActionResult Confirm(int Id)
        {
            return View(db.Cancels.Where(d => d.Id == Id).ToList());
        }

		[HttpPost]
        public ActionResult Confirm(int Id, string signature)
        {
			if (!string.IsNullOrEmpty(signature))
			{
                var c = db.Cancels.Find(Id);
                c.signature = signature;
                c.status = "CANCELLED";

                var w = db.Wallets.Where(d => d.Email == c.email).FirstOrDefault();
                w.amount += c.price;
                w.income += c.price;

                db.Entry(c).State = EntityState.Modified;
                db.Entry(w).State = EntityState.Modified;
                db.SaveChanges();
                TempData["MyWallet"] = "Your appointment has been cancelled." +c.price.ToString("R0.00")+ " has been added to your E-wallet";
                return RedirectToAction("MyWallet");
            }
			else
			{
                TempData["Confirm"] = "Something went wrong.";
                return RedirectToAction("Confirm",new { id = Id });
            }
        }

        public ActionResult MyWallet()
        {
            var user = User.Identity.GetUserName();
            var w = db.Wallets.Where(d => d.Email == user).ToList();
            var wallet = db.Wallets.Where(d => d.Email == user).FirstOrDefault();
            if (wallet == null)
            {
                var customer = db.Customers.FirstOrDefault(d => d.Email == user);
                if (customer != null)
                {
                    Wallet newWallet = new Wallet
                    {
                        Email = customer.Email,
                        Name = customer.FirstName,
                        Surname = customer.LastName,
                        amount = 0.0,
                        income = 0.0,
                        expense = 0.0,
                    };
                    db.Wallets.Add(newWallet);
                    db.SaveChanges();
                    wallet = newWallet;
                }
            }
            return View(w);
        }


        public ActionResult Pay(double TotalPrice, int id, int bookId)
        {
            var user = User.Identity.GetUserName();
            var w = db.Wallets.Where(d => d.Email == user).FirstOrDefault();
			if (w.amount < TotalPrice)
			{
                TempData["Index"] = "Insufficient balance in your e-wallet account make this payment, please use another payment method";
                return RedirectToAction("Index","Fumigations", new { id = bookId });
            }
			else
			{
                var gameOrder = db.Fumigations.Where(b => b.Id == id).FirstOrDefault();
                var book = db.Bookings.Where(b => b.Id == bookId).FirstOrDefault();
                gameOrder.isPaid = true;

                // Get a list of all drivers from the drivers table
                var drivers = db.Assistants.ToList();

                // Count the number of deliveries for each driver
                var driverTasks = db.AppDeliveries.GroupBy(d => d.FumEmail)
                                               .Select(g => new { DriverEmail = g.Key, TaskCount = g.Count() })
                                               .ToList();

                // Order drivers by the number of deliveries they have, in ascending order
                var orderedDrivers = drivers.OrderBy(d => driverTasks.FirstOrDefault(t => t.DriverEmail == d.Email)?.TaskCount ?? 0);

                // Select the driver who appears the least in the delivery table
                var selectedDriver = orderedDrivers.FirstOrDefault();

                const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()_-+={}[]\\|/?<>,.";
                byte[] randomBytes = new byte[8];
                using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
                {
                    rng.GetBytes(randomBytes);
                }
                char[] chars = new char[8];
                for (int i = 0; i < 8; i++)
                {
                    int index = randomBytes[i] % validChars.Length;
                    chars[i] = validChars[index];

                }

                var charsz = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
                var stringChars = new char[8];
                var random = new Random();

                for (int i = 0; i < stringChars.Length; i++)
                {
                    stringChars[i] = charsz[random.Next(charsz.Length)];
                }

                var finalString = new String(stringChars);

                // Create a new delivery object and add it to the database
                var newDelivery = new AppDelivery
                {
                    FumEmail = selectedDriver.Email,
                    FumName = selectedDriver.FirstName + " " + selectedDriver.LastName,
                    Address = book.CustomerAddress,
                    Name = book.CustomerName,
                    DeliveryDate = DateTime.Now.ToLongDateString(),
                    orderId = bookId,
                    pass = finalString
                };

                w.amount -= TotalPrice;
                w.expense += TotalPrice;
                db.Entry(gameOrder).State = EntityState.Modified;
                db.Entry(w).State = EntityState.Modified;
                db.AppDeliveries.Add(newDelivery);
                db.SaveChanges();

                double TotalAmount = TotalPrice;

                var invoice = Url.Action("Home", "Invoice", new { id }, protocol: Request.Url.Scheme);
                var proof = Url.Action("Home", "Proof", new { id }, protocol: Request.Url.Scheme);
                var callbackUrl = Url.Action("Cancel", "Bookings", new { id = book.Id }, protocol: Request.Url.Scheme);
                MailMessage mm = new MailMessage("Plantationgrp33@gmail.com", book.CustomerEmail);
                mm.Subject = "CONFIRMATION CODE";
                mm.Body = "Dear " + book.CustomerName + "," + "<br/><br/>" +
                          "Please see your booking access code below:" + "<br/><br/>" +
                          "<b style=\"color: green; font-size:20px;\">-" + finalString + "</b><br/><br/>" +
                          "Use this code when confirming you identity" + "<br/><br/>" +
                          "We also care about our customer's decisions, so if you wish to cancel booking, visit the linkink in the button below" + "<br/>" +
                          "<ul style=\"list-style:none\">" +
                          "     <li><a style=\"color:white;background-color:red; padding:2%; border-radius:5px;text-decoration:none\" href=\"" + callbackUrl + "\">Cancel Booking<a></li>" +
                          "</ul>" +
                          "Best regards," + "<br/>" +
                          "Plantation Management";
                mm.IsBodyHtml = true;

                SmtpClient smtp = new SmtpClient();
                smtp.Host = "smtp.gmail.com";
                smtp.Port = 587;
                smtp.EnableSsl = true;

                NetworkCredential nc = new NetworkCredential("Plantationgrp33@gmail.com", "jkmabstufjzldlsc");
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = nc;
                smtp.Send(mm);

                TempData["MyWallet"] = "Thank you for making payment  please check your emails and NOTE: " + selectedDriver.FirstName + " " + selectedDriver.LastName + " has been selected as your fumigator, please wait so they start their journey to you";
                return RedirectToAction("MyWallet");
            }
        }
    }
}
