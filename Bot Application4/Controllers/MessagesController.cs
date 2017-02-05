using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Emotion.Contract;
using System.Collections.Generic;
using System.IO;
using System.Web;

namespace Bot_Application4
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public virtual async Task<HttpResponseMessage> Post([FromBody] Activity activity)
        {
            ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
            const string emotionApiKey = "3924831d18aa4656b2078804e7cc05ed";

            //Emotion SDK objects that take care of the hard work
            EmotionServiceClient emotionServiceClient = new EmotionServiceClient(emotionApiKey);
            Emotion[] emotionResult = null;
            Activity reply = activity.CreateReply("Could not find a face, or something went wrong. " +
                                                      "Try sending me a photo with a face");
            if (activity.Attachments.Any() && activity.Attachments.First().ContentType.Contains("image"))
            {
                //stores image url (parsed from attachment or message)
                string uploadedImageUrl = activity.Attachments.First().ContentUrl;
                uploadedImageUrl = HttpUtility.UrlDecode(uploadedImageUrl.Substring(uploadedImageUrl.IndexOf("file=") + 5));

                /* reply = activity.CreateReply(uploadedImageUrl);
                 await connector.Conversations.ReplyToActivityAsync(reply);
                 return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);*/

                using (Stream imageFileStream = File.OpenRead(uploadedImageUrl))
                {
                    try
                    {
                        emotionResult = await emotionServiceClient.RecognizeAsync(imageFileStream);
                    }
                    catch (Exception e)
                    {
                        emotionResult = null; //on error, reset analysis result to null
                    }
                }
            }
            else
            {
                try
                {
                    emotionResult = await emotionServiceClient.RecognizeAsync(activity.Text);
                }
                catch (Exception e)
                {
                    emotionResult = null;
                }

            }
            

            if (emotionResult != null)
            {

                //Retrieve list of emotions for first face detected and sort by emotion score (desc)
                IEnumerable<KeyValuePair<string, float>> emotionList = new Dictionary<string, float>()
        {
            { "angry", emotionResult[0].Scores.Anger},
            { "contemptuous", emotionResult[0].Scores.Contempt },
            { "disgusted", emotionResult[0].Scores.Disgust },
            { "frightened", emotionResult[0].Scores.Fear },
            { "happy", emotionResult[0].Scores.Happiness},
            { "neutral", emotionResult[0].Scores.Neutral},
            { "sad", emotionResult[0].Scores.Sadness },
            { "surprised", emotionResult[0].Scores.Surprise}
        }
                .OrderByDescending(kv => kv.Value)
                .ThenBy(kv => kv.Key)
                .ToList();

                KeyValuePair<string, float> topEmotion = emotionList.ElementAt(0);
                string topEmotionKey = topEmotion.Key;
                float topEmotionScore = topEmotion.Value;

                reply = activity.CreateReply("I found a face! I am " + (int)(topEmotionScore * 100) +
                                             "% sure the person seems " + topEmotionKey);
            }
            await connector.Conversations.ReplyToActivityAsync(reply);
            return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}