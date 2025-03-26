// Lexxys Infrastructural library.
// file: SR.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Text;
using System.Globalization;

namespace Lexxys
{
	internal static class SR
	{
		public static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

		// General

		internal static string FileNotFound(string fileName) => String.Format(Culture, "File not found \"{0}\".", fileName);

		internal static string CannotLoadAssembly(string assemblyName) => String.Format(Culture, "Cannot load assembly ({0}).", assemblyName);

		#region Exceptions

		internal static string FormatException() => "Invalid value format.";
		internal static string FormatException(string value) => String.Format(Culture, "Invalid format of the value: \"{0}\"", value);
		internal static string FormatException(string? value, Type valueType) => String.Format(Culture, "Invalid format of {1} value: \"{0}\"", value, valueType);
		internal static string CannotParseValue() => "Cannot parse value.";
		internal static string CannotParseValue(string value) => String.Format(Culture, "Cannot parse value: \"{0}\"", value);
		internal static string CannotParseValue(string? value, Type valueType) => String.Format(Culture, "Cannot parse value {1}: \"{0}\"", value, valueType);
		internal static string CannotFindConstructor(Type objectType, IEnumerable<Type> argTypes) => String.Format(Culture, "Cannot find constructor of {0} with arguments ({1})", objectType, String.Join(", ", argTypes));
		internal static string UnauthorizedAccess() => "Attempted to perform an unauthorized operation.";
		internal static string UnauthorizedAccess(string? resource) => String.IsNullOrEmpty(resource) ? UnauthorizedAccess(): String.Format(Culture, "Attempted to access an unauthorized resource \"{0}\".", resource);
		internal static string OperationNotSupported() => "Operation is not supported.";
		internal static string OperationNotSupported(string operationName) => String.Format(Culture, "Operation \"{0}\" is not supported.", operationName);
		internal static string OperationNotImplemented() => "Operation is not implemented.";
		internal static string OperationNotImplemented(string operationName) => String.Format(Culture, "Operation \"{0}\" is not implemented.", operationName);
		internal static string ArgumentException() => "Value(s) does not meet required conditions.";
		internal static string ArgumentNullException() => "Value cannot be empty.";
		internal static string ArgumentNullException(string paramName) => String.Format(Culture, "Parameter \"{0}\" cannot be empty.", paramName);
		internal static string ArgumentOutOfRangeException() => "Value out of range of valid values.";
		internal static string ArgumentOutOfRangeException(string paramName) => String.Format(Culture, "Value of parameter \"{0}\" out of range of valid values.", paramName);
		internal static string ArgumentOutOfRangeException(string paramName, object actualValue) => String.Format(Culture, "Value ({1}) of parameter \"{0}\" out of range of valid values.", paramName, actualValue);
		internal static string ArgumentWrongTypeException() => "Wrong parameter type.";
		internal static string ArgumentWrongTypeException(string paramName) => String.Format(Culture, "The Type of parameter \"{0}\" is wrong.", paramName);
		internal static string ArgumentWrongTypeException(string paramName, Type actualType) => String.Format(Culture, "The Type \"{1}\" of parameter \"{0}\" is wrong.", paramName, actualType);
		internal static string ArgumentWrongTypeException(string paramName, Type actualType, Type expectedType) => String.Format(Culture, "The Type \"{1}\" of parameter \"{0}\" is wrong. Required type is {2}", paramName, actualType, expectedType);
		internal static string ArgumentItemIsNull() => "One or more items of a parameter is null.";
		internal static string ArgumentItemIsNull(string paramName) => $"One or more items of parameter \"{paramName}\" is null.";
		internal static string OverflowException() => "The value is exceed of valid value range.";
		internal static string OverflowException(string value) => String.Format(Culture, "The value ({0}) is exceed of valid value range.", value);
		internal static string SyntaxException() => "Syntax Error.";
		internal static string ReadOnlyException() => "The object is readonly.";
		internal static string ReadOnlyException(object objectInfo) => String.Format(Culture, "The object {0} is readonly.", objectInfo);
		internal static string ReadOnlyException(object objectInfo, object item) => String.Format(Culture, "The object {0} is readonly (item: {1}).", objectInfo, item);

		#endregion

		// AssocNode
		internal static string AssocNodeMissReference() => "Lists of forward and backward references are unbalanced";

		#region Configuration

