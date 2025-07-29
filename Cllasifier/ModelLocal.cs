using MaIN.Core.Hub;
using MaIN.Services.Services.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cllasifier
{
    public class ModelLocal : IModel
    {
        public string prompt {  get; set; }
        public string systemPrompt { get; set;}
        public ChatResult result { get; set; }

        public ModelLocal(string prompt, string systemPrompt)
        {
            this.prompt = prompt;
            this.systemPrompt = systemPrompt;
        }

        public ModelLocal()
        {
        }

        public async Task Start()
        {
            AIHub.Extensions.DisableLLamaLogs();
            AIHub.Extensions.DisableNotificationsLogs();

            var context = AIHub.Agent()
                .WithModel("gemma2-2b")
                .WithInitialPrompt(systemPrompt)
                .Create();

            MaIN.Services.Services.Models.ChatResult chatResult = await context
                .ProcessAsync(prompt);

            result = chatResult;

        }

    }
}
