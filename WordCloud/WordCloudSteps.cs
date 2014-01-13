using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using AlteryxGalleryAPIWrapper;
using Newtonsoft.Json;
using NUnit.Framework;
using TechTalk.SpecFlow;


namespace WordCloud
{
    [Binding]
    public class WordCloudSteps
    {

        private string alteryxurl;
        private string _sessionid;
        private string _appid;
        private string _userid;
        private string _appName;
        private string jobid;
        private string _appActualName;
        private dynamic statusresp;
        private int messagecount;
     

        private Client Obj = new Client("https://gallery.alteryx.com/api/");

        private RootObject jsString = new RootObject();

        [Given(@"alteryx running at""(.*)""")]
        public void GivenAlteryxRunningAt(string url)
        {
            alteryxurl = Environment.GetEnvironmentVariable(url);
        }
        
        [Given(@"I am logged in using ""(.*)"" and ""(.*)""")]
        public void GivenIAmLoggedInUsingAnd(string user, string password)
        {
            _sessionid = Obj.Authenticate(user, password).sessionId;
        }
        
        [When(@"I run the app ""(.*)"" that makes the word cloud with the text ""(.*)""")]
        public void WhenIRunTheAppThatMakesTheWordCloudWithTheText(string app, string text)
        {
            //url + "/apps/gallery/?search=" + appName + "&limit=20&offset=0"
            //Search for App & Get AppId & userId 
            string response = Obj.SearchAppsGallery(app);
            var appresponse =
                new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Dictionary<string, dynamic>>(
                    response);
            int count = appresponse["recordCount"];
            if (count == 1)
            {
                _appid = appresponse["records"][0]["id"];
                _userid = appresponse["records"][0]["owner"]["id"];
                _appName = appresponse["records"][0]["primaryApplication"]["fileName"];
            }
            else
            {
                for (int i = 0; i <= count - 1; i++)
                {

                    _appActualName = appresponse["records"][i]["primaryApplication"]["metaInfo"]["name"];
                    if (_appActualName == app)
                    {
                        _appid = appresponse["records"][i]["id"];
                        _userid = appresponse["records"][i]["owner"]["id"];
                        _appName = appresponse["records"][i]["primaryApplication"]["fileName"];
                        break;
                    }
                }

            }
            jsString.appPackage.id = _appid;
            jsString.userId = _userid;
            jsString.appName = _appName;

            //url +"/apps/" + appPackageId + "/interface/
            //Get the app interface - not required
            string appinterface = Obj.GetAppInterface(_appid);
            dynamic interfaceresp = JsonConvert.DeserializeObject(appinterface);
            
            List<JsonPayload.Question> questionAnsls = new List<JsonPayload.Question>();
            questionAnsls.Add(new JsonPayload.Question("corpus", "\"" + text + "\""));
            jsString.questions.AddRange(questionAnsls);

        }
        
        [When(@"I give the imagewidth (.*) and height (.*) and resolution (.*)")]
        public void WhenIGiveTheImagewidthAndHeightAndResolution(int width, int height, int resolution)
        {
            List<JsonPayload.Question> questionAnsls1 = new List<JsonPayload.Question>();
            questionAnsls1.Add(new JsonPayload.Question("width", "\"" + width.ToString() + "\""));
            questionAnsls1.Add(new JsonPayload.Question("height", "\"" + height.ToString() + "\""));
            questionAnsls1.Add(new JsonPayload.Question("res", "\"" + resolution.ToString() + "\""));
            
            jsString.questions.AddRange(questionAnsls1);
        }
        
        [When(@"I also give the sequential colors (.*) and the palette ""(.*)""")]
        public void WhenIAlsoGiveTheSequentialColorsAndThePalette(int seqNum, string seqColor)
        {
            List<JsonPayload.Question> questionAnsls2 = new List<JsonPayload.Question>();
            questionAnsls2.Add(new JsonPayload.Question("seqRad", "true"));
            questionAnsls2.Add(new JsonPayload.Question("divRad", "false"));
            questionAnsls2.Add(new JsonPayload.Question("seqNum", "\"" + seqNum.ToString() + "\""));
            questionAnsls2.Add(new JsonPayload.Question("divNum", "8"));

            var sColor = new List<JsonPayload.datac>();
            sColor.Add(new JsonPayload.datac() { key = "Blues", value = "true" });
            var dColor = new List<JsonPayload.datac>();
            dColor.Add(new JsonPayload.datac() { key = "BrBG", value = "true" });
            string sColorField = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(sColor);
            string dcolorField = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(dColor);



            for (int i = 0; i < 3; i++)
            {

                if (i == 0)
                {
                    JsonPayload.Question questionAns = new JsonPayload.Question();
                    questionAns.name = "seqColor";
                    questionAns.answer = sColorField;
                    jsString.questions.Add(questionAns);
                }
                else if (i == 1)
                {
                    JsonPayload.Question questionAns = new JsonPayload.Question();
                    questionAns.name = "divColor";
                    questionAns.answer = dcolorField;
                    jsString.questions.Add(questionAns);
                }
                else
                {
                    jsString.questions.AddRange(questionAnsls2);
                }
            }
            jsString.jobName = "Job Name";

            // Make Call to run app

            var postData = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(jsString);
            string postdata = postData.ToString();
            string resjobqueue = Obj.QueueJob(postdata);

            var jobqueue =
                new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Dictionary<string, dynamic>>(
                    resjobqueue);
            jobid = jobqueue["id"];

            string status = "";
            while (status != "Completed")
            {
                string jobstatusresp = Obj.GetJobStatus(jobid);
                statusresp =
                   new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Dictionary<string, dynamic>>(
                       jobstatusresp);
                status = statusresp["status"];
                messagecount = statusresp["messages"].Count;
            }      



        }
        
        [Then(@"I see the there is an image file as output ""(.*)""")]
        public void ThenISeeTheThereIsAnImageFileAsOutput(string output)
        {
            //check the output message if it contains the output png file
            for (int i = 1; i < messagecount - 1; i++)
            {
                int toolid = statusresp["messages"][i]["toolId"];
                if (toolid == 6)
                {
                    string text = statusresp["messages"][i]["text"];
                    string term = text.TrimStart();
                    string[] split = term.Split(new Char[] { ' ', ',', '.', ':', '\t' });                  
                    StringAssert.Contains(output,text);
                }
            }
        }
    }
}
