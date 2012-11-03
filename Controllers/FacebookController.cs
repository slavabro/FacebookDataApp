using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Facebook;
using System.ComponentModel;
using System.Threading;
using System.EnterpriseServices.Internal;
using System.Collections;
using System.Dynamic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;
using System.Xml;
namespace MVC_testAPP.Controllers
{
    /// <summary>
    /// The Facebook app controller class
    /// </summary>
    public class FacebookController : Controller
    {
        /// <summary>
        /// The controller of the Facebook page
        /// </summary>
        public FacebookController()
        {
        }

        /// <summary>
        /// Occurs on HttpPost action
        /// </summary>
        /// <param name="collection"></param>
        /// <returns>The page you want to be redirect the user to, after retreiving his data</returns>
        [HttpPost]
        public ActionResult Post(FormCollection collection)
        {


            ////BackgroundWorker bgWorker;
            ThreadPool.QueueUserWorkItem(new WaitCallback((o) => UpdateFacebookData(collection)));

            ////bgWorker = new BackgroundWorker();

            ////bgWorker.DoWork += new DoWorkEventHandler(UpdateFacebookData);

            ////bgWorker.RunWorkerAsync(collection);

            //string menuEpsik = _epsikDB.GetMenuEpsik(collection["restaurant"], collection["table"]);
            //ViewBag.MenuEpsik = collection["restaurant"];

            #region return view
            return RedirectToAction("About", "Home");
            //return new EmptyResult() ;
            //return View("About"); 
            #endregion
        }

        /// <summary>
        /// Retreive all the information we want from the logged user
        /// </summary>
        /// <param name="collection"></param>
        private void UpdateFacebookData(FormCollection collection)
        {
            //Initializing new facebook client object, withh the access token of the loged user
            var accessToken = collection["AccessToken"].ToString();
            var client = new FacebookClient(accessToken);
            dynamic me = client.Get("me");
            string firstName = me.first_name;

            //getting friends
            //In an OO way, through .NET objects
            dynamic friends = client.Get("/me/friends");
            if (friends != null)
            {
                List<string> list = new List<string>();
                foreach (dynamic friend in friends.data)
                {
                    list.Add("Name: " + friend.name + "Facebook id: " + friend.id);
                }
            }

            #region Getting feeds
            ////getting feeds
            //dynamic feeds = client.Get("/me/feed");
            //if (feeds != null)
            //{
            //    List<string> list = new List<string>();
            //    foreach (dynamic feed in feeds.data)
            //    {
            //        list.Add("Story: " + feed.story + "Facebook id: " + feed.id);
            //    }
            //} 
            #endregion

            // working in an FQL way
            dynamic parameters = new ExpandoObject();
            parameters.q = "SELECT status_id, time, message FROM status WHERE uid=me()";
            dynamic result = client.Get("fql", parameters);

            JsonArray newArr = getObjectsInTimeRange(result["data"], new DateTime(2011, 1, 1), new DateTime(2013, 1, 1));

            //write the data to a file
            using (XmlTextWriter xmlWriter = new XmlTextWriter("d://" + me.name + ".xml", null))
            {

                // Opens the document
                xmlWriter.WriteStartDocument();
                // Write root element
                xmlWriter.WriteStartElement("FacebookData");
                xmlWriter.Formatting = System.Xml.Formatting.Indented;
                                
                #region Statuses & their likes
                xmlWriter.WriteStartElement("Statuses");
                //writing statuses
                foreach (var item in newArr)
                {
                    string objID = (string)(((JsonObject)item)["status_id"]).ToString();
                    int likes_num = getLikeCount(client, objID);
                    //sWriter.WriteLine("status: " + ((JsonObject)item)["message"] + ";   likes num: " + likes_num);

                    xmlWriter.WriteStartElement("status");
                    xmlWriter.WriteAttributeString("likesCount", likes_num.ToString());
                    xmlWriter.WriteString(((JsonObject)item)["message"].ToString());
                    xmlWriter.WriteEndElement();
                }
                xmlWriter.WriteEndElement(); // end of 'Statuses' element
                #endregion

                //foreach (var item in (jsonarray)result["data"])
                //{
                //    string timestr = (string)(((jsonobject)item)["time"]).tostring();
                //    datetime datetime = convertfromunixtimestamp(int32.parse(timestr));
                //    string bbb = datetime.tostring();
                //}

                #region Friends
                parameters = new ExpandoObject();
                parameters.q = "SELECT uid2 FROM friend WHERE uid1=me()";
                result = client.Get("fql", parameters);
                string val = result["data"].ToString();


                IList<string> list = val.Split(',').ToList<string>();
                //xmlWriter.WriteStartElement("FriendCount", "");
                //xmlWriter.WriteString(list.Count.ToString());
                //xmlWriter.WriteEndElement();
                //sWriter.WriteLine("Friends count: " + list.Count);
                xmlWriter.WriteStartElement("FriendS");
                foreach (var friend in list)
                {
                    xmlWriter.WriteStartElement("Friend", "");
                    xmlWriter.WriteString(friend);
                    xmlWriter.WriteEndElement();
                    //sWriter.WriteLine(friend);
                }
                xmlWriter.WriteEndElement();
                #endregion

                // Getting full friend data
                //parameters.q = string.Format("SELECT uid, first_name, last_name, email FROM user WHERE uid IN ({0})", friendList);
                //result = client.Get("fql", parameters);
                //val = result["data"].ToString();
                //sWriter.WriteLine(val);

                #region All basic profile data
                parameters.q = "SELECT username,name,pic_small,pic,affiliations,religion,birthday,devices,sex,status,wall_count,friend_count,likes_count FROM user WHERE uid=me()";
                result = client.Get("fql", parameters);
                val = result["data"].ToString();
                sWriter.WriteLine(val); 
                #endregion

                #region Mailbox
                //Read all Outbox
                parameters.q = "SELECT thread_id FROM thread WHERE folder_id = 0";
                result = client.Get("fql", parameters);
                JsonArray dataArr = result["data"];
                xmlWriter.WriteStartElement("Mailbox");
                foreach (var item in dataArr)
                {
                    parameters.q = "SELECT body, created_time FROM message WHERE thread_id = " + ((JsonObject)item)["thread_id"].ToString();
                    result = client.Get("fql", parameters);
                    JsonArray dataArr2 = result["data"];
                    xmlWriter.WriteStartElement("Conv");
                    foreach (var item2 in dataArr2)
                    {
                        //sWriter.Write("message was sent on: " + " content " + ((JsonObject)item2)["body"].ToString() + "   ");
                        xmlWriter.WriteElementString("message", ((JsonObject)item2)["body"].ToString());
                    }
                    xmlWriter.WriteEndElement(); // end of 'conv' element
                }
                xmlWriter.WriteEndElement(); // end of 'Mailbox' element
                #endregion

                // Ends the document.
                xmlWriter.WriteEndDocument();
                // close writer
                xmlWriter.Close();
            }



            #region Post on users wall
            //var postparameters = new Dictionary<string, object>();
            //postparameters["message"] = "I am drinking " + collection["product"] + " at " + collection["restaurant"];
            //postparameters["name"] = "ePsik";
            //postparameters["link"] = @"http://www.yourLink.com";
            //postparameters["description"] = "Ordered";

            //var result = client.Post("/me/feed", postparameters); 
            #endregion

        }