		public static Func<string> ConfigurationResourceNotFound(Uri location) => () => String.Format(Culture, "Cannot find configuration resource ({0}).", location);
		public static Func<string> ConfigurationProviderNotFound(Uri location) => () => String.Format(Culture, "Cannot find configuration provider ({0}).", location);
		public static string ConfigurationLoaded(string? location, int position) => String.Format(Culture, "Configuration loaded {1}. ({0})", location, position);
		public static string ConfigurationChanged(Configuration.IXmlConfigurationSource? source) => String.Format(Culture, "Configuration changed ({0}).", source?.ToString());
		public static Func<string> ConfigurationChanged(Configuration.IConfigSource? source) => () => String.Format(Culture, "Configuration changed ({0}).", source?.ToString());
		public static string ConfigurationFileIncluded(string fileName) => String.Format(Culture, "Configuration file included ({0}).", fileName);
		public static string UnknownOption(string option, string? fileName) => fileName == null ? String.Format(Culture, "Unknown Option: {0}.", option):
				String.Format(Culture, "Unknown Option: {0}, file: {1}.", option, fileName);
		public static string OptionIncludeFileNotFound(string? fileName, string? baseDirectory) => fileName == null ?
			baseDirectory == null ?
				"Including file not found.":
				String.Format(Culture, "Including file not found. directory: {0}.", baseDirectory):
			baseDirectory == null ?
				String.Format(Culture, "Including file not found. file: {0}.", fileName):
				String.Format(Culture, "Including file not found. file: {0}, directory: {1}.", fileName, baseDirectory);
		public static string ConfigValueNotFound(string key, Type? type) => type == null ? String.Format(Culture, "Configuration at path \"{0}\" not found.", key):
			String.IsNullOrEmpty(key) ? String.Format(Culture, "Configuration value of type {0} not found.", type):
			String.Format(Culture, "Configuration value of type {0} not found at path \"{1}\".", type, key);
		public static string ConfigurationXmlFile(Uri location, string reference) => String.Format(Culture, "Bad xml configuration source ({0}) or node reference ({1})", location, reference);

		#endregion

		#region Logging

		internal static string LoggingConfigurationMissing() => "Missing Logging Configuration.";

		internal static string LOG_BeginGroup() => "entering";
		internal static string LOG_EndGroup() => "exiting";
		internal static string LOG_BeginSection() => "entering";
		internal static string LOG_BeginSection(string sectionName) => String.Format(Culture, "entering {0}", sectionName);
		internal static string LOG_EndSection() => "exiting";
		internal static string LOG_EndSection(string sectionName) => String.Format(Culture, "exiting {0}", sectionName);
		internal static string LOG_CannotOpenLogFile(string fileName) => String.Format(Culture, "Cannot open log file \"{0}\"", fileName);
		internal static string LOG_CannotCreateLogWriter(string? writerName, string? className = null, Exception? exception = null)
		{
			var text = new StringBuilder("Cannot create Log Writer");
			if (writerName != null)
				text.Append(" (name=").Append(writerName);
			if (className != null)
				text.Append(writerName == null ? "(class=": ", class=").Append(className);
			text.Append(writerName == null && className == null ? '.': ')');
			if (exception != null)
				text.Append("\nException:").Append(exception);
			return text.ToString();
		}

		internal static string LOG_CannotCreateLogFormatter(string className, Exception? exception = null) => exception == null ?
			String.Format(Culture, "Cannot create Log Formatter (class={0}).", className):
			String.Format(Culture, "Cannot create Log Formatter (class={0})\nException: {1}", className, exception);

		internal static string ValueCannotBeGreaterThan(object min, object max) => String.Format(Culture, "{0} cannot be greater then {1}", min, max);

		internal static string LOG_MissingLogWriterName() => "Missing log writer name.";

		#endregion

		// Char Stream

		internal static string CHR_AtPosition(CultureInfo? culture, int line, int column, int position = 0) => position > 0 ?
			String.Format(culture ?? Culture, "at ({2}) L{0}, C{1}", line, column, position):
			String.Format(culture ?? Culture, "at L{0}, C{1}", line, column);

		//#region Expression

		//internal static string EXP_UnbalancedBraces() => "Unbalanced braces";
		//internal static string EXP_MissingOperation() => "Missing Operation";

		//internal static string EXP_UnknownSymbol(string symbol) => String.Format(Culture, "Unknown symbol in the stream ({0})", symbol);
		//internal static string EXP_MissingParameters(string operationName) => String.Format(Culture, "Not enough parameters for operation \"{0}\".", operationName);

		//#endregion

		//#region XML Structure

