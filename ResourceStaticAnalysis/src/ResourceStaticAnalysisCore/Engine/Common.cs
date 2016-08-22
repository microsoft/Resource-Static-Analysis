/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.ResourceStaticAnalysis.Core.KnowledgeBase;
using Microsoft.ResourceStaticAnalysis.Core.Output;

namespace Microsoft.ResourceStaticAnalysis.Core.Engine
{
    /// <summary>
    /// Delegate used to execute Rules on Classification objects
    /// </summary>
    /// <param name="co">Classification Object to execute rules on.</param>
    /// <param name="outputEntries">List of output entries created by each rule that ran on the CO. Each rule must append its output, if any, to the list.</param>
    public delegate void ClassifyCOEventHandler(ClassificationObject co, IList<OutputItem> outputEntries);

    /// <summary>
    /// Delegate used in the multi-thread scenario to allow each rule to process all COs on one thread.
    /// </summary>
    /// <param name="cos"></param>
    public delegate void ClassifyCollectionOfCOsEventHandler(IEnumerable<ClassificationObject> cos);

    /// <summary>
    /// Base exeption for ResourceStaticAnalysis core. All internal specific exeptions must derive from this exception
    /// so that they are organized into a hierarchy.
    /// </summary>
    public class ResourceStaticAnalysisException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the ResourceStaticAnalysisException class.
        /// </summary>
        public ResourceStaticAnalysisException() { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
        public ResourceStaticAnalysisException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified</param>
        public ResourceStaticAnalysisException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination</param>
        protected ResourceStaticAnalysisException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// Initialization of ResourceStaticAnalysis failed due to incorrect config or other reasons.
    /// </summary>
    public class ResourceStaticAnalysisEngineInitializationException : ResourceStaticAnalysisException
    {
        /// <summary>
        /// Initializes a new instance of the RuleManagerException class.
        /// </summary>
        public ResourceStaticAnalysisEngineInitializationException() { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
        public ResourceStaticAnalysisEngineInitializationException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified</param>
        public ResourceStaticAnalysisEngineInitializationException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination</param>
        protected ResourceStaticAnalysisEngineInitializationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// ResourceStaticAnalysis config is incorrect.
    /// </summary>
    public class ResourceStaticAnalysisEngineConfigException : ResourceStaticAnalysisException
    {
        /// <summary>
        /// Initializes a new instance of the RuleManagerException class.
        /// </summary>
        public ResourceStaticAnalysisEngineConfigException() { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
        public ResourceStaticAnalysisEngineConfigException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified</param>
        public ResourceStaticAnalysisEngineConfigException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination</param>
        protected ResourceStaticAnalysisEngineConfigException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// Exception specific to events happening in RuleManager section.
    /// </summary>
    public class RuleManagerException : ResourceStaticAnalysisException
    {
        /// <summary>
        /// Initializes a new instance of the RuleManagerException class.
        /// </summary>
        public RuleManagerException() { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
        public RuleManagerException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified</param>
        public RuleManagerException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination</param>
        protected RuleManagerException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// Exception occurring during attempts to compile a rule assembly from rule source code.
    /// </summary>
    public class RuntimeRuleCompilerException : RuleManagerException
    {
        /// <summary>
        /// Initializes a new instance of the RuntimeRuleCompilerException class.
        /// </summary>
        public RuntimeRuleCompilerException() { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
        public RuntimeRuleCompilerException(string message) : base(message) { }


        /// <summary>
        /// Initializes a new instance of the System.Exception class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified</param>
        public RuntimeRuleCompilerException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination</param>
        protected RuntimeRuleCompilerException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// Root for exceptions in Output subsystem.
    /// </summary>
    public class OutputException : ResourceStaticAnalysisException
    {
        /// <summary>
        /// Initializes a new instance of the OutputException class.
        /// </summary>
        public OutputException() { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
        public OutputException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified</param>
        public OutputException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination</param>
        protected OutputException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// Thrown when output thread detected it is in inconsistent state.
    /// </summary>
    public class OutputInconsistencyException : ResourceStaticAnalysisException
    {
        /// <summary>
        /// Initializes a new instance of the OutputInconsistencyException class.
        /// </summary>
        public OutputInconsistencyException() { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
        public OutputInconsistencyException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified</param>
        public OutputInconsistencyException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination</param>
        protected OutputInconsistencyException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// Exception specific to events happening in ObjectFactory section (input side of ResourceStaticAnalysis).
    /// </summary>
    public class RequiredDataSourceMissing : ResourceStaticAnalysisException
    {
        /// <summary>
        /// Initializes a new instance of the RequiredDataSourceMissing class.
        /// </summary>
        public RequiredDataSourceMissing() { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
        public RequiredDataSourceMissing(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified</param>
        public RequiredDataSourceMissing(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination</param>
        protected RequiredDataSourceMissing(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// Exception specific to events happening in ObjectFactory section (input side of ResourceStaticAnalysis).
    /// </summary>
    public class ErrorRetrievingObjectFactory : ResourceStaticAnalysisException
    {
        /// <summary>
        /// Initializes a new instance of the ErrorRetrievingObjectFactory class.
        /// </summary>
        public ErrorRetrievingObjectFactory() { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
        public ErrorRetrievingObjectFactory(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified</param>
        public ErrorRetrievingObjectFactory(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination</param>
        protected ErrorRetrievingObjectFactory(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// Exception specific to events happening in ObjectFactory section (input side of ResourceStaticAnalysis).
    /// </summary>
    public class PropertyProviderNotAvailableException : ResourceStaticAnalysisException
    {
        /// <summary>
        /// Initializes a new instance of the PropertyProviderNotAvailableException class.
        /// </summary>
        public PropertyProviderNotAvailableException() { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
        public PropertyProviderNotAvailableException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified</param>
        public PropertyProviderNotAvailableException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination</param>
        protected PropertyProviderNotAvailableException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// Exception specific to events happening in SpaceFactory section (object organization side of ResourceStaticAnalysis).
    /// </summary>
    public class ErrorRetrievingSpaceFactoryException : ResourceStaticAnalysisException
    {
        /// <summary>
        /// Initializes a new instance of the ErrorRetrievingSpaceFactoryException class.
        /// </summary>
        public ErrorRetrievingSpaceFactoryException() { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
        public ErrorRetrievingSpaceFactoryException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified</param>
        public ErrorRetrievingSpaceFactoryException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination</param>
        protected ErrorRetrievingSpaceFactoryException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// Thrown by a property adapter when it fails to retrieve value of a specified property.
    /// </summary>
    public class PropertyRetrievalException : ResourceStaticAnalysisException
    {
        /// <summary>
        /// Initializes a new instance of the PropertyRetrievalException class.
        /// </summary>
        public PropertyRetrievalException() { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
        public PropertyRetrievalException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified</param>
        public PropertyRetrievalException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination</param>
        protected PropertyRetrievalException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// Root exception for more specific exceptions happening when executing individual rules.
    /// </summary>
    public class RuleException : RuleManagerException
    {
        /// <summary>
        /// Initializes a new instance of the RuleException class.
        /// </summary>
        public RuleException() { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
        public RuleException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified</param>
        public RuleException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination</param>
        protected RuleException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// General exception detected by RuleManager when executing <seealso cref="Rule"/>'s run method.
    /// </summary>
    public class ExecutingRuleException : RuleException
    {
        /// <summary>
        /// Initializes a new instance of the ExecutingRuleFailed class.
        /// </summary>
        public ExecutingRuleException() { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
        public ExecutingRuleException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified</param>
        public ExecutingRuleException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination</param>
        protected ExecutingRuleException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
    /// <summary>
    /// Exception during Rule initialization phase.
    /// </summary>
    public class InitializingRuleException : RuleException
    {
        /// <summary>
        /// Initializes a new instance of the InitializingRuleException class.
        /// </summary>
        public InitializingRuleException() { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
        public InitializingRuleException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified</param>
        public InitializingRuleException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination</param>
        protected InitializingRuleException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// Rule container could not be loaded
    /// </summary>
    public class RuleContainerException : RuleException
    {
        /// <summary>
        /// Initializes a new instance of the ExecutingRuleFailed class.
        /// </summary>
        public RuleContainerException() { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
        public RuleContainerException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified</param>
        public RuleContainerException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the System.Exception class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination</param>
        protected RuleContainerException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}