        /// <summary>
        /// Shows the Publish view page
        /// Accures once user logs on /Facebook/PostConfirm/
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult PostConfirm()
        {
            return View("Publish");
        }

        /// <summary>
        /// Converts Unix timestamp to DataTime object
        /// </summary>
        /// <param name="timestamp">The UNIX timestamp</param>
        /// <returns>The corresponding DataTime object</returns>
        private DateTime convertFromUnixTimestamp(double timestamp)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return origin.AddSeconds(timestamp);
        }

        /// <summary>
        /// returns all the objects in the JasonArray for which the time is in the range
        /// of the start and the end time
        /// </summary>
        /// <param name="arr"> the JasonArray</param>
        /// <param name="startDate">the start date and time</param>
        /// <param name="endDate">the end date and time</param>
        /// <returns>the new array</returns>
        private JsonArray getObjectsInTimeRange(JsonArray arr, DateTime startDate, DateTime endDate)
        {
            JsonArray retArr = new JsonArray();

            foreach (var item in arr)
            {
                string timeStr = (string)(((JsonObject)item)["time"]).ToString();
                DateTime dateTime = convertFromUnixTimestamp(Int32.Parse(timeStr));
                //check if in range
                if (dateTime >= startDate && dateTime <= endDate)
                    retArr.Add(item);
            }

            return retArr;
        }

       /// <summary>
        /// Returns the number of likes for an item
       /// </summary>
       /// <param name="client"></param>
       /// <param name="objectID">the id of the object who's likes we want to count</param>
       /// <returns></returns>
        private int getLikeCount(FacebookClient client ,string objectID)
        {
            dynamic parameters = new ExpandoObject();
            parameters.q = "SELECT user_id FROM like WHERE object_id=\"" + objectID + "\"";
            dynamic result = client.Get("fql", parameters);
            return ((JsonArray)result["data"]).Count;
        }
    }
}
