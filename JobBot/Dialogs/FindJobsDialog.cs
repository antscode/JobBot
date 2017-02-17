using JobBot.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.FormFlow.Advanced;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace JobBot.Dialogs
{
    [LuisModel("<<application ID>>", "<< subscription ID >>")]
    [Serializable]
    public class FindJobsDialog : LuisDialog<JobCriteria>
    {
        [LuisIntent("FindJobs")]
        public async Task FindJobs(IDialogContext context, LuisResult result)
        {
            context.UserData.Clear();

            var entities = new List<EntityRecommendation>(result.Entities);

            var title = GetEntityValue("Title", result.Entities);
            var location = GetEntityValue("Location", result.Entities);
            var category = GetEntityValue("Category", result.Entities);
            var workType = GetEntityValue("WorkType", result.Entities);

            await ProcessJobSearch(context, title, location, category, workType);
        }

        private async Task ProcessJobSearch(IDialogContext context, string title, string location, string category, string workType)
        {
            title = GetCriteria("Title", context, title);
            location = GetCriteria("Location", context, location);
            category = GetCriteria("Category", context, category);
            workType = GetCriteria("WorkType", context, workType);

            var searchResults = (await JobSearch(
                title,
                location,
                category,
                workType)).ToList();

            if (searchResults.Count > 5)
            {
                context.UserData.SetValue("SearchResults", searchResults);

                await context.PostAsync("Hmmm, we found a lot of results. Lets try and narrow them down a bit...");

                if (string.IsNullOrWhiteSpace(title))
                {
                    PromptDialog.Text(
                        context: context,
                        resume: AskForTitleComplete,
                        prompt: "What kind of job are you looking for?",
                        retry: "I didn't understand. Please try again.");
                }
                else if (string.IsNullOrWhiteSpace(location))
                {
                    PromptDialog.Choice<string>(
                        context: context,
                        resume: AskForLocationComplete,
                        options: GetLocationOptions(context),
                        prompt: "Similar jobs exist in a number of locations, do you have a preference?",
                        retry: "I didn't understand. Please try again.");
                }
            }
            else if (searchResults.Count > 0)
            {
                await context.PostAsync($"We found some jobs! Take a look at these: ");

                var replyToConversation = (Activity)context.MakeMessage();

                replyToConversation.Recipient = replyToConversation.Recipient;
                replyToConversation.Type = "message";
                replyToConversation.AttachmentLayout = "carousel";
                replyToConversation.Attachments = new List<Attachment>();

                foreach (var searchResult in searchResults)
                {
                    replyToConversation.Attachments.Add(new HeroCard()
                    {
                        //Images = cardImages,
                        Buttons = new List<CardAction>
                        {
                            new CardAction
                            {
                                Type = "openUrl",
                                Title = "Apply Now",
                                Value = searchResult.ApplyUrl
                            }
                        },
                        Title = searchResult.Title,
                        Subtitle = searchResult.Summary
                    }.ToAttachment());
                }

                // Send the reply
                await context.PostAsync(replyToConversation);
                context.Wait(MessageReceived);
            }
            else
            {
                await context.PostAsync($"Sorry! No jobs found :(");

                context.Wait(MessageReceived);
            }
        }

        private static IEnumerable<string> GetLocationOptions(IDialogContext context)
        {
            var searchResults = context.UserData.Get<IEnumerable<JobSearchResult>>("SearchResults");

            return searchResults.SelectMany(s => s.LocationList).Distinct().Select(s => s.IndexOf("|") > -1 ? s.Split('|')[1] : s);
        }


        public async Task AskForLocationComplete(IDialogContext context, IAwaitable<string> argument)
        {
            var location = await argument;

            await ProcessJobSearch(context, null, location, null, null);
        }

        public async Task AskForTitleComplete(IDialogContext context, IAwaitable<string> argument)
        {
            var title = await argument;

            await ProcessJobSearch(context, title, null, null, null);
        }

        private string GetEntityValue(string type, IList<EntityRecommendation> entities)
        {
            var entity = entities.FirstOrDefault(e => e.Type == type);
            string value = null;

            if (entity != null)
            {
                value = entity.Entity;

            }

            return value;
        }

        private string GetCriteria(string type, IDialogContext context, string value)
        {
            if (value != null)
            {
                // Update stored user value
                context.UserData.SetValue(type, value);
            }
            else
            {
                // Look for a stored user value
                context.UserData.TryGetValue(type, out value);
            }

            return value;
        }

        private async Task<IEnumerable<JobSearchResult>> JobSearch(
            string title,
            string location,
            string category,
            string workType)
        {
            using (var client = new HttpClient())
            {
                var results = new List<JobSearchResult>();

                var url = $"http://jobs.pageuppeople.com/caw/en/jobs.json" +
                    $"?search-keyword={HttpUtility.UrlEncode(title)}" +
                    $"&location={HttpUtility.UrlEncode(location)}" +
                    $"&category={HttpUtility.UrlEncode(category)}" +
                    $"&work-type={HttpUtility.UrlEncode(workType)}";

                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    // Parse the response body
                    results = response.Content.ReadAsAsync<List<JobSearchResult>>().Result;
                }

                return results;
            }
        }
    }
}