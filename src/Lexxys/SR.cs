// Lexxys Infrastructural library.
// file: SR.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Lexxys
{
	internal static class SR
	{
		private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;
		// Ap

		// General

		internal static string FileNotFound(string fileName)
		{
			return String.Format(Culture, "File not found '{0}'.", fileName);
		}

		internal static string CannotLoadAssembly(string assemblyName)
		{
			return String.Format(Culture, "Cannot load assembly ({0}).", assemblyName);
		}

		// Exception

		internal static string FormatException()
		{
			return "Invalid value format.";
		}
		internal static string FormatException(string value)
		{
			return String.Format(Culture, "Invalid format of the value: \"{0}\"", value);
		}
		internal static string FormatException(string value, Type valueType)
		{
			return String.Format(Culture, "Invalid format of {1} value: \"{0}\"", value, valueType);
		}
		internal static string CannotParseValue()
		{
			return "Cannot parse value.";
		}
		internal static string CannotParseValue(string value)
		{
			return String.Format(Culture, "Cannot parse value: \"{0}\"", value);
		}
		internal static string CannotParseValue(string value, Type valueType)
		{
			return String.Format(Culture, "Cannot parse value {1}: \"{0}\"", value, valueType);
		}
		internal static string CannotFindConstructor(Type objectType, IEnumerable<Type> argTypes)
		{
			return String.Format(Culture, "Cannot find constructor of {0} with arguments ({1})", objectType, String.Join(", ", argTypes));
		}
		internal static string UnauthorizedAccess()
		{
			return "Attempted to perform an unauthorized operation.";
		}
		internal static string UnauthorizedAccess(string resource)
		{
			return String.IsNullOrEmpty(resource) ? UnauthorizedAccess(): String.Format(Culture, "Attempted to access an unauthorized resource '{0}'.", resource);
		}
		internal static string OperationNotSupported()
		{
			return "Operation is not supported.";
		}
		internal static string OperationNotSupported(string operationName)
		{
			return String.Format(Culture, "Operation '{0}' is not supported.", operationName);
		}
		internal static string OperationNotImplemented()
		{
			return "Operation is not implemented.";
		}
		internal static string OperationNotImplemented(string operationName)
		{
			return String.Format(Culture, "Operation '{0}' is not implemented.", operationName);
		}
		internal static string ArgumentException()
		{
			return "Value(s) does not meet required conditions.";
		}
		internal static string ArgumentNullException()
		{
			return "Value cannot be empty.";
		}
		internal static string ArgumentNullException(string paramName)
		{
			return String.Format(Culture, "Parameter '{0}' cannot be empty.", paramName);
		}
		internal static string ArgumentOutOfRangeException()
		{
			return "Value out of range of valid values.";
		}
		internal static string ArgumentOutOfRangeException(string paramName)
		{
			return String.Format(Culture, "Value of parameter '{0}' out of range of valid values.", paramName);
		}
		internal static string ArgumentOutOfRangeException(string paramName, object actualValue)
		{
			return String.Format(Culture, "Value ({1}) of parameter '{0}' out of range of valid values.", paramName, actualValue);
		}
		//internal static string ArgumentOutOfRangeException(string paramName, object actualValue, object expectedValue)
		//{
		//    return String.Format(_culture, "Value ({1}) of parameter '{0}' out of range of valid values ({2}).", paramName, actualValue, expectedValue);
		//}
		//internal static string ArgumentOutOfRangeException(string paramName, object actualValue, object minValue, object maxValue)
		//{
		//    return String.Format(_culture, "Value ({1}) of parameter '{0}' out of range of valid values ({2}, {3}).", paramName, actualValue, minValue, maxValue);
		//}
		internal static string ArgumentWrongTypeException()
		{
			return "Wrong parameter type.";
		}
		internal static string ArgumentWrongTypeException(string paramName)
		{
			return String.Format(Culture, "The Type of parameter '{0}' is wrong.", paramName);
		}
		internal static string ArgumentWrongTypeException(string paramName, Type actualType)
		{
			return String.Format(Culture, "The Type '{1}' of parameter '{0}' is wrong.", paramName, actualType);
		}
		internal static string ArgumentWrongTypeException(string paramName, Type actualType, Type expectedType)
		{
			return String.Format(Culture, "The Type '{1}' of parameter '{0}' is wrong. Required type is {2}", paramName, actualType, expectedType);
		}
		internal static string OverflowException()
		{
			return "The value is exceed of valid value range.";
		}
		internal static string OverflowException(string value)
		{
			return String.Format(Culture, "The value ({0}) is exceed of valid value range.", value);
		}
		internal static string SyntaxException()
		{
			return "Syntax Error.";
		}
		internal static string ReadOnlyException()
		{
			return "The object is readonly.";
		}
		internal static string ReadOnlyException(object objectInfo)
		{
			return String.Format(Culture, "The object {0} is readonly.", objectInfo);
		}
		internal static string ReadOnlyException(object objectInfo, object item)
		{
			return String.Format(Culture, "The object {0} is readonly (item: {1}).", objectInfo, item);
		}
		public static string CheckInvariantFailed(ValidationResults results = null, string source = null)
		{
			string message = String.IsNullOrEmpty(source) ?
				(results == null || results.Success ? "Invariant check failed.": "Invariant check failed with message: \"{1}\"."):
				(results == null || results.Success ? "Invariant check failed in {0}.": "Check invariant failed in {0} with message: \"{1}\".");
			return String.Format(Culture, message, source, results);
		}
		public static string ValidationFailed(ValidationResults validation)
		{
			var text = new StringBuilder();
			text.Append("Validation Failed. {");
			string prefix = "";
			foreach (var item in validation.Items)
			{
				if (item.Field == null)
					if (item.Message == null)
						continue;
					else
						text.Append(prefix).Append('"').Append(item.Message).Append('"');
				else
					if (item.Message == null)
						text.Append(prefix).Append(item.Field);
					else
						text.Append(prefix).Append(item.Field).Append(": \"").Append(item.Message).Append('"');
				prefix = "; ";
			}
			text.Append('}');
			return text.ToString();
		}

		// AssocNode
		internal static string AssocNodeMissReference()
		{
			return "Lists of forward and backward references are unbalanced";
		}

		#region DC

		public static string ConnectionInitialized(Data.ConnectionStringInfo connectionInfo)
		{
			return String.Format(Culture, "Connection initialized: {0}", connectionInfo);
		}

		public static string ConnectionChanged(Data.ConnectionStringInfo connectionInfo)
		{
			return String.Format(Culture, "Connection changed: {0}", connectionInfo);
		}

		public static string ConnectionTiming(long timeValue)
		{
			return "SQL Connection Timing: " + WatchTimer.ToString(timeValue, false);
		}

		public static string SqlQueryTiming(long timeValue, string query)
		{
			return "SQL Timing: " + WatchTimer.ToString(timeValue) + "\n" + Strings.CutIndents(query.Split(Nls, StringSplitOptions.RemoveEmptyEntries), 4, "\n");
		}

		public static string SqlGroupQueryTiming(long timeValue, long timeOffset, string query)
		{
			return WatchTimer.ToString(timeValue) + " (+" + WatchTimer.ToString(timeOffset) + ")\n" + Strings.CutIndents(query.Split(Nls, StringSplitOptions.RemoveEmptyEntries), 4, "\n");
		}
		private static readonly char[] Nls = { '\r', '\n' };

		public static Func<string> DC_InitConnectionString(string connection)
		{
			return () => String.Format(Culture, "ConnectionString: {0}", connection);
		}

		internal static Func<string> TransactionDisposedWithCommit()
		{
			return () => String.Format(Culture, "Auto commit.");
		}

		internal static Func<string> TransactionDisposedWithRollback()
		{
			return () => String.Format(Culture, "Auto rollback.");
		}

		internal static string NothingToCommit()
		{
			return "Nothing to do on commit. Transaction is absent.";
		}

		internal static string NothingToRollback()
		{
			return "Nothing to do on rollback. Transaction is absent.";
		}
		#endregion

		#region Configuration

		public static Func<string> ConfigurationResourceNotFound(Uri location)
		{
			return () => String.Format(Culture, "Cannot find configuration resource ({0}).", location);
		}
		public static Func<string> ConfigurationProviderNotFound(Uri location)
		{
			return () => String.Format(Culture, "Cannot find configuration provider ({0}).", location);
		}
		public static Func<string> ConfigurationLoaded(Uri location, int position)
		{
			return () => String.Format(Culture, "Configuration loaded {1}. ({0})", location, position);
		}
		public static Func<string> ConfigurationChanged(Configuration.IXmlConfigurationSource source)
		{
			return () => String.Format(Culture, "Configuration changed ({0}).", source?.Name);
		}
		public static Func<string> ConfigurationFileIncluded(string fileName)
		{
			return () => String.Format(Culture, "Configuration file included ({0}).", fileName);
		}
		public static Func<string> UnknownOption(string option, string fileName)
		{
			return () => String.Format(Culture, "Unknown Option: {0}, file: {1}.", option, fileName);
		}
		public static Func<string> OptionIncludeFileNotFound(string fileName, string baseDirectory)
		{
			return () => String.Format(Culture, "Including file not found. file: {0}, directory: {1}.", fileName, baseDirectory);
		}
		public static string ConfigValueNotFound(string key, Type type)
		{
			return
				type == null ? String.Format(Culture, "Configuration at path \"{0}\" not found.", key):
				String.IsNullOrEmpty(key) ? String.Format(Culture, "Configuration value of type {0} not found.", type):
				String.Format(Culture, "Configuration value of type {0} not found at path \"{1}\".", type, key);
		}
		public static string ConfigurationXmlFile(Uri location, string reference)
		{
			return String.Format(Culture, "Bad xml configuration source ({0}) or node reference ({1})", location, reference);
		}

		#endregion

		#region Crypto

		internal static string CR_CannotCreateAgorithm(string type)
		{
			return String.Format(Culture, "Cannot create instance of the cryptographic algorithm for type {0}.", type);
		}
		internal static string CR_BadCriptingClass()
		{
			return "Cannot find specified interface in crypting algorithm.";
		}
		internal static string CR_CriptographicAlgorithmNotFound(string type)
		{
			return String.Format(Culture, "Criptographic algorithm of type \"{0}\" not found in configuration.", type);
		}
		#endregion

		#region Logging

		internal static Func<string> LoggingConfidurationMissing()
		{
			return () => "Missing Logging Configuration.";
		}

		internal static string LOG_BeginGroup()
		{
			return "entering";
		}
		internal static string LOG_EndGroup()
		{
			return "exiting";
		}
		internal static string LOG_BeginSection()
		{
			return "entering";
		}
		internal static string LOG_BeginSection(string sectionName)
		{
			return String.Format(Culture, "entering {0}", sectionName);
		}
		internal static string LOG_EndSection()
		{
			return "exiting";
		}
		internal static string LOG_EndSection(string sectionName)
		{
			return String.Format(Culture, "exiting {0}", sectionName);
		}
		internal static string LOG_CannotOpenLogFile(string fileName)
		{
			return String.Format(Culture, "Cannot open log file '{0}'", fileName);
		}
		internal static string LOG_CannotCreateLogWriter(string writerName, string className = null, Exception exception = null)
		{
			return exception == null ?
				String.Format(Culture, "Cannot create Log Writer (name={0}, class={1}).", writerName, className ?? "(null)"):
				String.Format(Culture, "Cannot create Log Writer (name={0}, class={1})\nException: {2}.", writerName, className, exception.Message);
		}

		internal static string LOG_CannotCreateLogFormatter(string className, Exception exception = null)
		{
			return exception == null ?
				String.Format(Culture, "Cannot create Log Formatter (class={1}).", className) :
				String.Format(Culture, "Cannot create Log Formatter (class={1})\nException: {2}.", className, exception.Message);
		}

		internal static string ValueCannotBeGreaterThan(object min, object max)
		{
			return String.Format(Culture, "{0} cannot be greater then {1}", min, max);
		}

		internal static string LOG_MissingLogWriterName()
		{
			return "Missing log writer name.";
		}
		#endregion

		// Char Stream

		internal static string CHR_AtPosition(CultureInfo culture, int line, int column, int position)
		{
			return String.Format(culture ?? Culture, "at ({2}) L{0}, C{1}", line, column, position);
		}

		// Expression
		internal static string EXP_UnbalancedBraces()
		{
			return "Unbalanced braces";
		}
		internal static string EXP_MissingOperation()
		{
			return "Missing Operation";
		}

		internal static string EXP_UnknownSymbol(string symbol)
		{
			return String.Format(Culture, "Unknown symbol in the stream ({0})", symbol);
		}
		internal static string EXP_MissingParameters(string operationName)
		{
			return String.Format(Culture, "Not enought parameters for operation '{0}'.", operationName);
		}

		// XML Structure
		internal static string WrongXmlNodeValue(string nodeName, string nodeValue)
		{
			return String.Format(Culture, "Wrong value of node {0} ({1}).", nodeName, nodeValue);
		}
		internal static string MissingXmlNode(string nodeName)
		{
			return String.Format(Culture, "Missing required node ({0}).", nodeName);
		}
		internal static string WrongXmlAttributeValue(string attributeName, string attributeValue)
		{
			return String.Format(Culture, "Wrong value of attibute {0} ({1}).", attributeName, attributeValue);
		}
		internal static string MissingXmlAttribute(string attribute)
		{
			return String.Format(Culture, "Missing required attribute ({0}).", attribute);
		}
		internal static string UnexpectedXmlNode(string expectedNode, string actualNode)
		{
			return String.Format(Culture, "Wrong XML node. Expected '{0}', got '{1}'.", expectedNode, actualNode);
		}

		// PermissionValue, Group
		internal static string PV_GroupChanged()
		{
			return "Structure of the targed permissons group was changed";
		}
		internal static string PG_IsReadonly()
		{
			return "The permission group is in readonly state.";
		}
		internal static string PG_CannotMakePermissionMapper()
		{
			return "Cannot make permission mapper.";
		}
		internal static string PV_CannotDecodeString(int startPosition, int endPosition, string actualValue)
		{
			return String.Format(Culture, "text[{0}:{1}] = '{2}'.", startPosition, endPosition, actualValue);
		}


		// Ternary

		internal static string TRN_BadFormat()
		{
			return "String was not recognized as a valid Ternary.";
		}


		// Tools

		internal static string TLS_BadHashFunction()
		{
			return "Has Function too bad for StaticSet.";
		}
		internal static string TLS_CannotCreateType(string typeName, string assemblyName)
		{
			string comma = ", ";
			if (assemblyName == null)
			{
				comma = "";
				assemblyName = "";
			}
			return String.Format(Culture, "Cannot find class type ({0}{1}{2}).", typeName, comma, assemblyName);
		}
		internal static string TLS_CannotFindType(string typeName)
		{
			return String.Format(Culture, "Cannot find class type '{0}'.", typeName);
		}

		internal static string ValidationFailed()
		{
			return "Validation failed";
		}

		internal static string LockTimeout(int timeout)
		{
			return String.Format(Culture, "Timeout ({0} ms) expires before the lock request is granted", timeout);
		}

		internal static Func<string> FileChanged(string fileName)
		{
			return () => String.Format(Culture, "File Chaged: {0}.", fileName);
		}

		internal static string CollectionIsEmpty()
		{
			return "Collection is empty.";
		}

		internal static string EndOfCollection()
		{
			return "End of Collection.";
		}

		internal static string ParserFirst()
		{
			return "Method Parse should be called first.";
		}

		internal static string EofInComments()
		{
			return "EOF in comments.";
		}

		internal static string UnrecognizedEscapeSequence(char c)
		{
			return String.Format(Culture, "Unrecognized Escape sequence \"\\c{0}\".", c);
		}

		internal static string EofInStringConstant()
		{
			return "EOF in string constant.";
		}

		internal static string UndentError()
		{
			return "Undent error.";
		}

		internal static string ExpectedAttributeName()
		{
			return "Attribute name expected.";
		}

		internal static string ExpectedMultilineAttribute()
		{
			return "Expected multiline attbribute value.";
		}

		internal static string ExpectedEndOfNode(string nodeName)
		{
			return String.Format(Culture, "end of node ({0}) expected", nodeName);
		}

		internal static string ExpectedNodeName()
		{
			return "Name of node expected.";
		}

		internal static string RuleNotFound()
		{
			return "Rule for anonymous node not found.";
		}

		internal static string ExpectedEndOfLine()
		{
			return "End of line expected.";
		}

		internal static string ExpectedNewLine()
		{
			return "Expected newline.";
		}

		internal static string ExpectedNodePattern()
		{
			return "Expected node pattern.";
		}

		internal static string UndefinedNodeType(System.Xml.XmlNodeType xmlNodeType)
		{
			return String.Format(Culture, "Node type '{0}' is undefined.", xmlNodeType);
		}

		internal static Func<string> Factory_CannotImportAssembly(string assembly)
		{
			return () => String.Format(Culture, "Cannot import assembly '{0}'.", assembly);
		}

		internal static string Factory_AssemblyLoadVersionMismatch()
		{
			return "Duplicate version of the same assembly is loaded";
		}

		internal static string Factory_CannotFindClass(string className)
		{
			return String.Format(Culture, "Cannot find class '{0}'.", className);
		}

		internal static string Factory_CannotFindConstructor(Type type)
		{
			return String.Format(Culture, "Cannot find constructor for type {0}.", type);
		}

		internal static string Factory_CannotCreateInstanceOfInterface(Type type)
		{
			return String.Format(Culture, "Cannot create instance of interface {0}.", type);
		}

		internal static string Factory_CannotFindConstructor(Type type, int count)
		{
			return String.Format(Culture, "Cannot find constructor with {1} parameters for type {0}.", type, count);
		}

		internal static string ConnectionStringIsEmpty()
		{
			return "Connection String is empty";
		}

		internal static string DifferentCurrencyCodes(Currency left, Currency right)
		{
			return String.Format(Culture, "The operands have different currency codes: {0} and {1}.", left?.Code, right?.Code);
		}
	}
}
