﻿// ----------------------------------------------------------------------------------
//
// Copyright 2011 Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using Microsoft.WindowsAzure.Management.Model;

namespace Microsoft.WindowsAzure.Management.Cmdlets
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Management.Automation;
    using Extensions;
    using Properties;
    using Services;

    /// <summary>
    /// Removes a previously imported subscription.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "AzureSubscription")]
    public class RemoveAzureSubscriptionCommand : PSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "Name of the subscription.")]
        [ValidateNotNullOrEmpty]
        public string SubscriptionName { get; set; }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "Subscription data file.")]
        [ValidateNotNullOrEmpty]
        public string SubscriptionDataFile { get; set; }

        internal void RemoveSubscriptionProcess(string subscriptionName, string subscriptionsDataFile)
        {
            // Import subscriptions from subscriptions file
            var globalComponents = GlobalComponents.Load(
                GlobalPathInfo.GlobalSettingsDirectory,
                this.ResolvePath(subscriptionsDataFile));

            if (globalComponents.Subscriptions.ContainsKey(subscriptionName))
            {
                var subscription = globalComponents.Subscriptions[subscriptionName];

                // Warn the user if the removed subscription is the default one.
                if (subscription.IsDefault)
                {
                    this.SafeWriteWarning(Resources.RemoveDefaultSubscription);
                    // Change default to another one
                    var newSubscriptionDefault = globalComponents.Subscriptions.Values.FirstOrDefault(s => !s.SubscriptionId.Equals(subscription.SubscriptionId));
                    if (newSubscriptionDefault != null)
                    {
                        newSubscriptionDefault.IsDefault = true;
                    }
                }

                // Warn the user if the removed subscription is the current one.
                SubscriptionData currentSubscription = this.GetCurrentSubscription();
                if (currentSubscription != null && currentSubscription.SubscriptionId.Equals(subscription.SubscriptionId))
                {
                    this.SafeWriteWarning(Resources.RemoveCurrentSubscription);

                    // Clear current subscription to another one
                    this.ClearCurrentSubscription();
                }

                globalComponents.Subscriptions.Remove(subscriptionName);
                globalComponents.SaveSubscriptions();
            }
            else
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.InvalidSubscription, subscriptionName));
            }
        }

        protected override void ProcessRecord()
        {
            try
            {
                base.ProcessRecord();
                RemoveSubscriptionProcess(
                    SubscriptionName,
                    this.ResolvePath(SubscriptionDataFile));
            }
            catch (Exception exception)
            {
                WriteError(new ErrorRecord(exception, string.Empty, ErrorCategory.CloseError, null));
            }
        }
    }
}