		//internal static string WrongXmlNodeValue(string nodeName, string nodeValue) => String.Format(Culture, "Wrong value of node {0} ({1}).", nodeName, nodeValue);
		//internal static string MissingXmlNode(string nodeName) => String.Format(Culture, "Missing required node ({0}).", nodeName);
		//internal static string WrongXmlAttributeValue(string attributeName, string attributeValue) => String.Format(Culture, "Wrong value of attribute {0} ({1}).", attributeName, attributeValue);
		//internal static string MissingXmlAttribute(string attribute) => String.Format(Culture, "Missing required attribute ({0}).", attribute);
		//internal static string UnexpectedXmlNode(string expectedNode, string actualNode) => String.Format(Culture, "Wrong XML node. Expected \"{0}\", got \"{1}\".", expectedNode, actualNode);

		//#endregion

		//#region PermissionValue, Group

		//internal static string PV_GroupChanged() => "Structure of the target permissions group was changed";
		//internal static string PG_IsReadonly() => "The permission group is in readonly state.";
		//internal static string PG_CannotMakePermissionMapper() => "Cannot make permission mapper.";
		//internal static string PV_CannotDecodeString(int startPosition, int endPosition, string actualValue) => String.Format(Culture, "text[{0}:{1}] = \"{2}\".", startPosition, endPosition, actualValue);

		//#endregion

		#region Tokenizer

		internal static string EofInComments() => "EOF in comments.";

		internal static string UnrecognizedEscapeSequence(char c) => String.Format(Culture, "Unrecognized Escape sequence \"\\c{0}\".", c);

		internal static string UnrecognizedEscapeSequence(string s) => String.Format(Culture, "Unrecognized Escape sequence \"\\{0}\".", s);

		internal static string EofInStringConstant() => "EOF in string constant.";

		internal static string EolInStringConstant() => "End of line in string constant.";

		internal static string ExpectedAttributeName() => "Attribute name expected.";

		internal static string ExpectedMultilineAttribute() => "Expected multiline attribute value.";

		internal static string ExpectedEndOfNode(string nodeName) => String.Format(Culture, "end of node ({0}) expected", nodeName);

		internal static string ExpectedNodeName() => "Name of node expected.";

		internal static string ExpectedVariableName() => "Name of variable expected.";

		internal static string RuleNotFound() => "Rule for anonymous node not found.";

		internal static string ExpectedEndOfLine() => "End of line expected.";

		internal static string ExpectedNewLine() => "Expected newline.";

		internal static string ExpectedNodePattern() => "Expected node pattern.";

		internal static string ActionNotFound(string name) => $"Action \"{name}\" not found.";

		internal static string UndefinedNodeType(System.Xml.XmlNodeType xmlNodeType) => String.Format(Culture, "Node type \"{0}\" is undefined.", xmlNodeType);

		#endregion

		#region Factory

		internal static string Factory_CannotFindConstructor(Type type) => String.Format(Culture, "Cannot find constructor for type {0}.", type);

		internal static string DifferentCurrencyCodes(Currency? left, Currency? right) => String.Format(Culture, "The operands have different currency codes: {0} and {1}.", left?.Code, right?.Code);

		internal static string Factory_CannotFindConstructor(Type type, int count) => String.Format(Culture, "Cannot find constructor with {1} parameters for type {0}.", type, count);

		#endregion

		internal static string LockTimeout(int timeout) => String.Format(Culture, "Timeout ({0} ms) expires before the lock request is granted", timeout);

		internal static Func<string> FileChanged(string fileName) => () => String.Format(Culture, "File Changed: {0}.", fileName);

		// Tools

		internal static string TLS_BadHashFunction() => "Has Function too bad for StaticSet.";
		internal static string TLS_CannotCreateType(string typeName, string? assemblyName)
		{
			string comma = ", ";
			if (assemblyName == null)
			{
				comma = "";
				assemblyName = "";
			}
			return String.Format(Culture, "Cannot find class type ({0}{1}{2}).", typeName, comma, assemblyName);
		}
		internal static string TLS_CannotFindType(string typeName) => String.Format(Culture, "Cannot find class type \"{0}\".", typeName);

		internal static string ValidationFailed() => "Validation failed";

		internal static string CollectionIsEmpty() => "Collection is empty.";

		internal static string ParserFirst() => "Method Parse should be called first.";

		internal static string UndentError() => "Undent error.";

		internal static Func<string> Factory_CannotImportAssembly(string assembly) => () => String.Format(Culture, "Cannot import assembly \"{0}\".", assembly);

		internal static string Factory_AssemblyLoadVersionMismatch() => "Duplicate version of the same assembly is loaded";

		internal static string Factory_CannotFindClass(string className) => String.Format(Culture, "Cannot find class \"{0}\".", className);

		internal static string Factory_CannotCreateInstanceOfInterface(Type type) => String.Format(Culture, "Cannot create instance of interface {0}.", type);
	}
}
