using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using AgricultureApp.Models;
using AgricultureApp.Models.EventManagement;
using ZXing;
using System.IO;

namespace AgricultureApp.Controllers.EventManagement
{
    public class EventsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Events
        public ActionResult Index(int id)
        {
            var user = User.Identity.GetUserName();
            ViewBag.Tickets = db.Tickets.Where(d => d.Id == id && d.email==user).ToList();
            return View(db.Events.ToList().Where(d=>d.Id==id));
        }

        public ActionResult Tickets(int id)
        {
            var user = User.Identity.GetUserName();
            ViewBag.Event = db.Events.Where(d => d.Id == id).ToList();
            return View(db.Tickets.ToList().Where(d=>d.eventId==id && d.email == user));
        }

        public ActionResult Reciept(int id)
        {
            var user = User.Identity.GetUserName();
            ViewBag.Event = db.Events.Where(d => d.Id == id).ToList();
            ViewBag.Tickets = db.Tickets.ToList().Where(d => d.eventId == id && d.email == user);
            return View();
        }

        // GET: Events
        public ActionResult Calendar(DateTime? date)
        {
            if (!date.HasValue)
            {
                return View();
            }
            else
            {
                return RedirectToAction("Create", new { date = date.Value.ToString("yyyy-MM-dd") });
            }
        }

        // GET: Events
        public ActionResult CalendarIndex(DateTime? date)
        {
            if (!date.HasValue)
            {
                return View();
            }
            else
            {
                return View(db.Events.ToList().Where(d=>d.Date == date.Value.ToString("dd MMMM yyyy dddd")));
            }
        }


