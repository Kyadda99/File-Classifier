using MaIN.Core;
using MaIN.Core.Hub;
using MaIN.Domain.Configuration;
using MaIN.Services.Services.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cllasifier
{
    public class GeminiModel : IModel
    {
        public string prompt { get; set; }
        public string systemPrompt { get; set; }
        public ChatResult result { get; set; }

        public GeminiModel(string prompt, string systemPrompt)
        {
            this.prompt = prompt;
            this.systemPrompt = systemPrompt;
        }

        public GeminiModel()
        {
        }

        public async Task Start()
        {
            AIHub.Extensions.DisableLLamaLogs();
            AIHub.Extensions.DisableNotificationsLogs();

            MaINBootstrapper.Initialize(configureSettings: (options) =>
            {
                options.BackendType = BackendType.Gemini;
                options.GeminiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            });

            var context = AIHub.Agent()
                .WithModel("gemini-2.0-flash")
                .WithInitialPrompt(this.systemPrompt)
                .Create();

            MaIN.Services.Services.Models.ChatResult chatResult = await context
                .ProcessAsync(this.prompt);

            this.result = chatResult;

        }

    }
}
