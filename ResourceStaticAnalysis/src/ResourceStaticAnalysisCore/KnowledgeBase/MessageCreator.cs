/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.ResourceStaticAnalysis.Core.KnowledgeBase
{
    /// <summary>
    /// You can extend this class to provide customized messages for Checks.
    /// This is useful if you want to keep relatively simple Rule syntax and provide customized messages that
    /// change depending on results (check) of each check.
    /// </summary>
    public class MessageCreator
    {
        /// <summary>
        /// Creates a default instance of the <see cref="MessageCreator"/> with an empty message.
        /// </summary>
        public MessageCreator()
        {
            BaseMessage = string.Empty;
        }

        /// <summary>
        /// Creates an instance of the <see cref="MessageCreator"/> with the specified message.
        /// </summary>
        public MessageCreator(string initMessage)
        {
            BaseMessage = initMessage;
        }

        protected string BaseMessage;

        /// <summary>
        /// This method should be called to set the default message for the check.
        /// </summary>
        /// <param name="newBaseMessage">Reinitializes the <see cref="MessageCreator"/> with this message.</param>
        /// <returns>Returns instance of self to simplify syntax; this method can be called inside a call to a Check() method.</returns>
        public MessageCreator SetInit(string newBaseMessage)
        {
            BaseMessage = newBaseMessage;
            return this;
        }

        /// <summary>
        /// This method will be called by rule manager to obtain the message.
        /// Implement your logic to build message based on any contex info you provide. Use _baseMessage field
        /// to obtain the base/default message and combine it with context.
        /// Base implementation simply returns the default string.
        /// This method is called if Check call resolved as true.
        /// </summary>
        public virtual string GetFullMessage()
        {
            return BaseMessage;
        }

        /// <summary>
        /// Gets only the base message. This method is called if Check call resolved as false
        /// </summary>
        /// <returns></returns>
        public string GetBaseMessage()
        {
            return BaseMessage;
        }
    }

    /// <summary>
    /// This class implements a generic message creator that accepts collections of strings as _context
    /// and returns a message that is a concatenation of default message and the _context strings separated by '; '
    /// </summary>
    public class StringAppendMessageCreator : MessageCreator
    {
        /// <summary>
        /// Creates a default instance of the <see cref="StringAppendMessageCreator"/> with an empty message.
        /// </summary>
        public StringAppendMessageCreator() : base() { }

        /// <summary>
        /// Creates an instance of the <see cref="StringAppendMessageCreator"/> with the specified message.
        /// </summary>
        public StringAppendMessageCreator(string initMessage) : base(initMessage) { }

        /// <summary>
        /// Context for the init message to be concatenated with.
        /// </summary>
        readonly List<string> _context = new List<string>();

        /// <summary>
        /// Resets the context of the message creator.
        /// </summary>
        public void ResetContext()
        {
            _context.Clear();
        }

        /// <summary>
        /// Replace existing _context with new collection of strings.
        /// </summary>
        /// <returns>Returns newContext in order to enable simple syntax and wrapping around collections.</returns>
        public IEnumerable<string> SetContext(IEnumerable<string> newContext)
        {
            _context.Clear();
            _context.AddRange(newContext);
            return newContext;
        }

        /// <summary>
        /// Replace existing _context with new collection of strings.
        /// </summary>
        /// <returns>Returns newContext in order to enable simple syntax and wrapping around collections.</returns>
        public string SetContext(string newContext)
        {
            _context.Clear();
            _context.Add(newContext);
            return newContext;
        }

        /// <summary>
        /// Add a collection of strings to the existing context, if any.
        /// </summary>
        /// <returns>Returns additionalContext in order to enable simple syntax and wrapping around collections.</returns>
        public IEnumerable<string> AddContext(IEnumerable<string> additionalContext)
        {
            _context.AddRange(additionalContext);
            return _context;
        }

        /// <summary>
        /// Concatenate the default message, if any, with available context.
        /// </summary>
        public override string GetFullMessage()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0} {1}", base.GetFullMessage(), string.Join("; ", _context.ToArray()));
        }
    }
}
