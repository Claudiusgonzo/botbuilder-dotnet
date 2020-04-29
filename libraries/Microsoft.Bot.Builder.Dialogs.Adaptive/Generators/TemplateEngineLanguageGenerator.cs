﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs.Debugging;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Builder.LanguageGeneration;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Dialogs.Adaptive.Generators
{
    /// <summary>
    /// ILanguageGenerator implementation which uses LGFile. 
    /// </summary>
    public class TemplateEngineLanguageGenerator : LanguageGenerator
    {
        [JsonProperty("$kind")]
        public const string Kind = "Microsoft.TemplateEngineLanguageGenerator";

        private const string DEFAULTLABEL = "Unknown";

        private static readonly TaskFactory TaskFactory = new TaskFactory(
            CancellationToken.None,
            TaskCreationOptions.None,
            TaskContinuationOptions.None,
            TaskScheduler.Default);

        private readonly LanguageGeneration.Templates lg;

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateEngineLanguageGenerator"/> class.
        /// </summary>
        public TemplateEngineLanguageGenerator()
        {
            this.lg = new LanguageGeneration.Templates();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateEngineLanguageGenerator"/> class.
        /// </summary>
        /// <param name="engine">template engine.</param>
        public TemplateEngineLanguageGenerator(LanguageGeneration.Templates engine = null)
        {
            this.lg = engine ?? new LanguageGeneration.Templates();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateEngineLanguageGenerator"/> class.
        /// </summary>
        /// <param name="lgText">lg template text.</param>
        /// <param name="id">optional label for the source of the templates (used for labeling source of template errors).</param>
        /// <param name="resourceMapping">template resource loader delegate (locale) -> <see cref="ImportResolverDelegate"/>.</param>
        public TemplateEngineLanguageGenerator(string lgText, string id, Dictionary<string, IList<Resource>> resourceMapping)
        {
            this.Id = id ?? DEFAULTLABEL;
            var (_, locale) = LGResourceLoader.ParseLGFileName(id);
            var importResolver = LanguageGeneratorManager.ResourceExplorerResolver(locale, resourceMapping);
            this.lg = LanguageGeneration.Templates.ParseText(lgText ?? string.Empty, Id, importResolver);
            RegisterSourcemap(lg);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateEngineLanguageGenerator"/> class.
        /// </summary>
        /// <param name="filePath">lg template file absolute path.</param>
        /// <param name="resourceMapping">template resource loader delegate (locale) -> <see cref="ImportResolverDelegate"/>.</param>
        public TemplateEngineLanguageGenerator(string filePath, Dictionary<string, IList<Resource>> resourceMapping)
        {
            filePath = PathUtils.NormalizePath(filePath);
            this.Id = Path.GetFileName(filePath);

            var (_, locale) = LGResourceLoader.ParseLGFileName(Id);
            var importResolver = LanguageGeneratorManager.ResourceExplorerResolver(locale, resourceMapping);
            this.lg = LanguageGeneration.Templates.ParseFile(filePath, importResolver);
            RegisterSourcemap(lg);
        }

        /// <summary>
        /// Gets or sets id of the source of this template (used for labeling errors).
        /// </summary>
        /// <value>
        /// Id of the source of this template (used for labeling errors).
        /// </value>
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Method to generate text from given template and data.
        /// </summary>
        /// <param name="dialogContext">Context for the current turn of conversation.</param>
        /// <param name="template">template to evaluate.</param>
        /// <param name="data">data to bind to.</param>
        /// <returns>generated text.</returns>
        public override async Task<string> Generate(DialogContext dialogContext, string template, object data)
        {
            EventHandler onEvent = (s, e) => RunSync(() => HandlerLGEvent(dialogContext, s, e));

            try
            {
                return await Task.FromResult(lg.EvaluateText(template, data, new EvaluationOptions { OnEvent = onEvent }).ToString());
            }
            catch (Exception err)
            {
                if (!string.IsNullOrEmpty(this.Id))
                {
                    throw new Exception($"{Id}:{err.Message}");
                }

                throw;
            }
        }

        private async Task HandlerLGEvent(DialogContext dialogContext, object sender, EventArgs e)
        {
            if (e is LGEventArgs le && Path.IsPathRooted(le.Source))
            {
                if (e is BeginTemplateEvaluationArgs be)
                {
                    await dialogContext.GetDebugger().StepAsync(dialogContext, sender, be.Type, new System.Threading.CancellationToken());
                }
                else if (e is BeginExpressionEvaluationArgs expr)
                {
                    await dialogContext.GetDebugger().StepAsync(dialogContext, sender, expr.Type, new System.Threading.CancellationToken());
                }
                else if (e is MessageArgs message && dialogContext.GetDebugger() is IDebugger dda)
                {
                    await dda.OutputAsync(message.Text, sender, message.Text, new System.Threading.CancellationToken());
                }
            }
        }

        private void RunSync(Func<Task> func)
        {
            var culture = CultureInfo.CurrentCulture;
            TaskFactory.StartNew(() =>
            {
                Thread.CurrentThread.CurrentCulture = culture;
                return func();
            }).Unwrap().GetAwaiter().GetResult();
        }

        private void RegisterSourcemap(LanguageGeneration.Templates templates)
        {
            foreach (var template in templates)
            {
                RegisterSourcemap(template, template.SourceRange);
                foreach (var expressionRef in template.Expressions)
                {
                    RegisterSourcemap(expressionRef, expressionRef.SourceRange);
                }
            }
        }

        private void RegisterSourcemap(object item, LanguageGeneration.SourceRange sr)
        {
            var debugSM = new Debugging.SourceRange(
                    sr.Source,
                    sr.Range.Start.Line,
                    sr.Range.Start.Character,
                    sr.Range.End.Line,
                    sr.Range.End.Character);

            if (!DebugSupport.SourceMap.TryGetValue(item, out var _))
            {
                DebugSupport.SourceMap.Add(item, debugSM);
            }
        }
    }
}
