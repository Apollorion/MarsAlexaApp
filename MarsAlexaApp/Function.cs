using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;

using Amazon.Lambda.Core;
using Alexa.NET;
using Alexa.NET.Response;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using System.Net.Http;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace MarsAlexaApp
{
    public class Function
    {

        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public SkillResponse FunctionHandler(SkillRequest input, ILambdaContext context)
        {       
            Task<string> JSON = getJsonFromURL("http://marsweather.ingenology.com/v1/latest/?format=json");
            JSON.Wait();

            dynamic jsonDe = JsonConvert.DeserializeObject(JSON.Result);
            dynamic marsData = jsonDe["report"];

            string sunrise = marsData["sunrise"];
            string sunset = marsData["sunset"];
            string[] sunriseArray = sunrise.Split(Char.Parse(" "));
            string[] sunsetArray = sunset.Split(Char.Parse(" "));

            var requestType = input.GetRequestType();

            if(requestType == typeof(LaunchRequest))
            {
                return ResponseReprompt("Bleep Bloop Beep. Its me Curiosity, transmitting from over thirty three point nine million miles away!" +
                    "It is currently soul " + marsData["sol"] + " and " + marsData["atmo_opacity"] + ". You can ask me things like, " +
                    "whats the weather on mars today? Or, When is the sunset? Or, When is the sunrise? Give it a shot",
                    "You can ask me things like whats the weather on mars today? Or, When is the sunset? Or, When is the sunrise? Give it a shot!");
            }
            else
            {
                return Intents(marsData, sunsetArray, sunriseArray, input);
            }
        }

        private SkillResponse Intents(dynamic marsData, string[] sunsetArray, string[] sunriseArray, SkillRequest input)
        {
            var intentRequest = input.Request as IntentRequest;

            if (intentRequest.Intent.Name.Equals("GetMarsDataIntent"))
            {
                return Response("It is currently " + marsData["atmo_opacity"] + " with a high of " + marsData["max_temp_fahrenheit"] + 
                    " , and a low of " + marsData["min_temp_fahrenheit"] + " on mars.");
            }
            else if (intentRequest.Intent.Name.Equals("GetMarsSunsetIntent"))
            {
                return Response("Today the sun will set at " + sunsetArray[1] + " on mars.");
            }
            else if (intentRequest.Intent.Name.Equals("GetMarsSunriseIntent"))
            {
                return Response("Tomorrow the sun will rise at " + sunriseArray[1] + " on mars.");
            }
            else if (intentRequest.Intent.Name.Equals("GetHelpIntent"))
            {
                string text = "You can say things like: whats the weather like on mars? when is the sunset? when is the sunrise? Give it a shot!";
                return ResponseReprompt(text, text);
            }
            else if(intentRequest.Intent.Name.Equals("StopIntent"))
            {
                return Response("");
            }
            else
            {
                return Response("I couldnt talk to mars right now, try again later.");
            }
        }

        private SkillResponse Response(string text)
        {
            var speech = new SsmlOutputSpeech();
            speech.Ssml = "<speak>" + text + "</speak>";
            var finalResponse = ResponseBuilder.Tell(speech);
            return finalResponse;
        }

        private SkillResponse ResponseReprompt(string text, string reprompt)
        {
            var speech = new Alexa.NET.Response.SsmlOutputSpeech();
            speech.Ssml = "<speak> " + text + " </speak>";

            var repromptMessage = new Alexa.NET.Response.PlainTextOutputSpeech();
            repromptMessage.Text = "<speak> " + reprompt + " </speak>";

            // create the reprompt
            var repromptBody = new Alexa.NET.Response.Reprompt();
            repromptBody.OutputSpeech = repromptMessage;
            var finalResponse = ResponseBuilder.Ask(speech, repromptBody);
            return finalResponse;
        }

        private async Task<string> getJsonFromURL(string url)
        {
            using (var httpClient = new HttpClient())
            {
                var json = await httpClient.GetStringAsync(url);
                return json;
            }
        }
    }
}
