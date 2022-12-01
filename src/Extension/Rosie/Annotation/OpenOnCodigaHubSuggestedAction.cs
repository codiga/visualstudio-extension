using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Extension.Rosie.Model;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Process = System.Diagnostics.Process;

namespace Extension.Rosie.Annotation
{
    /// <summary>
    /// Action to open an associated rule on Codiga Hub, in the browser, to learn more about a violation and a rule.
    /// </summary>
    public class OpenOnCodigaHubSuggestedAction : ISuggestedAction
    {
        private readonly RosieAnnotation _annotation;
        private readonly string _displayText;

        public OpenOnCodigaHubSuggestedAction(RosieAnnotation annotation)
        {
            _annotation = annotation;
            _displayText = $"See rule '{annotation.RuleName}' on the Codiga Hub";
        }
        
        /// <summary>
        /// Opens the rule's page on Codiga Hub for this particular violation.
        /// <br/>
        /// Related VS extension: https://github.com/tunnelvisionlabs/OpenInExternalBrowser/blob/master/OpenInExternalBrowser/ 
        /// </summary>
        public void Invoke(CancellationToken cancellationToken)
        {
            try
            {
                Process.Start($"https://app.codiga.io/hub/ruleset/{_annotation.RulesetName}/{_annotation.RuleName}");
            }
            catch
            {
                // ignored
            }
        }

        #region Action sets and preview

        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            //There is no subset/submenu of actions for this action
            return Task.FromResult<IEnumerable<SuggestedActionSet>>(null);
        }

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            //No preview provided
            return Task.FromResult<object>(null);
        }

        #endregion

        #region Disposal
        
        public void Dispose()
        {
        }
        
        #endregion

        #region Properties
        
        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }
        
        public bool HasActionSets => false;

        public string DisplayText => _displayText;

        public ImageMoniker IconMoniker => default;

        public string IconAutomationText => null;

        public string InputGestureText => null;

        public bool HasPreview => false;
        
        #endregion
    }
}