        // GET: Events/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Event @event = db.Events.Find(id);
            if (@event == null)
            {
                return HttpNotFound();
            }
            return View(@event);
        }

        // GET: Events/Create
        public ActionResult Create(DateTime date)
        {
            ViewBag.Date = date;
            return View();
        }

        // POST: Events/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Title,Description,vName,vCapacity")] Event @event, HttpPostedFileBase img_upload, DateTime date, DateTime time)
        {
            if (ModelState.IsValid)
            {
                byte[] data = null;
                data = new byte[img_upload.ContentLength];
                img_upload.InputStream.Read(data, 0, img_upload.ContentLength);
                @event.EventImageUrl = data;
                @event.Date = date.ToString("dd MMMM yyyy dddd");
                @event.Time = time.ToString("HH'h'mm");
                db.Events.Add(@event);
                db.SaveChanges();

                var e = db.Events.Find(@event.Id);
                var l = db.Customers.ToList();
                foreach (var i in l)
                {
                    var callbackUrl = Url.Action("RSVP", "Events", new { id = e.Id }, protocol: Request.Url.Scheme);
                    MailMessage mm = new MailMessage("Appdevproject98@gmail.com", i.Email);
                    mm.Subject = "Event Invite";
                    mm.Body = "Dear " + i.FirstName + "," + "<br/><br/>" +
                              "<p>As our trusted customer, you are invited to our anuall event, where we shed light about plantation!!!" + "</p>" +
                              "<ul>" +
                                 "<li>" +
                                    "Title : " + @event.Title + "" +
                                 "</li>" +

                                 "<li>" +
                                    "Date : " + @event.Date + "" +
                                 "</li>" +

                                 "<li>" +
                                    "Time : " + @event.Time + "" +
                                 "</li>" +
                                 "<li>" +
                                    "Description : " + @event.Description + "" +
                                 "</li>" +
                                 "<li>" +
                                    "Venue : "+@event.vName+ 
                                 "</li>" +
                                 "<li>" +
                                   "Entrance is free of charge" +
                                "</li>" +
                                "</ul>" +

                              "<p><a href=\"" + callbackUrl + "\">RSVP</a> as soon as possible to reserve your self a place" + "</p>" +
                              "Make sure to RSPV asap to avoid dissapointments." + "<br/><br/>" +
                              "Best regards," + "<br/>" +
                              "Plantation Management";
                    mm.IsBodyHtml = true;

                    SmtpClient smtp = new SmtpClient();
                    smtp.Host = "smtp.gmail.com";
                    smtp.Port = 587;
                    smtp.EnableSsl = true;

                    NetworkCredential nc = new NetworkCredential("Appdevproject98@gmail.com", "yxhoojydubagmucb");
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = nc;
                    smtp.Send(mm);
                }
                    return RedirectToAction("Index", new { e.Id });
            }
            return View(@event);
        }

		[Authorize(Roles ="Customer")]
        public ActionResult RSVP(int id, string address, string rsvp)
		{
			if (!string.IsNullOrEmpty(rsvp)&& address != null)
			{
                var e = db.Events.Find(id);

                Random random = new Random();
                var barcodeWriter = new BarcodeWriter();
                barcodeWriter.Format = BarcodeFormat.CODE_128;
                var user = User.Identity.GetUserName();
                Ticket t = new Ticket
                {
                    seatNo = new Random().Next(1000, 9999),
                    code = random.Next(10000000, 99999999),
                    eventId = id,
                    email = user,
                };
                e.ticketSold += 1;
                var barcodeBitmap = barcodeWriter.Write(t.code.ToString());

                using (var stream = new MemoryStream())
                {
                    barcodeBitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    t.codeImage = stream.ToArray();
                }
                db.Tickets.Add(t);
                db.Entry(e).State = EntityState.Modified;
                db.SaveChanges();
                TempData["Index"] = "You have reserved your seat. view your ticket below";
                return RedirectToAction("Index", new { id });
            }
			else
			{
                ViewBag.Id = id;
                return View();
            }
		}

        public ActionResult Invoice(int id, string seatNo, string Method)
        {
            ViewBag.Method = Method;
            ViewBag.Id = id;

            // Split the comma-separated string into an array of strings
            string[] seatNumbers = seatNo.Split(',');

            // Get the length of the array
            int seatCount = seatNumbers.Length;

            ViewBag.SeatCount = seatCount; // Store the length in ViewBag
            ViewBag.Seats = seatNo;

            return View(db.Events.ToList().Where(d => d.Id == id));
        }

        public ActionResult Attend(int? barcode, int id)
        {
            ViewBag.Id = id;

            if (barcode != null)
            {
                var t = db.Tickets.FirstOrDefault(d => d.code == barcode && d.eventId == id);
                var e = db.Events.Find(id);
                if (t != null && !t.isChecked)
                {
                    t.isChecked = true;
                    e.isStarted = true;
                    e.attended += 1;
                    db.Entry(t).State = EntityState.Modified;
                    db.Entry(e).State = EntityState.Modified;
                    db.SaveChanges();
                    TempData["Attend"] = "Ticket checked in; inform the learner to pass through.";
                    return RedirectToAction("Attend", new { id = id });
                }
                else
                {
                    TempData["Attend"] = "Invalid ticket";
                    return RedirectToAction("Attend", new { id = id });
                }
            }
            return View();
        }
        public ActionResult End(int id, string signature)
        {
            ViewBag.Id = id;

            if (signature != null)
            {
                var e = db.Events.Find(id);

                // Retrieve distinct email addresses of attendees for the specified event
                var distinctEmails = db.Tickets
                    .Where(t => t.eventId == id)
                    .Select(t => t.email)
                    .Distinct()
                    .ToList();

                // Loop through each distinct email and send the email
                foreach (var email in distinctEmails)
                {
                    MailMessage mm = new MailMessage("Appdevproject98@gmail.com", email);
                    mm.Subject = "EVENT UPDATE";
                    mm.Body = "Hey there," + "<br/><br/>" +
                              "Thank you for attending this event with us, it was an honor to have you here." + "<br/><br/>" +
                              "Best regards," + "<br/>" +
                              "Plantation management";

                    mm.IsBodyHtml = true;

                    SmtpClient smtp = new SmtpClient();
                    smtp.Host = "smtp.gmail.com";
                    smtp.Port = 587;
                    smtp.EnableSsl = true;

                    NetworkCredential nc = new NetworkCredential("Appdevproject98@gmail.com", "yxhoojydubagmucb");
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = nc;
                    smtp.Send(mm);
                }
                e.signature = signature;
                e.isEnded = true;
                db.Entry(e).State = EntityState.Modified;
                db.SaveChanges();
                TempData["Index"] = "The event has been successfully ended, and attendees have been sent acknowledgment emails";
                return RedirectToAction("Index", new { id = id });
            }
            return View();
        }
    }
}
