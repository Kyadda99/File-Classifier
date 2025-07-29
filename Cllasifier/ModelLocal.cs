using MaIN.Core.Hub;
using MaIN.Services.Services.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
            string selectedFile = null;
            var thread = new Thread(() =>
            {
                using (var dialog = new OpenFileDialog())
                {
                    dialog.Title = "Select your model file";
                    dialog.Filter = "All files (*.*)|*.*"; // Customize the filter as needed
                    dialog.Multiselect = false;

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        selectedFile = dialog.FileName;
                    }
                }
            });

            thread.SetApartmentState(ApartmentState.STA); // required for folder dialog
            thread.Start();
            thread.Join(); // wait for thread to finish

            if (string.IsNullOrEmpty(selectedFile))
            {
                throw new Exception("No folder was selected for the model.");
            }

            var path = selectedFile;
            AIHub.Extensions.DisableLLamaLogs();
            AIHub.Extensions.DisableNotificationsLogs();

            var context = AIHub.Agent()
                .WithCustomModel("model",path)
                .WithInitialPrompt(systemPrompt)
                //.WithMemoryParams(new MaIN.Domain.Entities.MemoryParams
                //{
                //    Grammar = @"
                //    root ::= ""{"" (mapping ("","" mapping)*)? ""}""
                //    mapping ::= path_string "":"" category_string

                //    path_string ::= ""\"""" escaped_path_chars ""\""""
                //    escaped_path_chars ::= (
                //        [^""\\\x00-\x1F] |
                //        ""\\"" ([""\\/bfnrt] | ""u"" [0-9a-fA-F]{4})
                //      )*

                //    category_string ::= ""\"""" (
                //        [^""\\\x00-\x1F] |
                //        ""\\"" ([""\\/bfnrt] | ""u"" [0-9a-fA-F]{4})
                //      )* ""\""""
                //    "
                //}
                //)
                .Create();

            MaIN.Services.Services.Models.ChatResult chatResult = await context
                .ProcessAsync(prompt);

            result = chatResult;

        }

    }
}
