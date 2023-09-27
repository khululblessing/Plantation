using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using AgricultureApp.Models;
using AgricultureApp.Models.EventsManager;
using Microsoft.AspNet.Identity;

namespace AgricultureApp.Controllers.RentalsManager
{
    public class RentalsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Rentals
        public ActionResult Index(int? id)
        {
            return View(db.Rentals.ToList().Where(d=>d.Id==id));
        }

        // GET: Rentals
        public ActionResult UserIndex()
        {
            var user = User.Identity.GetUserName();
            return View(db.Rentals.ToList().Where(d=>d.email==user));
        }

        public ActionResult Collections()
        {
            DateTime currentDate = DateTime.Now.Date;

            var rentals = db.Rentals.ToList();

            var collections = rentals
                .Where(d => DateTime.ParseExact(d.endDate, "dd MMMM yyyy dddd", CultureInfo.InvariantCulture) >= currentDate && d.isPaid && !d.isCollected)
                .ToList();

            return View(collections);
        }
        public ActionResult Returns()
        {
            DateTime currentDate = DateTime.Now.Date;

            var rentals = db.Rentals.ToList();

            var collections = rentals
                .Where(d => d.isCollected && !d.isReturned)
                .ToList();

            return View(collections);
        }

        // GET: Rentals
        public ActionResult RentalItems()
        {
            return View(db.Items.ToList().Where(d=>d.isForRentals==true));
        }
		// GET: Rentals/Create
		[Authorize]
        public ActionResult Create(int? id)
        {
            var i = db.Items.Find(id);
            ViewBag.Price = i.rentalPrice;
            ViewBag.Image = i.picture;
            ViewBag.Name = i.name;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id")] Rental rental, DateTime startDate, DateTime endDate, int? id)
        {
            if (ModelState.IsValid)
            {
                var i = db.Items.Find(id);
                rental.itemCost = i.rentalPrice;
                rental.itemName = i.name;
                rental.itemId = i.id;
                rental.itemImage = i.picture;

                rental.startDate = startDate.ToString("dd MMMM yyyy dddd");
                rental.endDate = endDate.ToString("dd MMMM yyyy dddd");

                TimeSpan timeSpan = endDate - startDate;
                rental.days = timeSpan.Days;
                rental.total = timeSpan.Days * i.rentalPrice;

                var user = User.Identity.GetUserName();
                var g = db.Customers.Where(d => d.Email == user).FirstOrDefault();

                rental.name = g.FirstName;
                rental.surname = g.LastName;
                rental.email = g.Email;

                rental.isRequested = true;

                db.Rentals.Add(rental);
                db.SaveChanges();
                return RedirectToAction("Index", new { rental.Id });
            }
            return View(rental);
        }

        public ActionResult Invoice(int? id)
		{
            var inv = db.Rentals.Where(d => d.Id == id).ToList();
            return View(inv);
		}

        public ActionResult Collect(int? id)
        {
            var r = db.Rentals.Find(id);

            // Generate a unique confirmation token (you can use libraries like Guid.NewGuid())
            string confirmationToken = Guid.NewGuid().ToString();

            // Store the timestamp when the token was generated
            DateTime tokenCreationTime = DateTime.Now;

            // Create the confirmation URL using the token
            string callbackUrl = Url.Action("Confirm", "Rentals", new { token = confirmationToken, id = id, createTime = tokenCreationTime }, protocol: Request.Url.Scheme);

            ViewBag.IsConfirmed = r.isConfirmed;
            ViewBag.Name = r.name+" "+r.surname;
            ViewBag.Id = r.Id;

			if (!r.isConfirmed)
			{
                // Send email with confirmation link
                SendConfirmationEmail(r.email, callbackUrl, tokenCreationTime); // Pass the token creation time
            }

            return View();
        }

		[HttpPost]
        public ActionResult Collect(int id, string signature)
		{
            var r = db.Rentals.Find(id);
            r.isCollected = true;
            r.signature = signature;
            db.Entry(r).State = EntityState.Modified;
            db.SaveChanges();
            string callbackUrl = Url.Action("Contract", "Rentals", new { id = id }, protocol: Request.Url.Scheme);
            MailMessage mm = new MailMessage("Plantationgrp33@gmail.com", r.email);
            mm.Subject = "COLLECTION CONFIRMATION";
            mm.Body = "Hello," +
                      "<br/><br/>" +
                      "<p>Your equipment has been collected, you can  click <a href=\"" + callbackUrl + "\">here</a> to see your rental report.</p> " +
                      "<p>Please contact us at 031 130 1300 if you suspect any fraudulant act.</p> " +
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
            TempData["Collections"] = "The equipment has now been collected";
            return RedirectToAction("Collections");
		}

        public ActionResult Contract(int? id)
		{
            var r = db.Rentals.Where(d => d.Id == id).ToList();
            return View(r);
		}

        public ActionResult Review(int? id)
		{
            var r = db.Rentals.Where(d => d.Id == id).ToList();
            return View(r);
		}

		[HttpPost]
        public ActionResult Review(int id, int q1, int q2, int q3, string signature)
		{
            var r = db.Rentals.Find(id);
            r.isReturned = true;
            r.assistantSign = signature;
            db.Entry(r).State = EntityState.Modified;
            db.SaveChanges();

            int expDemage = -6;
            int demage = q1 + q2 + q3;

            double totalDem = demage / expDemage;
            double total = totalDem * 100;

            double penalty = r.total * totalDem;

            string callbackUrl = Url.Action("Contract", "Rentals", new { id = id }, protocol: Request.Url.Scheme);
            MailMessage mm = new MailMessage("Plantationgrp33@gmail.com", r.email);
            mm.Subject = "RETURN CONFIRMATION";
            mm.Body = "Hello," +
                      "<br/><br/>" +
                      "<p>Thank you for returning an item, you can  click <a href=\"" + callbackUrl + "\">here</a> to see your rental report.</p> " +
                      "<p>Please contact us at 031 130 1300 if you suspect any fraudulant act.</p> " +
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

			if (total >= 50)
			{
                TempData["Returns"] = "The equipment has now been returned with the demade of "+ total+"% and a penalty of "+penalty.ToString("R0.00")+" has to be paid by cash at our store";
            }
			else
			{
                TempData["Returns"] = "The equipment has now been returned with demade of " + total+" which will not be penalized for due to being low";
            }
            return RedirectToAction("Returns");
		}
        

        public ActionResult Confirm(int? id,string token)
        {
            // Validate the token and time within the allowed window (e.g., 5-10 minutes)
            if (IsValidToken(token))
            {
                var r = db.Rentals.Find(id);
                r.isConfirmed = true;
                db.Entry(r).State = EntityState.Modified;
                db.SaveChanges();
                // Return a view confirming the action
                return View("Confirmation");
            }
            // Return a view indicating invalid or expired token
            return View("InvalidToken");
        }

        public ActionResult Confirmation()
		{
            return View();
		}

        private void SendConfirmationEmail(string toEmail, string callbackUrl, DateTime tokenCreationTime)
        {
            MailMessage mm = new MailMessage("Plantationgrp33@gmail.com", toEmail);
            mm.Subject = "CONFIRM EMAIL";
            mm.Body = "Hello," +
                      "<br/><br/>" +
                      "<p>You have requested to collected an Item you rented on our site, click <a href=\"" + callbackUrl + "\">yes</a> if this is you or ignore.</p> " +
                      "<p>Please contact us at 031 130 1300 if you suspect any fraudulant act.</p> " +
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
        }

        private bool IsValidToken(string token)
        {
            // Implement your token validation logic here
            // Check if the token is valid and within the allowed time window
            // Example: Compare token with a stored list of valid tokens and check timestamp
            return true; // Replace with your actual validation logic
        }

        public ActionResult CheckConfirmation(int? id)
        {
            var rental = db.Rentals.Find(id);

            if (rental != null)
            {
                return Json(new { isConfirmed = rental.isConfirmed }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { isConfirmed = false }, JsonRequestBehavior.AllowGet);
        }
    }
}
